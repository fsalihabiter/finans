using Finans.Application.Pricing;
using Finans.Domain.Enums;
using Finans.Domain.Portfolio;
using Finans.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Finans.Infrastructure.Pricing;

/// <summary>
/// <see cref="IPriceFetchService"/> EF uygulaması (T2.2). Akış: aktif fiyatlanabilir
/// varlıklar (altın + döviz) → enstrüman; <see cref="IPriceProvider.CanQuote"/>
/// yönlendirmesiyle çek (her sağlayıcı izole — biri çökse diğeri sürer); sonra yaz:
/// <c>PriceSnapshots</c> (geçmiş, gün-içi yineleme atlanır) + <c>FxRates</c> (converter)
/// + ilgili <c>Holding.CurrentPrice</c> (global; kullanıcıdan bağımsız). Sonuç kısa
/// TTL ile cache'lenir → dış çağrı/yazma TTL'de bir (10 §3-4). Fiyatlar kullanıcı-bağımsız.
/// </summary>
public sealed class PriceFetchService(
    FinansDbContext db,
    IEnumerable<IPriceProvider> providers,
    IMemoryCache cache,
    TimeProvider clock,
    ILogger<PriceFetchService> logger) : IPriceFetchService
{
    internal const string CacheKey = "prices:refresh";
    internal static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    /// <summary>Bir kaynak çökerse (bayat sonuç) kısa TTL ile cache'le → çöken kaynağı yakında yeniden dene.</summary>
    internal static readonly TimeSpan StaleRetryTtl = TimeSpan.FromMinutes(1);

    public async Task<PriceRefreshResult> RefreshAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheKey, out PriceRefreshResult? cached) && cached is not null)
            return cached with { FromCache = true };

        // 1) Fiyatlanabilir aktif varlıklar → enstrüman eşlemesi (Faz 2: altın + döviz).
        var assets = await db.Assets
            .Where(a => a.IsActive && (a.Type == AssetType.Gold || a.Type == AssetType.Fx))
            .ToListAsync(ct);

        var assetsByInstrument = new Dictionary<PriceInstrument, List<Asset>>();
        foreach (var asset in assets)
        {
            if (!TryMapInstrument(asset, out var instrument))
                continue;
            if (!assetsByInstrument.TryGetValue(instrument, out var list))
                assetsByInstrument[instrument] = list = [];
            list.Add(asset);
        }

        var refreshedAt = clock.GetUtcNow().UtcDateTime;

        if (assetsByInstrument.Count == 0)
            return CacheAndReturn(new PriceRefreshResult([], refreshedAt, FromCache: false, FailedSources: []));

        // 2) Sağlayıcılara CanQuote'a göre yönlendir; her sağlayıcı izole. Çökerse → son
        //    bilinen fiyattan bayat (stale) tırnak üret (T2.3, NFR-5) — uygulama çökmez.
        var instruments = assetsByInstrument.Keys.ToList();
        var fresh = new List<PriceQuote>();
        var stale = new List<PriceQuote>();
        var failed = new List<string>();
        foreach (var provider in providers)
        {
            var handled = instruments.Where(provider.CanQuote).ToList();
            if (handled.Count == 0)
                continue;
            try
            {
                fresh.AddRange(await provider.GetQuotesAsync(handled, ct));
            }
            catch (Exception ex)
            {
                failed.Add(provider.Source);
                logger.LogWarning(ex,
                    "Fiyat sağlayıcı {Source} başarısız; son bilinen fiyata düşülüyor (stale).", provider.Source);
                stale.AddRange(await LoadLastKnownAsync(handled, assetsByInstrument, ct));
            }
        }

        // 3) Kalıcılaştır: yalnız TAZE tırnaklar → snapshot (geçmiş) + fxrate (converter) +
        //    holding.CurrentPrice (okuma yolu). Bayat tırnak zaten son-bilineni taşır → yazılmaz.
        await PersistAsync(fresh, assetsByInstrument, refreshedAt, ct);

        var result = new PriceRefreshResult(
            [.. fresh, .. stale], refreshedAt, FromCache: false, FailedSources: failed);

        // Bir kaynak çöktüyse kısa TTL → yakında yeniden dene; aksi halde tam TTL.
        return CacheAndReturn(result, failed.Count == 0 ? Ttl : StaleRetryTtl);
    }

    private PriceRefreshResult CacheAndReturn(PriceRefreshResult result, TimeSpan? ttl = null)
    {
        cache.Set(CacheKey, result, ttl ?? Ttl);
        return result;
    }

    /// <summary>
    /// Çöken sağlayıcının enstrümanları için DB'deki <b>son bilinen</b> değeri okuyup bayat
    /// (<c>IsStale=true</c>) tırnak üretir: döviz → en güncel <c>FxRate</c>; altın → en güncel
    /// <c>PriceSnapshot</c>. Hiç geçmiş yoksa o enstrüman atlanır (loglanır).
    /// </summary>
    private async Task<List<PriceQuote>> LoadLastKnownAsync(
        IReadOnlyCollection<PriceInstrument> instruments,
        IReadOnlyDictionary<PriceInstrument, List<Asset>> assetsByInstrument,
        CancellationToken ct)
    {
        var result = new List<PriceQuote>();
        foreach (var instrument in instruments)
        {
            if (instrument.Kind == PriceInstrumentKind.Currency)
            {
                var from = instrument.Currency;
                var latest = await db.FxRates
                    .Where(r => r.FromCurrency == from && r.ToCurrency == CurrencyCode.TRY)
                    .OrderByDescending(r => r.AsOfUtc).ThenByDescending(r => r.CreatedAtUtc)
                    .FirstOrDefaultAsync(ct);
                if (latest is not null)
                    result.Add(new PriceQuote(
                        instrument, latest.Rate, CurrencyCode.TRY, latest.AsOfUtc, latest.Source, IsStale: true));
                else
                    logger.LogWarning("Son bilinen FX yok: {From}→TRY; bayat tırnak üretilemedi.", from);
            }
            else // Gold
            {
                if (!assetsByInstrument.TryGetValue(instrument, out var assetsForInst) || assetsForInst.Count == 0)
                    continue;
                var assetIds = assetsForInst.Select(a => a.Id).ToList();
                var pricingCcy = assetsForInst[0].PricingCurrency;
                var latest = await db.PriceSnapshots
                    .Where(p => assetIds.Contains(p.AssetId))
                    .OrderByDescending(p => p.AsOfUtc).ThenByDescending(p => p.CreatedAtUtc)
                    .FirstOrDefaultAsync(ct);
                if (latest is not null)
                    result.Add(new PriceQuote(
                        instrument, latest.Price, pricingCcy, latest.AsOfUtc, latest.Source, IsStale: true));
                else
                    logger.LogWarning("Son bilinen altın fiyatı yok; bayat tırnak üretilemedi.");
            }
        }
        return result;
    }

    private async Task PersistAsync(
        IReadOnlyList<PriceQuote> quotes,
        IReadOnlyDictionary<PriceInstrument, List<Asset>> assetsByInstrument,
        DateTime nowUtc,
        CancellationToken ct)
    {
        // Yalnız taze tırnaklar yazılır; bayat (son-bilinen) tırnak geçmişi kirletmez.
        var freshQuotes = quotes.Where(q => !q.IsStale).ToList();
        if (freshQuotes.Count == 0)
            return;

        var priceByAsset = new Dictionary<Guid, decimal>();

        foreach (var quote in freshQuotes)
        {
            if (!assetsByInstrument.TryGetValue(quote.Instrument, out var quoteAssets))
                continue;

            foreach (var asset in quoteAssets)
            {
                priceByAsset[asset.Id] = quote.Price;

                // Geçmiş: aynı (asset, AsOf) zaten varsa tekrar yazma (gün-içi FX yinelemesi).
                var snapshotExists = await db.PriceSnapshots
                    .AnyAsync(p => p.AssetId == asset.Id && p.AsOfUtc == quote.AsOfUtc, ct);
                if (!snapshotExists)
                    db.PriceSnapshots.Add(new PriceSnapshot
                    {
                        AssetId = asset.Id,
                        Price = quote.Price,
                        Source = quote.Source,
                        AsOfUtc = quote.AsOfUtc,
                        CreatedAtUtc = nowUtc,
                    });
            }

            // Döviz → CurrencyConverter'ın okuduğu FxRate (currency → TRY).
            if (quote.Instrument.Kind == PriceInstrumentKind.Currency)
            {
                var from = quote.Instrument.Currency;
                var to = quote.QuoteCurrency;
                var rateExists = await db.FxRates
                    .AnyAsync(r => r.FromCurrency == from && r.ToCurrency == to && r.AsOfUtc == quote.AsOfUtc, ct);
                if (!rateExists)
                    db.FxRates.Add(new FxRate
                    {
                        FromCurrency = from,
                        ToCurrency = to,
                        Rate = quote.Price,
                        Source = quote.Source,
                        AsOfUtc = quote.AsOfUtc,
                        CreatedAtUtc = nowUtc,
                    });
            }
        }

        // Okuma yolu: ilgili tüm holding'lerin CurrentPrice'ı (global; kullanıcıdan bağımsız).
        if (priceByAsset.Count > 0)
        {
            var assetIds = priceByAsset.Keys.ToList();
            var holdings = await db.Holdings
                .Where(h => assetIds.Contains(h.AssetId))
                .ToListAsync(ct);
            foreach (var holding in holdings)
            {
                holding.CurrentPrice = priceByAsset[holding.AssetId];
                holding.UpdatedAtUtc = nowUtc;
            }
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Varlığı kaynaktan bağımsız enstrümana eşler (Faz 2: TRY-fiyatlı altın + döviz).</summary>
    private static bool TryMapInstrument(Asset asset, out PriceInstrument instrument)
    {
        switch (asset.Type)
        {
            case AssetType.Gold when asset.PricingCurrency == CurrencyCode.TRY:
                instrument = PriceInstrument.GramGold(asset.PricingCurrency);
                return true;

            case AssetType.Fx when asset.PricingCurrency == CurrencyCode.TRY
                                   && Enum.TryParse<CurrencyCode>(asset.Symbol ?? asset.Unit, out var ccy)
                                   && ccy != CurrencyCode.TRY:
                instrument = PriceInstrument.ForCurrency(ccy);
                return true;

            default:
                instrument = default;
                return false;
        }
    }
}

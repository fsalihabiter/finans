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

        // 2) Sağlayıcılara CanQuote'a göre yönlendir; her sağlayıcı izole.
        var instruments = assetsByInstrument.Keys.ToList();
        var quotes = new List<PriceQuote>();
        var failed = new List<string>();
        foreach (var provider in providers)
        {
            var handled = instruments.Where(provider.CanQuote).ToList();
            if (handled.Count == 0)
                continue;
            try
            {
                quotes.AddRange(await provider.GetQuotesAsync(handled, ct));
            }
            catch (Exception ex)
            {
                failed.Add(provider.Source);
                logger.LogWarning(ex,
                    "Fiyat sağlayıcı {Source} başarısız; bu turda atlanıyor (fallback T2.3).", provider.Source);
            }
        }

        // 3) Kalıcılaştır: snapshot (geçmiş) + fxrate (converter) + holding.CurrentPrice (okuma yolu).
        await PersistAsync(quotes, assetsByInstrument, refreshedAt, ct);

        return CacheAndReturn(new PriceRefreshResult(quotes, refreshedAt, FromCache: false, FailedSources: failed));
    }

    private PriceRefreshResult CacheAndReturn(PriceRefreshResult result)
    {
        cache.Set(CacheKey, result, Ttl);
        return result;
    }

    private async Task PersistAsync(
        IReadOnlyList<PriceQuote> quotes,
        IReadOnlyDictionary<PriceInstrument, List<Asset>> assetsByInstrument,
        DateTime nowUtc,
        CancellationToken ct)
    {
        if (quotes.Count == 0)
            return;

        var priceByAsset = new Dictionary<Guid, decimal>();

        foreach (var quote in quotes)
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

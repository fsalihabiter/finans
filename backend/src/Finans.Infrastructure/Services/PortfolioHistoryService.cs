using Finans.Application.Common;
using Finans.Application.Portfolio;
using Finans.Domain.Enums;
using Finans.Domain.Portfolio;
using Finans.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Finans.Infrastructure.Services;

/// <summary>
/// <see cref="IPortfolioHistoryService"/> EF uygulaması (T5.2): geçerli kullanıcının
/// Transactions + PriceSnapshots + FxRates verisini T5.1 saf servisinin girdisine
/// indirger, tam seriyi hesaplar, dönem dilimler, ≤500 noktaya seyrekleştirir.
/// **Kullanıcıya kapsanır** (11 §3); cache anahtarı UserId içerir (10 §3).
///
/// İndirgeme kuralları:
/// <list type="bullet">
/// <item><b>Normal pozisyon:</b> işlemler olay; snapshot'lar + (varsa) güncel fiyat
/// (bugüne çapa) fiyat gözlemi. Hiç işlemi olmayan pozisyon (örn. elle girilen nakit)
/// oluşturulma tarihinde tek açılış olayına indirgenir.</item>
/// <item><b>BES:</b> nominal hesap — kendi katkılar ödeme tarihinde miktar olayı
/// (birim fiyat 1), devlet katkısı <b>yatma tarihinde</b> (katkı ayını izleyen ay sonu,
/// BesCalculator) birim fiyata işler, bugünkü fon değeri son gözlem. Böylece serinin
/// son günü özet ekranıyla birebir tutarlıdır (maliyet = kendi katkı; değer = fon).</item>
/// </list>
/// </summary>
public sealed class PortfolioHistoryService(
    FinansDbContext db,
    ICurrentUser currentUser,
    IAppCache cache,
    TimeProvider clock) : IPortfolioHistoryService
{
    /// <summary>Kısa TTL: işlem eklenince/fiyat tazelenince en geç 60 sn'de yansır (10 §3).</summary>
    internal static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    /// <summary>Grafik için üst nokta sayısı (StockHistoryService ile aynı payload dengesi).</summary>
    public const int MaxPoints = 500;

    /// <summary>Dönem anahtarı → gün sayısı (null = tüm seri).</summary>
    private static readonly Dictionary<string, int?> Periods = new(StringComparer.OrdinalIgnoreCase)
    {
        ["1m"] = 31,
        ["3m"] = 92,
        ["1y"] = 366,
        ["all"] = null,
    };

    public async Task<PortfolioHistoryDto> GetHistoryAsync(
        string? period, CurrencyCode? baseCurrency = null, CancellationToken ct = default)
    {
        var periodKey = string.IsNullOrWhiteSpace(period) ? "all" : period.Trim().ToLowerInvariant();
        if (!Periods.TryGetValue(periodKey, out var days))
            throw new ValidationException("period", "invalid",
                "Geçersiz dönem. Kullanılabilir: 1m, 3m, 1y, all.");

        var userId = currentUser.UserId;
        var baseCcy = await HoldingMapping.ResolveBaseCurrencyAsync(db, userId, baseCurrency, ct);

        // Cache anahtarı UserId İÇERİR — bir kullanıcının serisi asla başkasına dönmez (10 §3, 11 §3).
        var key = $"portfolio:history:{userId}:{baseCcy}:{periodKey}";
        return await cache.GetOrCreateAsync(key, Ttl,
            innerCt => BuildAsync(userId, baseCcy, periodKey, days, innerCt), ct);
    }

    private async Task<PortfolioHistoryDto> BuildAsync(
        Guid userId, CurrencyCode baseCcy, string periodKey, int? days, CancellationToken ct)
    {
        var nowUtc = clock.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(nowUtc);

        var holdings = await db.Holdings
            .Where(h => h.UserId == userId)
            .Include(h => h.Asset)
            .Include(h => h.Transactions)
            .Include(h => h.BesContributions)
            .ToListAsync(ct);

        List<DailyValuePoint> full = [];
        if (holdings.Count > 0)
        {
            var assetIds = holdings.Select(h => h.AssetId).Distinct().ToList();

            // Projeksiyonlu okuma (10 §4); AsOf sırası korunur ki gün içi son kayıt kazansın.
            var snapshots = await db.PriceSnapshots
                .Where(p => assetIds.Contains(p.AssetId))
                .OrderBy(p => p.AsOfUtc)
                .Select(p => new { p.AssetId, p.AsOfUtc, p.Price })
                .ToListAsync(ct);
            var snapshotsByAsset = snapshots
                .GroupBy(p => p.AssetId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(p => new PricePoint(DateOnly.FromDateTime(p.AsOfUtc), p.Price)).ToList());

            // Kurlar kullanıcı-bağımsız (global piyasa verisi).
            var fxRates = (await db.FxRates
                    .OrderBy(r => r.AsOfUtc)
                    .Select(r => new { r.FromCurrency, r.ToCurrency, r.Rate, r.AsOfUtc })
                    .ToListAsync(ct))
                .Select(r => new FxRatePoint(
                    DateOnly.FromDateTime(r.AsOfUtc), r.FromCurrency, r.ToCurrency, r.Rate))
                .ToList();

            var inputs = holdings
                .Select(h => ToInput(h, snapshotsByAsset.GetValueOrDefault(h.AssetId), today))
                .ToList();

            full = [.. PortfolioValueHistoryService.Calculate(inputs, fxRates, baseCcy, today)];
        }

        var firstDate = full.Count > 0 ? full[0].Date : (DateOnly?)null;

        IReadOnlyList<DailyValuePoint> sliced = full;
        if (days is { } d && full.Count > 0)
        {
            var cutoff = today.AddDays(-d);
            var window = full.Where(p => p.Date >= cutoff).ToList();
            if (window.Count > 0)
                sliced = window;
        }

        var sampled = Downsample(sliced, MaxPoints);
        decimal? changeRatio = sampled.Count > 0 && sampled[0].Value > 0m
            ? (sampled[^1].Value - sampled[0].Value) / sampled[0].Value
            : null;

        var points = sampled.Select(p => new PortfolioHistoryPointDto(p.Date, p.Value, p.Cost)).ToList();
        return new PortfolioHistoryDto(baseCcy, periodKey, points, changeRatio, firstDate, nowUtc);
    }

    /// <summary>Tek pozisyonu saf servis girdisine indirger (sınıf özetindeki kurallar).</summary>
    private static AssetValueHistoryInput ToInput(
        Holding holding, IReadOnlyList<PricePoint>? snapshots, DateOnly today)
    {
        return holding.Asset.Type == AssetType.Bes
            ? ToBesInput(holding, today)
            : ToStandardInput(holding, snapshots, today);
    }

    private static AssetValueHistoryInput ToStandardInput(
        Holding holding, IReadOnlyList<PricePoint>? snapshots, DateOnly today)
    {
        var events = holding.Transactions
            .OrderBy(t => t.TransactedAtUtc)
            .Select(t => new PositionEvent(
                DateOnly.FromDateTime(t.TransactedAtUtc), t.Type, t.Quantity, t.UnitPrice, t.Fee))
            .ToList();

        // İşlemsiz pozisyon (örn. elle girilen nakit): oluşturulma günü tek açılış olayı —
        // özet bu pozisyonu Quantity×AvgCost ile sayar, seri de aynı tabana oturur.
        if (events.Count == 0 && holding.Quantity != 0m)
        {
            events.Add(new PositionEvent(
                DateOnly.FromDateTime(holding.CreatedAtUtc),
                TransactionType.Buy, holding.Quantity, holding.AvgCost));
        }

        var prices = new List<PricePoint>(snapshots?.Count + 1 ?? 1);
        if (snapshots is not null)
            prices.AddRange(snapshots);

        // Güncel fiyat bugüne çapalanır: elle güncellenen fiyatların snapshot'ı yok; otomatik
        // fiyatlananlarda son snapshot'la aynıdır → serinin son günü özetle tutarlı kalır.
        if (holding.CurrentPrice is { } current)
            prices.Add(new PricePoint(today, current));

        return new AssetValueHistoryInput(holding.Asset.Name, holding.Asset.PricingCurrency, events, prices);
    }

    /// <summary>
    /// BES → nominal hesap indirgemesi: miktar = kümülatif kendi katkı (birim fiyat 1);
    /// devlet katkısı yatma tarihinde birim fiyata işler; bugünkü fon değeri son gözlem.
    /// </summary>
    private static AssetValueHistoryInput ToBesInput(Holding holding, DateOnly today)
    {
        // Yatırılmış (bugüne dek ödenmiş) kendi katkılar — miktar olayları.
        var contributions = holding.BesContributions
            .Where(c => DateOnly.FromDateTime(c.PaidAtUtc) <= today)
            .OrderBy(c => c.PaidAtUtc)
            .ToList();

        var events = contributions
            .Where(c => c.OwnAmount > 0m)
            .Select(c => new PositionEvent(
                DateOnly.FromDateTime(c.PaidAtUtc), TransactionType.Buy, c.OwnAmount, UnitPrice: 1m))
            .ToList();

        // Birim fiyat zaman çizgisi: (kümülatif kendi + yatmış devlet) / kümülatif kendi.
        // Değişim noktaları: kendi katkı ödeme günleri + devlet katkısı yatma günleri.
        var changeDates = new SortedSet<DateOnly>();
        foreach (var c in contributions)
        {
            changeDates.Add(DateOnly.FromDateTime(c.PaidAtUtc));
            if (c.StateAmount > 0m)
            {
                var deposit = DateOnly.FromDateTime(BesCalculator.StateDepositDateFor(c.PaidAtUtc));
                if (deposit <= today)
                    changeDates.Add(deposit);
            }
        }

        var prices = new List<PricePoint>(changeDates.Count + 1);
        foreach (var date in changeDates)
        {
            decimal cumOwn = 0m;
            decimal cumState = 0m;
            foreach (var c in contributions)
            {
                if (DateOnly.FromDateTime(c.PaidAtUtc) <= date)
                    cumOwn += c.OwnAmount;
                if (c.StateAmount > 0m &&
                    DateOnly.FromDateTime(BesCalculator.StateDepositDateFor(c.PaidAtUtc)) <= date)
                    cumState += c.StateAmount;
            }

            if (cumOwn > 0m)
                prices.Add(new PricePoint(date, (cumOwn + cumState) / cumOwn));
        }

        // Bugünkü gerçek fon değeri (fon getirisi dahil) — yalnız bugün bilinir, geçmişe yayılmaz
        // (geçmişi gösteriyoruz, uydurmuyoruz — CLAUDE.md §2).
        var totalOwn = contributions.Sum(c => c.OwnAmount);
        if (holding.CurrentPrice is { } fundValue && totalOwn > 0m)
            prices.Add(new PricePoint(today, fundValue / totalOwn));

        return new AssetValueHistoryInput(
            holding.Asset.Name, holding.Asset.PricingCurrency, events, prices);
    }

    /// <summary>Eşit adımlı seyrekleştirme — ilk ve son nokta daima korunur (değişim uçlardan).</summary>
    internal static IReadOnlyList<DailyValuePoint> Downsample(IReadOnlyList<DailyValuePoint> points, int max)
    {
        if (points.Count <= max)
            return points;

        var result = new List<DailyValuePoint>(max);
        var step = (double)(points.Count - 1) / (max - 1);
        for (var i = 0; i < max; i++)
            result.Add(points[(int)Math.Round(i * step)]);
        result[^1] = points[^1];
        return result;
    }
}

using Finans.Application.Common;
using Finans.Application.Portfolio;
using Finans.Domain.Enums;
using Finans.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Finans.Infrastructure.Services;

/// <summary>
/// <see cref="IPortfolioHistoryService"/> EF uygulaması (T5.2): geçerli kullanıcının
/// Transactions + PriceSnapshots + FxRates verisini T5.1 saf servisinin girdisine
/// indirger (ortak kurallar: <see cref="HoldingHistoryInputs"/>), tam seriyi hesaplar,
/// dönem dilimler, ≤500 noktaya seyrekleştirir. **Kullanıcıya kapsanır** (11 §3);
/// cache anahtarı UserId içerir (10 §3).
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
                .Select(h => HoldingHistoryInputs.ToInput(h, snapshotsByAsset.GetValueOrDefault(h.AssetId), today))
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

    /// <summary>Eşit adımlı seyrekleştirme — ortak yardımcıya delege (Senaryo da kullanır).</summary>
    internal static IReadOnlyList<DailyValuePoint> Downsample(IReadOnlyList<DailyValuePoint> points, int max) =>
        HoldingHistoryInputs.Downsample(points, max);
}

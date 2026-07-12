using Finans.Application.Common;
using Finans.Application.Portfolio;
using Finans.Domain.Enums;
using Finans.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Finans.Infrastructure.Services;

/// <summary>
/// <see cref="IScenarioService"/> EF uygulaması (T5.4): tek pozisyonu T5.1 saf servisine
/// indirger (<see cref="HoldingHistoryInputs"/> — Değer Seyri ile AYNI kurallar) → günlük
/// (değer, yatırılan) serisi; üzerine alım gücü eşiği (enflasyon düzeltmeli yatırılan,
/// <see cref="ScenarioCalculationService"/>) eklenir. **Tahmin yok** (CLAUDE.md §2).
/// **Kullanıcıya kapsanır**: başkasının pozisyonu → 404 (IDOR yok, 11 §3); cache anahtarı
/// UserId + HoldingId içerir (10 §3).
/// </summary>
public sealed class ScenarioService(
    FinansDbContext db,
    ICurrentUser currentUser,
    IInflationRateProvider inflationRateProvider,
    IAppCache cache,
    TimeProvider clock) : IScenarioService
{
    internal static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    /// <summary>Grafik için üst nokta sayısı (Değer Seyri ile aynı payload dengesi).</summary>
    public const int MaxPoints = 500;

    public async Task<ScenarioComparisonDto> CompareAsync(
        Guid holdingId, CurrencyCode? baseCurrency = null, CancellationToken ct = default)
    {
        var userId = currentUser.UserId;
        var baseCcy = await HoldingMapping.ResolveBaseCurrencyAsync(db, userId, baseCurrency, ct);

        // Damga (stamp) anahtara girer: işlem/pozisyon değişince seri ANINDA yeniden hesaplanır.
        var stamp = await PortfolioCacheStamp.GetAsync(cache, userId, ct);
        var key = $"portfolio:scenario:{userId}:{stamp}:{holdingId}:{baseCcy}";
        return await cache.GetOrCreateAsync(key, Ttl,
            innerCt => BuildAsync(userId, holdingId, baseCcy, innerCt), ct);
    }

    private async Task<ScenarioComparisonDto> BuildAsync(
        Guid userId, Guid holdingId, CurrencyCode baseCcy, CancellationToken ct)
    {
        var nowUtc = clock.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(nowUtc);

        // Per-user kapsam: başkasının kaydı yokmuş gibi davranır (404 — varlığı sızdırma).
        var holding = await db.Holdings
            .Where(h => h.Id == holdingId && h.UserId == userId)
            .Include(h => h.Asset)
            .Include(h => h.Transactions)
            .Include(h => h.BesContributions)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException();

        var snapshots = (await db.PriceSnapshots
                .Where(p => p.AssetId == holding.AssetId)
                .OrderBy(p => p.AsOfUtc)
                .Select(p => new { p.AsOfUtc, p.Price })
                .ToListAsync(ct))
            .Select(p => new PricePoint(DateOnly.FromDateTime(p.AsOfUtc), p.Price))
            .ToList();

        var fxRates = (await db.FxRates
                .OrderBy(r => r.AsOfUtc)
                .Select(r => new { r.FromCurrency, r.ToCurrency, r.Rate, r.AsOfUtc })
                .ToListAsync(ct))
            .Select(r => new FxRatePoint(
                DateOnly.FromDateTime(r.AsOfUtc), r.FromCurrency, r.ToCurrency, r.Rate))
            .ToList();

        var input = HoldingHistoryInputs.ToInput(holding, snapshots, today);
        var series = PortfolioValueHistoryService.Calculate([input], fxRates, baseCcy, today);

        // Alım gücü eşiği — enflasyon verisi yoksa çizgi üretilmez (null; UI iki çizgiyle kalır).
        var inflation = await inflationRateProvider.GetAnnualRateAsync(ct);
        IReadOnlyList<decimal>? threshold = inflation is { } rate
            ? ScenarioCalculationService.InflationAdjustedCostSeries(series, rate)
            : null;

        var fullPoints = series
            .Select((p, i) => new ScenarioPointDto(p.Date, p.Value, p.Cost, threshold?[i]))
            .ToList();
        var points = HoldingHistoryInputs.Downsample(fullPoints, MaxPoints);

        var last = series.Count > 0 ? series[^1] : null;
        var summary = new ScenarioSummaryDto(
            CurrentValue: last?.Value ?? 0m,
            Invested: last?.Cost ?? 0m,
            Difference: (last?.Value ?? 0m) - (last?.Cost ?? 0m),
            DifferenceRatio: last is { Cost: not 0m } ? (last.Value - last.Cost) / last.Cost : null,
            InflationAdjustedInvested: threshold is { Count: > 0 } ? threshold[^1] : null,
            AnnualInflationRate: inflation);

        return new ScenarioComparisonDto(
            holding.Id,
            holding.Asset.Name,
            holding.Asset.Type,
            baseCcy,
            points,
            summary,
            series.Count > 0 ? series[0].Date : null,
            nowUtc);
    }
}

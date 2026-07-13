using Finans.Application.Common;
using Finans.Application.Portfolio;
using Finans.Application.Pricing;
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
///
/// FX yarışı (SC-42 — Değer Seyri ile aynı desen): kur satırları fiyat tazeleme turunda
/// yazılır; kur commit edilmeden gelen istekte tazelemeyi bir kez kendisi tetikler
/// (single-flight → paralel /prices ile birleşir) ve yeniden hesaplar; kur yine yoksa
/// 500 yerine sözleşmeli 502 döner.
/// </summary>
public sealed class ScenarioService(
    FinansDbContext db,
    ICurrentUser currentUser,
    IInflationRateProvider inflationRateProvider,
    IAppCache cache,
    IPriceFetchService priceFetch,
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

        var input = HoldingHistoryInputs.ToInput(holding, snapshots, today);

        IReadOnlyList<DailyValuePoint> series;
        try
        {
            series = PortfolioValueHistoryService.Calculate([input], await LoadFxAsync(ct), baseCcy, today);
        }
        catch (MissingFxRateException)
        {
            // İlk yükleme FX yarışı: kur satırları paralel fiyat tazeleme turunda henüz
            // yazılmamış olabilir. Turu bir kez tetikle (cache'li/single-flight), kurları
            // yeniden okuyup dene; hâlâ yoksa sözleşmeli 502 — 500 değil (NFR-5).
            await priceFetch.RefreshAsync(ct);
            try
            {
                series = PortfolioValueHistoryService.Calculate([input], await LoadFxAsync(ct), baseCcy, today);
            }
            catch (MissingFxRateException)
            {
                throw new UpstreamException(
                    "Kur verisi şu anda hazırlanamadı; senaryo geçici olarak gösterilemiyor. Lütfen birazdan tekrar dene.");
            }
        }

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

    /// <summary>Kurlar kullanıcı-bağımsız (global piyasa verisi); tazeleme sonrası yeniden okunabilir.</summary>
    private async Task<List<FxRatePoint>> LoadFxAsync(CancellationToken ct) =>
        (await db.FxRates
            .OrderBy(r => r.AsOfUtc)
            .Select(r => new { r.FromCurrency, r.ToCurrency, r.Rate, r.AsOfUtc })
            .ToListAsync(ct))
        .Select(r => new FxRatePoint(
            DateOnly.FromDateTime(r.AsOfUtc), r.FromCurrency, r.ToCurrency, r.Rate))
        .ToList();
}

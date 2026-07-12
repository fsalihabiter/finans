using Finans.Domain.Enums;

namespace Finans.Application.Portfolio;

/// <summary>
/// Senaryo v1 use-case'i (T5.4 — 04 §7.2): tek pozisyon için geçmişe dönük
/// "almasaydım / nakitte dursaydı" karşılaştırması. **Tahmin yok** — yalnız birikmiş
/// geçmiş veri (CLAUDE.md §2). Geçerli kullanıcıya kapsanır: başkasının pozisyonu →
/// <see cref="Common.NotFoundException"/> (IDOR yok, 11 §3).
/// </summary>
public interface IScenarioService
{
    Task<ScenarioComparisonDto> CompareAsync(
        Guid holdingId, CurrencyCode? baseCurrency = null, CancellationToken ct = default);
}

/// <summary>
/// GET /api/portfolio/scenario/{holdingId} yanıtı. Üç seri aynı noktalarda:
/// gerçek değer · yatırılan (nakitte dursaydı, nominal) · alım gücü eşiği
/// (enflasyon düzeltmeli yatırılan; enflasyon verisi yoksa null).
/// </summary>
public sealed record ScenarioComparisonDto(
    Guid HoldingId,
    string Name,
    AssetType AssetType,
    CurrencyCode BaseCurrency,
    IReadOnlyList<ScenarioPointDto> Points,
    ScenarioSummaryDto Summary,
    DateOnly? FirstDate,
    DateTime AsOf);

/// <summary>Serinin bir günü (baz pb). <see cref="InflationAdjustedCost"/> null = enflasyon verisi yok.</summary>
public sealed record ScenarioPointDto(
    DateOnly Date,
    decimal Value,
    decimal Cost,
    decimal? InflationAdjustedCost);

/// <summary>
/// Karşılaştırmanın bugünkü özeti — sayılar KODDAN, yorum yok (tavsiye değil çerçevesi UI'da).
/// </summary>
public sealed record ScenarioSummaryDto(
    /// <summary>Pozisyonun bugünkü değeri.</summary>
    decimal CurrentValue,
    /// <summary>Yatırılan toplam (nakitte dursaydı elde olacak nominal tutar).</summary>
    decimal Invested,
    /// <summary>Fark = bugünkü değer − yatırılan.</summary>
    decimal Difference,
    /// <summary>Fark oranı; yatırılan 0 ise null.</summary>
    decimal? DifferenceRatio,
    /// <summary>Alım gücü eşiği (bugün); enflasyon verisi yoksa null.</summary>
    decimal? InflationAdjustedInvested,
    /// <summary>Kullanılan yıllık enflasyon oranı (şeffaflık için); yoksa null.</summary>
    decimal? AnnualInflationRate);

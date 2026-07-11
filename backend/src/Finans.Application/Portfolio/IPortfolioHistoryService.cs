using Finans.Domain.Enums;

namespace Finans.Application.Portfolio;

/// <summary>
/// Portföy değer geçmişi use-case'i (T5.2 — 04 §4). Geçerli kullanıcıya kapsanır
/// (11 §3); cache anahtarı UserId içerir (10 §3). Seri hesabı T5.1 saf servisinde
/// (<see cref="PortfolioValueHistoryService"/>); burada EF verisi girdiye indirgenir.
/// </summary>
public interface IPortfolioHistoryService
{
    /// <summary>
    /// Günlük değer + yatırılan maliyet serisi. <paramref name="period"/>: 1m | 3m | 1y | all
    /// (boş = all). Geçersiz dönem → <see cref="Common.ValidationException"/> (400).
    /// </summary>
    Task<PortfolioHistoryDto> GetHistoryAsync(
        string? period, CurrencyCode? baseCurrency = null, CancellationToken ct = default);
}

/// <summary>
/// GET /api/portfolio/history yanıtı. Noktalar günlük, baz para biriminde; grafik
/// gösterim amaçlıdır (tahmin değil, geçmiş — CLAUDE.md §2). Seri ≤500 noktaya
/// seyrekleştirilir (uçlar korunur); <see cref="ChangeRatio"/> dönem uçlarından.
/// </summary>
/// <param name="FirstDate">TÜM serinin ilk günü (dönemden bağımsız) — "veri şu tarihten beri".</param>
public sealed record PortfolioHistoryDto(
    CurrencyCode BaseCurrency,
    string Period,
    IReadOnlyList<PortfolioHistoryPointDto> Points,
    decimal? ChangeRatio,
    DateOnly? FirstDate,
    DateTime AsOf);

/// <summary>Serinin bir günü: portföy değeri + o güne dek yatırılan maliyet (baz pb).</summary>
public sealed record PortfolioHistoryPointDto(DateOnly Date, decimal Value, decimal Cost);

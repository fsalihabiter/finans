namespace Finans.Application.Stocks;

/// <summary>
/// Hisse fiyat geçmişi (T4.5 — kullanıcı isteği: "halka arzdan beri, TradingView/Investing
/// gibi dönemlerle"). Günlük kapanış serisi; dönem dilimleme + değişim oranı KODDA
/// (CLAUDE.md §3.1). Geçmiş gösterimdir, gelecek tahmini DEĞİLDİR (CLAUDE.md §2).
/// </summary>
public sealed record StockHistory(
    string Symbol,
    /// <summary>İstenen dönem anahtarı: 1w · 1m · 3m · 1y · 5y · max.</summary>
    string Range,
    IReadOnlyList<StockPricePoint> Points,
    /// <summary>Dönem başı → sonu değişim oranı (0,12 = %12); dönem başı 0/boşsa null.</summary>
    decimal? ChangeRatio,
    /// <summary>Serinin ilk kaydı (halka arz sonrası ilk veri) — "piyasaya girişten beri" bağlamı.</summary>
    DateOnly FirstTradeDate,
    string Source);

/// <summary>Tek günlük kapanış noktası.</summary>
public sealed record StockPricePoint(DateOnly Date, decimal Close);

/// <summary>
/// Dış fiyat geçmişi kaynağı soyutlaması. TÜM günlük seriyi döner (dilimleme serviste);
/// sembol kaynakta yoksa null. Taşıma/ayrıştırma hatasında istisna — üst katman 502'ye eşler.
/// </summary>
public interface IStockHistoryProvider
{
    string Source { get; }
    Task<IReadOnlyList<StockPricePoint>?> GetDailyHistoryAsync(string symbol, CancellationToken ct = default);
}

/// <summary>Fiyat geçmişi use-case'i: doğrulama + cache + dönem dilimleme.</summary>
public interface IStockHistoryService
{
    /// <summary>Geçersiz sembol/dönem → 400; kaynakta yok → 404; kaynak hatası → 502.</summary>
    Task<StockHistory> GetHistoryAsync(string symbol, string? range, CancellationToken ct = default);
}

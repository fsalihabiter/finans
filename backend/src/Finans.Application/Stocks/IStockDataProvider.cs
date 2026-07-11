namespace Finans.Application.Stocks;

/// <summary>
/// Dış hisse verisi kaynağı soyutlaması (T4.2 — desen: Faz 2 <c>IPriceProvider</c>).
/// Somut sağlayıcı Infrastructure'da (Finnhub); anahtar yapılandırılmamışsa DI
/// <c>NotConfiguredStockDataProvider</c> bağlar → anlamlı 502 (uygulama çökmez).
/// </summary>
public interface IStockDataProvider
{
    /// <summary>Kaynak anahtarı — yanıt/log için (örn. "finnhub").</summary>
    string Source { get; }

    /// <summary>
    /// Sembolün güncel metriklerini çeker. Sembol kaynakta yoksa <c>null</c> döner
    /// (üst katman 404'e çevirir). Taşıma/ayrıştırma hatasında istisna fırlatır —
    /// üst katman 502'ye eşler (T4.2; NFR-5: uygulama çökmez).
    /// </summary>
    Task<StockMetricsDto?> GetMetricsAsync(string symbol, CancellationToken ct = default);
}

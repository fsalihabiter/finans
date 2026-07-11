namespace Finans.Application.Stocks;

/// <summary>
/// Hisse metrik use-case'i (T4.2): sembol doğrulama + cache + sağlayıcı çağrısı + hata eşleme.
/// Piyasa verisi kullanıcıya özgü DEĞİLDİR → cache anahtarı sembol bazlı (UserId'siz; 10 §3
/// per-user kuralı kullanıcı verisi içindir, halka açık piyasa verisi ortak cache'lenir).
/// </summary>
public interface IStockDataService
{
    /// <summary>
    /// Sembolün metriklerini döner. Geçersiz sembol → <c>ValidationException</c> (400);
    /// kaynakta yok → <c>NotFoundException</c> (404); kaynak yapılandırılmamış/erişilemez →
    /// <c>UpstreamException</c> (502). Uygulama hiçbir durumda çökmez (NFR-5).
    /// </summary>
    Task<StockMetricsDto> GetMetricsAsync(string symbol, CancellationToken ct = default);
}

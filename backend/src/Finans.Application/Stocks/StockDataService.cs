using Finans.Application.Common;
using Microsoft.Extensions.Logging;

namespace Finans.Application.Stocks;

/// <summary>
/// <see cref="IStockDataService"/> uygulaması (T4.2). Akış: sembol doğrula → cache →
/// tek-uçuşta sağlayıcıdan çek → cache'e yaz. Hata eşleme: sembol yok → 404; kaynak
/// erişilemez/yapılandırılmamış → 502 (NFR-5: uygulama çökmez, anlamlı hata).
/// <para>Cache: piyasa verisi ortak (UserId'siz anahtar) + 1 saat TTL — temel metrikler
/// gün içi nadiren değişir; Finnhub ücretsiz kota (60 çağrı/dk) da korunur (NFR-9).</para>
/// </summary>
public sealed class StockDataService(
    IStockDataProvider provider,
    IAppCache cache,
    ILogger<StockDataService> logger) : IStockDataService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    public async Task<StockMetricsDto> GetMetricsAsync(string symbol, CancellationToken ct = default)
    {
        var normalized = StockSymbols.Normalize(symbol);
        var key = $"stock:metrics:{normalized}";

        var cached = await cache.GetAsync<StockMetricsDto>(key, ct);
        if (cached is not null) return cached;

        // Tek-uçuş: aynı sembole eşzamanlı istekler tek dış çağrıya iner (kota koruması).
        return await cache.SingleFlightAsync(key, async innerCt =>
        {
            var again = await cache.GetAsync<StockMetricsDto>(key, innerCt);
            if (again is not null) return again;

            StockMetricsDto? dto;
            try
            {
                dto = await provider.GetMetricsAsync(normalized, innerCt);
            }
            catch (Exception ex) when (
                ex is not AppException &&
                (ex is not OperationCanceledException || !innerCt.IsCancellationRequested))
            {
                // Taşıma/ayrıştırma/zaman aşımı — iç detay kullanıcıya sızmaz (11 §4), log'da kalır.
                logger.LogWarning(ex, "Hisse veri kaynağı hatası ({Symbol}).", normalized);
                throw new UpstreamException("Hisse veri kaynağına şu an ulaşılamıyor. Biraz sonra tekrar dene.");
            }

            if (dto is null)
                throw new NotFoundException("Bu sembol için veri bulunamadı.");

            await cache.SetAsync(key, dto, CacheTtl, innerCt);
            return dto;
        }, ct);
    }
}

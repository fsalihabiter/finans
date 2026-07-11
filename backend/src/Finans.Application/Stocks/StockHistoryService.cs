using Finans.Application.Common;
using Microsoft.Extensions.Logging;

namespace Finans.Application.Stocks;

/// <summary>
/// <see cref="IStockHistoryService"/> uygulaması (T4.5). Tüm günlük seri sembol başına
/// 24 saat ORTAK cache'lenir (piyasa verisi; günlük kapanış günde bir değişir); dönem
/// dilimleme + değişim oranı + seyrekleştirme KODDA ve deterministik.
/// </summary>
public sealed class StockHistoryService(
    IStockHistoryProvider provider,
    IAppCache cache,
    TimeProvider time,
    ILogger<StockHistoryService> logger) : IStockHistoryService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    /// <summary>Grafik için üst nokta sayısı — payload/çizim dengesi (5 yıl ≈ 1250 gün → örneklenir).</summary>
    public const int MaxPoints = 500;

    /// <summary>Dönem anahtarı → gün sayısı (null = tüm seri, "halka arzdan beri").</summary>
    private static readonly Dictionary<string, int?> Ranges = new(StringComparer.OrdinalIgnoreCase)
    {
        ["1w"] = 7,
        ["1m"] = 31,
        ["3m"] = 92,
        ["1y"] = 366,
        ["5y"] = 1830,
        ["max"] = null,
    };

    public async Task<StockHistory> GetHistoryAsync(string symbol, string? range, CancellationToken ct = default)
    {
        var normalized = StockSymbols.Normalize(symbol);
        var rangeKey = string.IsNullOrWhiteSpace(range) ? "1y" : range.Trim().ToLowerInvariant();
        if (!Ranges.TryGetValue(rangeKey, out var days))
            throw new ValidationException("range", "invalid",
                "Geçersiz dönem. Kullanılabilir: 1w, 1m, 3m, 1y, 5y, max.");

        var full = await GetFullHistoryAsync(normalized, ct);
        if (full.Count == 0)
            throw new NotFoundException("Bu sembol için fiyat geçmişi bulunamadı.");

        var firstTrade = full[0].Date;

        IReadOnlyList<StockPricePoint> sliced = full;
        if (days is not null)
        {
            var cutoff = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime).AddDays(-days.Value);
            var window = full.Where(p => p.Date >= cutoff).ToList();
            // Yeni halka arz: pencere boşsa eldeki tüm seri gösterilir (yanıltıcı boş grafik yerine).
            if (window.Count > 0) sliced = window;
        }

        var sampled = Downsample(sliced, MaxPoints);
        var first = sampled[0].Close;
        var last = sampled[^1].Close;
        decimal? changeRatio = first > 0m ? (last - first) / first : null;

        return new StockHistory(normalized, rangeKey, sampled, changeRatio, firstTrade, provider.Source);
    }

    /// <summary>Tam günlük seri — sembol başına 24s cache + tek-uçuş; hata cache'lenmez.</summary>
    private async Task<IReadOnlyList<StockPricePoint>> GetFullHistoryAsync(string symbol, CancellationToken ct)
    {
        var key = $"stock:history:{symbol}";
        var cached = await cache.GetAsync<List<StockPricePoint>>(key, ct);
        if (cached is not null) return cached;

        return await cache.SingleFlightAsync<IReadOnlyList<StockPricePoint>>(key, async innerCt =>
        {
            var again = await cache.GetAsync<List<StockPricePoint>>(key, innerCt);
            if (again is not null) return again;

            IReadOnlyList<StockPricePoint>? points;
            try
            {
                points = await provider.GetDailyHistoryAsync(symbol, innerCt);
            }
            catch (Exception ex) when (
                ex is not AppException &&
                (ex is not OperationCanceledException || !innerCt.IsCancellationRequested))
            {
                logger.LogWarning(ex, "Fiyat geçmişi kaynağı hatası ({Symbol}).", symbol);
                throw new UpstreamException("Fiyat geçmişi kaynağına şu an ulaşılamıyor. Biraz sonra tekrar dene.");
            }

            if (points is null || points.Count == 0)
                throw new NotFoundException("Bu sembol için fiyat geçmişi bulunamadı.");

            var ordered = points.OrderBy(p => p.Date).ToList();
            await cache.SetAsync(key, ordered, CacheTtl, innerCt);
            return ordered;
        }, ct);
    }

    /// <summary>
    /// Eşit adımlı seyrekleştirme — İLK ve SON nokta daima korunur (dönem değişimi tam
    /// uçlardan hesaplandığı için uçlar feda edilemez).
    /// </summary>
    internal static IReadOnlyList<StockPricePoint> Downsample(IReadOnlyList<StockPricePoint> points, int max)
    {
        if (points.Count <= max) return points;
        var result = new List<StockPricePoint>(max);
        var step = (double)(points.Count - 1) / (max - 1);
        for (var i = 0; i < max; i++)
            result.Add(points[(int)Math.Round(i * step)]);
        result[^1] = points[^1];
        return result;
    }
}

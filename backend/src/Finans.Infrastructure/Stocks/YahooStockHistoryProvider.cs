using System.Net;
using System.Text.Json;
using Finans.Application.Stocks;

namespace Finans.Infrastructure.Stocks;

/// <summary>
/// Fiyat geçmişi sağlayıcısı — <b>Yahoo Finance chart API</b> (T4.5). Neden Yahoo: ANAHTARSIZ
/// ve halka arzdan bugüne TAM günlük seri (AAPL: 1980'den beri ~11.5k nokta; canlı doğrulandı).
/// Denenen alternatifler: Finnhub ücretsiz katmanı mum verisini kapattı; Stooq bot doğrulaması
/// (JavaScript proof-of-work) istiyor — sunucudan kullanılamaz.
/// Uç: <c>GET /v8/finance/chart/{sembol}?period1=0&amp;period2=9999999999&amp;interval=1d</c>.
/// User-Agent başlığı gerekir (DI'da ayarlanır). Noktalı semboller Yahoo'da tire (BRK.B → BRK-B).
/// Bilinmeyen sembolde HTTP 404 döner → null (üst katman 404'e çevirir).
/// </summary>
public sealed class YahooStockHistoryProvider(HttpClient http) : IStockHistoryProvider
{
    public const string SourceKey = "yahoo";

    public string Source => SourceKey;

    public async Task<IReadOnlyList<StockPricePoint>?> GetDailyHistoryAsync(
        string symbol, CancellationToken ct = default)
    {
        var yahooSymbol = symbol.Replace('.', '-');
        using var resp = await http.GetAsync(
            $"v8/finance/chart/{Uri.EscapeDataString(yahooSymbol)}?period1=0&period2=9999999999&interval=1d", ct);

        if (resp.StatusCode == HttpStatusCode.NotFound)
            return null; // bilinmeyen sembol — hata değil, "yok"

        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));

        if (!doc.RootElement.TryGetProperty("chart", out var chart) ||
            !chart.TryGetProperty("result", out var results) ||
            results.ValueKind != JsonValueKind.Array || results.GetArrayLength() == 0)
            return null;

        var result = results[0];
        if (!result.TryGetProperty("timestamp", out var timestamps) ||
            timestamps.ValueKind != JsonValueKind.Array)
            return null;

        var closes = result.GetProperty("indicators").GetProperty("quote")[0].GetProperty("close");

        var count = timestamps.GetArrayLength();
        var points = new List<StockPricePoint>(count);
        for (var i = 0; i < count; i++)
        {
            var closeEl = closes[i];
            if (closeEl.ValueKind != JsonValueKind.Number) continue; // tatil/boş gün null gelir — atla
            var close = closeEl.GetDecimal();
            if (close <= 0m) continue;

            var date = DateOnly.FromDateTime(
                DateTimeOffset.FromUnixTimeSeconds(timestamps[i].GetInt64()).UtcDateTime);
            // Fazla ondalık gürültüsünü kırp (315.32000732421875 → 315.3200); hesap değil gösterim verisi.
            points.Add(new StockPricePoint(date, Math.Round(close, 4)));
        }

        return points.Count > 0 ? points : null;
    }
}

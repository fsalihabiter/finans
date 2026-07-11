using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Finans.Application.Stocks;

namespace Finans.Infrastructure.Stocks;

/// <summary>
/// Finnhub hisse veri sağlayıcısı (T4.2 — karar T4.1, ABD hisseleri; BIST ertelendi).
/// Üç uç paralel çekilir: <c>/stock/metric?metric=all</c> (4 metrik), <c>/quote</c>
/// (fiyat/değişim), <c>/stock/profile2</c> (ad/borsa/para birimi). Anahtar
/// <c>X-Finnhub-Token</c> başlığıyla gider (URL'de değil → log'a sızmaz, 12 §3).
///
/// <para><b>Alan eşlemesi (T4.1 notu):</b> F/K=peTTM→peNormalizedAnnual · PD/DD=pb ·
/// temettü=dividendYieldIndicatedAnnual→currentDividendYieldTTM · büyüme=epsGrowthTTMYoy→
/// epsGrowth5Y. Finnhub yüzde alanlarını 0-100 ölçeğinde verir (0,55 = %0,55) → oran
/// (0-1) için 100'e bölünür; F/K ve PD/DD zaten düz orandır. Boş/eksik alan → null
/// ("veri yok", 07 §5 çizgisi).</para>
///
/// <para><b>Sembol yok tespiti:</b> Finnhub bilinmeyen sembole 200 + boş gövdeler döner
/// (profile2 <c>{}</c>, quote sıfırlar) → ad boş VE fiyat 0 ise <c>null</c> (üst katman 404).</para>
/// </summary>
public sealed class FinnhubStockDataProvider(HttpClient http, TimeProvider time) : IStockDataProvider
{
    public const string SourceKey = "finnhub";

    public string Source => SourceKey;

    public async Task<StockMetricsDto?> GetMetricsAsync(string symbol, CancellationToken ct = default)
    {
        var metricTask = http.GetFromJsonAsync<MetricResponse>($"stock/metric?symbol={symbol}&metric=all", ct);
        var quoteTask = http.GetFromJsonAsync<QuoteResponse>($"quote?symbol={symbol}", ct);
        var profileTask = http.GetFromJsonAsync<ProfileResponse>($"stock/profile2?symbol={symbol}", ct);
        await Task.WhenAll(metricTask, quoteTask, profileTask);

        var metric = metricTask.Result?.Metric;
        var quote = quoteTask.Result;
        var profile = profileTask.Result;

        // Bilinmeyen sembol: profil adı yok + fiyat 0/yok → 404'e çevrilecek null.
        var name = profile?.Name;
        var price = quote?.Current is > 0m ? quote.Current : null;
        if (string.IsNullOrWhiteSpace(name) && price is null)
            return null;

        var values = new StockMetricValues(
            PeRatio: metric?.PeTtm ?? metric?.PeNormalizedAnnual,
            PbRatio: metric?.Pb,
            DividendYield: Percent(metric?.DividendYieldIndicatedAnnual ?? metric?.CurrentDividendYieldTtm),
            EarningsGrowth: Percent(metric?.EpsGrowthTtmYoy ?? metric?.EpsGrowth5Y));

        return new StockMetricsDto(
            Symbol: symbol,
            Name: string.IsNullOrWhiteSpace(name) ? symbol : name!,
            Exchange: profile?.Exchange,
            Currency: string.IsNullOrWhiteSpace(profile?.Currency) ? "USD" : profile!.Currency!,
            Price: price,
            ChangeRatio: Percent(quote?.ChangePercent),
            Metrics: values,
            SectorContext: StockMetricContext.Contextualize(values),
            AsOfUtc: time.GetUtcNow().UtcDateTime,
            Source: SourceKey);
    }

    /// <summary>Finnhub yüzde ölçeğini (0-100) orana (0-1) çevirir; null geçer.</summary>
    private static decimal? Percent(decimal? v) => v is null ? null : v.Value / 100m;

    // ── Finnhub yanıt şekilleri (yalnız kullandığımız alanlar; fazlası yutulur) ──

    private sealed record MetricResponse(
        [property: JsonPropertyName("metric")] MetricFields? Metric);

    private sealed record MetricFields(
        [property: JsonPropertyName("peTTM")] decimal? PeTtm,
        [property: JsonPropertyName("peNormalizedAnnual")] decimal? PeNormalizedAnnual,
        [property: JsonPropertyName("pb")] decimal? Pb,
        [property: JsonPropertyName("dividendYieldIndicatedAnnual")] decimal? DividendYieldIndicatedAnnual,
        [property: JsonPropertyName("currentDividendYieldTTM")] decimal? CurrentDividendYieldTtm,
        [property: JsonPropertyName("epsGrowthTTMYoy")] decimal? EpsGrowthTtmYoy,
        [property: JsonPropertyName("epsGrowth5Y")] decimal? EpsGrowth5Y);

    private sealed record QuoteResponse(
        [property: JsonPropertyName("c")] decimal? Current,
        [property: JsonPropertyName("dp")] decimal? ChangePercent);

    private sealed record ProfileResponse(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("exchange")] string? Exchange,
        [property: JsonPropertyName("currency")] string? Currency);
}

/// <summary>
/// Anahtar yapılandırılmadığında bağlanan güvenli varsayılan (Noop deseni — T3.1
/// <c>NoopLlmClient</c> gibi): anlamlı 502 mesajı, çökme yok (NFR-5).
/// </summary>
public sealed class NotConfiguredStockDataProvider : IStockDataProvider
{
    public string Source => "none";

    public Task<StockMetricsDto?> GetMetricsAsync(string symbol, CancellationToken ct = default) =>
        throw new Finans.Application.Common.UpstreamException(
            "Hisse veri kaynağı yapılandırılmamış. Ücretsiz Finnhub anahtarı alıp .env dosyasına FINNHUB_API_KEY olarak ekle (SETUP.md).");
}

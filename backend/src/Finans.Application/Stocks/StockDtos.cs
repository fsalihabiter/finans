namespace Finans.Application.Stocks;

/// <summary>
/// Hisse temel analiz yanıtı (T4.2 — 04 §7). Tüm sayılar dış kaynaktan (Finnhub) gelir ve
/// KODDA normalize edilir (oranlar 0-1 aralığında ondalık; yüzde değil — CLAUDE.md §3.1).
/// Kaynağın vermediği metrik <c>null</c> kalır → UI "veri yok" gösterir (07 §5 çizgisi).
/// <b>Yatırım tavsiyesi DEĞİL</b>: bu DTO yalnız mevcut durumu taşır; yorum T4.3'te ayrı.
/// </summary>
public sealed record StockMetricsDto(
    string Symbol,
    string Name,
    string? Exchange,
    string Currency,
    decimal? Price,
    /// <summary>Günlük değişim oranı (0,012 = %1,2).</summary>
    decimal? ChangeRatio,
    StockMetricValues Metrics,
    StockSectorContext SectorContext,
    /// <summary>Verinin çekildiği an (cache'lenen yanıtta üretim anı).</summary>
    DateTime AsOfUtc,
    /// <summary>Veri kaynağı anahtarı (örn. "finnhub") — şeffaflık/log.</summary>
    string Source);

/// <summary>Dört çekirdek metrik (CLAUDE.md §6): F/K, PD/DD, temettü verimi, kâr büyümesi.</summary>
public sealed record StockMetricValues(
    decimal? PeRatio,
    decimal? PbRatio,
    /// <summary>Temettü verimi oran olarak (0,0052 = %0,52).</summary>
    decimal? DividendYield,
    /// <summary>Yıllık EPS büyümesi oran olarak (0,091 = %9,1).</summary>
    decimal? EarningsGrowth);

/// <summary>
/// Kaba bağlam etiketleri (04 §7 örnek değerleri: "above/high/low/positive"). Ücretsiz katmanda
/// sektör ortalaması yok → eşikler KODDA, genel piyasa kabulleriyle (T4.1 kararı). Etiket,
/// metriğin "iyi/kötü" olduğunu SÖYLEMEZ — yalnız hangi banda düştüğünü adlandırır (tavsiye değil).
/// Metrik yoksa etiket de <c>null</c>.
/// </summary>
public sealed record StockSectorContext(
    string? PeRatio,
    string? PbRatio,
    string? DividendYield,
    string? EarningsGrowth);

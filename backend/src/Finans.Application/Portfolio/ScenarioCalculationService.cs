namespace Finans.Application.Portfolio;

/// <summary>
/// Senaryo v1 saf hesapları (T5.4 — 14 §4-C1). **Tahmin yok**: geçmiş seriden
/// deterministik referans çizgileri türetir (CLAUDE.md §2).
/// </summary>
public static class ScenarioCalculationService
{
    /// <summary>
    /// Alım gücü eşiği (enflasyon düzeltmeli yatırılan): her günkü yatırılan tutar
    /// deltası kendi gününden itibaren günlük bileşik enflasyonla büyür —
    /// <c>eşik(d) = eşik(d−1) × g + Δmaliyet(d)</c>, <c>g = (1+π)^(1/365,25)</c>.
    /// "Bu para nakitte dursaydı alım gücünü koruması için bugün kaç TL olmalıydı"
    /// sorusunun cevabıdır; eğitici referans çizgisi (yatırım tavsiyesi değil).
    ///
    /// <para>Notlar: satış (negatif delta) eşikten nominal düşer (illüstrasyon
    /// sadeliği — kalan birikimin enflasyonu işlemeye devam eder). Günlük çarpan
    /// tek sefer <see cref="Math.Pow"/> ile hesaplanır (yıllık oranın köküne kapalı
    /// form decimal yok); sonrası tam hassasiyet decimal çarpımdır ve deterministiktir.
    /// Parasal doğruluk kaynağı DEĞİLDİR — gösterge çizgisidir.</para>
    /// </summary>
    public static IReadOnlyList<decimal> InflationAdjustedCostSeries(
        IReadOnlyList<DailyValuePoint> points, decimal annualInflationRate)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count == 0)
            return [];

        if (annualInflationRate == 0m)
            return points.Select(p => p.Cost).ToList();

        var dailyFactor = (decimal)Math.Pow((double)(1m + annualInflationRate), 1.0 / 365.25);

        var result = new List<decimal>(points.Count);
        decimal threshold = points[0].Cost;
        result.Add(threshold);

        for (int i = 1; i < points.Count; i++)
        {
            var delta = points[i].Cost - points[i - 1].Cost; // o gün yatırılan/çekilen
            threshold = threshold * dailyFactor + delta;
            result.Add(threshold);
        }

        return result;
    }
}

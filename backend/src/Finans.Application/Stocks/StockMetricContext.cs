namespace Finans.Application.Stocks;

/// <summary>
/// Kaba bağlam eşikleri (T4.1 kararı: ücretsiz katmanda sektör ortalaması yok → MVP'de
/// genel piyasa kabulleri KODDA; ileride gerçek sektör verisiyle zenginleşir). Saf ve
/// deterministik — birim testli (NFR-1). Etiketler 04 §7 sözleşme örnekleriyle uyumlu.
/// <para><b>Tavsiye değil:</b> etiket yalnız bandı adlandırır ("above" = genel kabullere
/// göre yüksek band); al/sat/iyi/kötü anlamı taşımaz (CLAUDE.md §2).</para>
/// </summary>
public static class StockMetricContext
{
    /// <summary>F/K: &lt;10 "low" · 10-25 "moderate" · &gt;25 "above" (yüksek band).</summary>
    public static string? ForPe(decimal? pe) => pe switch
    {
        null => null,
        < 0m => "negative",       // zarar eden şirkette F/K anlamsızlaşır — ayrı bant
        < 10m => "low",
        <= 25m => "moderate",
        _ => "above",
    };

    /// <summary>PD/DD: &lt;1 "low" (defter değerinin altı) · 1-5 "moderate" · &gt;5 "high".</summary>
    public static string? ForPb(decimal? pb) => pb switch
    {
        null => null,
        < 1m => "low",
        <= 5m => "moderate",
        _ => "high",
    };

    /// <summary>Temettü verimi: 0/yok "none" · &lt;%1 "low" · %1-3 "moderate" · &gt;%3 "high".</summary>
    public static string? ForDividendYield(decimal? y) => y switch
    {
        null => null,
        <= 0m => "none",
        < 0.01m => "low",
        <= 0.03m => "moderate",
        _ => "high",
    };

    /// <summary>Kâr büyümesi: &lt;0 "negative" · 0-%5 "flat" · &gt;%5 "positive".</summary>
    public static string? ForEarningsGrowth(decimal? g) => g switch
    {
        null => null,
        < 0m => "negative",
        <= 0.05m => "flat",
        _ => "positive",
    };

    /// <summary>Dört metriğin bağlam etiketlerini tek seferde üretir.</summary>
    public static StockSectorContext Contextualize(StockMetricValues m) => new(
        ForPe(m.PeRatio),
        ForPb(m.PbRatio),
        ForDividendYield(m.DividendYield),
        ForEarningsGrowth(m.EarningsGrowth));
}

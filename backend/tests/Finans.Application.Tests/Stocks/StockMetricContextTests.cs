using Finans.Application.Stocks;

namespace Finans.Application.Tests.Stocks;

/// <summary>
/// T4.2 — Kaba bağlam eşikleri (SC-28). Bant etiketleri deterministik ve sınır değerleri
/// netleştirilmiş olmalı (NFR-1'e komşu: yanlış etiket = yanıltıcı bağlam). Etiketler
/// 04 §7 sözleşme örneğiyle uyumlu doğrulanır.
/// </summary>
public class StockMetricContextTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData(-3.2, "negative")]   // zarar eden şirket
    [InlineData(5.0, "low")]
    [InlineData(10.0, "moderate")]   // sınır: 10 → moderate
    [InlineData(25.0, "moderate")]   // sınır: 25 → moderate
    [InlineData(28.4, "above")]      // 04 §7 örneği
    public void Pe_bands(double? pe, string? expected) =>
        Assert.Equal(expected, StockMetricContext.ForPe((decimal?)pe));

    [Theory]
    [InlineData(null, null)]
    [InlineData(0.8, "low")]         // defter değerinin altı
    [InlineData(1.0, "moderate")]
    [InlineData(5.0, "moderate")]
    [InlineData(44.1, "high")]       // 04 §7 örneği
    public void Pb_bands(double? pb, string? expected) =>
        Assert.Equal(expected, StockMetricContext.ForPb((decimal?)pb));

    [Theory]
    [InlineData(null, null)]
    [InlineData(0.0, "none")]
    [InlineData(0.0052, "low")]      // 04 §7 örneği (%0,52)
    [InlineData(0.01, "moderate")]
    [InlineData(0.03, "moderate")]
    [InlineData(0.045, "high")]
    public void DividendYield_bands(double? y, string? expected) =>
        Assert.Equal(expected, StockMetricContext.ForDividendYield((decimal?)y));

    [Theory]
    [InlineData(null, null)]
    [InlineData(-0.02, "negative")]
    [InlineData(0.0, "flat")]
    [InlineData(0.05, "flat")]
    [InlineData(0.091, "positive")]  // 04 §7 örneği (%9,1)
    public void EarningsGrowth_bands(double? g, string? expected) =>
        Assert.Equal(expected, StockMetricContext.ForEarningsGrowth((decimal?)g));

    [Fact]
    public void Contextualize_maps_all_four_metrics_like_the_contract_example()
    {
        var ctx = StockMetricContext.Contextualize(
            new StockMetricValues(PeRatio: 28.4m, PbRatio: 44.1m, DividendYield: 0.0052m, EarningsGrowth: 0.091m));

        Assert.Equal("above", ctx.PeRatio);
        Assert.Equal("high", ctx.PbRatio);
        Assert.Equal("low", ctx.DividendYield);
        Assert.Equal("positive", ctx.EarningsGrowth);
    }
}

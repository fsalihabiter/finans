using Finans.Application.Portfolio;

namespace Finans.Application.Tests.Portfolio;

/// <summary>
/// ScenarioCalculationService.InflationAdjustedCostSeries birim testleri (SC-36, NFR-1):
/// alım gücü eşiği — günlük bileşik enflasyon + yatırılan deltaları.
/// </summary>
public class ScenarioCalculationServiceTests
{
    private static readonly DateOnly D0 = new(2026, 1, 1);

    private static List<DailyValuePoint> Series(params decimal[] costs) =>
        costs.Select((c, i) => new DailyValuePoint(D0.AddDays(i), Value: c, Cost: c)).ToList();

    [Fact]
    public void Zero_inflation_returns_cost_series_unchanged()
    {
        var series = Series(1000m, 1000m, 1500m);

        var result = ScenarioCalculationService.InflationAdjustedCostSeries(series, 0m);

        Assert.Equal([1000m, 1000m, 1500m], result);
    }

    [Fact]
    public void Empty_series_yields_empty_result()
    {
        Assert.Empty(ScenarioCalculationService.InflationAdjustedCostSeries([], 0.38m));
    }

    [Fact]
    public void Single_investment_compounds_daily()
    {
        // 3 gün, %38 yıllık: eşik(0)=1000 · eşik(1)=1000×g · eşik(2)=1000×g²  (g=(1,38)^(1/365,25))
        var series = Series(1000m, 1000m, 1000m);
        var g = (decimal)Math.Pow(1.38, 1.0 / 365.25);

        var result = ScenarioCalculationService.InflationAdjustedCostSeries(series, 0.38m);

        Assert.Equal(1000m, result[0]);
        Assert.Equal(1000m * g, result[1]);
        Assert.Equal(1000m * g * g, result[2]);
        Assert.True(result[2] > result[1] && result[1] > result[0]); // eşik hep büyür (pozitif enflasyon)
    }

    [Fact]
    public void New_contribution_adds_nominal_on_its_day()
    {
        // g1'de +500 yatırıldı: eşik(1) = 1000×g + 500 (yeni para o gün enflasyonsuz eklenir).
        var series = Series(1000m, 1500m);
        var g = (decimal)Math.Pow(1.38, 1.0 / 365.25);

        var result = ScenarioCalculationService.InflationAdjustedCostSeries(series, 0.38m);

        Assert.Equal(1000m * g + 500m, result[1]);
    }

    [Fact]
    public void Withdrawal_reduces_threshold_nominally()
    {
        // g1'de −400 (satış): eşik(1) = 1000×g − 400.
        var series = Series(1000m, 600m);
        var g = (decimal)Math.Pow(1.38, 1.0 / 365.25);

        var result = ScenarioCalculationService.InflationAdjustedCostSeries(series, 0.38m);

        Assert.Equal(1000m * g - 400m, result[1]);
    }

    [Fact]
    public void One_year_compounding_approximates_annual_rate()
    {
        // 366 nokta (365 gün ileri): eşik ≈ 1000 × 1,38^(365/365,25) → yıllık orana çok yakın.
        var costs = Enumerable.Repeat(1000m, 366).ToArray();
        var result = ScenarioCalculationService.InflationAdjustedCostSeries(Series(costs), 0.38m);

        var expected = 1000m * (decimal)Math.Pow(1.38, 365.0 / 365.25);
        Assert.Equal(Math.Round(expected, 6), Math.Round(result[^1], 6));
    }
}

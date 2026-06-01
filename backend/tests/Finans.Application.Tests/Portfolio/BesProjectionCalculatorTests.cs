using Finans.Application.Portfolio;

namespace Finans.Application.Tests.Portfolio;

/// <summary>
/// BesProjectionCalculator (T-BES.5): kullanıcının verdiği varsayımlardan deterministik
/// birikim illüstrasyonu. Yatırım tavsiyesi DEĞİL — yalnız aritmetik doğruluk testlenir
/// (CLAUDE.md §2 — gelecek tahmini yapılmaz; varsayımların sonucu hesaplanır).
/// </summary>
public sealed class BesProjectionCalculatorTests
{
    private static readonly DateTime Start2026 = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Zero_return_means_own_value_equals_own_contribution()
    {
        // %0 getiri → fon büyümez; fon değeri = own_total + state_total; her birinin değeri tabanına eşit.
        var r = BesProjectionCalculator.Project(new BesProjectionInput(
            OwnMonthly: 1000m, Years: 1, AnnualReturnRatio: 0m, StartDate: Start2026));

        // 12 ay × 1000 = 12.000 own; 2026 oran %20 → 200 state/ay × 12 = 2.400 state.
        Assert.Equal(12000m, r.TotalOwnContribution);
        Assert.Equal(2400m, r.TotalStateContribution);
        Assert.Equal(14400m, r.FundValue);
        Assert.Equal(12000m, r.OwnValue);
        Assert.Equal(2400m, r.StateValue);
        Assert.Equal(0m, r.OwnProfit);
        Assert.Equal(0m, r.StateProfit);
    }

    [Fact]
    public void Positive_return_distributes_growth_proportionally_to_own_and_state()
    {
        // own + state aynı r_y ile büyür → kâr/zarar oranı (own_profit/own = state_profit/state).
        var r = BesProjectionCalculator.Project(new BesProjectionInput(
            OwnMonthly: 1000m, Years: 5, AnnualReturnRatio: 0.20m, StartDate: Start2026));

        Assert.True(r.OwnProfit > 0m);
        Assert.True(r.StateProfit > 0m);

        // own_value + state_value ≈ fund_value (yuvarlamadan ±0,02 tolerans 12 ondalık yuvarlama × 2).
        var sum = r.OwnValue + r.StateValue;
        Assert.InRange(sum, r.FundValue - 0.02m, r.FundValue + 0.02m);

        // own ve state aynı oran payını taşır: own_profit/own ≈ state_profit/state.
        var ownRate = r.OwnProfit / r.TotalOwnContribution;
        var stateRate = r.StateProfit / r.TotalStateContribution;
        Assert.InRange(ownRate - stateRate, -0.0005m, 0.0005m);
    }

    [Fact]
    public void Yearly_series_has_one_entry_per_year()
    {
        var r = BesProjectionCalculator.Project(new BesProjectionInput(
            OwnMonthly: 500m, Years: 10, AnnualReturnRatio: 0.15m, StartDate: Start2026));

        Assert.Equal(10, r.Yearly.Count);
        Assert.Equal(1, r.Yearly[0].Year);
        Assert.Equal(10, r.Yearly[^1].Year);
        // Son yıl serisi final sonucuyla tutarlı (yuvarlama farkı ±0,01).
        Assert.InRange(r.Yearly[^1].FundValue, r.FundValue - 0.01m, r.FundValue + 0.01m);
    }

    [Fact]
    public void State_rate_follows_payment_date_year()
    {
        // 2025'te başlayıp 13 ay süre: ilk 12 ay 2025 oran %30, son ay 2026 oran %20.
        // Yıl 1 sonu (2025-06 → 2026-05): 11 ay %30 + 1 ay %20.
        var start = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var r = BesProjectionCalculator.Project(new BesProjectionInput(
            OwnMonthly: 1000m, Years: 2, AnnualReturnRatio: 0m, StartDate: start));

        // 2025-06..2025-12 = 7 ay × 300 = 2.100; 2026-01..2027-05 = 17 ay × 200 = 3.400; toplam 5.500.
        Assert.Equal(5500m, r.TotalStateContribution);
        Assert.Equal(24000m, r.TotalOwnContribution); // 24 ay × 1000
    }

    [Fact]
    public void Zero_monthly_yields_zero_everywhere()
    {
        // Hiç katkı yapmazsa hiçbir şey büyümez — taban 0, payları 0.
        var r = BesProjectionCalculator.Project(new BesProjectionInput(
            OwnMonthly: 0m, Years: 5, AnnualReturnRatio: 0.30m, StartDate: Start2026));

        Assert.Equal(0m, r.TotalOwnContribution);
        Assert.Equal(0m, r.TotalStateContribution);
        Assert.Equal(0m, r.FundValue);
        Assert.Equal(0m, r.OwnValue);
        Assert.Equal(0m, r.StateValue);
        Assert.Equal(0m, r.OwnProfit);
        Assert.Equal(0m, r.StateProfit);
    }

    [Theory]
    [InlineData(-100, 5, 0.20)]    // negatif aylık
    [InlineData(1000, 0, 0.20)]    // 0 yıl
    [InlineData(1000, 51, 0.20)]   // 50 yıl üstü
    [InlineData(1000, 5, 3.0)]     // %300 getiri (üst sınır aşımı)
    [InlineData(1000, 5, -0.999)]  // -%99,9 makul olmayan kayıp
    public void Invalid_inputs_throw(decimal own, int years, decimal r)
    {
        Assert.Throws<ArgumentException>(() => BesProjectionCalculator.Project(
            new BesProjectionInput(own, years, r, Start2026)));
    }
}

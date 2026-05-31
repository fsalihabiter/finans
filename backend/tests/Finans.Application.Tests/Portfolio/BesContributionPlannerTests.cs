using Finans.Application.Portfolio;

namespace Finans.Application.Tests.Portfolio;

/// <summary>`BesContributionPlanner` (T-BES.6): tarih aralığında aylık ödeme tarihleri.</summary>
public sealed class BesContributionPlannerTests
{
    private static DateTime D(int y, int m, int day) => new(y, m, day, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Emits_one_date_per_month_in_range()
    {
        var dates = BesContributionPlanner.MonthlyDates(15, D(2025, 1, 1), D(2025, 3, 31));
        Assert.Equal(new[] { D(2025, 1, 15), D(2025, 2, 15), D(2025, 3, 15) }, dates);
    }

    [Fact]
    public void Excludes_endpoints_outside_range()
    {
        // Ocak 15 < from(20) → Ocak yok; Mart 10 < Mart 15 → Mart yok; sadece Şubat.
        var dates = BesContributionPlanner.MonthlyDates(15, D(2025, 1, 20), D(2025, 3, 10));
        Assert.Equal(new[] { D(2025, 2, 15) }, dates);
    }

    [Fact]
    public void Clamps_day_to_28()
    {
        var dates = BesContributionPlanner.MonthlyDates(31, D(2025, 1, 1), D(2025, 2, 28));
        Assert.Equal(new[] { D(2025, 1, 28), D(2025, 2, 28) }, dates);
    }

    [Fact]
    public void Reversed_range_is_empty()
    {
        Assert.Empty(BesContributionPlanner.MonthlyDates(15, D(2025, 3, 1), D(2025, 1, 1)));
    }
}

using Finans.Application.Portfolio;
using Finans.Domain.Enums;

namespace Finans.Application.Tests.Portfolio;

/// <summary>
/// `BesCalculator` (T-BES): devlet katkısı = %20 (2026); hak ediş durumu sistemde kalış
/// yılından kaba türetilir (&lt;3 NotVested · 3–10 PartiallyVested · ≥10 Vested).
/// </summary>
public sealed class BesCalculatorTests
{
    private static readonly DateTime AsOf = new(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(1000, 200)]   // %20
    [InlineData(2500, 500)]
    [InlineData(0, 0)]
    [InlineData(-50, 0)]      // negatif → 0
    public void StateContributionFor_applies_rate(decimal own, decimal expected)
    {
        Assert.Equal(expected, BesCalculator.StateContributionFor(own));
    }

    [Fact]
    public void YearsInSystem_counts_full_years()
    {
        Assert.Equal(6, BesCalculator.YearsInSystem(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), AsOf));
        // Yıl dönümünden bir gün önce → henüz tam yıl dolmadı.
        Assert.Equal(5, BesCalculator.YearsInSystem(new DateTime(2020, 6, 1, 0, 0, 0, DateTimeKind.Utc), AsOf));
        Assert.Equal(0, BesCalculator.YearsInSystem(null, AsOf));
        Assert.Equal(0, BesCalculator.YearsInSystem(new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc), AsOf)); // gelecek
    }

    [Theory]
    [InlineData(2024, VestingState.NotVested)]        // ~2 yıl
    [InlineData(2022, VestingState.PartiallyVested)]  // ~4 yıl
    [InlineData(2014, VestingState.Vested)]           // ~12 yıl
    public void VestingStateFor_derives_from_years(int joinYear, VestingState expected)
    {
        var joined = new DateTime(joinYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal(expected, BesCalculator.VestingStateFor(joined, AsOf));
    }

    [Fact]
    public void VestingStateFor_null_is_not_vested()
    {
        Assert.Equal(VestingState.NotVested, BesCalculator.VestingStateFor(null, AsOf));
    }
}

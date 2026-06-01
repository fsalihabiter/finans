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
    [InlineData(1000, 2026, 200)]   // 2026 → %20
    [InlineData(2500, 2026, 500)]
    [InlineData(1000, 2025, 300)]   // 2026 ÖNCESİ → %30 (oran geriye dönük değil)
    [InlineData(0, 2026, 0)]
    [InlineData(-50, 2026, 0)]      // negatif → 0
    public void StateContributionFor_uses_rate_for_payment_date(decimal own, int year, decimal expected)
    {
        var paidAt = new DateTime(year, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal(expected, BesCalculator.StateContributionFor(own, paidAt));
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

    // ── Devlet katkısı yatma tarihi (ödeme ayını izleyen ayın sonu) ──
    [Theory]
    [InlineData(2026, 5, 1, 2026, 6, 30)]   // Mayıs ödeme → 30 Haziran
    [InlineData(2026, 1, 15, 2026, 2, 28)]  // Ocak → 28 Şubat
    [InlineData(2026, 12, 5, 2027, 1, 31)]  // Aralık → 31 Ocak (yıl döner)
    public void StateDepositDateFor_is_end_of_following_month(int y, int m, int d, int ey, int em, int ed)
    {
        var paid = new DateTime(y, m, d, 0, 0, 0, DateTimeKind.Utc);
        var expected = new DateTime(ey, em, ed, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal(expected, BesCalculator.StateDepositDateFor(paid));
    }

    // ── Katkı durumu (tarihten): gelecek / devlet bekliyor / yatırıldı ──
    [Fact]
    public void ContributionStatusFor_future_when_paid_date_ahead()
    {
        var paid = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc); // AsOf 31.05.2026 → gelecek
        Assert.Equal(BesContributionStatus.Future, BesCalculator.ContributionStatusFor(paid, AsOf));
    }

    [Fact]
    public void ContributionStatusFor_state_pending_when_own_paid_but_state_not_yet()
    {
        // 01.05.2026 ödendi (geçti) ama devlet yatma tarihi 30.06.2026 henüz gelmedi → StatePending.
        var paid = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal(BesContributionStatus.StatePending, BesCalculator.ContributionStatusFor(paid, AsOf));
    }

    [Fact]
    public void ContributionStatusFor_deposited_when_state_date_passed()
    {
        // 01.03.2026 ödeme → devlet 30.04.2026 yattı (AsOf 31.05 sonrası) → Deposited.
        var paid = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal(BesContributionStatus.Deposited, BesCalculator.ContributionStatusFor(paid, AsOf));
    }

    // ── Kademeli hak ediş oranı ──
    [Theory]
    [InlineData(2, null, 0.00)]    // <3 yıl
    [InlineData(4, null, 0.15)]    // 3–6
    [InlineData(7, null, 0.35)]    // 6–10
    [InlineData(12, null, 0.60)]   // 10+ yaşsız → %60
    [InlineData(12, 60, 1.00)]     // 10+ ve 56+ yaş → %100
    [InlineData(12, 50, 0.60)]     // 10+ ama yaş <56 → %60
    public void VestedRateFor_is_staged(int years, int? age, decimal expected)
    {
        Assert.Equal(expected, BesRules.VestedRateFor(years, age));
    }

    [Fact]
    public void AgeFor_uses_birth_year_or_null()
    {
        Assert.Equal(41, BesCalculator.AgeFor(1985, AsOf));
        Assert.Null(BesCalculator.AgeFor(null, AsOf));
    }

    // ── Fon getirisi (T-BES.10): own ve state aynı oranla işler ─────────────

    [Fact]
    public void FundReturnFor_distributes_growth_to_own_and_state_with_same_rate()
    {
        // own=120.000, state=28.554, fund=279.378 → taban 148.554; r ≈ 0,8806.
        var r = BesCalculator.FundReturnFor(120000m, 28554m, 279378m);

        Assert.NotNull(r.Rate);
        Assert.Equal(279378m / 148554m - 1m, r.Rate!.Value);

        // own*r ve state*r — taban × r toplamı = fon kâr/zararı (own_value + state_value ≈ fund).
        Assert.Equal(Math.Round(120000m * r.Rate.Value, 2), r.OwnProfit);
        Assert.Equal(Math.Round(28554m * r.Rate.Value, 2), r.StateProfit);
        Assert.Equal(Math.Round(120000m * (1m + r.Rate.Value), 2), r.OwnValue);
        Assert.Equal(Math.Round(28554m * (1m + r.Rate.Value), 2), r.StateValue);
        // Round farkı her birinde ±0,5 kuruş → toplam ≈ fund ±0,01.
        Assert.InRange(r.OwnValue + r.StateValue, 279378m - 0.01m, 279378m + 0.01m);
    }

    [Fact]
    public void FundReturnFor_negative_rate_for_loss()
    {
        // 100.000 yatırıldı, fon 90.000'e düştü → r = -0,1; her iki katkı için kayıp.
        var r = BesCalculator.FundReturnFor(80000m, 20000m, 90000m);

        Assert.Equal(-0.1m, r.Rate!.Value);
        Assert.Equal(-8000m, r.OwnProfit);
        Assert.Equal(-2000m, r.StateProfit);
        Assert.Equal(72000m, r.OwnValue);
        Assert.Equal(18000m, r.StateValue);
    }

    [Fact]
    public void FundReturnFor_no_fund_value_returns_null_rate_and_principal()
    {
        // Fon değeri girilmediyse: oran yok; değerler tabana eşit; kâr/zarar 0.
        var r = BesCalculator.FundReturnFor(100000m, 25000m, fundValue: null);

        Assert.Null(r.Rate);
        Assert.Equal(100000m, r.OwnValue);
        Assert.Equal(25000m, r.StateValue);
        Assert.Equal(0m, r.OwnProfit);
        Assert.Equal(0m, r.StateProfit);
    }

    [Fact]
    public void FundReturnFor_zero_base_returns_null_rate()
    {
        // Henüz katkı yok ama fon değeri girilmiş → oran tanımsız (taban 0); bölme yok.
        var r = BesCalculator.FundReturnFor(0m, 0m, 500m);

        Assert.Null(r.Rate);
        Assert.Equal(0m, r.OwnValue);
        Assert.Equal(0m, r.StateValue);
        Assert.Equal(0m, r.OwnProfit);
        Assert.Equal(0m, r.StateProfit);
    }
}

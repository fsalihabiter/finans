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

    // ── Yıllık devlet katkısı üst sınırı (T-BES.4) ─────────────────────────────

    [Fact]
    public void ApplyAnnualStateCap_returns_full_amount_when_room_remaining()
    {
        // 2026 tavan 79.272; bu yıl önceden 10.000 yatmış → kalan 69.272. Önerilen 200 → tam geçer.
        var result = BesCalculator.ApplyAnnualStateCap(200m, 2026, alreadyContributedInYear: 10_000m);
        Assert.Equal(200m, result);
    }

    [Fact]
    public void ApplyAnnualStateCap_clamps_to_remaining_when_near_limit()
    {
        // 2026 tavan 79.272; önceden 79.000 yatmış → kalan 272. Önerilen 500 → 272'de kesilir.
        var result = BesCalculator.ApplyAnnualStateCap(500m, 2026, alreadyContributedInYear: 79_000m);
        Assert.Equal(272m, result);
    }

    [Fact]
    public void ApplyAnnualStateCap_returns_zero_when_quota_exhausted()
    {
        // Tavan dolmuş → 0; negatif kalan olmaz.
        var result = BesCalculator.ApplyAnnualStateCap(500m, 2026, alreadyContributedInYear: 79_272m);
        Assert.Equal(0m, result);
    }

    [Fact]
    public void ApplyAnnualStateCap_returns_zero_for_non_positive_proposed()
    {
        Assert.Equal(0m, BesCalculator.ApplyAnnualStateCap(0m, 2026, 0m));
        Assert.Equal(0m, BesCalculator.ApplyAnnualStateCap(-50m, 2026, 0m));
    }

    [Fact]
    public void AnnualStateContributionCapFor_unknown_year_falls_back_to_latest_known()
    {
        // Tablo dışı yıl (2030) → en son bilinen yıl (2026) tavanını döner — illüstrasyon dostu.
        Assert.Equal(BesRules.AnnualStateContributionCapFor(2026),
                     BesRules.AnnualStateContributionCapFor(2030));
        // Bilinen yıllar tablodakini döner.
        Assert.Equal(79_272m, BesRules.AnnualStateContributionCapFor(2026));
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

    // ── GD-002: iki ayrı fon havuzu (kendi katkı fonu ↔ devlet katkısı fonu) ──

    [Fact]
    public void FundReturnFor_two_pools_uses_each_pools_own_rate()
    {
        // Kendi katkı 100.000 → 120.000 (+%20); devlet katkısı 30.000 → 33.000 (+%10).
        // Eski model tek fon değerinden ORANTILI bölerdi ve ikisine de aynı oranı
        // yazardı — havuzlar farklı fonlarda olduğu için bu yanlıştı.
        var r = BesCalculator.FundReturnFor(100_000m, 30_000m, 120_000m, 33_000m);

        Assert.Equal(0.20m, r.OwnRate);
        Assert.Equal(0.10m, r.StateRate);
        Assert.Equal(120_000m, r.OwnValue);
        Assert.Equal(20_000m, r.OwnProfit);
        Assert.Equal(33_000m, r.StateValue);
        Assert.Equal(3_000m, r.StateProfit);

        // Birleşik oran toplamlardan: 153.000 / 130.000 − 1 ≈ %17,69 — iki havuzun
        // ortalaması DEĞİL, ağırlıklı gerçek sonucu.
        Assert.Equal(Math.Round(153_000m / 130_000m - 1m, 6), Math.Round(r.Rate!.Value, 6));
    }

    [Fact]
    public void FundReturnFor_two_pools_missing_value_falls_back_to_contribution()
    {
        // Devlet katkısının fon değeri henüz girilmemiş → o havuz katkı tutarına eşit
        // sayılır (kâr/zarar 0). Eksik veri UYDURULMAZ.
        var r = BesCalculator.FundReturnFor(100_000m, 30_000m, 118_000m, null);

        Assert.Equal(0.18m, r.OwnRate);
        Assert.Null(r.StateRate);
        Assert.Equal(30_000m, r.StateValue);
        Assert.Equal(0m, r.StateProfit);
    }

    [Theory]
    // Hak ediş kademeleri (BesRules): 0 · 0,15 · 0,35 · 0,60 · 1,00
    [InlineData(0.00, 120_000)]       // hiç hak edilmedi → yalnız kendi katkının değeri
    [InlineData(0.15, 124_950)]       // 120.000 + 0,15 × 33.000
    [InlineData(0.60, 139_800)]
    [InlineData(1.00, 153_000)]       // tamamı hak edildi → iki havuz toplamı
    public void VestedPortfolioValueFor_adds_only_the_vested_share_of_state_fund(
        double vestedRate, decimal expected)
    {
        // Kendi katkının fon değeri 120.000; devlet katkısının fon değeri 33.000.
        var value = BesCalculator.VestedPortfolioValueFor(120_000m, 33_000m, (decimal)vestedRate);

        Assert.Equal(expected, value);
    }

    [Fact]
    public void DepositedTotals_excludes_state_contribution_still_in_transit()
    {
        // 2026-09-20 itibarıyla:
        //  · Temmuz ödemesi → devlet yatma = Ağustos sonu (geçti)   → Deposited
        //  · 10 Eylül ödemesi → devlet yatma = Ekim sonu (gelmedi)  → StatePending ("yolda")
        //  · Ekim ödemesi → henüz ödenmedi                          → Future
        var asOf = new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc);
        var contributions = new[]
        {
            (new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc), 1_000m, 200m),
            (new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc), 1_000m, 200m),
            (new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc), 1_000m, 200m),
        };

        var (own, state) = BesCalculator.DepositedTotals(contributions, asOf);

        // Kendi katkı: ödenmiş iki kayıt. Devlet: YALNIZ yatmış olan — yoldaki fonda değil,
        // getirisi olamaz. (Canlı veride bu ayrım gözetilmeyince iki havuzun getirisi
        // yapay olarak %39 ↔ %48 ayrışmıştı.)
        Assert.Equal(2_000m, own);
        Assert.Equal(200m, state);
    }

    [Fact]
    public void SplitTotalFundValue_splits_by_contribution_share_and_preserves_total()
    {
        // 72.000 → 50/60 ve 10/60 → 60.000 + 12.000
        var (own, state) = BesCalculator.SplitTotalFundValue(72_000m, 50_000m, 10_000m);
        Assert.Equal(60_000m, own);
        Assert.Equal(12_000m, state);

        // Yuvarlama kalanı devlet havuzuna gider → toplam kuruşu kuruşuna korunur.
        var (o2, s2) = BesCalculator.SplitTotalFundValue(100m, 1m, 2m);
        Assert.Equal(100m, o2!.Value + s2!.Value);
    }

    [Fact]
    public void SplitTotalFundValue_zero_base_returns_nulls_not_invented_values()
    {
        var (own, state) = BesCalculator.SplitTotalFundValue(5_000m, 0m, 0m);
        Assert.Null(own);
        Assert.Null(state);
    }

    [Fact]
    public void VestedPortfolioValueFor_rejects_rate_outside_zero_one()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BesCalculator.VestedPortfolioValueFor(100m, 10m, 1.5m));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BesCalculator.VestedPortfolioValueFor(100m, 10m, -0.1m));
    }
}

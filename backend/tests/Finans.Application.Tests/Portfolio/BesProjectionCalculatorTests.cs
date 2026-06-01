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

    // ── Süre sonu hak ediş (T-BES.5 ek): sözleşme kademeleri 3/6/10/+56 yaş ──

    [Fact]
    public void VestedRateAtEnd_zero_for_under_three_years_in_system()
    {
        // Yeni sözleşme (joined = start), 2 yıl projeksiyon → süre sonunda 2 yıl, %0.
        var r = BesProjectionCalculator.Project(new BesProjectionInput(
            1000m, 2, 0m, Start2026, JoinedAtUtc: Start2026, BirthYear: null));

        Assert.Equal(0m, r.VestedRateAtEnd);
        Assert.Equal(0m, r.VestedStateAmountAtEnd);
    }

    [Fact]
    public void VestedRateAtEnd_15_percent_at_three_to_six_years()
    {
        // 5 yıl sonra → 3-6 yıl bandında: %15.
        var r = BesProjectionCalculator.Project(new BesProjectionInput(
            1000m, 5, 0m, Start2026, JoinedAtUtc: Start2026, BirthYear: null));

        Assert.Equal(0.15m, r.VestedRateAtEnd);
        // VestedStateAmount = 0,15 × state_value (= state, sıfır getiri).
        Assert.Equal(Math.Round(0.15m * r.StateValue, 2), r.VestedStateAmountAtEnd);
    }

    [Fact]
    public void VestedRateAtEnd_60_percent_at_ten_years_without_age()
    {
        // 10 yıl → 56 yaş kontrolü yapılamıyor (BirthYear null) → %60 (10+ yaşsız).
        var r = BesProjectionCalculator.Project(new BesProjectionInput(
            1000m, 10, 0m, Start2026, JoinedAtUtc: Start2026, BirthYear: null));

        Assert.Equal(0.60m, r.VestedRateAtEnd);
    }

    [Fact]
    public void VestedRateAtEnd_100_percent_at_ten_years_with_age_over_56()
    {
        // 10 yıl + süre sonunda 56+ yaş → tam emeklilik hak edişi (%100).
        // BirthYear=1965, StartDate=2026 → süre sonu 2036, yaş 71 → 56+.
        var r = BesProjectionCalculator.Project(new BesProjectionInput(
            1000m, 10, 0m, Start2026, JoinedAtUtc: Start2026, BirthYear: 1965));

        Assert.Equal(1.00m, r.VestedRateAtEnd);
    }

    [Fact]
    public void VestedRateAtEnd_uses_existing_contract_years_when_joined_in_past()
    {
        // Sözleşme 2020'de başlamış (6 yıl önce), 4 yıl projeksiyon → süre sonu 10 yıl → %60.
        var joined = new DateTime(2020, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var r = BesProjectionCalculator.Project(new BesProjectionInput(
            1000m, 4, 0m, Start2026, JoinedAtUtc: joined, BirthYear: null));

        Assert.Equal(0.60m, r.VestedRateAtEnd);
    }

    // ── Yıllık devlet katkısı üst sınırı (T-BES.4) projeksiyonda da uygulanır ──

    [Fact]
    public void Projection_caps_state_at_annual_limit_for_high_monthly_contributions()
    {
        // 2026 cap = 79.272 ₺. Yıl başından başla, 1 yıl: aylık 50.000 × 12 × %20 = 120.000 raw → 79.272'de kesilir.
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var r = BesProjectionCalculator.Project(new BesProjectionInput(
            OwnMonthly: 50_000m, Years: 1, AnnualReturnRatio: 0m, StartDate: start));

        // Own kapasiteyle alakasız: 12 × 50.000 = 600.000 ₺ tam yatar.
        Assert.Equal(600_000m, r.TotalOwnContribution);
        // State 2026 tavanında durur — tam 79.272 ₺.
        Assert.Equal(79_272m, r.TotalStateContribution);
    }

    [Fact]
    public void Projection_state_resets_at_new_calendar_year()
    {
        // Yıl başından, 2 yıl: yıl 1 (2026) cap dolar; yıl 2 (2027) tavan tekrar açılır (fallback son bilinen).
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var r = BesProjectionCalculator.Project(new BesProjectionInput(
            OwnMonthly: 50_000m, Years: 2, AnnualReturnRatio: 0m, StartDate: start));

        // 2 yıl × 79.272 = 158.544 (her yıl tavan; 2027 fallback olarak 2026 tavanını kullanır).
        Assert.Equal(158_544m, r.TotalStateContribution);

        // Yıllık seri **kümülatif** değerleri tutar: yıl 1 sonu 79272 (yıl 1 capped), yıl 2 sonu 158544.
        // Yıl 2'nin tek başına eklediği = 158544 − 79272 = 79272 → cap tekrar açıldığı kanıtı.
        Assert.Equal(2, r.Yearly.Count);
        Assert.Equal(79_272m, r.Yearly[0].StateContribution);
        Assert.Equal(158_544m, r.Yearly[1].StateContribution);
        Assert.Equal(79_272m, r.Yearly[1].StateContribution - r.Yearly[0].StateContribution);
    }

    [Fact]
    public void Projection_does_not_cap_when_under_limit()
    {
        // Aylık 1.000 × 12 × %20 = 2.400 (2026 cap 79.272'nin çok altı) → kesme yok.
        var r = BesProjectionCalculator.Project(new BesProjectionInput(
            OwnMonthly: 1_000m, Years: 1, AnnualReturnRatio: 0m, StartDate: Start2026));

        Assert.Equal(2_400m, r.TotalStateContribution);
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

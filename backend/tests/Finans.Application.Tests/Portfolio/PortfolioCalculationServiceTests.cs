using Finans.Application.Portfolio;
using Finans.Domain.Enums;

namespace Finans.Application.Tests.Portfolio;

/// <summary>
/// PortfolioCalculationService birim testleri (NFR-1: parasal hesap zorunlu test).
/// Veri seti seed/taslakla BİREBİR tutar (03 §12, 06 §4): toplam maliyet 422.970,
/// değer 641.403, kâr +218.433 (+%51,6); altın 40gr @4.546,275 → 181.851, +%43.
/// </summary>
public class PortfolioCalculationServiceTests
{
    private readonly PortfolioCalculationService _sut = new();

    // Seed pozisyonları (Infrastructure/Seed/SeedData.cs ile aynı sayılar).
    private static readonly IReadOnlyList<HoldingInput> SeedHoldings =
    [
        new(AssetType.Gold, "Altın (gram)", Quantity: 40m, AvgCost: 4546.275m, CurrentPrice: 6500m),
        new(AssetType.Fx, "ABD Doları", Quantity: 2000m, AvgCost: 43.270m, CurrentPrice: 48m),
        new(AssetType.Bes, "Bireysel Emeklilik", Quantity: 1m, AvgCost: 148554m, CurrentPrice: 279378m),
        new(AssetType.Cash, "Nakit (TL)", Quantity: 6025m, AvgCost: 1m, CurrentPrice: 1m),
    ];

    // ── Tek kalem formülleri (CLAUDE.md §6) ──────────────────────────────────

    [Fact]
    public void TotalCost_multiplies_quantity_by_avg_cost()
    {
        // Altın: 40 gr × 4.546,275 → 181.851,00 (tam hassasiyet, yuvarlama yok)
        Assert.Equal(181851.000m, PortfolioCalculationService.TotalCost(40m, 4546.275m));
    }

    [Fact]
    public void CurrentValue_multiplies_quantity_by_price()
    {
        Assert.Equal(260000m, PortfolioCalculationService.CurrentValue(40m, 6500m));
    }

    [Fact]
    public void CurrentValue_is_null_when_price_missing()
    {
        Assert.Null(PortfolioCalculationService.CurrentValue(40m, null));
    }

    [Fact]
    public void Profit_is_value_minus_cost()
    {
        // Altın: 260.000 − 181.851 = 78.149
        Assert.Equal(78149.000m, PortfolioCalculationService.Profit(260000m, 181851m));
    }

    [Fact]
    public void Profit_is_null_when_value_missing()
    {
        Assert.Null(PortfolioCalculationService.Profit(null, 181851m));
    }

    [Fact]
    public void ReturnRatio_gold_is_about_43_percent()
    {
        // 78.149 / 181.851 = 0,429745... → gösterimde ~%43
        decimal? ratio = PortfolioCalculationService.ReturnRatio(260000m, 181851m);
        Assert.NotNull(ratio);
        Assert.Equal(0.43m, Math.Round(ratio!.Value, 2));
        Assert.Equal(0.4297m, Math.Round(ratio.Value, 4));
    }

    [Fact]
    public void ReturnRatio_is_null_when_cost_is_zero()
    {
        Assert.Null(PortfolioCalculationService.ReturnRatio(100m, 0m));
    }

    [Fact]
    public void ReturnRatio_is_null_when_value_missing()
    {
        Assert.Null(PortfolioCalculationService.ReturnRatio(null, 181851m));
    }

    // ── Ağırlıklı ortalama maliyet (T1.5'in saf çekirdeği) ───────────────────

    [Fact]
    public void WeightedAverageCost_blends_lots_by_quantity()
    {
        // 40 @ 4.000 + 60 @ 5.000 = 460.000 / 100 = 4.600
        decimal? avg = PortfolioCalculationService.WeightedAverageCost(
            [(40m, 4000m), (60m, 5000m)]);
        Assert.Equal(4600m, avg);
    }

    [Fact]
    public void WeightedAverageCost_is_null_for_empty_lots()
    {
        Assert.Null(PortfolioCalculationService.WeightedAverageCost([]));
    }

    // ── Reel getiri (T1.4'ün saf çekirdeği) ──────────────────────────────────

    [Fact]
    public void RealReturn_applies_fisher_formula()
    {
        // (1 + 0,20) / (1 + 0,10) − 1 = 0,0909...
        decimal? real = PortfolioCalculationService.RealReturn(0.20m, 0.10m);
        Assert.NotNull(real);
        Assert.Equal(0.0909m, Math.Round(real!.Value, 4));
    }

    [Fact]
    public void RealReturn_can_be_negative_when_inflation_exceeds_nominal()
    {
        // Nominal %51,6, enflasyon %38 → reel pozitif ama erimiş
        decimal? real = PortfolioCalculationService.RealReturn(0.516m, 0.38m);
        Assert.NotNull(real);
        Assert.True(real < 0.516m);
        Assert.True(real > 0m);
    }

    [Fact]
    public void RealReturn_is_null_without_inflation()
    {
        Assert.Null(PortfolioCalculationService.RealReturn(0.516m, null));
    }

    [Fact]
    public void RealReturn_is_null_without_nominal()
    {
        Assert.Null(PortfolioCalculationService.RealReturn(null, 0.38m));
    }

    // ── Portföy özeti — seed seti BİREBİR (06 §4) ────────────────────────────

    [Fact]
    public void CalculateSummary_matches_seed_totals()
    {
        var summary = _sut.CalculateSummary(SeedHoldings);

        Assert.Equal(422970m, summary.TotalCost);
        Assert.Equal(641403m, summary.TotalValue);
        Assert.Equal(218433m, summary.NetProfit);
        Assert.NotNull(summary.ReturnRatio);
        Assert.Equal(0.516m, Math.Round(summary.ReturnRatio!.Value, 3));
    }

    [Fact]
    public void CalculateSummary_allocation_weights_sum_to_one_and_match_contract()
    {
        var summary = _sut.CalculateSummary(SeedHoldings);

        // 04 §4 sözleşmesindeki ağırlıklar
        decimal Weight(AssetType t) => summary.Allocation.Single(a => a.AssetType == t).Weight;
        Assert.Equal(0.405m, Math.Round(Weight(AssetType.Gold), 3));
        Assert.Equal(0.436m, Math.Round(Weight(AssetType.Bes), 3));
        Assert.Equal(0.150m, Math.Round(Weight(AssetType.Fx), 3));
        Assert.Equal(0.009m, Math.Round(Weight(AssetType.Cash), 3));

        // Ağırlıklar toplamı 1 (fiyatlı kalemler portföyün tamamını oluşturur).
        // Dört ayrı decimal bölmenin toplamı → çok küçük yuvarlama payı tolere edilir.
        Assert.Equal(1m, Math.Round(summary.Allocation.Sum(a => a.Weight), 10));
    }

    [Fact]
    public void CalculateSummary_computes_real_return_when_inflation_given()
    {
        var summary = _sut.CalculateSummary(SeedHoldings, inflationRate: 0.38m);

        Assert.NotNull(summary.RealReturnRatio);
        // (1 + 0,51643) / 1,38 − 1 ≈ 0,0989
        Assert.Equal(0.0989m, Math.Round(summary.RealReturnRatio!.Value, 4));
    }

    [Fact]
    public void CalculateSummary_real_return_null_without_inflation()
    {
        var summary = _sut.CalculateSummary(SeedHoldings);
        Assert.Null(summary.RealReturnRatio);
    }

    [Fact]
    public void CalculateSummary_handles_empty_portfolio()
    {
        var summary = _sut.CalculateSummary([]);

        Assert.Equal(0m, summary.TotalCost);
        Assert.Equal(0m, summary.TotalValue);
        Assert.Equal(0m, summary.NetProfit);
        Assert.Null(summary.ReturnRatio);
        Assert.Empty(summary.Allocation);
    }

    [Fact]
    public void CalculateHoldings_marks_priceless_holding_value_as_null()
    {
        var holdings = new HoldingInput[]
        {
            new(AssetType.Gold, "Altın", 10m, 5000m, CurrentPrice: 6000m),
            new(AssetType.Stock, "Fiyatsız", 5m, 100m, CurrentPrice: null),
        };

        var results = _sut.CalculateHoldings(holdings);

        var priceless = results.Single(r => r.Name == "Fiyatsız");
        Assert.Null(priceless.CurrentValue);        // satır "fiyatsız" işaretlenebilsin
        Assert.Null(priceless.Profit);
        Assert.Null(priceless.ReturnRatio);
        Assert.Equal(500m, priceless.TotalCost);    // maliyet yine hesaplanır
        // SC-40: ağırlığa MALİYETİYLE katılır (etkin değer) — toplam 60.000 + 500.
        Assert.Equal(500m / 60500m, priceless.Weight);
        Assert.Equal(1m, Math.Round(results.Sum(r => r.Weight), 10));
    }

    // ── SC-40: fiyatsız kalem özete maliyetiyle girer (sahte −%100 yok) ──────

    [Fact]
    public void CalculateSummary_priceless_holding_counts_at_cost_not_minus_100_percent()
    {
        // Tek fiyatsız kalem: değer = maliyet → kâr 0, getiri %0 (eskiden −%100 görünüyordu).
        var summary = _sut.CalculateSummary(
            [new HoldingInput(AssetType.Stock, "Fiyatsız Hisse", 5m, 100m, CurrentPrice: null)]);

        Assert.Equal(500m, summary.TotalCost);
        Assert.Equal(500m, summary.TotalValue);
        Assert.Equal(0m, summary.NetProfit);
        Assert.Equal(0m, summary.ReturnRatio);

        var slice = Assert.Single(summary.Allocation);
        Assert.Equal(500m, slice.Value);
        Assert.Equal(1m, slice.Weight);
    }

    [Fact]
    public void CalculateSummary_mixed_portfolio_carries_priceless_at_cost_in_totals_and_allocation()
    {
        var summary = _sut.CalculateSummary(
        [
            new HoldingInput(AssetType.Gold, "Altın", 10m, 5000m, CurrentPrice: 6000m),  // değer 60.000
            new HoldingInput(AssetType.Stock, "Fiyatsız", 5m, 100m, CurrentPrice: null), // maliyet 500
        ]);

        Assert.Equal(60500m, summary.TotalValue);  // 60.000 + 500 (maliyetiyle, 0 değil)
        Assert.Equal(50500m, summary.TotalCost);
        Assert.Equal(10000m, summary.NetProfit);   // yalnız fiyatlı kalemin kârı

        Assert.Equal(2, summary.Allocation.Count); // fiyatsız kalem dağılımdan düşmez
        Assert.Equal(500m, summary.Allocation.Single(a => a.Name == "Fiyatsız").Value);
        Assert.Equal(1m, Math.Round(summary.Allocation.Sum(a => a.Weight), 10));
    }
}

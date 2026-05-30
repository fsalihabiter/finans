using Finans.Application.Portfolio;
using Finans.Domain.Enums;

namespace Finans.Application.Tests.Portfolio;

/// <summary>
/// PortfolioCalculationService.DerivePosition birim testleri (SC-06, NFR-1):
/// işlemlerden ağırlıklı ort. maliyet + miktar türetimi (03 §11 kuralı).
/// </summary>
public class PositionDerivationTests
{
    private static TransactionInput Buy(decimal qty, decimal price, decimal fee = 0m) =>
        new(TransactionType.Buy, qty, price, fee);

    private static TransactionInput Sell(decimal qty, decimal price = 0m) =>
        new(TransactionType.Sell, qty, price);

    [Fact]
    public void Single_buy_yields_unit_price_as_avg_cost()
    {
        // Seed altın: 40 gr @ 4.546,275 → Qty 40, AvgCost 4.546,275
        var pos = PortfolioCalculationService.DerivePosition([Buy(40m, 4546.275m)]);

        Assert.Equal(40m, pos.Quantity);
        Assert.Equal(4546.275m, pos.AvgCost);
    }

    [Fact]
    public void Multiple_buys_are_weighted_by_quantity()
    {
        // 40 @ 4.000 + 60 @ 5.000 = 460.000 / 100 = 4.600
        var pos = PortfolioCalculationService.DerivePosition([Buy(40m, 4000m), Buy(60m, 5000m)]);

        Assert.Equal(100m, pos.Quantity);
        Assert.Equal(4600m, pos.AvgCost);
    }

    [Fact]
    public void Fee_is_included_in_cost_basis()
    {
        // 10 @ 100 + komisyon 50 = 1.050 / 10 = 105
        var pos = PortfolioCalculationService.DerivePosition([Buy(10m, 100m, fee: 50m)]);

        Assert.Equal(10m, pos.Quantity);
        Assert.Equal(105m, pos.AvgCost);
    }

    [Fact]
    public void Sell_reduces_quantity_but_not_avg_cost()
    {
        // Buy 40 @ 4.546,275 sonra Sell 10 → Qty 30, AvgCost değişmez
        var pos = PortfolioCalculationService.DerivePosition(
        [
            Buy(40m, 4546.275m),
            Sell(10m),
        ]);

        Assert.Equal(30m, pos.Quantity);
        Assert.Equal(4546.275m, pos.AvgCost); // ortalama maliyet yöntemi — satış bozmaz
    }

    [Fact]
    public void Buy_sell_buy_keeps_weighted_avg_of_buys_only()
    {
        // Alışlar: 40@4.000 + 60@5.000 → ort 4.600; araya Sell 20 → Qty 80, AvgCost 4.600
        var pos = PortfolioCalculationService.DerivePosition(
        [
            Buy(40m, 4000m),
            Sell(20m),
            Buy(60m, 5000m),
        ]);

        Assert.Equal(80m, pos.Quantity);   // 40 − 20 + 60
        Assert.Equal(4600m, pos.AvgCost);  // satış payı/paydaya girmez
    }

    [Fact]
    public void No_transactions_yields_zero_position()
    {
        var pos = PortfolioCalculationService.DerivePosition([]);

        Assert.Equal(0m, pos.Quantity);
        Assert.Equal(0m, pos.AvgCost);
    }

    [Fact]
    public void Only_sells_yields_zero_avg_cost_and_negative_quantity()
    {
        // Alış yokken AvgCost = 0 (bölme-sıfır yok); miktar negatif olabilir (veri tutarsızlığı sinyali)
        var pos = PortfolioCalculationService.DerivePosition([Sell(5m)]);

        Assert.Equal(-5m, pos.Quantity);
        Assert.Equal(0m, pos.AvgCost);
    }

    [Fact]
    public void Derived_total_cost_matches_seed_gold_181851()
    {
        // Türetilen pozisyon, saklanan Holding ile tutarlı olmalı: 40 × 4.546,275 = 181.851
        var pos = PortfolioCalculationService.DerivePosition([Buy(40m, 4546.275m)]);

        Assert.Equal(181851.000m, PortfolioCalculationService.TotalCost(pos.Quantity, pos.AvgCost));
    }
}

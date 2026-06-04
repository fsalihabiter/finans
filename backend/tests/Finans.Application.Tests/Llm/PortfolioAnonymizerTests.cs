using Finans.Application.Llm;
using Finans.Application.Portfolio;
using Finans.Domain.Enums;

namespace Finans.Application.Tests.Llm;

/// <summary>
/// T3.3: PII'siz portföy özeti. Test kapısı: kullanıcı varlık adları kaybolur, türler birleşir,
/// oranlar 3 basamağa yuvarlanır, top-2 yoğunlaşma türetilir.
/// </summary>
public class PortfolioAnonymizerTests
{
    private static PortfolioSummaryDto Summary(params (AssetType type, string name, decimal weight)[] slices) =>
        new(
            BaseCurrency: CurrencyCode.TRY,
            TotalValue: 1_000_000m,
            TotalCost: 800_000m,
            NetProfit: 200_000m,
            ReturnRatio: 0.25m,
            RealReturnRatio: 0.1234567m,
            Allocation: slices
                .Select(s => new AllocationDto(s.type, s.name, s.weight * 1_000_000m, s.weight))
                .ToList(),
            AsOf: new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void Drops_user_facing_names_and_collapses_to_type_buckets()
    {
        var input = Summary(
            (AssetType.Gold, "Gram Altın", 0.30m),
            (AssetType.Bes, "ZIRAATBANK BES — Hayat Plan", 0.40m),
            (AssetType.Stock, "AAPL", 0.15m),
            (AssetType.Cash, "Vadesiz TL", 0.15m));

        var anon = PortfolioAnonymizer.Anonymize(input);

        // Kullanıcının görebileceği adlar (özel BES adı, "Vadesiz TL" gibi) DTO'ya sızmaz.
        Assert.All(anon.Allocation, slice =>
        {
            var s = slice.ToString();
            Assert.DoesNotContain("ZIRAATBANK", s);
            Assert.DoesNotContain("Vadesiz", s);
        });
        // Sadece türler kalır.
        Assert.Equal(4, anon.Allocation.Count);
        Assert.Contains(anon.Allocation, a => a.Type == AssetType.Gold);
        Assert.Contains(anon.Allocation, a => a.Type == AssetType.Bes);
    }

    [Fact]
    public void Groups_same_type_slices_into_one()
    {
        // Aynı türde 2 holding (örn. iki ayrı hisse pozisyonu) → tek dilim.
        var input = Summary(
            (AssetType.Stock, "AAPL", 0.20m),
            (AssetType.Stock, "MSFT", 0.10m),
            (AssetType.Gold, "Gram", 0.70m));

        var anon = PortfolioAnonymizer.Anonymize(input);

        Assert.Equal(2, anon.Allocation.Count);
        var stock = anon.Allocation.Single(a => a.Type == AssetType.Stock);
        Assert.Equal(0.300m, stock.Weight);
    }

    [Fact]
    public void Rounds_ratios_to_three_decimals_and_total_to_integer()
    {
        var input = Summary((AssetType.Gold, "G", 1m)) with
        {
            TotalValue = 1_234_567.89m,
            ReturnRatio = 0.123456m,
            RealReturnRatio = 0.987654m,
        };

        var anon = PortfolioAnonymizer.Anonymize(input);

        Assert.Equal(1_234_568m, anon.TotalValue);
        Assert.Equal(0.123m, anon.ReturnRatio);
        Assert.Equal(0.988m, anon.RealReturnRatio);
    }

    [Fact]
    public void ConcentrationTop2_is_sum_of_two_largest_weights()
    {
        var input = Summary(
            (AssetType.Gold, "G", 0.40m),
            (AssetType.Bes, "B", 0.44m),
            (AssetType.Fx, "USD", 0.10m),
            (AssetType.Cash, "TRY", 0.06m));

        var anon = PortfolioAnonymizer.Anonymize(input);

        Assert.Equal(0.840m, anon.ConcentrationTop2);
        // En büyük → ilk sırada
        Assert.Equal(AssetType.Bes, anon.Allocation[0].Type);
    }

    [Fact]
    public void Preserves_null_ratios()
    {
        var input = Summary((AssetType.Cash, "TRY", 1m)) with
        {
            ReturnRatio = null,
            RealReturnRatio = null,
        };

        var anon = PortfolioAnonymizer.Anonymize(input);

        Assert.Null(anon.ReturnRatio);
        Assert.Null(anon.RealReturnRatio);
    }
}

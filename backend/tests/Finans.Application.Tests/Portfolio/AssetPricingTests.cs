using Finans.Application.Portfolio;
using Finans.Domain.Enums;

namespace Finans.Application.Tests.Portfolio;

/// <summary>
/// Sabit fiyatlı varlık kuralı (INC-003). Nakdin birim fiyatı tanım gereği 1'dir;
/// diğer türlerde fiyat dışarıdan gelir → null (kural onlara dokunmaz).
/// </summary>
public class AssetPricingTests
{
    [Fact]
    public void Cash_is_always_priced_at_one()
    {
        Assert.Equal(1m, AssetPricing.FixedUnitPriceFor(AssetType.Cash));
    }

    [Theory]
    [InlineData(AssetType.Gold)]
    [InlineData(AssetType.Fx)]
    [InlineData(AssetType.Stock)]
    [InlineData(AssetType.Fund)]
    [InlineData(AssetType.Bes)]
    public void Other_types_have_no_fixed_price(AssetType type)
    {
        Assert.Null(AssetPricing.FixedUnitPriceFor(type));
    }
}

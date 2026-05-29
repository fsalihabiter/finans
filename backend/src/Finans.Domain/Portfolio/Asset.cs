using Finans.Domain.Common;
using Finans.Domain.Enums;

namespace Finans.Domain.Portfolio;

/// <summary>
/// Varlık *tanımı* — kullanıcıdan bağımsız katalog (03 §A).
/// Unit = miktarın birimi (gram/adet/USD); PricingCurrency = birim fiyatın
/// ifade edildiği para birimi (örn. USD varlığı: Unit=USD, PricingCurrency=TRY).
/// </summary>
public class Asset : Entity
{
    public AssetType Type { get; set; }
    public string Name { get; set; } = null!;
    public string? Symbol { get; set; }
    public string Unit { get; set; } = null!;
    public CurrencyCode PricingCurrency { get; set; }
    public string? Exchange { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Holding> Holdings { get; set; } = new List<Holding>();
    public ICollection<PriceSnapshot> PriceSnapshots { get; set; } = new List<PriceSnapshot>();
}

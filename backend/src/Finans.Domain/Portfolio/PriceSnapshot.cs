using Finans.Domain.Common;

namespace Finans.Domain.Portfolio;

/// <summary>Fiyat geçmişi — reel getiri ve senaryo grafiği için (03 §A).</summary>
public class PriceSnapshot : Entity
{
    public Guid AssetId { get; set; }

    /// <summary>PricingCurrency cinsinden birim fiyat.</summary>
    public decimal Price { get; set; }

    /// <summary>Manual | &lt;providerKey&gt; (açık uçlu, 03 §2).</summary>
    public string Source { get; set; } = "Manual";

    public DateTime AsOfUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Asset Asset { get; set; } = null!;
}

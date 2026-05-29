using Finans.Domain.Common;
using Finans.Domain.Enums;

namespace Finans.Domain.Portfolio;

/// <summary>
/// Alış/satış işlemi — ortalama maliyet ve miktarın DOĞRULUK KAYNAĞI (03 §A, §11).
/// </summary>
public class Transaction : Entity
{
    public Guid HoldingId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Quantity { get; set; }

    /// <summary>PricingCurrency cinsinden birim fiyat.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Komisyon/masraf (varsayılan 0).</summary>
    public decimal Fee { get; set; }

    public DateTime TransactedAtUtc { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Holding Holding { get; set; } = null!;
}

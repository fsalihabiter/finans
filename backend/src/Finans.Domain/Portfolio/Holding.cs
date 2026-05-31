using Finans.Domain.Common;
using Finans.Domain.Identity;

namespace Finans.Domain.Portfolio;

/// <summary>
/// Kullanıcının bir varlıktaki pozisyonu (03 §A). Quantity ve AvgCost
/// Transactions'tan TÜRETİLİR (okuma yolu hızlansın diye cache'lenir, 03 §11).
/// Her erişim UserId ile kapsanır (per-user izolasyon, 11 §3).
/// </summary>
public class Holding : Entity, ISoftDelete
{
    public Guid UserId { get; set; }
    public Guid AssetId { get; set; }

    /// <summary>Σ Buy.Qty − Σ Sell.Qty (Transactions'tan türetilir).</summary>
    public decimal Quantity { get; set; }

    /// <summary>PricingCurrency cinsinden ağırlıklı ortalama birim maliyet (türetilir).</summary>
    public decimal AvgCost { get; set; }

    /// <summary>Faz 1 elle; Faz 2 PriceSnapshots'tan beslenir (cache).</summary>
    public decimal? CurrentPrice { get; set; }

    public string? Notes { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public User User { get; set; } = null!;
    public Asset Asset { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public BesDetails? BesDetails { get; set; }
    public ICollection<BesContribution> BesContributions { get; set; } = new List<BesContribution>();
}

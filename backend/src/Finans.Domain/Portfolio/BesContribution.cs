using Finans.Domain.Common;

namespace Finans.Domain.Portfolio;

/// <summary>
/// BES'e yapılan tek bir katkı ödemesi kaydı (T-BES.6). BES nominal hesaptır; bu kayıtlar
/// işlem geçmişini ve düzenli katkı takibini sağlar (`Transaction` BES'te kullanılmaz, T1.17).
/// Devlet katkısı ödeme tarihindeki orana göredir (geriye dönük değil).
/// </summary>
public class BesContribution : Entity
{
    public Guid HoldingId { get; set; }

    /// <summary>Kendi katkı payı.</summary>
    public decimal OwnAmount { get; set; }

    /// <summary>Devlet katkısı (ödeme tarihindeki orana göre).</summary>
    public decimal StateAmount { get; set; }

    /// <summary>Katkının ödendiği (sayıldığı) tarih.</summary>
    public DateTime PaidAtUtc { get; set; }

    /// <summary>Kaynak: "Manual" (tek giriş) | "Plan" (tarih aralığından üretilen).</summary>
    public string Source { get; set; } = "Manual";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Holding Holding { get; set; } = null!;
}

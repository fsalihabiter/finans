using System.Net;
using Finans.Domain.Common;

namespace Finans.Domain.Identity;

/// <summary>
/// Yenileme token'ı (03 §B, Faz 5 kullanım). Token'ın HASH'i saklanır, düz değil
/// (11 §2). Rotasyon/iptal için RevokedAtUtc.
/// </summary>
public class RefreshToken : Entity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = null!;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public IPAddress? CreatedByIp { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}

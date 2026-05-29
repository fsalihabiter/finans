using System.Net;
using Finans.Domain.Common;
using Finans.Domain.Enums;

namespace Finans.Domain.Identity;

/// <summary>
/// Güvenlik & inkâr-edilemezlik kaydı (03 §B, 12 §7). Salt-ekleme (append-only).
/// PII/sır YAZILMAZ (12 §3). Başarısız girişte UserId null olabilir.
/// </summary>
public class AuditLog : Entity
{
    public Guid? UserId { get; set; }
    public AuditAction Action { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public AuditResult Result { get; set; }
    public IPAddress? IpAddress { get; set; }
    public DateTime AtUtc { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}

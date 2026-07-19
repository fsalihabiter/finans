using Finans.Domain.Common;
using Finans.Domain.Enums;
using Finans.Domain.Portfolio;

namespace Finans.Domain.Identity;

/// <summary>
/// Kullanıcı (03 §B). Faz 1-4'te tekil seed kullanıcı; gerçek kimlik Faz 5.
/// Email/PasswordHash Faz 5'te dolar (PII minimum tut, 11 §7). Parola asla düz.
/// </summary>
public class User : Entity
{
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }
    public string? DisplayName { get; set; }
    public CurrencyCode BaseCurrency { get; set; } = CurrencyCode.TRY;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    // ── Okuryazarlık profili (T6.6, 15 §4) — tanılama testinden ──────────────

    /// <summary>
    /// Bilgi seviyesi → içerik <b>derinliğini</b> belirler (L1/L2/L3, 15 §2.2).
    /// <c>null</c> = ölçülmedi → Başlangıç gibi davranılır (test atlanabilir).
    /// </summary>
    public LessonLevel? LiteracyLevel { get; set; }

    /// <summary>
    /// Risk tutumu → yalnız ders <b>sırasını/vurgusunu</b> etkiler.
    /// <b>⚠ Arayüze ve API yanıtlarına ÇIKMAZ</b> (15 §1.1, SC-E4).
    /// </summary>
    public RiskAttitude? RiskAttitude { get; set; }

    /// <summary>Tanılamanın çözüldüğü an; <c>null</c> ise onboarding'de sorulur.</summary>
    public DateTime? ProfiledAtUtc { get; set; }

    public ICollection<Holding> Holdings { get; set; } = new List<Holding>();
    public ICollection<UserRoleAssignment> UserRoles { get; set; } = new List<UserRoleAssignment>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

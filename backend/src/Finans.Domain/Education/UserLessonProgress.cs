using Finans.Domain.Common;
using Finans.Domain.Enums;
using Finans.Domain.Identity;

namespace Finans.Domain.Education;

/// <summary>
/// Kullanıcının ders ilerlemesi (03 §C). **Her erişim UserId ile kapsanır** (11 §3).
/// UNIQUE(UserId, LessonId); ProgressPercent 0-100. "Locked" saklanmaz (türetilir).
/// </summary>
public class UserLessonProgress : Entity
{
    public Guid UserId { get; set; }
    public Guid LessonId { get; set; }
    public LessonStatus Status { get; set; }

    /// <summary>0-100 (CHECK kısıtı).</summary>
    public int ProgressPercent { get; set; }

    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Lesson Lesson { get; set; } = null!;
}

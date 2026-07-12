using Finans.Domain.Common;
using Finans.Domain.Identity;

namespace Finans.Domain.Education;

/// <summary>
/// Kullanıcının bir mini test denemesi (03 §C). **UserId kapsamlı** (11 §3).
/// Cevap-düzey detay (`UserQuizAnswers`) MVP dışı — analitik gerekirse eklenir.
/// </summary>
public class UserQuizAttempt : Entity
{
    public Guid UserId { get; set; }
    public Guid QuizId { get; set; }

    /// <summary>Skor (yüzde, 0-100).</summary>
    public int Score { get; set; }

    public bool Passed { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }

    public User User { get; set; } = null!;
    public Quiz Quiz { get; set; } = null!;
}

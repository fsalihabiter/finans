using Finans.Domain.Common;
using Finans.Domain.Enums;

namespace Finans.Domain.Education;

/// <summary>Ders kümesi — örn. "Temeller" (03 §C, T5E.1).</summary>
public class LearningTrack : Entity
{
    /// <summary>URL dostu tekil anahtar (örn. "temeller").</summary>
    public string Slug { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public LessonLevel Level { get; set; }
    public int OrderIndex { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}

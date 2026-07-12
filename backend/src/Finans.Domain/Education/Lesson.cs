using Finans.Domain.Common;
using Finans.Domain.Enums;

namespace Finans.Domain.Education;

/// <summary>
/// Tek ders (03 §C). İçerik DB'de Markdown; "kilitli" durumu SAKLANMAZ,
/// <see cref="Prerequisites"/> + kullanıcı ilerlemesinden türetilir.
/// </summary>
public class Lesson : Entity
{
    public Guid TrackId { get; set; }

    /// <summary>URL dostu tekil anahtar (örn. "enflasyon-ve-reel-getiri").</summary>
    public string Slug { get; set; } = null!;

    /// <summary>Track içi sıra (1,2,3…).</summary>
    public int OrderIndex { get; set; }

    public string Title { get; set; } = null!;

    /// <summary>Kart açıklaması ("Param büyüdü mü, yoksa sadece rakam mı?").</summary>
    public string Summary { get; set; } = null!;

    /// <summary>Ders gövdesi (Markdown). Uzun/yapılı içerikte <see cref="Sections"/> kullanılır.</summary>
    public string BodyMarkdown { get; set; } = null!;

    public int EstimatedMinutes { get; set; }
    public LessonLevel Level { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public LearningTrack Track { get; set; } = null!;
    public ICollection<LessonSection> Sections { get; set; } = new List<LessonSection>();

    /// <summary>Bu dersin ön-koşulları (tamamlanmadan ders kilitli sayılır).</summary>
    public ICollection<LessonPrerequisite> Prerequisites { get; set; } = new List<LessonPrerequisite>();

    public ICollection<LessonConceptTag> ConceptTags { get; set; } = new List<LessonConceptTag>();

    /// <summary>Derse bağlı mini test (opsiyonel).</summary>
    public Quiz? Quiz { get; set; }
}

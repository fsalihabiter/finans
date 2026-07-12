namespace Finans.Domain.Education;

/// <summary>Ders ↔ kavram etiketi bağı (03 §C — M:N). Bileşik PK (LessonId, ConceptTagId).</summary>
public class LessonConceptTag
{
    public Guid LessonId { get; set; }
    public Guid ConceptTagId { get; set; }

    public Lesson Lesson { get; set; } = null!;
    public ConceptTag ConceptTag { get; set; } = null!;
}

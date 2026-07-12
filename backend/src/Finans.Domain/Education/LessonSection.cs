using Finans.Domain.Common;

namespace Finans.Domain.Education;

/// <summary>Ders içi içerik bloğu (03 §C — zengin/yapılı içerik için, MVP'de opsiyonel).</summary>
public class LessonSection : Entity
{
    public Guid LessonId { get; set; }
    public int OrderIndex { get; set; }
    public string? Heading { get; set; }
    public string BodyMarkdown { get; set; } = null!;

    public Lesson Lesson { get; set; } = null!;
}

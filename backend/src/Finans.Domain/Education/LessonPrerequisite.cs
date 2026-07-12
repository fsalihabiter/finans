namespace Finans.Domain.Education;

/// <summary>
/// Kilit mantığı (03 §C — M:N, kendine): <see cref="PrerequisiteLessonId"/> tamamlanmadan
/// <see cref="LessonId"/> kilitli sayılır. Bileşik PK (LessonId, PrerequisiteLessonId).
/// </summary>
public class LessonPrerequisite
{
    public Guid LessonId { get; set; }
    public Guid PrerequisiteLessonId { get; set; }

    public Lesson Lesson { get; set; } = null!;
    public Lesson PrerequisiteLesson { get; set; } = null!;
}

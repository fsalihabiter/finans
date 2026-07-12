using Finans.Domain.Common;

namespace Finans.Domain.Education;

/// <summary>Derse bağlı mini test (03 §C). LessonId null = bağımsız test.</summary>
public class Quiz : Entity
{
    public Guid? LessonId { get; set; }
    public string Title { get; set; } = null!;

    /// <summary>Geçme eşiği (yüzde, 0-100).</summary>
    public int PassingScore { get; set; }

    public Lesson? Lesson { get; set; }
    public ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
}

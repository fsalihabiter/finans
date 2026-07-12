using Finans.Domain.Common;

namespace Finans.Domain.Education;

/// <summary>Mini test şıkkı (03 §C).</summary>
public class QuizOption : Entity
{
    public Guid QuestionId { get; set; }
    public int OrderIndex { get; set; }
    public string Text { get; set; } = null!;
    public bool IsCorrect { get; set; }

    public QuizQuestion Question { get; set; } = null!;
}

using Finans.Domain.Common;
using Finans.Domain.Enums;

namespace Finans.Domain.Education;

/// <summary>Mini test sorusu (03 §C). Explanation = doğru cevabın NEDEN doğru olduğu (eğitici).</summary>
public class QuizQuestion : Entity
{
    public Guid QuizId { get; set; }
    public int OrderIndex { get; set; }
    public QuizQuestionType Type { get; set; }
    public string Prompt { get; set; } = null!;

    /// <summary>
    /// Soru zorluğu (T6.11) — varsayılan <see cref="QuizDifficulty.Easy"/> (geriye dönük
    /// uyum: eski sorular herkese görünmeye devam eder).
    /// </summary>
    public QuizDifficulty Difficulty { get; set; } = QuizDifficulty.Easy;

    /// <summary>Cevap sonrası gösterilen eğitici açıklama.</summary>
    public string Explanation { get; set; } = null!;

    public Quiz Quiz { get; set; } = null!;
    public ICollection<QuizOption> Options { get; set; } = new List<QuizOption>();
}

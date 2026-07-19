using Finans.Domain.Enums;

namespace Finans.Application.Education;

// Eğitim modülü DTO'ları (04 §7.5). İçerik herkese açık (okuma); ders durumu/ilerleme
// geçerli kullanıcıya kapsanır (UserId, 11 §3). Enum'lar tel üzerinde string (global
// JsonStringEnumConverter): Level="Beginner", Status="Completed" vb.

/// <summary>Ders seti kartı — `lessonCount` yayındaki ders sayısı.</summary>
public sealed record LearningTrackDto(
    Guid Id, string Slug, string Title, string? Description, LessonLevel Level, int LessonCount);

/// <summary>Ders listesi öğesi — `status`/`progressPercent` kullanıcının; `locked` ön-koşuldan türetilir.</summary>
public sealed record LessonListItemDto(
    Guid Id, string Slug, int Order, string Title, string Summary, int EstimatedMinutes,
    LessonLevel Level, LessonStatus Status, int ProgressPercent, bool Locked);

/// <summary>
/// Ders içi içerik bloğu (T6.5, 15 §2). <paramref name="DepthTier"/> derinlik
/// katmanı, <paramref name="Kind"/> blok türü — DİK eksenler. İstemci
/// kullanıcının seviyesine kadarki katmanları açık, üstünü "Daha derine in"
/// arkasında gösterir (T6.7).
/// </summary>
public sealed record LessonSectionDto(
    int Order, string? Heading, string BodyMarkdown, DepthTier DepthTier, SectionKind Kind);

public sealed record ConceptTagDto(string Key, string Label);

/// <summary>Test şıkkı — <b>doğru cevap SIZDIRILMAZ</b> (IsCorrect yok; deneme sonrası results'ta döner).</summary>
public sealed record QuizOptionDto(Guid Id, int Order, string Text);

/// <summary>Test sorusu — Explanation SIZDIRILMAZ (yalnız deneme sonucu results'ta).</summary>
public sealed record QuizQuestionDto(
    Guid Id, int Order, QuizQuestionType Type, string Prompt, IReadOnlyList<QuizOptionDto> Options);

public sealed record QuizDto(Guid Id, string Title, int PassingScore, IReadOnlyList<QuizQuestionDto> Questions);

/// <summary>Tek ders detayı — gövde + bölümler + (varsa) mini test + kavram etiketleri + kullanıcı durumu.</summary>
/// <summary>
/// Settaki bir sonraki ders (T6.2 ilerleme akışı). <paramref name="Locked"/> ön-koşuldan
/// TÜRETİLİR: bu ders tamamlanınca sonraki kilit açılır ve doğrudan geçilebilir.
/// Set sonundaysa <c>null</c>.
/// </summary>
public sealed record NextLessonDto(Guid Id, string Slug, string Title, bool Locked);

public sealed record LessonDetailDto(
    Guid Id, string Slug, int Order, string Title, string Summary, string BodyMarkdown,
    int EstimatedMinutes, LessonLevel Level, LessonStatus Status, int ProgressPercent, bool Locked,
    IReadOnlyList<LessonSectionDto> Sections, QuizDto? Quiz, IReadOnlyList<ConceptTagDto> ConceptTags,
    /// <summary>"Senin portföyünde" bloğunun veri kaynağı; bağlam bloğu yoksa null (15 §3.2).</summary>
    LessonContextState? ContextState = null,
    /// <summary>Bağlam verisinin ait olduğu an — yalnız <c>Stale</c> durumunda anlamlı.</summary>
    DateTime? ContextAsOf = null,
    /// <summary>Settaki bir sonraki ders (ilerleme akışı); set sonundaysa null.</summary>
    NextLessonDto? NextLesson = null);

/// <summary>Ders ilerleme güncelleme isteği (upsert; UserId kapsamlı).</summary>
public sealed record UpdateLessonProgressRequest(LessonStatus Status, int ProgressPercent);

public sealed record LessonProgressDto(Guid LessonId, LessonStatus Status, int ProgressPercent);

public sealed record QuizAnswerInput(Guid QuestionId, IReadOnlyList<Guid> SelectedOptionIds);

public sealed record SubmitQuizAttemptRequest(IReadOnlyList<QuizAnswerInput> Answers);

/// <summary>Soru sonucu — deneme SONRASI: doğru mu + eğitici açıklama + doğru şık(lar).</summary>
public sealed record QuizQuestionResultDto(
    Guid QuestionId, bool Correct, string Explanation, IReadOnlyList<Guid> CorrectOptionIds);

public sealed record QuizAttemptResultDto(int Score, bool Passed, IReadOnlyList<QuizQuestionResultDto> Results);

using Finans.Application.Common;
using Finans.Application.Education;
using Finans.Domain.Education;
using Finans.Domain.Enums;
using Finans.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Finans.Infrastructure.Services;

/// <summary>
/// Eğitim modülü servisi (T5E.3, 04 §7.5). İçerik (track/ders/quiz sorusu) herkese açık;
/// ders durumu/ilerleme ve quiz denemesi <b>daima geçerli kullanıcıya</b> kapsanır (11 §3).
/// Kilit türetilir: bir dersin ön-koşullarından biri kullanıcı tarafından tamamlanmadıysa
/// ders kilitlidir (03 §C). Quiz cevap-anahtarı yalnız deneme SONUCUNDA açılır (sızıntı yok).
/// </summary>
public sealed class EducationService(
    FinansDbContext db,
    ICurrentUser currentUser,
    ILessonContextService lessonContext) : IEducationService
{
    public async Task<IReadOnlyList<LearningTrackDto>> GetTracksAsync(CancellationToken ct = default) =>
        await db.LearningTracks
            .Where(t => t.IsPublished)
            .OrderBy(t => t.OrderIndex)
            .Select(t => new LearningTrackDto(
                t.Id, t.Slug, t.Title, t.Description, t.Level,
                t.Lessons.Count(l => l.IsPublished)))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LessonListItemDto>> GetTrackLessonsAsync(
        string trackSlug, CancellationToken ct = default)
    {
        var track = await db.LearningTracks
            .FirstOrDefaultAsync(t => t.Slug == trackSlug && t.IsPublished, ct)
            ?? throw new NotFoundException("Eğitim seti bulunamadı.");

        var lessons = await db.Lessons
            .Where(l => l.TrackId == track.Id && l.IsPublished)
            .OrderBy(l => l.OrderIndex)
            .ToListAsync(ct);

        return await BuildLessonListAsync(lessons, ct);
    }

    public async Task<IReadOnlyList<LessonListItemDto>> GetLessonsByConceptAsync(
        string conceptKey, CancellationToken ct = default)
    {
        var lessons = await db.Lessons
            .Where(l => l.IsPublished && l.ConceptTags.Any(lt => lt.ConceptTag.Key == conceptKey))
            .OrderBy(l => l.OrderIndex)
            .ToListAsync(ct);

        // Bilinmeyen/kavramsız → boş liste (hata değil).
        return await BuildLessonListAsync(lessons, ct);
    }

    public async Task<LessonDetailDto> GetLessonAsync(string lessonSlug, CancellationToken ct = default)
    {
        var lesson = await db.Lessons
            .Include(l => l.Sections)
            .Include(l => l.Quiz!).ThenInclude(q => q.Questions).ThenInclude(qq => qq.Options)
            .Include(l => l.ConceptTags).ThenInclude(lt => lt.ConceptTag)
            .FirstOrDefaultAsync(l => l.Slug == lessonSlug && l.IsPublished, ct)
            ?? throw new NotFoundException("Ders bulunamadı.");

        var userId = currentUser.UserId;
        var progress = await db.UserLessonProgress
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lesson.Id, ct);
        var completed = await LoadCompletedSetAsync(ct);
        var prereqs = await LoadPrereqsAsync([lesson.Id], ct);

        // Katmanlı içerik (T6.5): derinlik + tür DİK eksenler; sıralama OrderIndex'e
        // sadık kalır (yazar hangi sırada kurguladıysa). Filtreleme YOK — istemci
        // seviyeye göre katlar (T6.7). Bölüm yoksa istemci BodyMarkdown'a düşer (SC-E2).
        //
        // T6.2: LiveContext blokları burada çözümlenir — {{anahtar}} token'ları KODDA
        // hesaplanmış portföy metrikleriyle değişir (CLAUDE.md §3.1). Portföy yoksa
        // etiketli demo değerlere düşer; hiçbir durumda ders kilitlenmez.
        LessonContextState? contextState = null;
        DateTime? contextAsOf = null;
        var sections = new List<LessonSectionDto>();

        foreach (var s in lesson.Sections.OrderBy(s => s.OrderIndex))
        {
            if (s.Kind != SectionKind.LiveContext)
            {
                sections.Add(new LessonSectionDto(s.OrderIndex, s.Heading, s.BodyMarkdown, s.DepthTier, s.Kind));
                continue;
            }

            var resolved = await lessonContext.ResolveAsync(s.BodyMarkdown, ct);
            // Tüm token'lar çözülemediyse blok boşalır → hiç gösterme (yarım cümle yok).
            if (string.IsNullOrWhiteSpace(resolved.Body))
                continue;

            contextState = resolved.State;
            contextAsOf = resolved.AsOf;
            sections.Add(new LessonSectionDto(s.OrderIndex, s.Heading, resolved.Body, s.DepthTier, s.Kind));
        }

        var tags = lesson.ConceptTags
            .Select(lt => new ConceptTagDto(lt.ConceptTag.Key, lt.ConceptTag.Label))
            .ToList();

        QuizDto? quiz = null;
        if (lesson.Quiz is not null)
        {
            // Cevap-anahtarı (IsCorrect) ve Explanation BURADA döndürülmez — sızıntı yok.
            var questions = lesson.Quiz.Questions
                .OrderBy(q => q.OrderIndex)
                .Select(q => new QuizQuestionDto(q.Id, q.OrderIndex, q.Type, q.Prompt,
                    q.Options
                        .OrderBy(o => o.OrderIndex)
                        .Select(o => new QuizOptionDto(o.Id, o.OrderIndex, o.Text))
                        .ToList()))
                .ToList();
            quiz = new QuizDto(lesson.Quiz.Id, lesson.Quiz.Title, lesson.Quiz.PassingScore, questions);
        }

        // İlerleme akışı (T6.2): settaki bir sonraki ders. Kilidi ön-koşuldan TÜRETİLİR —
        // bu ders tamamlanınca sonraki kendiliğinden açılır ve okuyucudan doğrudan geçilir.
        var next = await db.Lessons
            .Where(l => l.TrackId == lesson.TrackId && l.IsPublished && l.OrderIndex > lesson.OrderIndex)
            .OrderBy(l => l.OrderIndex)
            .Select(l => new { l.Id, l.Slug, l.Title })
            .FirstOrDefaultAsync(ct);

        NextLessonDto? nextDto = null;
        if (next is not null)
        {
            var nextPrereqs = await LoadPrereqsAsync([next.Id], ct);
            nextDto = new NextLessonDto(next.Id, next.Slug, next.Title, IsLocked(next.Id, nextPrereqs, completed));
        }

        return new LessonDetailDto(
            lesson.Id, lesson.Slug, lesson.OrderIndex, lesson.Title, lesson.Summary, lesson.BodyMarkdown,
            lesson.EstimatedMinutes, lesson.Level,
            progress?.Status ?? LessonStatus.NotStarted, progress?.ProgressPercent ?? 0,
            IsLocked(lesson.Id, prereqs, completed),
            sections, quiz, tags, contextState, contextAsOf, nextDto);
    }

    public async Task<LessonProgressDto> UpdateProgressAsync(
        Guid lessonId, UpdateLessonProgressRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ProgressPercent is < 0 or > 100)
            throw new ValidationException("progressPercent", "out_of_range", "İlerleme yüzdesi 0-100 arası olmalıdır.");
        if (!Enum.IsDefined(request.Status))
            throw new ValidationException("status", "invalid", "Geçersiz ders durumu.");

        if (!await db.Lessons.AnyAsync(l => l.Id == lessonId && l.IsPublished, ct))
            throw new NotFoundException("Ders bulunamadı.");

        var userId = currentUser.UserId;
        var now = DateTime.UtcNow;
        var progress = await db.UserLessonProgress
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lessonId, ct);

        if (progress is null)
        {
            progress = new UserLessonProgress { UserId = userId, LessonId = lessonId };
            db.UserLessonProgress.Add(progress);
        }

        progress.Status = request.Status;
        // Tamamlandıysa yüzde tutarlı biçimde 100'e sabitlenir.
        progress.ProgressPercent = request.Status == LessonStatus.Completed ? 100 : request.ProgressPercent;
        if (request.Status != LessonStatus.NotStarted && progress.StartedAtUtc is null)
            progress.StartedAtUtc = now;
        progress.CompletedAtUtc = request.Status == LessonStatus.Completed
            ? progress.CompletedAtUtc ?? now
            : null;
        progress.UpdatedAtUtc = now;

        await db.SaveChangesAsync(ct);
        return new LessonProgressDto(lessonId, progress.Status, progress.ProgressPercent);
    }

    public async Task<QuizAttemptResultDto> SubmitQuizAttemptAsync(
        Guid quizId, SubmitQuizAttemptRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var quiz = await db.Quizzes
            .Include(q => q.Questions).ThenInclude(qq => qq.Options)
            .FirstOrDefaultAsync(q => q.Id == quizId, ct)
            ?? throw new NotFoundException("Test bulunamadı.");

        // Kullanıcı seçimlerini soru başına küme olarak topla (birden çok yanıt satırı birleşir).
        var selectedByQuestion = (request.Answers ?? [])
            .GroupBy(a => a.QuestionId)
            .ToDictionary(
                g => g.Key,
                g => g.SelectMany(a => a.SelectedOptionIds ?? []).ToHashSet());

        var questions = quiz.Questions.OrderBy(q => q.OrderIndex).ToList();
        var results = new List<QuizQuestionResultDto>(questions.Count);
        var correctCount = 0;

        foreach (var q in questions)
        {
            var correctOrdered = q.Options.Where(o => o.IsCorrect).OrderBy(o => o.OrderIndex)
                .Select(o => o.Id).ToList();
            var correctSet = correctOrdered.ToHashSet();
            var selected = selectedByQuestion.GetValueOrDefault(q.Id) ?? [];

            // Tam eşleşme: seçilen küme = doğru küme (SingleChoice/TrueFalse tek; MultipleChoice tümü).
            var isCorrect = selected.SetEquals(correctSet);
            if (isCorrect) correctCount++;
            results.Add(new QuizQuestionResultDto(q.Id, isCorrect, q.Explanation, correctOrdered));
        }

        var score = questions.Count == 0
            ? 0
            : (int)Math.Round(correctCount * 100.0 / questions.Count, MidpointRounding.AwayFromZero);
        var passed = score >= quiz.PassingScore;

        var now = DateTime.UtcNow;
        db.UserQuizAttempts.Add(new UserQuizAttempt
        {
            UserId = currentUser.UserId,
            QuizId = quizId,
            Score = score,
            Passed = passed,
            StartedAtUtc = now,
            CompletedAtUtc = now,
        });
        await db.SaveChangesAsync(ct);

        return new QuizAttemptResultDto(score, passed, results);
    }

    // ── Kullanıcı-kapsamlı yardımcılar (kilit türetimi + durum) ──────────────────

    private async Task<IReadOnlyList<LessonListItemDto>> BuildLessonListAsync(
        List<Lesson> lessons, CancellationToken ct)
    {
        if (lessons.Count == 0) return [];

        var lessonIds = lessons.Select(l => l.Id).ToList();
        var progress = await LoadProgressAsync(lessonIds, ct);
        var completed = await LoadCompletedSetAsync(ct);
        var prereqs = await LoadPrereqsAsync(lessonIds, ct);

        return lessons.Select(l =>
        {
            var p = progress.GetValueOrDefault(l.Id);
            return new LessonListItemDto(
                l.Id, l.Slug, l.OrderIndex, l.Title, l.Summary, l.EstimatedMinutes, l.Level,
                p?.Status ?? LessonStatus.NotStarted, p?.ProgressPercent ?? 0,
                IsLocked(l.Id, prereqs, completed));
        }).ToList();
    }

    private async Task<Dictionary<Guid, UserLessonProgress>> LoadProgressAsync(
        List<Guid> lessonIds, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        return await db.UserLessonProgress
            .Where(p => p.UserId == userId && lessonIds.Contains(p.LessonId))
            .ToDictionaryAsync(p => p.LessonId, ct);
    }

    /// <summary>Kullanıcının tamamladığı TÜM ders id'leri (ön-koşul kilit türetimi için).</summary>
    private async Task<HashSet<Guid>> LoadCompletedSetAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var ids = await db.UserLessonProgress
            .Where(p => p.UserId == userId && p.Status == LessonStatus.Completed)
            .Select(p => p.LessonId)
            .ToListAsync(ct);
        return ids.ToHashSet();
    }

    private async Task<Dictionary<Guid, List<Guid>>> LoadPrereqsAsync(
        List<Guid> lessonIds, CancellationToken ct)
    {
        var pairs = await db.LessonPrerequisites
            .Where(p => lessonIds.Contains(p.LessonId))
            .Select(p => new { p.LessonId, p.PrerequisiteLessonId })
            .ToListAsync(ct);
        return pairs
            .GroupBy(p => p.LessonId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.PrerequisiteLessonId).ToList());
    }

    /// <summary>Ders kilitli mi: ön-koşullarından biri kullanıcı tarafından tamamlanmadıysa evet.</summary>
    private static bool IsLocked(Guid lessonId, Dictionary<Guid, List<Guid>> prereqs, HashSet<Guid> completed) =>
        prereqs.TryGetValue(lessonId, out var reqs) && reqs.Any(r => !completed.Contains(r));
}

using Finans.Application.Education;
using Microsoft.AspNetCore.Mvc;

namespace Finans.Api.Controllers;

/// <summary>
/// Eğitim modülü uçları (04 §7.5). İçerik okuma herkese açık; ders durumu/ilerleme ve
/// quiz denemeleri servis katmanında geçerli kullanıcıya kapsanır (UserId, 11 §3).
/// </summary>
[ApiController]
[Route("api/education")]
public sealed class EducationController(
    IEducationService education,
    IDiagnosticService diagnostic) : ControllerBase
{
    // ── Tanılama testi (T6.6, 15 §4) ─────────────────────────────────────────
    // ⚠ Hiçbir uç RiskAttitude DÖNMEZ (15 §1.1 · SC-E4).

    /// <summary>GET /api/education/diagnostic — 8 tanılama sorusu (cevap anahtarı YOK).</summary>
    [HttpGet("diagnostic")]
    public ActionResult<IReadOnlyList<DiagnosticQuestionDto>> GetDiagnostic() =>
        Ok(diagnostic.GetQuestions());

    /// <summary>GET /api/education/profile — kullanıcının seviyesi + ölçülmüş mü (onboarding kararı).</summary>
    [HttpGet("profile")]
    public async Task<ActionResult<LiteracyProfileDto>> GetProfile(CancellationToken ct) =>
        Ok(await diagnostic.GetProfileAsync(ct));

    /// <summary>POST /api/education/diagnostic — cevapları değerlendirir, profili yazar. Boş liste = atla.</summary>
    [HttpPost("diagnostic")]
    public async Task<ActionResult<DiagnosticResultDto>> SubmitDiagnostic(
        [FromBody] SubmitDiagnosticRequest request, CancellationToken ct) =>
        Ok(await diagnostic.SubmitAsync(request, ct));

    /// <summary>GET /api/education/tracks — yayındaki ders setleri (+ ders sayısı).</summary>
    [HttpGet("tracks")]
    public async Task<ActionResult<IReadOnlyList<LearningTrackDto>>> GetTracks(CancellationToken ct) =>
        Ok(await education.GetTracksAsync(ct));

    /// <summary>GET /api/education/tracks/{slug}/lessons — setin dersleri + kullanıcı durumu/kilit (set yoksa 404).</summary>
    [HttpGet("tracks/{slug}/lessons")]
    public async Task<ActionResult<IReadOnlyList<LessonListItemDto>>> GetTrackLessons(
        string slug, CancellationToken ct) =>
        Ok(await education.GetTrackLessonsAsync(slug, ct));

    /// <summary>GET /api/education/lessons/by-concept/{conceptKey} — kavrama bağlı dersler (derin bağlantı).</summary>
    [HttpGet("lessons/by-concept/{conceptKey}")]
    public async Task<ActionResult<IReadOnlyList<LessonListItemDto>>> GetLessonsByConcept(
        string conceptKey, CancellationToken ct) =>
        Ok(await education.GetLessonsByConceptAsync(conceptKey, ct));

    /// <summary>GET /api/education/lessons/{slug} — tek ders detayı + kullanıcı durumu (ders yoksa 404).</summary>
    [HttpGet("lessons/{slug}")]
    public async Task<ActionResult<LessonDetailDto>> GetLesson(string slug, CancellationToken ct) =>
        Ok(await education.GetLessonAsync(slug, ct));

    /// <summary>PUT /api/education/lessons/{id}/progress — ilerleme upsert (UserId kapsamlı; ders yoksa 404, yüzde dışı 400).</summary>
    [HttpPut("lessons/{id:guid}/progress")]
    public async Task<ActionResult<LessonProgressDto>> UpdateProgress(
        Guid id, [FromBody] UpdateLessonProgressRequest request, CancellationToken ct) =>
        Ok(await education.UpdateProgressAsync(id, request, ct));

    /// <summary>POST /api/education/quizzes/{id}/attempts — denemeyi değerlendir + kaydet (quiz yoksa 404).</summary>
    [HttpPost("quizzes/{id:guid}/attempts")]
    public async Task<ActionResult<QuizAttemptResultDto>> SubmitQuizAttempt(
        Guid id, [FromBody] SubmitQuizAttemptRequest request, CancellationToken ct) =>
        Ok(await education.SubmitQuizAttemptAsync(id, request, ct));
}

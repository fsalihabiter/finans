using Finans.Application.Common;
using Finans.Application.Education;
using Finans.Domain.Enums;
using Finans.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Finans.Infrastructure.Services;

/// <summary>
/// Tanılama testi servisi (T6.6, 15 §4). Soruları verir, cevapları puanlar ve
/// profili <c>Users</c> üzerine yazar — <b>daima geçerli kullanıcıya kapsanır</b> (11 §3).
/// </summary>
/// <remarks>
/// ⚠ <b>SPK sınırı (15 §1.1):</b> <see cref="RiskAttitude"/> DB'ye yazılır ama
/// hiçbir DTO'da dışarı verilmez ve hiçbir dağılım/portföy çıktısı üretmez.
/// Yalnız davranış derslerinin sırasını etkilemek için saklanır (SC-E4).
/// </remarks>
public sealed class DiagnosticService(FinansDbContext db, ICurrentUser currentUser) : IDiagnosticService
{
    public IReadOnlyList<DiagnosticQuestionDto> GetQuestions() => DiagnosticContent.Questions();

    public async Task<LiteracyProfileDto> GetProfileAsync(CancellationToken ct = default)
    {
        var userId = currentUser.UserId;
        var user = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.LiteracyLevel, u.ProfiledAtUtc })
            .FirstOrDefaultAsync(ct);

        // Kullanıcı yoksa "ölçülmedi" davran — onboarding yine gösterilebilir.
        return new LiteracyProfileDto(user?.LiteracyLevel, user?.ProfiledAtUtc is not null);
    }

    public async Task<DiagnosticResultDto> SubmitAsync(
        SubmitDiagnosticRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Bilinmeyen soru/şık anahtarı sessizce yok sayılmaz — girdi sunucuda doğrulanır (11 §4).
        var answers = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var a in request.Answers ?? [])
        {
            if (a is null || !DiagnosticContent.IsKnownQuestion(a.QuestionKey))
                throw new ValidationException("answers", "unknown_question", "Geçersiz soru anahtarı.");
            if (!DiagnosticContent.IsKnownOption(a.QuestionKey, a.OptionKey))
                throw new ValidationException("answers", "unknown_option", "Geçersiz şık anahtarı.");
            if (!answers.TryAdd(a.QuestionKey, a.OptionKey))
                throw new ValidationException("answers", "duplicate_question", "Aynı soru birden fazla yanıtlanamaz.");
        }

        var level = DiagnosticContent.ScoreLiteracy(answers);
        var attitude = DiagnosticContent.ScoreRiskAttitude(answers);

        var userId = currentUser.UserId;
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is not null)
        {
            user.LiteracyLevel = level;
            user.RiskAttitude = attitude; // saklanır, ASLA dönülmez (15 §1.1)
            user.ProfiledAtUtc = DateTime.UtcNow;
            user.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return new DiagnosticResultDto(level, DiagnosticContent.Message(level));
    }
}

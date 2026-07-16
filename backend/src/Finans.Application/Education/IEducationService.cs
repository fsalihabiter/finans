namespace Finans.Application.Education;

/// <summary>
/// Eğitim modülü use-case'leri (04 §7.5). İçerik okuma herkese açık; ders durumu/ilerleme
/// ve quiz denemeleri geçerli kullanıcıya kapsanır (UserId, 11 §3). "Locked" saklanmaz —
/// ön-koşul dersleri tamamlanmadıysa türetilir (03 §C).
/// </summary>
public interface IEducationService
{
    /// <summary>Yayındaki ders setleri (+ ders sayısı), sıralı.</summary>
    Task<IReadOnlyList<LearningTrackDto>> GetTracksAsync(CancellationToken ct = default);

    /// <summary>Bir setin dersleri + kullanıcının durumu/ilerlemesi + kilit. Set yoksa 404.</summary>
    Task<IReadOnlyList<LessonListItemDto>> GetTrackLessonsAsync(string trackSlug, CancellationToken ct = default);

    /// <summary>Tek ders detayı (gövde, bölümler, quiz, kavramlar) + kullanıcı durumu. Ders yoksa 404.</summary>
    Task<LessonDetailDto> GetLessonAsync(string lessonSlug, CancellationToken ct = default);

    /// <summary>Ders ilerlemesini upsert eder (UserId kapsamlı). Ders yoksa 404; yüzde 0-100 dışı 400.</summary>
    Task<LessonProgressDto> UpdateProgressAsync(Guid lessonId, UpdateLessonProgressRequest request, CancellationToken ct = default);

    /// <summary>Quiz denemesini değerlendirir, kaydeder (UserId), sonuç + açıklama döner. Quiz yoksa 404.</summary>
    Task<QuizAttemptResultDto> SubmitQuizAttemptAsync(Guid quizId, SubmitQuizAttemptRequest request, CancellationToken ct = default);

    /// <summary>Bir kavram etiketine bağlı dersler (Analiz/Hisse kartından derin bağlantı). Boş olabilir.</summary>
    Task<IReadOnlyList<LessonListItemDto>> GetLessonsByConceptAsync(string conceptKey, CancellationToken ct = default);
}

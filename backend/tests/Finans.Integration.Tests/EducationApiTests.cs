using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Finans.Domain.Education;
using Finans.Domain.Enums;
using Finans.Domain.Identity;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Finans.Integration.Tests;

/// <summary>
/// Eğitim API testleri (T5E.3, SC-44, 04 §7.5): tracks/lessons/detay/progress/quiz.
/// **Per-user izolasyon (11 §3):** aynı derslerin durumu kullanıcıya göre değişir
/// (Investor'ın ilerlemesi Admin'e sızmaz); cevap-anahtarı ders detayında sızmaz,
/// yalnız deneme sonucunda açılır. Kilit ön-koşuldan türetilir. SQLite fixture (09 §2).
/// </summary>
public sealed class EducationApiTests : IClassFixture<SqliteWebApplicationFactory>, IAsyncLifetime
{
    private readonly SqliteWebApplicationFactory _factory;

    private static readonly Guid Investor = SeedData.Id("user-1"); // seed'de ders 1-3 Tamam, 4 Devam
    private static readonly Guid Admin = SeedData.Id("admin-1");    // seed'de ilerleme YOK

    private static readonly Guid Lesson1 = SeedData.Id("lesson-enflasyon");
    private static readonly Guid Quiz = SeedData.Id("quiz-enflasyon");
    private static readonly Guid Q1 = SeedData.Id("quiz-enflasyon-q1");
    private static readonly Guid Q1Correct = SeedData.Id("q1-o3");
    private static readonly Guid Q2 = SeedData.Id("quiz-enflasyon-q2");
    private static readonly Guid Q2Correct = SeedData.Id("q2-o2");
    private static readonly Guid Q3 = SeedData.Id("quiz-enflasyon-q3");
    private static readonly Guid Q3Correct = SeedData.Id("q3-o2");
    private static readonly Guid Q3Wrong = SeedData.Id("q3-o1");

    public EducationApiTests(SqliteWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedData.SeedAsync(db);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient ClientAs(Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", userId.ToString());
        return client;
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage resp) =>
        JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement.Clone();

    private static async Task<string?> ErrorCodeAsync(HttpResponseMessage resp)
    {
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("error").GetProperty("code").GetString();
    }

    private static List<JsonElement> ByOrder(JsonElement lessons) =>
        lessons.EnumerateArray().OrderBy(l => l.GetProperty("order").GetInt32()).ToList();

    /// <summary>Paylaşılan SQLite'a taze bir kullanıcı ekler → mutasyon testleri seed
    /// kullanıcılarının (Investor/Admin) durumunu kirletmez (sıradan bağımsız).</summary>
    private async Task<Guid> NewLearnerAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
        var user = new User
        {
            DisplayName = "Test Öğrenci",
            BaseCurrency = CurrencyCode.TRY,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    // ── İçerik (herkese açık) ────────────────────────────────────────────────

    [Fact]
    public async Task Tracks_lists_temeller_with_lesson_count()
    {
        var resp = await ClientAs(Investor).GetAsync("/api/education/tracks");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var arr = await JsonAsync(resp);
        arr.GetArrayLength().Should().Be(1);
        arr[0].GetProperty("slug").GetString().Should().Be("temeller");
        arr[0].GetProperty("level").GetString().Should().Be("Beginner");
        arr[0].GetProperty("lessonCount").GetInt32().Should().Be(5);
    }

    [Fact]
    public async Task Unknown_track_is_404()
    {
        var resp = await ClientAs(Investor).GetAsync("/api/education/tracks/yok-boyle/lessons");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ErrorCodeAsync(resp)).Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task Lesson_detail_has_quiz_without_leaking_answers()
    {
        var resp = await ClientAs(Investor).GetAsync("/api/education/lessons/enflasyon-ve-reel-getiri");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var raw = await resp.Content.ReadAsStringAsync();
        raw.Should().NotContain("isCorrect"); // şık doğruluğu hiç serileşmez

        var lesson = JsonDocument.Parse(raw).RootElement;
        lesson.GetProperty("bodyMarkdown").GetString().Should().NotBeNullOrEmpty();
        lesson.GetProperty("status").GetString().Should().Be("Completed"); // Investor ders 1'i tamamladı
        lesson.GetProperty("locked").GetBoolean().Should().BeFalse();
        var quiz = lesson.GetProperty("quiz");
        quiz.GetProperty("passingScore").GetInt32().Should().Be(60);
        quiz.GetProperty("questions").GetArrayLength().Should().Be(3);

        // Cevap-anahtarı YAPISAL olarak yok: soruda `explanation`, şıkta `isCorrect` alanı bulunmaz
        // (yalnız deneme SONUCUNDA açılır). Dersin gövdesinde geçen ifadelerle karışmayan kesin kontrol.
        var q0 = quiz.GetProperty("questions")[0];
        q0.TryGetProperty("explanation", out _).Should().BeFalse();
        q0.GetProperty("options")[0].TryGetProperty("isCorrect", out _).Should().BeFalse();

        lesson.GetProperty("conceptTags").EnumerateArray()
            .Select(t => t.GetProperty("key").GetString()).Should().Contain("real-return");
    }

    [Fact]
    public async Task Lesson_without_sections_falls_back_to_body_markdown()
    {
        // SC-E2 (T6.5) — geriye dönük uyum: seed'lenmiş 5 dersin hiç `LessonSection`'ı
        // yok. Katmanlı şema eklendikten SONRA da bu dersler kırılmamalı: `sections`
        // boş dizi döner ve istemci `bodyMarkdown`'a düşer.
        var lesson = await JsonAsync(
            await ClientAs(Investor).GetAsync("/api/education/lessons/enflasyon-ve-reel-getiri"));

        lesson.GetProperty("sections").GetArrayLength().Should().Be(0);
        lesson.GetProperty("bodyMarkdown").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Lesson_sections_expose_depth_tier_and_kind()
    {
        // T6.5 — katmanlı bölümler API'ye derinlik + tür bilgisiyle çıkar; sıralama
        // OrderIndex'e sadıktır (filtreleme YOK — seviyeye göre katlama istemcide, T6.7).
        Guid lessonId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
            var lesson = await db.Lessons.SingleAsync(l => l.Slug == "bilesik-getirinin-gucu");
            lessonId = lesson.Id;

            db.LessonSections.AddRange(
                new LessonSection
                {
                    LessonId = lessonId,
                    OrderIndex = 1,
                    Heading = "Özü",
                    BodyMarkdown = "Çekirdek anlatım",
                    DepthTier = DepthTier.Core,
                    Kind = SectionKind.Explain,
                },
                new LessonSection
                {
                    LessonId = lessonId,
                    OrderIndex = 2,
                    Heading = "Sık yapılan hata",
                    BodyMarkdown = "Tuzak metni",
                    DepthTier = DepthTier.Context,
                    Kind = SectionKind.Trap,
                });
            await db.SaveChangesAsync();
        }

        var detail = await JsonAsync(
            await ClientAs(Investor).GetAsync("/api/education/lessons/bilesik-getirinin-gucu"));

        var sections = detail.GetProperty("sections");
        sections.GetArrayLength().Should().Be(2);
        sections[0].GetProperty("depthTier").GetString().Should().Be("Core");
        sections[0].GetProperty("kind").GetString().Should().Be("Explain");
        sections[1].GetProperty("depthTier").GetString().Should().Be("Context");
        sections[1].GetProperty("kind").GetString().Should().Be("Trap");
        sections[1].GetProperty("heading").GetString().Should().Be("Sık yapılan hata");
    }

    [Fact]
    public async Task Lessons_by_concept_returns_tagged_lessons_and_empty_for_unknown()
    {
        var tagged = await JsonAsync(await ClientAs(Investor).GetAsync("/api/education/lessons/by-concept/diversification"));
        tagged.GetArrayLength().Should().Be(1);
        tagged[0].GetProperty("slug").GetString().Should().Be("cesitlendirme-neden-onemli");

        var empty = await JsonAsync(await ClientAs(Investor).GetAsync("/api/education/lessons/by-concept/yok-kavram"));
        empty.GetArrayLength().Should().Be(0);
    }

    // ── Per-user durum + kilit türetimi + İZOLASYON (11 §3) ──────────────────

    [Fact]
    public async Task Track_lessons_reflect_investor_progress_and_derived_lock()
    {
        var lessons = ByOrder(await JsonAsync(
            await ClientAs(Investor).GetAsync("/api/education/tracks/temeller/lessons")));

        lessons.Should().HaveCount(5);
        lessons[0].GetProperty("status").GetString().Should().Be("Completed");
        lessons[0].GetProperty("progressPercent").GetInt32().Should().Be(100);
        lessons[0].GetProperty("locked").GetBoolean().Should().BeFalse();
        lessons[2].GetProperty("status").GetString().Should().Be("Completed");
        lessons[3].GetProperty("status").GetString().Should().Be("InProgress");
        lessons[3].GetProperty("locked").GetBoolean().Should().BeFalse();  // ders 3 tamam → 4 açık
        lessons[4].GetProperty("status").GetString().Should().Be("NotStarted");
        lessons[4].GetProperty("locked").GetBoolean().Should().BeTrue();   // ders 4 tamam değil → 5 kilitli
    }

    [Fact]
    public async Task Track_lessons_for_user_without_progress_are_isolated()
    {
        // Admin'in HİÇ ilerlemesi yok → aynı dersler ona farklı görünür (izolasyon):
        // hepsi NotStarted; ders 1 kilitsiz (ön-koşulsuz), 2..5 ön-koşuldan kilitli.
        var lessons = ByOrder(await JsonAsync(
            await ClientAs(Admin).GetAsync("/api/education/tracks/temeller/lessons")));

        lessons.Should().OnlyContain(l => l.GetProperty("status").GetString() == "NotStarted");
        lessons[0].GetProperty("locked").GetBoolean().Should().BeFalse();
        lessons[1].GetProperty("locked").GetBoolean().Should().BeTrue();
        lessons[4].GetProperty("locked").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Update_progress_upserts_for_current_user_unlocks_next_and_stays_isolated()
    {
        // Taze kullanıcı (ilerlemesi yok) → seed kullanıcılarını kirletmez.
        var learner = ClientAs(await NewLearnerAsync());

        // Başta: ders 1 açık, ders 2 kilitli.
        var before = ByOrder(await JsonAsync(await learner.GetAsync("/api/education/tracks/temeller/lessons")));
        before[0].GetProperty("locked").GetBoolean().Should().BeFalse();
        before[1].GetProperty("locked").GetBoolean().Should().BeTrue();

        var put = await learner.PutAsJsonAsync($"/api/education/lessons/{Lesson1}/progress",
            new { status = "Completed", progressPercent = 100 });
        put.StatusCode.Should().Be(HttpStatusCode.OK);
        (await JsonAsync(put)).GetProperty("status").GetString().Should().Be("Completed");

        // Ders 1 tamamlandı → ders 2 açıldı (bu kullanıcı için).
        var after = ByOrder(await JsonAsync(await learner.GetAsync("/api/education/tracks/temeller/lessons")));
        after[0].GetProperty("status").GetString().Should().Be("Completed");
        after[1].GetProperty("locked").GetBoolean().Should().BeFalse();

        // İZOLASYON: taze kullanıcının yazımı Investor'ın verisine dokunmaz.
        var invLessons = ByOrder(await JsonAsync(
            await ClientAs(Investor).GetAsync("/api/education/tracks/temeller/lessons")));
        invLessons[3].GetProperty("status").GetString().Should().Be("InProgress");
        invLessons[4].GetProperty("locked").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Progress_percent_out_of_range_is_400()
    {
        var resp = await ClientAs(Investor).PutAsJsonAsync($"/api/education/lessons/{Lesson1}/progress",
            new { status = "InProgress", progressPercent = 150 });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorCodeAsync(resp)).Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task Progress_on_unknown_lesson_is_404()
    {
        var resp = await ClientAs(Investor).PutAsJsonAsync($"/api/education/lessons/{Guid.NewGuid()}/progress",
            new { status = "Completed", progressPercent = 100 });
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Quiz değerlendirme ───────────────────────────────────────────────────

    [Fact]
    public async Task Quiz_attempt_all_correct_scores_100_and_reveals_explanations()
    {
        var body = new
        {
            answers = new[]
            {
                new { questionId = Q1, selectedOptionIds = new[] { Q1Correct } },
                new { questionId = Q2, selectedOptionIds = new[] { Q2Correct } },
                new { questionId = Q3, selectedOptionIds = new[] { Q3Correct } },
            },
        };
        var resp = await ClientAs(Investor).PostAsJsonAsync($"/api/education/quizzes/{Quiz}/attempts", body);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await JsonAsync(resp);
        result.GetProperty("score").GetInt32().Should().Be(100);
        result.GetProperty("passed").GetBoolean().Should().BeTrue();
        var results = result.GetProperty("results");
        results.GetArrayLength().Should().Be(3);
        results[0].GetProperty("correct").GetBoolean().Should().BeTrue();
        results[0].GetProperty("explanation").GetString().Should().NotBeNullOrEmpty(); // deneme sonucu açar
        results[0].GetProperty("correctOptionIds").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Quiz_attempt_partial_scores_proportionally()
    {
        var body = new
        {
            answers = new[]
            {
                new { questionId = Q1, selectedOptionIds = new[] { Q1Correct } },
                new { questionId = Q2, selectedOptionIds = new[] { Q2Correct } },
                new { questionId = Q3, selectedOptionIds = new[] { Q3Wrong } }, // 1 yanlış
            },
        };
        var result = await JsonAsync(
            await ClientAs(Investor).PostAsJsonAsync($"/api/education/quizzes/{Quiz}/attempts", body));
        result.GetProperty("score").GetInt32().Should().Be(67); // 2/3 → 66,67 → 67
        result.GetProperty("passed").GetBoolean().Should().BeTrue(); // 67 ≥ 60
    }

    [Fact]
    public async Task Quiz_attempt_on_unknown_quiz_is_404()
    {
        var resp = await ClientAs(Investor).PostAsJsonAsync($"/api/education/quizzes/{Guid.NewGuid()}/attempts",
            new { answers = Array.Empty<object>() });
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

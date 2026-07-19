using FluentAssertions;
using Finans.Domain.Enums;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;

namespace Finans.Integration.Tests;

/// <summary>
/// Eğitim seed içeriği doğrulaması (T5E.2, SC-43, 03 §12.5). "Temeller" track'i +
/// 5 ders (sıralı ön-koşul zinciri) + Ders 1 mini testi (3 soru) + örnek ilerleme
/// (User#1: 1-3 Tamamlandı, 4 Devam, 5 türetilmiş Kilitli). Seed = test fixture (09 §2).
/// EF InMemory ile sağlayıcısız koşar; şema kısıtları ayrıca EducationModelTests'te (SQLite).
/// </summary>
public sealed class EducationSeedTests
{
    private static FinansDbContext NewContext() =>
        new(new DbContextOptionsBuilder<FinansDbContext>()
            .UseInMemoryDatabase($"edu-seed-{Guid.CreateVersion7()}")
            .Options);

    [Fact]
    public async Task Track_and_lessons_match_draft()
    {
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        var track = await db.LearningTracks.SingleAsync();
        track.Slug.Should().Be("temeller");
        track.Title.Should().Be("Temeller");
        track.Level.Should().Be(LessonLevel.Beginner);
        track.IsPublished.Should().BeTrue();

        var lessons = await db.Lessons.Where(l => l.TrackId == track.Id)
            .OrderBy(l => l.OrderIndex).ToListAsync();

        lessons.Select(l => l.Slug).Should().ContainInOrder(
            "enflasyon-ve-reel-getiri",
            "cesitlendirme-neden-onemli",
            "fk-pddd-nedir",
            "risk-ve-getiri-iliskisi",
            "bilesik-getirinin-gucu");
        lessons.Select(l => l.OrderIndex).Should().Equal(1, 2, 3, 4, 5);
        lessons.Select(l => l.EstimatedMinutes).Should().Equal(4, 5, 6, 5, 5);
        lessons.Should().OnlyContain(l => l.IsPublished && l.EstimatedMinutes > 0
            && l.Summary.Length > 0 && l.BodyMarkdown.Length > 0);
    }

    [Fact]
    public async Task Prerequisite_chain_is_sequential()
    {
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        // Her ders (2..5) yalnız bir öncekini ön-koşul ister; Ders 1'in ön-koşulu yok.
        var lessons = await db.Lessons.OrderBy(l => l.OrderIndex).ToListAsync();
        var prereqs = await db.LessonPrerequisites.ToListAsync();

        prereqs.Should().HaveCount(4);
        for (var i = 1; i < lessons.Count; i++)
        {
            prereqs.Should().ContainSingle(p =>
                p.LessonId == lessons[i].Id && p.PrerequisiteLessonId == lessons[i - 1].Id);
        }
        prereqs.Should().NotContain(p => p.LessonId == lessons[0].Id); // Ders 1 kilitsiz
    }

    [Fact]
    public async Task Concept_tags_link_lessons_to_glossary_keys()
    {
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        (await db.ConceptTags.Select(t => t.Key).ToListAsync())
            .Should().BeEquivalentTo(new[]
            {
                "real-return", "diversification", "pe-ratio", "pb-ratio", "risk-return", "compound",
            });

        // F/K dersi iki etikete bağlı (F/K + PD/DD); diğerleri tekil.
        var fkLesson = await db.Lessons.SingleAsync(l => l.Slug == "fk-pddd-nedir");
        var fkTagKeys = await db.LessonConceptTags
            .Where(lt => lt.LessonId == fkLesson.Id)
            .Join(db.ConceptTags, lt => lt.ConceptTagId, t => t.Id, (_, t) => t.Key)
            .ToListAsync();
        fkTagKeys.Should().BeEquivalentTo(new[] { "pe-ratio", "pb-ratio" });
    }

    [Fact]
    public async Task Lesson_one_quiz_has_three_questions_each_with_one_correct_option()
    {
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        var lesson1 = await db.Lessons.SingleAsync(l => l.Slug == "enflasyon-ve-reel-getiri");
        var quiz = await db.Quizzes.SingleAsync(q => q.LessonId == lesson1.Id);
        quiz.PassingScore.Should().Be(60);

        var questions = await db.QuizQuestions.Where(q => q.QuizId == quiz.Id)
            .OrderBy(q => q.OrderIndex).ToListAsync();
        questions.Should().HaveCount(3);
        questions.Should().OnlyContain(q => q.Explanation.Length > 0); // her soruda eğitici açıklama

        foreach (var question in questions)
        {
            var options = await db.QuizOptions.Where(o => o.QuestionId == question.Id).ToListAsync();
            options.Should().HaveCountGreaterThanOrEqualTo(2);
            options.Count(o => o.IsCorrect).Should().Be(1); // tam bir doğru cevap
        }

        // Bağımsız (derse bağlı olmayan) test yok — her quiz bir derse bağlı.
        // T6.1 ile quiz sayısı 1 → 5 oldu (2-5. dersler de test aldı).
        (await db.Quizzes.CountAsync()).Should().Be(5);
        (await db.Quizzes.CountAsync(q => q.LessonId == null)).Should().Be(0);
    }

    [Fact]
    public async Task Sample_progress_leaves_lesson_five_locked_by_derivation()
    {
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        var userId = SeedData.Id("user-1");
        var lessons = await db.Lessons.OrderBy(l => l.OrderIndex).ToListAsync();
        var progress = await db.UserLessonProgress
            .Where(p => p.UserId == userId).ToListAsync();

        // 1-3 Tamamlandı (%100), 4 Devam (%0), 5 için KAYIT YOK (kilit türetilir).
        progress.Should().HaveCount(4);
        progress.Single(p => p.LessonId == lessons[0].Id).Status.Should().Be(LessonStatus.Completed);
        progress.Single(p => p.LessonId == lessons[1].Id).Status.Should().Be(LessonStatus.Completed);
        progress.Single(p => p.LessonId == lessons[2].Id).Status.Should().Be(LessonStatus.Completed);
        progress.Where(p => p.Status == LessonStatus.Completed).Should().OnlyContain(p => p.ProgressPercent == 100);

        var l4 = progress.Single(p => p.LessonId == lessons[3].Id);
        l4.Status.Should().Be(LessonStatus.InProgress);
        l4.ProgressPercent.Should().Be(0);
        l4.CompletedAtUtc.Should().BeNull();

        // Ders 5: ilerleme kaydı yok + ön-koşulu (Ders 4) tamamlanmadı → türetilmiş Kilitli.
        progress.Should().NotContain(p => p.LessonId == lessons[4].Id);
        l4.Status.Should().NotBe(LessonStatus.Completed);
    }

    [Fact]
    public async Task Education_seed_is_idempotent()
    {
        await using var db = NewContext();
        await SeedData.SeedAsync(db);
        await SeedData.SeedAsync(db); // ikinci çağrı çoğaltmamalı

        (await db.LearningTracks.CountAsync()).Should().Be(1);
        (await db.Lessons.CountAsync()).Should().Be(5);
        (await db.ConceptTags.CountAsync()).Should().Be(6);
        (await db.LessonConceptTags.CountAsync()).Should().Be(6);
        (await db.LessonPrerequisites.CountAsync()).Should().Be(4);
        (await db.Quizzes.CountAsync()).Should().Be(5);          // T6.1: 1 → 5 (her derse bir test)
        (await db.QuizQuestions.CountAsync()).Should().Be(15);    // 5 × 3
        (await db.QuizOptions.CountAsync()).Should().Be(50);      // 5 × (4+2+4)
        (await db.LessonSections.CountAsync()).Should().Be(25);   // T6.1: 5 ders × 5 blok
        (await db.UserLessonProgress.CountAsync()).Should().Be(4);
    }

    // ── T6.1: katmanlı içerik (SC-E12) ───────────────────────────────────────

    [Fact]
    public async Task Every_lesson_has_full_depth_ladder_and_example_and_trap()
    {
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        var lessons = await db.Lessons.OrderBy(l => l.OrderIndex).ToListAsync();
        var sections = await db.LessonSections.ToListAsync();

        foreach (var lesson in lessons)
        {
            var own = sections.Where(s => s.LessonId == lesson.Id).OrderBy(s => s.OrderIndex).ToList();

            // Derinlik merdiveni eksiksiz: her ders üç katmanda da anlatım taşır.
            own.Should().Contain(s => s.DepthTier == DepthTier.Core && s.Kind == SectionKind.Explain,
                $"'{lesson.Slug}' L1 Core anlatımı taşımalı");
            own.Should().Contain(s => s.DepthTier == DepthTier.Context && s.Kind == SectionKind.Explain,
                $"'{lesson.Slug}' L2 Context anlatımı taşımalı");
            own.Should().Contain(s => s.DepthTier == DepthTier.Deep && s.Kind == SectionKind.Explain,
                $"'{lesson.Slug}' L3 Deep anlatımı taşımalı");

            // Dik eksen: jenerik örnek + tuzak blokları.
            own.Should().Contain(s => s.Kind == SectionKind.Example, $"'{lesson.Slug}' jenerik örnek taşımalı");
            own.Should().Contain(s => s.Kind == SectionKind.Trap, $"'{lesson.Slug}' tuzak bloğu taşımalı");

            own.Select(s => s.OrderIndex).Should().Equal(1, 2, 3, 4, 5);
            own.Should().OnlyContain(s => s.BodyMarkdown.Length > 100); // boş/yer tutucu içerik yok
        }
    }

    [Fact]
    public async Task Lesson_content_uses_only_supported_markdown_and_avoids_advice()
    {
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        var bodies = await db.LessonSections.Select(s => s.BodyMarkdown).ToListAsync();

        foreach (var body in bodies)
        {
            // MiniMarkdown alt kümesi: tablo/link/kod bloğu render EDİLMEZ (T6.8'e kadar).
            body.Should().NotContain("|", "tablo MiniMarkdown'da desteklenmiyor");
            body.Should().NotContain("](", "link MiniMarkdown'da desteklenmiyor");
            body.Should().NotContain("```", "kod bloğu MiniMarkdown'da desteklenmiyor");

            // Başlıklar yalnız ## / ### (h1/h2 yok).
            foreach (var line in body.Split('\n').Where(l => l.TrimStart().StartsWith('#')))
                line.TrimStart().Should().MatchRegex("^#{2,3} ");
        }

        // CLAUDE.md §2 — yönlendirme fiilleri içerikte geçmemeli (tavsiye YOK).
        var all = string.Join("\n", bodies);
        foreach (var banned in new[] { "almalısın", "satmalısın", "tavsiye ederiz", "öneririz", "yükselecek", "düşecek" })
            all.Should().NotContainEquivalentOf(banned, $"eğitim içeriği tavsiye vermez (yasak ifade: {banned})");
    }

    [Fact]
    public async Task Section_seed_backfills_databases_that_already_have_lessons()
    {
        // Gerçek dünya senaryosu: eğitim T5E.2 ile gelmiş, bölümler YOK (canlı kurulum).
        // SeedEducationAsync "track var mı?" kapısıyla korunduğu için oradan bölüm gelmez;
        // ayrı kapı sayesinde ikinci açılışta katmanlı içerik geriye dönük yüklenmeli.
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        db.LessonSections.RemoveRange(await db.LessonSections.ToListAsync());
        db.Quizzes.RemoveRange(await db.Quizzes.Where(q => q.Id != SeedData.Id("quiz-enflasyon")).ToListAsync());
        await db.SaveChangesAsync();
        (await db.LessonSections.CountAsync()).Should().Be(0);

        await SeedData.SeedAsync(db); // "bir sonraki açılış"

        (await db.LessonSections.CountAsync()).Should().Be(25);
        (await db.Quizzes.CountAsync()).Should().Be(5);
        (await db.Lessons.CountAsync()).Should().Be(5); // dersler çoğaltılmadı
    }

    [Fact]
    public async Task Every_lesson_has_a_quiz_with_three_scorable_questions()
    {
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        var lessons = await db.Lessons.ToListAsync();
        var quizzes = await db.Quizzes.ToListAsync();
        var questions = await db.QuizQuestions.ToListAsync();
        var options = await db.QuizOptions.ToListAsync();

        foreach (var lesson in lessons)
        {
            var quiz = quizzes.Should().ContainSingle(q => q.LessonId == lesson.Id).Subject;
            quiz.PassingScore.Should().Be(60);

            var own = questions.Where(q => q.QuizId == quiz.Id).OrderBy(q => q.OrderIndex).ToList();
            own.Should().HaveCount(3);
            own.Select(q => q.OrderIndex).Should().Equal(1, 2, 3);

            foreach (var q in own)
            {
                q.Explanation.Should().NotBeNullOrWhiteSpace("her soru eğitici açıklama taşır");
                var opts = options.Where(o => o.QuestionId == q.Id).ToList();
                opts.Should().HaveCountGreaterThanOrEqualTo(2);
                opts.Count(o => o.IsCorrect).Should().Be(1, "tek doğru şık (tam-eşleşme puanlaması)");
            }
        }
    }
}

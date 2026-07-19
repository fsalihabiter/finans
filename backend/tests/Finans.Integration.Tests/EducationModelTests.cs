using FluentAssertions;
using Finans.Domain.Education;
using Finans.Domain.Enums;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Finans.Integration.Tests;

/// <summary>
/// Eğitim şeması bütünlük testleri (T5E.1, SC-38): unique slug, ilerleme yüzdesi CHECK,
/// kendine ön-koşul yasağı, track→ders kaskad silme. Model SQLite fixture'ında da
/// aynı kısıtlarla ayağa kalkmalı (09 §2).
/// </summary>
public sealed class EducationModelTests : IClassFixture<SqliteWebApplicationFactory>, IAsyncLifetime
{
    private readonly SqliteWebApplicationFactory _factory;

    public EducationModelTests(SqliteWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedData.SeedAsync(db);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private FinansDbContext NewDb(out IServiceScope scope)
    {
        scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<FinansDbContext>();
    }

    private static LearningTrack NewTrack(string slug) => new()
    {
        Slug = slug,
        Title = "Test Track",
        Level = LessonLevel.Beginner,
        OrderIndex = 99,
        IsPublished = true,
    };

    private static Lesson NewLesson(Guid trackId, string slug, int order = 1) => new()
    {
        TrackId = trackId,
        Slug = slug,
        OrderIndex = order,
        Title = "Test Ders",
        Summary = "Özet",
        BodyMarkdown = "İçerik",
        EstimatedMinutes = 4,
        Level = LessonLevel.Beginner,
        IsPublished = true,
    };

    [Fact]
    public async Task Duplicate_lesson_slug_is_rejected()
    {
        var db = NewDb(out var scope);
        using (scope)
        {
            var track = NewTrack("t-slug-dup");
            db.LearningTracks.Add(track);
            db.Lessons.Add(NewLesson(track.Id, "ayni-slug", 1));
            db.Lessons.Add(NewLesson(track.Id, "ayni-slug", 2));

            var act = () => db.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateException>(); // Lessons.Slug UNIQUE (03 §14)
        }
    }

    [Fact]
    public async Task Progress_percent_over_100_violates_check()
    {
        var db = NewDb(out var scope);
        using (scope)
        {
            var track = NewTrack("t-progress");
            var lesson = NewLesson(track.Id, "progress-dersi");
            db.LearningTracks.Add(track);
            db.Lessons.Add(lesson);
            db.UserLessonProgress.Add(new UserLessonProgress
            {
                UserId = SeedData.Id("user-1"),
                LessonId = lesson.Id,
                Status = LessonStatus.InProgress,
                ProgressPercent = 101, // CHECK 0-100 ihlali
            });

            var act = () => db.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateException>();
        }
    }

    [Fact]
    public async Task Lesson_cannot_be_its_own_prerequisite()
    {
        var db = NewDb(out var scope);
        using (scope)
        {
            var track = NewTrack("t-self-prereq");
            var lesson = NewLesson(track.Id, "kendine-onkosul");
            db.LearningTracks.Add(track);
            db.Lessons.Add(lesson);
            db.LessonPrerequisites.Add(new LessonPrerequisite
            {
                LessonId = lesson.Id,
                PrerequisiteLessonId = lesson.Id, // kilit hiç açılamazdı — CHECK engeller
            });

            var act = () => db.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateException>();
        }
    }

    // ── T6.5: katmanlı içerik şeması (15 §2, SC-E2) ──────────────────────────

    [Fact]
    public async Task Section_defaults_to_core_explain_when_not_specified()
    {
        // Geriye dönük uyum: değer verilmeyen bölüm Core/Explain'e düşer —
        // eski içerik "herkesin gördüğü anlatım" olarak davranmaya devam eder.
        Guid sectionId;

        var db = NewDb(out var scope);
        using (scope)
        {
            var track = NewTrack("t-section-default");
            var lesson = NewLesson(track.Id, "varsayilan-bolum");
            var section = new LessonSection
            {
                LessonId = lesson.Id,
                OrderIndex = 1,
                BodyMarkdown = "Eski usul içerik", // DepthTier/Kind VERİLMEDİ
            };
            db.LearningTracks.Add(track);
            db.Lessons.Add(lesson);
            db.LessonSections.Add(section);
            await db.SaveChangesAsync();
            sectionId = section.Id;
        }

        var db2 = NewDb(out var scope2);
        using (scope2)
        {
            var saved = await db2.LessonSections.SingleAsync(s => s.Id == sectionId);
            saved.DepthTier.Should().Be(DepthTier.Core);
            saved.Kind.Should().Be(SectionKind.Explain);
        }
    }

    [Fact]
    public async Task Section_roundtrips_all_depth_tiers_and_kinds()
    {
        // Derinlik ve tür DİK eksenler: her kombinasyon saklanıp geri okunabilmeli.
        Guid lessonId;

        var db = NewDb(out var scope);
        using (scope)
        {
            var track = NewTrack("t-section-matrix");
            var lesson = NewLesson(track.Id, "katmanli-ders");
            db.LearningTracks.Add(track);
            db.Lessons.Add(lesson);

            var order = 1;
            foreach (var tier in Enum.GetValues<DepthTier>())
            {
                foreach (var kind in Enum.GetValues<SectionKind>())
                {
                    db.LessonSections.Add(new LessonSection
                    {
                        LessonId = lesson.Id,
                        OrderIndex = order++,
                        BodyMarkdown = $"{tier}/{kind}",
                        DepthTier = tier,
                        Kind = kind,
                    });
                }
            }

            await db.SaveChangesAsync();
            lessonId = lesson.Id;
        }

        var db2 = NewDb(out var scope2);
        using (scope2)
        {
            var sections = await db2.LessonSections
                .Where(s => s.LessonId == lessonId)
                .ToListAsync();

            var expected = Enum.GetValues<DepthTier>().Length * Enum.GetValues<SectionKind>().Length;
            sections.Should().HaveCount(expected);
            sections.Should().OnlyContain(s => s.BodyMarkdown == $"{s.DepthTier}/{s.Kind}");
        }
    }

    [Fact]
    public async Task Deleting_lesson_cascades_sections()
    {
        // KVKK/temizlik: ders silinince katmanlı içerik artık kalmaz.
        Guid lessonId;
        Guid sectionId;

        var db = NewDb(out var scope);
        using (scope)
        {
            var track = NewTrack("t-section-cascade");
            var lesson = NewLesson(track.Id, "kaskad-bolum-dersi");
            var section = new LessonSection
            {
                LessonId = lesson.Id,
                OrderIndex = 1,
                BodyMarkdown = "Derin katman",
                DepthTier = DepthTier.Deep,
                Kind = SectionKind.Trap,
            };
            db.LearningTracks.Add(track);
            db.Lessons.Add(lesson);
            db.LessonSections.Add(section);
            await db.SaveChangesAsync();
            (lessonId, sectionId) = (lesson.Id, section.Id);
        }

        var db2 = NewDb(out var scope2);
        using (scope2)
        {
            db2.Lessons.Remove(await db2.Lessons.SingleAsync(l => l.Id == lessonId));
            await db2.SaveChangesAsync();

            (await db2.LessonSections.AnyAsync(s => s.Id == sectionId)).Should().BeFalse();
        }
    }

    [Fact]
    public async Task Deleting_track_cascades_lessons_and_quiz()
    {
        Guid trackId;
        Guid lessonId;
        Guid quizId;

        var db = NewDb(out var scope);
        using (scope)
        {
            var track = NewTrack("t-cascade");
            var lesson = NewLesson(track.Id, "kaskad-dersi");
            var quiz = new Quiz { LessonId = lesson.Id, Title = "Mini Test", PassingScore = 60 };
            db.LearningTracks.Add(track);
            db.Lessons.Add(lesson);
            db.Quizzes.Add(quiz);
            await db.SaveChangesAsync();
            (trackId, lessonId, quizId) = (track.Id, lesson.Id, quiz.Id);
        }

        var db2 = NewDb(out var scope2);
        using (scope2)
        {
            db2.LearningTracks.Remove(await db2.LearningTracks.SingleAsync(t => t.Id == trackId));
            await db2.SaveChangesAsync();

            (await db2.Lessons.AnyAsync(l => l.Id == lessonId)).Should().BeFalse();
            (await db2.Quizzes.AnyAsync(q => q.Id == quizId)).Should().BeFalse();
        }
    }
}

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

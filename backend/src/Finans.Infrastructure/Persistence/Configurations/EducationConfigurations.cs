using Finans.Domain.Education;
using Finans.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finans.Infrastructure.Persistence.Configurations;

// Eğitim modülü şeması (03 §C, T5E.1). İndeks/unique/CHECK kuralları 03 §14 ile birebir.

internal sealed class LearningTrackConfiguration : IEntityTypeConfiguration<LearningTrack>
{
    public void Configure(EntityTypeBuilder<LearningTrack> b)
    {
        b.ToTable("LearningTracks", t =>
            t.HasCheckConstraint("CK_LearningTracks_Level", Check.EnumIn<LessonLevel>("Level")));
        b.Property(x => x.Slug).HasMaxLength(80).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
    }
}

internal sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> b)
    {
        b.ToTable("Lessons", t =>
        {
            t.HasCheckConstraint("CK_Lessons_Level", Check.EnumIn<LessonLevel>("Level"));
            t.HasCheckConstraint("CK_Lessons_EstimatedMinutes", "\"EstimatedMinutes\" > 0");
        });
        b.Property(x => x.Slug).HasMaxLength(80).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => new { x.TrackId, x.OrderIndex });

        b.HasOne(x => x.Track)
            .WithMany(t => t.Lessons)
            .HasForeignKey(x => x.TrackId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class LessonSectionConfiguration : IEntityTypeConfiguration<LessonSection>
{
    public void Configure(EntityTypeBuilder<LessonSection> b)
    {
        b.ToTable("LessonSections");
        b.HasIndex(x => new { x.LessonId, x.OrderIndex });

        b.HasOne(x => x.Lesson)
            .WithMany(l => l.Sections)
            .HasForeignKey(x => x.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class LessonPrerequisiteConfiguration : IEntityTypeConfiguration<LessonPrerequisite>
{
    public void Configure(EntityTypeBuilder<LessonPrerequisite> b)
    {
        b.ToTable("LessonPrerequisites", t =>
            // Kendi kendine ön-koşul anlamsız (kilit hiç açılamaz) — DB seviyesinde engelle.
            t.HasCheckConstraint("CK_LessonPrerequisites_NoSelf", "\"LessonId\" <> \"PrerequisiteLessonId\""));
        b.HasKey(x => new { x.LessonId, x.PrerequisiteLessonId });

        b.HasOne(x => x.Lesson)
            .WithMany(l => l.Prerequisites)
            .HasForeignKey(x => x.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ön-koşul ders silinirse bağ da düşer; ama Lesson→Prerequisites cascade'iyle çift yol
        // (multiple cascade path) oluşmasın diye Restrict — silme akışı önce bağları temizler.
        b.HasOne(x => x.PrerequisiteLesson)
            .WithMany()
            .HasForeignKey(x => x.PrerequisiteLessonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ConceptTagConfiguration : IEntityTypeConfiguration<ConceptTag>
{
    public void Configure(EntityTypeBuilder<ConceptTag> b)
    {
        b.ToTable("ConceptTags");
        b.Property(x => x.Key).HasMaxLength(60).IsRequired();
        b.Property(x => x.Label).HasMaxLength(120).IsRequired();
        b.HasIndex(x => x.Key).IsUnique();
    }
}

internal sealed class LessonConceptTagConfiguration : IEntityTypeConfiguration<LessonConceptTag>
{
    public void Configure(EntityTypeBuilder<LessonConceptTag> b)
    {
        b.ToTable("LessonConceptTags");
        b.HasKey(x => new { x.LessonId, x.ConceptTagId });

        b.HasOne(x => x.Lesson)
            .WithMany(l => l.ConceptTags)
            .HasForeignKey(x => x.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.ConceptTag)
            .WithMany(c => c.Lessons)
            .HasForeignKey(x => x.ConceptTagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class QuizConfiguration : IEntityTypeConfiguration<Quiz>
{
    public void Configure(EntityTypeBuilder<Quiz> b)
    {
        b.ToTable("Quizzes", t =>
            t.HasCheckConstraint("CK_Quizzes_PassingScore", "\"PassingScore\" BETWEEN 0 AND 100"));
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        // Derse en fazla bir quiz (1:0..1); LessonId null = bağımsız test.
        b.HasIndex(x => x.LessonId).IsUnique();

        b.HasOne(x => x.Lesson)
            .WithOne(l => l.Quiz)
            .HasForeignKey<Quiz>(x => x.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class QuizQuestionConfiguration : IEntityTypeConfiguration<QuizQuestion>
{
    public void Configure(EntityTypeBuilder<QuizQuestion> b)
    {
        b.ToTable("QuizQuestions", t =>
            t.HasCheckConstraint("CK_QuizQuestions_Type", Check.EnumIn<QuizQuestionType>("Type")));
        b.HasIndex(x => new { x.QuizId, x.OrderIndex });

        b.HasOne(x => x.Quiz)
            .WithMany(q => q.Questions)
            .HasForeignKey(x => x.QuizId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class QuizOptionConfiguration : IEntityTypeConfiguration<QuizOption>
{
    public void Configure(EntityTypeBuilder<QuizOption> b)
    {
        b.ToTable("QuizOptions");
        b.HasIndex(x => new { x.QuestionId, x.OrderIndex });

        b.HasOne(x => x.Question)
            .WithMany(q => q.Options)
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class UserLessonProgressConfiguration : IEntityTypeConfiguration<UserLessonProgress>
{
    public void Configure(EntityTypeBuilder<UserLessonProgress> b)
    {
        b.ToTable("UserLessonProgress", t =>
        {
            t.HasCheckConstraint("CK_UserLessonProgress_Status", Check.EnumIn<LessonStatus>("Status"));
            t.HasCheckConstraint("CK_UserLessonProgress_Percent", "\"ProgressPercent\" BETWEEN 0 AND 100");
        });
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => new { x.UserId, x.LessonId }).IsUnique();

        b.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade); // KVKK "verimi sil" (11 §7)

        b.HasOne(x => x.Lesson)
            .WithMany()
            .HasForeignKey(x => x.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class UserQuizAttemptConfiguration : IEntityTypeConfiguration<UserQuizAttempt>
{
    public void Configure(EntityTypeBuilder<UserQuizAttempt> b)
    {
        b.ToTable("UserQuizAttempts", t =>
            t.HasCheckConstraint("CK_UserQuizAttempts_Score", "\"Score\" BETWEEN 0 AND 100"));
        b.HasIndex(x => new { x.UserId, x.QuizId });

        b.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade); // KVKK "verimi sil" (11 §7)

        b.HasOne(x => x.Quiz)
            .WithMany()
            .HasForeignKey(x => x.QuizId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

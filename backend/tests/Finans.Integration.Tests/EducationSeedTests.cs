using System.Text.RegularExpressions;
using FluentAssertions;
using Finans.Application.Education;
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
        // T6.11 — üç zorluk kademesinde üçer soru.
        questions.Should().HaveCount(9);
        questions.GroupBy(q => q.Difficulty).Should().HaveCount(3);
        questions.Should().OnlyContain(q => q.Explanation.Length > 0); // her soruda eğitici açıklama

        foreach (var question in questions)
        {
            var options = await db.QuizOptions.Where(o => o.QuestionId == question.Id).ToListAsync();
            options.Should().HaveCountGreaterThanOrEqualTo(2);
            // Çoktan seçmelide birden çok doğru olabilir; tek seçimde tam bir doğru.
            var correct = options.Count(o => o.IsCorrect);
            if (question.Type == QuizQuestionType.MultipleChoice)
                correct.Should().BeGreaterThan(1);
            else
                correct.Should().Be(1);
        }

        // Bağımsız (derse bağlı olmayan) test yok — her quiz bir derse bağlı.
        // T6.1 ile quiz sayısı 1 → 5 oldu (2-5. dersler de test aldı).
        (await db.Quizzes.CountAsync()).Should().Be(5);
        (await db.Quizzes.CountAsync(q => q.LessonId == null)).Should().Be(0);
    }

    [Fact]
    public async Task Seed_starts_everyone_from_zero_progress()
    {
        // KARAR 2026-07-19: seed artık HİÇ ilerleme yazmaz. Önceden 1-3 "Tamamlandı"
        // geliyordu; kullanıcı okumadığı dersleri bitirmiş görünüyordu. Artık Ders 1
        // açık, 2-5 ön-koşuldan TÜRETİLMİŞ kilitli.
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        (await db.UserLessonProgress.CountAsync()).Should().Be(0);
        (await db.UserQuizAttempts.CountAsync()).Should().Be(0);
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
        (await db.QuizQuestions.CountAsync()).Should().Be(27);    // Ders1+2: 9+9 (3 zorluk) + 3×3
        (await db.QuizOptions.CountAsync()).Should().Be(94);      // Ders1+2: 32+32 · 3×10
        (await db.LessonSections.CountAsync()).Should().Be(44);   // Ders1+2: 13+13 + 3×6
        (await db.UserLessonProgress.CountAsync()).Should().Be(0);  // seed ilerleme yazmaz
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

            own.Should().Contain(s => s.Kind == SectionKind.LiveContext,
                $"'{lesson.Slug}' \"Senin portföyünde\" bağlam bloğu taşımalı (T6.2)");
            own.Select(s => s.OrderIndex).Should().BeInAscendingOrder();
            own.Select(s => s.OrderIndex).Should().Equal(Enumerable.Range(1, own.Count));
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
            // MiniMarkdown alt kümesi (T6.8 ile tablo + link EKLENDİ; kod bloğu hâlâ yok).
            body.Should().NotContain("```", "kod bloğu MiniMarkdown'da desteklenmiyor");

            // Başlıklar yalnız ## / ### (h1/h2 yok).
            foreach (var line in body.Split('\n').Where(l => l.TrimStart().StartsWith('#')))
                line.TrimStart().Should().MatchRegex("^#{2,3} ");

            // 🔒 Bağlantı hedefi güvenli şema olmalı. Renderer güvensiz hedefi zaten
            // bağlantıya çevirmez (safeHref) ama o durumda içerikte ham "[metin](...)"
            // görünür — yani bu aslında bir İÇERİK hatasıdır, burada yakalanır.
            foreach (Match link in Regex.Matches(body, @"\]\(([^)\s]+)\)"))
            {
                var target = link.Groups[1].Value;
                target.Should().MatchRegex(
                    @"^(?:https?://|mailto:|/(?!/))",
                    $"bağlantı hedefi güvenli şema kullanmalı (bulunan: {target})");
            }

            // Tablo yazıldıysa hizalama satırı ŞART — yoksa MiniMarkdown onu tablo
            // saymaz ve içerik ekranda düz paragraf olarak bozulur (sessiz hata).
            var lines = body.Split('\n').Select(l => l.Trim()).ToList();
            if (lines.Any(l => l.StartsWith('|')))
            {
                lines.Should().Contain(
                    l => Regex.IsMatch(l, @"^\|(?:\s*:?-+:?\s*\|)+$"),
                    "boru işaretli tablo hizalama satırı (| --- | --- |) olmadan render edilmez");
                lines[0].Should().NotMatchRegex(
                    @"^\|(?:\s*:?-+:?\s*\|)+$",
                    "hizalama satırı başlık satırından SONRA gelir");
            }
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

        (await db.LessonSections.CountAsync()).Should().Be(44);
        (await db.Quizzes.CountAsync()).Should().Be(5);
        (await db.Lessons.CountAsync()).Should().Be(5); // dersler çoğaltılmadı
    }

    [Fact]
    public async Task Section_seed_backfills_a_newly_added_block_type()
    {
        // REGRESYON (T6.2): içeriğe sonradan blok eklenince (LiveContext gibi) mevcut
        // kurulumlar da almalı. Kaba "hiç bölüm var mı?" kapısı bunu KAÇIRIRDI —
        // kapı blok bazında (deterministik Id) olduğu için yakalanıyor.
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        // "Eski sürüm" simülasyonu: LiveContext blokları henüz yokmuş gibi sil.
        var live = await db.LessonSections.Where(s => s.Kind == SectionKind.LiveContext).ToListAsync();
        live.Should().HaveCount(5);
        db.LessonSections.RemoveRange(live);
        await db.SaveChangesAsync();
        (await db.LessonSections.CountAsync()).Should().Be(39); // diğer bloklar yerinde

        await SeedData.SeedAsync(db); // "bir sonraki açılış"

        // Eksik blok tipi geriye dönük geldi, var olanlar çoğaltılmadı.
        (await db.LessonSections.CountAsync(s => s.Kind == SectionKind.LiveContext)).Should().Be(5);
        (await db.LessonSections.CountAsync()).Should().Be(44);
    }

    [Fact]
    public async Task Section_seed_reconciles_edited_content()
    {
        // REGRESYON (T6.7): içerik DÜZELTİLDİĞİNDE (metin/katman/figür) çalışan
        // kurulumlar da almalı. Salt "eksikse ekle" yaklaşımı bunu kaçırıyordu —
        // aynı Id'li blok var sayılıp güncelleme atlanıyordu.
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        var section = await db.LessonSections.FirstAsync(s => s.Kind == SectionKind.Trap);
        var (id, original, originalHeading) = (section.Id, section.BodyMarkdown, section.Heading);

        // "Eski sürüm" simülasyonu: içeriğin HER alanı bozulmuş/eskimiş.
        section.Heading = "eski başlık";
        section.BodyMarkdown = "eski metin";
        section.DepthTier = DepthTier.Deep;
        section.FigureKey = "eski-figur";
        await db.SaveChangesAsync();

        await SeedData.SeedAsync(db); // "bir sonraki açılış"

        var after = await db.LessonSections.SingleAsync(s => s.Id == id);
        // Heading de mutabakata dahil — karşılaştırma listesinden düşerse sessizce eskir.
        after.Heading.Should().Be(originalHeading);
        after.BodyMarkdown.Should().Be(original);
        after.DepthTier.Should().Be(DepthTier.Core, "tuzak bloğu başlangıç seviyesine de görünmeli");
        after.FigureKey.Should().BeNull();
        (await db.LessonSections.CountAsync()).Should().Be(44); // çoğaltma yok
    }

    [Fact]
    public async Task Every_section_has_a_heading_and_body_without_duplicate_title()
    {
        // T6.10 — yol haritası adım başlıklarını gösterir; başlık gövdeden AYRIŞTIRILIR
        // ki aynı başlık hem adım başlığında hem metinde iki kez görünmesin.
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        var sections = await db.LessonSections.ToListAsync();

        sections.Should().OnlyContain(s => !string.IsNullOrWhiteSpace(s.Heading),
            "her bölüm yol haritasında bir adım adı taşımalı");
        sections.Should().OnlyContain(s => !s.BodyMarkdown.StartsWith("## "),
            "başlık gövdeden düşürülmeli (çift gösterim olmaz)");
        sections.Should().OnlyContain(s => s.Heading!.Length > 3 && s.Heading!.Length < 60,
            "adım adı hem anlamlı hem tek satıra sığar olmalı");

        // Ders içi başlıklar birbirinden ayırt edilebilir olmalı (yol haritası okunur kalsın).
        foreach (var group in sections.GroupBy(s => s.LessonId))
            group.Select(s => s.Heading).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Example_blocks_declare_a_figure_and_traps_stay_in_core_tier()
    {
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        var sections = await db.LessonSections.ToListAsync();

        // Her dersin örnek bloğu bir figür anahtarı bildirir (T6.7 görselleştirme).
        var examples = sections.Where(s => s.Kind == SectionKind.Example).ToList();
        examples.Should().HaveCountGreaterThanOrEqualTo(5); // her derste ≥1 (zengin derslerde birkaç)

        // Her DERS en az bir görsel taşımalı; ama metin-ağırlıklı bazı örneklerin
        // figürü olmayabilir (görsel katkı sağlamıyorsa zorlamıyoruz).
        var lessonIds = sections.Select(s => s.LessonId).Distinct();
        foreach (var lessonId in lessonIds)
        {
            sections.Where(s => s.LessonId == lessonId)
                .Should().Contain(s => !string.IsNullOrWhiteSpace(s.FigureKey),
                    "her ders en az bir açıklayıcı görsel taşımalı");
        }

        // Figür anahtarları tekil — aynı görsel iki yerde çizilmemeli.
        sections.Where(s => s.FigureKey != null).Select(s => s.FigureKey)
            .Should().OnlyHaveUniqueItems();

        // Tuzak blokları Core katmanda → başlangıç seviyesinde KATLANMAZ.
        sections.Where(s => s.Kind == SectionKind.Trap)
            .Should().OnlyContain(s => s.DepthTier == DepthTier.Core);

        // Figür yalnız örnek bloklarında; anlatım blokları görselsiz.
        sections.Where(s => s.Kind == SectionKind.Explain)
            .Should().OnlyContain(s => s.FigureKey == null);
    }

    [Fact]
    public async Task Live_context_blocks_carry_resolvable_tokens()
    {
        // Bağlam şablonları {{anahtar}} token'ı taşımalı (T6.2) ve anahtarlar
        // ContextKeys'te TANIMLI olmalı — yazım hatası sessizce satır düşürürdü.
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        var known = typeof(ContextKeys)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToHashSet();

        var live = await db.LessonSections
            .Where(s => s.Kind == SectionKind.LiveContext).ToListAsync();

        live.Should().HaveCount(5);
        foreach (var s in live)
        {
            var tokens = System.Text.RegularExpressions.Regex
                .Matches(s.BodyMarkdown, @"\{\{\s*(?<key>[a-z0-9_]+)\s*\}\}")
                .Select(m => m.Groups["key"].Value)
                .ToList();

            tokens.Should().NotBeEmpty("her bağlam bloğu en az bir metrik göstermeli");
            tokens.Should().OnlyContain(t => known.Contains(t),
                "bilinmeyen token satırı sessizce düşürür (yazım hatası riski)");
        }
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
            own.Should().HaveCountGreaterThanOrEqualTo(3);
            own.Select(q => q.OrderIndex).Should().BeInAscendingOrder();
            // T6.11 — her testte en az bir KOLAY soru olmalı; aksi hâlde Başlangıç
            // seviyesi hiç soru görmez ve öğrenme kapısı hiç açılmaz.
            own.Should().Contain(q => q.Difficulty == QuizDifficulty.Easy);

            foreach (var q in own)
            {
                q.Explanation.Should().NotBeNullOrWhiteSpace("her soru eğitici açıklama taşır");
                var opts = options.Where(o => o.QuestionId == q.Id).ToList();
                opts.Should().HaveCountGreaterThanOrEqualTo(2);
                var correct = opts.Count(o => o.IsCorrect);
                if (q.Type == QuizQuestionType.MultipleChoice)
                    correct.Should().BeGreaterThan(1, "çoktan seçmelide birden çok doğru olur");
                else
                    correct.Should().Be(1, "tek seçimde tam bir doğru şık (tam-eşleşme puanlaması)");
            }
        }
    }
}

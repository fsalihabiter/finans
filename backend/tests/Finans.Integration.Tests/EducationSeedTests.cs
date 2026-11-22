using System.IO;
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

        // T6.16 — artık iki set var: "İlk Adımlar" (Set 0) + "Yatırım Kavramları"
        // (eski "Temeller"; slug KORUNDU, başlık yeniden adlandırıldı).
        var track = await db.LearningTracks.SingleAsync(t => t.Slug == "temeller");
        track.Title.Should().Be("Yatırım Kavramları");
        track.Level.Should().Be(LessonLevel.Beginner);
        track.IsPublished.Should().BeTrue();

        var set0 = await db.LearningTracks.SingleAsync(t => t.Slug == "ilk-adimlar");
        set0.Title.Should().Be("İlk Adımlar");
        set0.OrderIndex.Should().Be(0, "Set 0 giriş setidir — en başta gelir");
        track.OrderIndex.Should().BeGreaterThan(set0.OrderIndex);

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
        // T6.16 — zincir TRACK İÇİNDE kalır; Set 0 dersleri karışmasın diye track'e kapsanır.
        var temeller = await db.LearningTracks.SingleAsync(t => t.Slug == "temeller");
        var lessons = await db.Lessons.Where(l => l.TrackId == temeller.Id)
            .OrderBy(l => l.OrderIndex).ToListAsync();
        var lessonIds = lessons.Select(l => l.Id).ToHashSet();
        var prereqs = (await db.LessonPrerequisites.ToListAsync())
            .Where(p => lessonIds.Contains(p.LessonId)).ToList();

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
                "investment", // T6.16 — Set 0 Ders 1 "Yatırım nedir"
                "income-expense", "savings-rate", "pay-yourself-first", // Set 0 Ders 2 "Paranın haritası"
                "emergency-fund", "debt-cost", "opportunity-cost", // Set 0 Ders 3 "Acil durum fonu ve borç"
                "inflation", "purchasing-power", "price-index", // Set 0 Ders 4 "Bekleyen para neden erir"
                "asset-class", "ownership-vs-lending", "liquidity", // Set 0 Ders 5 "Varlık türleri turu"
                "return-source", "capital-gain", "cash-flow-return", // Set 0 Ders 6 "Getiri nereden gelir"
                "risk", "volatility", "guaranteed-return-fallacy", // Set 0 Ders 7 "Risk ne demek"
                "time-horizon", "portfolio", "position", "weight", // Set 0 Ders 8 "Vade, hedef ve portföy"
                "market", "supply-demand", "last-price", "bid-ask", "spread", // Set 0 Ders 9 "Fiyat nereden çıkıyor"
                "exchange", "broker", "order", "order-type", "settlement", "commission", // Set 0 Ders 10 "Bir alım nasıl gerçekleşir"
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
        // T6.1: 1→5 · T6.16 (Set 0 L1-L6): 5→11.
        (await db.Quizzes.CountAsync()).Should().Be(15);
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

        // T6.16 — Set 0: S0-L1..L6. +1 track, +6 ders, +16 kavram, +6 quiz
        // (54 soru, 192 seçenek), +78 bölüm, +5 ön-koşul (S0 zinciri).
        (await db.LearningTracks.CountAsync()).Should().Be(2);
        (await db.Lessons.CountAsync()).Should().Be(15);
        (await db.ConceptTags.CountAsync()).Should().Be(40);
        (await db.LessonConceptTags.CountAsync()).Should().Be(40);
        (await db.LessonPrerequisites.CountAsync()).Should().Be(13);
        (await db.Quizzes.CountAsync()).Should().Be(15);
        (await db.QuizQuestions.CountAsync()).Should().Be(117);   // + S0-L10 9
        (await db.QuizOptions.CountAsync()).Should().Be(418);     // + S0-L10 34 (1 doğru-yanlış 2 şıklı, 8×4)
        (await db.LessonSections.CountAsync()).Should().Be(187);  // + S0-L10 14
        (await db.UserLessonProgress.CountAsync()).Should().Be(0);  // seed ilerleme yazmaz
    }

    /// <summary>
    /// D-017 · <c>16-CURRICULUM.md</c>: "Senin portföyünde" (<see cref="SectionKind.LiveContext"/>)
    /// bloğu, künyede tanımlı olan derslerde ZORUNLUDUR. Aşağıdaki derslerin künyesi
    /// bilinçli olarak "LiveContext — yok" diyor; bu liste o beyanın makine karşılığıdır.
    ///
    /// <para><b>Liste büyüyorsa bu bir uyarı sinyalidir:</b> "Senin portföyünde" vaadi
    /// (<c>14</c> §4-A1) aşınıyor demektir — gözden geçir (D-017 Consequences).</para>
    ///
    /// <para>Bir derse uygun bir bağlam metriği geldiğinde: künyeyi güncelle, bloğu ekle,
    /// satırı BURADAN ÇIKAR. Test iki yönlü çalışır, unutulamaz.</para>
    /// </summary>
    private static readonly Dictionary<string, string> LessonsWithoutLiveContext = new()
    {
        ["risk-ne-demek"] = "S0-L7: risk kavramının karşılığı olan bir portföy metriği yok; "
            + "mevcut 9 ContextKey'den hiçbiri dürüstçe bu dersin kavramına bağlanmıyor",
        ["bir-alim-nasil-gerceklesir"] = "S0-L10: ders emir MEKANİĞİNİ anlatıyor (borsa · aracı "
            + "kurum · emir türü · takas); mevcut 9 ContextKey portföyün DURUMUNU ölçüyor, "
            + "işlem sürecini değil. Uydurma bir bağ kurmak D-017'nin kaçındığı şey. "
            + "⚠ Bu listenin İKİNCİ kaydı — Set 0'ın 10 dersinden 2'si bağlamsız kapanıyor "
            + "(GD-005 §3'te gerekçelendirildi, sinyal kayda geçti).",
    };

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

            // D-017: "Senin portföyünde" bloğunun zorunluluğu KÜNYEDEN gelir (16-CURRICULUM).
            // Künye bir ders için "LiveContext — yok (karşılığı bir metrik yok)" diyorsa
            // bloksuzluk sözleşmeye UYGUNDUR; istisna sessiz bir boşluk değil, burada
            // adıyla ve gerekçesiyle duran, testle doğrulanan bir beyandır.
            if (LessonsWithoutLiveContext.TryGetValue(lesson.Slug, out var whyNoLiveContext))
            // (İstisna listesinin kendisi aşağıda ayrıca denetlenir — RV-002.)
            {
                own.Should().NotContain(s => s.Kind == SectionKind.LiveContext,
                    $"'{lesson.Slug}' künyesi LiveContext taşımıyor ({whyNoLiveContext}) — " +
                    "blok eklendiyse künye ve bu liste birlikte güncellenmeli (D-017)");
            }
            else
            {
                own.Should().Contain(s => s.Kind == SectionKind.LiveContext,
                    $"'{lesson.Slug}' \"Senin portföyünde\" bağlam bloğu taşımalı (T6.2)");
            }
            own.Select(s => s.OrderIndex).Should().BeInAscendingOrder();
            own.Select(s => s.OrderIndex).Should().Equal(Enumerable.Range(1, own.Count));
            own.Should().OnlyContain(s => s.BodyMarkdown.Length > 100); // boş/yer tutucu içerik yok
        }

        // REVIEW-001 · RV-002: istisna listesinin KENDİSİ denetlenir. Bir ders
        // yeniden adlandırılır ya da kaldırılırsa `TryGetValue` sessizce hiç
        // eşleşmez ve istisna ölü kalırdı — D-017 bu listeyi "testle doğrulanan
        // beyan" sayıyor, dolayısıyla beyanın karşılığı olmalı.
        var slugs = lessons.Select(l => l.Slug).ToHashSet();
        foreach (var (slug, why) in LessonsWithoutLiveContext)
        {
            slugs.Should().Contain(slug,
                $"LiveContext istisnası '{slug}' dersi için beyan edilmiş ({why}) ama böyle bir ders yok — " +
                "ders yeniden adlandırıldıysa listeyi güncelle, kaldırıldıysa satırı sil (D-017)");
        }
    }

    /// <summary>
    /// T6.19 · SC-E21 (<c>16</c> §2.5, §6.1 · sözleşme M1/M2): her ders sabit bir
    /// iskelet taşır — <b>açılış bloğu ilk</b>, <b>kaynak bloğu son</b> aşamadır.
    /// </summary>
    [Fact]
    public async Task Every_lesson_opens_with_outcomes_and_closes_with_sources()
    {
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        var lessons = await db.Lessons.Where(l => l.IsPublished).ToListAsync();
        var sections = await db.LessonSections.ToListAsync();
        lessons.Should().NotBeEmpty();

        foreach (var lesson in lessons)
        {
            var own = sections.Where(s => s.LessonId == lesson.Id).OrderBy(s => s.OrderIndex).ToList();

            // ── M2: açılış bloğu, İLK aşama ──────────────────────────────────
            var intro = own.First();
            intro.Heading.Should().Be("Bu derste ne öğreneceksin?",
                $"'{lesson.Slug}' öğrenme çıktılarıyla açılmalı (aşamalı okuyucunun ilk adımı)");
            intro.Kind.Should().Be(SectionKind.Explain);
            intro.DepthTier.Should().Be(DepthTier.Core, "açılış her seviyede görünür olmalı");

            // Çıktılar madde madde: en az üç kazanım (16 §2.1 — 3-5 çıktı).
            intro.BodyMarkdown.Split('\n').Count(l => l.TrimStart().StartsWith("- "))
                .Should().BeGreaterThanOrEqualTo(3, $"'{lesson.Slug}' en az 3 öğrenme çıktısı listelemeli");

            // ── M1: kaynak bloğu, SON aşama ──────────────────────────────────
            own.Should().ContainSingle(s => s.Kind == SectionKind.Source,
                $"'{lesson.Slug}' tam bir kaynak bloğu taşımalı (16 §6.1)");
            var source = own.Last();
            source.Kind.Should().Be(SectionKind.Source,
                $"'{lesson.Slug}' kaynak bloğuyla KAPANMALI — arada kalırsa okuyucu görmez");
            source.Heading.Should().Be("Bu bilgiler nereden geliyor?");

            // Kaynak bloğu üç şeyi söyler (16 §6.1) — en kritiği "sayılar kurgusal"
            // beyanı: gerçek veriyle karıştırılması güven kırıcıdır.
            source.BodyMarkdown.Should().ContainEquivalentOf("kurgusal",
                $"'{lesson.Slug}' örnek sayıların kurgusal olduğunu BEYAN etmeli");

            // CLAUDE.md §2 — "yatırım tavsiyesi değildir" çerçevesi her derste görünür.
            source.BodyMarkdown.Should().ContainEquivalentOf("yatırım tavsiyesi değildir",
                $"'{lesson.Slug}' tavsiye-değildir çerçevesini taşımalı");
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

    /// <summary>
    /// <b>M13 — seed'deki figür künyede DE var mı?</b> (REVIEW-006 · RV-067).
    /// <para>
    /// M4 tek yönlüdür: seed'deki her figür anahtarı web kayıt defterinde tanımlı
    /// olmalı. Ters yön denetlenmiyordu ve M12 de yalnız künyenin KENDİ içinde
    /// tutarlılık istiyordu — iki taraf da düzenlenebilir olduğu için kırmızı bir
    /// M12'yi <b>planı küçülterek</b> yeşile çevirmek mümkündü: plan girdisi silinir,
    /// başlıktaki sayaç bir azaltılır, test yeşil kalır. Sonuç: yayında olan,
    /// kullanıcıya görünen bir figür D-017'nin kanonik ilan ettiği dokümandan
    /// <b>sessizce kaybolur</b> ve sonraki GD gerçeği eksik gösteren bir künyeden
    /// planlamaya başlar — GD-006'nın durdurmak için kurulduğu hatanın ters yönü.
    /// </para>
    /// <para>
    /// ⚠ Bu kural TD-002'nin <b>kapatılabilen yarısıdır.</b> "Planlandı ama hiç
    /// yazılmadı" yönü (plan → seed) bugün yazılamaz: Set 2-5'in künyelerinde
    /// planlı ama henüz yazılmamış figürler var ve böyle bir test meşru biçimde
    /// kırmızı olurdu. "Yazıldı, yayınlandı, sonra plandan düştü" yönü (seed →
    /// plan) ise hiçbir şeye bağlı değil, çünkü seed'deki her figür zaten yazılmış
    /// bir derse ait. TD-002 bu yüzden <b>daraldı</b>, kapanmadı.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Every_seeded_figure_appears_in_its_lesson_blueprint()
    {
        var path = Path.Combine(RepoRoot(), ".claude", "docs", "16-CURRICULUM.md");
        var doc = File.ReadAllText(path);
        var blocks = Regex.Split(doc, @"(?m)^#### ").Skip(1).ToList();

        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        var dersler = await db.Lessons.Select(l => new { l.Id, l.Slug }).ToListAsync();
        var bolumler = await db.LessonSections
            .Where(s => s.FigureKey != null)
            .Select(s => new { s.LessonId, Key = s.FigureKey! })
            .ToListAsync();

        bolumler.Should().NotBeEmpty("seed figür taşıyan bölümler içermeli");
        var eksikler = new List<string>();
        var denetlenenDers = 0;

        foreach (var ders in dersler)
        {
            var figurler = bolumler.Where(b => b.LessonId == ders.Id)
                .Select(b => b.Key).Distinct().ToList();
            if (figurler.Count == 0) continue;

            // Künye bloğu: slug'ı İÇEREN blok. Slug biçimi künyeden künyeye değişiyor
            // (Set 0 tabloda `| **Slug** | `x` |`, Set 3 satır içinde), slug ise tekil.
            var blok = blocks.FirstOrDefault(b => b.Contains($"`{ders.Slug}`", StringComparison.Ordinal));
            if (blok is null)
            {
                eksikler.Add($"{ders.Slug}: künyesi bulunamadı (16-CURRICULUM.md'de `{ders.Slug}` yok)");
                continue;
            }
            denetlenenDers++;

            var baslik = Regex.Match(blok,
                @"\*\*(?:Figür planı|Mevcut figürler|Figürler) \((?<n>\d+)(?:\s*\+\s*(?<m>\d+)\s*etkileşimli)?\)");
            if (!baslik.Success)
            {
                eksikler.Add($"{ders.Slug}: künyede figür başlığı yok");
                continue;
            }

            var kalan = blok[baslik.Index..];
            var bosSatir = Regex.Match(kalan, @"\r?\n\r?\n");
            var plan = bosSatir.Success ? kalan[..bosSatir.Index] : kalan;
            var planAnahtarlari = Regex.Matches(plan, @"`(?<k>[a-z0-9-]+)`")
                .Select(m => m.Groups["k"].Value).ToHashSet();

            foreach (var k in figurler.Where(k => !planAnahtarlari.Contains(k)))
                eksikler.Add($"{ders.Slug}: `{k}` seed'de VAR, künye planında YOK");
        }

        denetlenenDers.Should().BeGreaterThan(0, "en az bir ders künyesi eşleşmeli");
        eksikler.Should().BeEmpty(
            "kullanıcıya görünen her figür, D-017 gereği kanonik olan künyede de yazılı " +
            "olmalı. Bir figür seed'de varken künye planında yoksa künye gerçeği EKSİK " +
            "gösteriyor ve sonraki GD yanlış plandan başlar. Kırmızı bir M12'yi plan " +
            "girdisini silerek yeşile çevirmek bu kuralla artık mümkün değil (M13 · RV-067)");
    }

    /// <summary>
    /// Aşama planına bağlanmamış, henüz yazılmamış ders künyelerindeki figürler —
    /// <b>TD-005</b> (REVIEW-006 · RV-066). Daralan bir kıskaç: çözüldükçe silinir,
    /// yeni kalem eklenmesi M12'yi kırar. Hangi aşamaya bağlanacağı editöryal karar.
    /// </summary>
    private static readonly string[] BilinenBagsizPlanFigurleri =
    [
        "S3-L3:currency-drift",
        "S3-L4:flows-change-measurement",
        "S4-L1:opportunity-of-waiting",
        "S4-L2:who-tells-the-story",
        "S4-L3:breakeven-illusion",
        "S5-L1:bes-timeline-summary",
        "S5-L2:gold-decomposition",
        "S5-L3:rate-vs-basket",
        "S5-L4:name-vs-content",
    ];

    /// <summary>
    /// <b>M12 — künye figür sayısı ↔ figür planı mutabakatı</b> (GD-006).
    /// <para>
    /// `16-CURRICULUM.md` D-017 gereği <b>kanonik kaynaktır</b>: ders yazımı oradan
    /// okunur. Künye başlığındaki figür sayısı ile künyenin kendi figür planı
    /// birbirinden kaydığında hiçbir test kırılmıyordu — ders yine yayına giriyordu,
    /// ama GD planlaması yanlış sayıyla başlıyordu. Hata <b>dört GD üst üste</b>
    /// (S0-L7 · S0-L8 · S0-L9 · S0-L10) yalnız elle yakalandı ve her seferinde
    /// sadece sıradaki ders düzeltildi; GD-006 taraması 18 künyenin <b>10'unda</b>
    /// birikmiş uyumsuzluk buldu.
    /// </para>
    /// <para>
    /// ⚠ Bu test künyenin <b>kendi iç tutarlılığını</b> denetler, künye ↔ seed
    /// mutabakatını değil (o hâlâ açık: <b>TD-002</b>). Koddaki figürün kayıtta
    /// tanımlı olmasını M4 yapar. Künyede planlanıp henüz yazılmamış dersler
    /// (Set 2-5) bu testi kırmaz — bilinçli kapsam sınırı. Tek istisna
    /// <c>*(mevcut)*</c> işaretli anahtarlar: "bu figür zaten var" diyen bir
    /// künye yanlış söylüyorsa yakalanır.
    /// </para>
    /// <para>
    /// <b>REVIEW-006 ile sağlamlaştırıldı</b> (RV-066 · RV-067 · RV-072 · RV-073 ·
    /// RV-079 · RV-080). İlk hâlindeki beş kaçış yolu:
    /// <list type="number">
    /// <item>Yalnız <c>**Figür planı (N)**</c> lehçesini tanıyordu. Künye ÜÇ
    /// lehçe kullanıyor — <c>Figür planı</c> (Set 0/2), <c>Mevcut figürler</c>
    /// (S1-L1/L2), <c>Figürler</c> (Set 1/3/4/5) — dolayısıyla 35 bloğun yalnız
    /// 18'i denetleniyordu ve kalan 17'si <b>sessizce</b> atlanıyordu. Lehçeler
    /// birleştirilince S1-L3'te gerçek bir sapma çıktı (7 ilan, 9 anahtar).</item>
    /// <item>Başlığı olmayan blok <c>continue</c> ile düşüyordu; artık her
    /// <c>####</c> bloğunun başlık taşıması ŞART.</item>
    /// <item>"İşaretli mi" kontrolü anahtardan sonraki <b>26 karakterlik
    /// pencereye</b> bakıyordu ve pencere bir sonraki kalemin açıklamasına
    /// taşabiliyordu; artık plan <c>·</c> ile KALEMLERE bölünüp her anahtar
    /// kendi kaleminde aranıyor.</item>
    /// <item>Aşama bölümü başlıktan sonra duruyorsa kuralın yarısı sessizce
    /// düşüyordu ama künye yine "denetlendi" sayılıyordu; artık aşama taşıyan
    /// blok sayısı da sayılıp eşiğe bağlanıyor.</item>
    /// <item><c>.Distinct()</c> yinelenen anahtarı maskeliyordu (ham 9, distinct
    /// 8, beyan 8 → yeşil); artık HAM sayım karşılaştırılıyor ve yineleme ayrı
    /// bir iddiayla yakalanıyor.</item>
    /// </list>
    /// Eşikler <c>>=</c> değil <b>eşitlik</b>: "en az N" koruması içerik N'i
    /// geçtiği anda korumayı bırakır (RV-080).
    /// </para>
    /// </summary>
    [Fact]
    public void Curriculum_figure_counts_match_their_own_figure_plans()
    {
        var path = Path.Combine(RepoRoot(), ".claude", "docs", "16-CURRICULUM.md");
        File.Exists(path).Should().BeTrue(
            $"müfredat künyeleri kanonik kaynaktır (D-017) ve bu kural onları denetler: {path}");

        var doc = File.ReadAllText(path);
        var kayitliFigurler = WebFigureRegistryKeys();

        // Künyeler "#### <ders-id> · <başlık>" ile başlar.
        var blocks = Regex.Split(doc, @"(?m)^#### ").Skip(1).ToList();
        var asamaTasiyan = 0;
        var asamasizlar = new List<string>();
        var bagsizlar = new List<string>();

        foreach (var block in blocks)
        {
            var ders = block.Split('\n')[0].Split('·')[0].Trim();

            // Üç lehçenin hepsi: "Figür planı" · "Mevcut figürler" · "Figürler".
            // "(N)" ya da "(N + M etkileşimli)".
            var baslik = Regex.Match(block,
                @"\*\*(?<tur>Figür planı|Mevcut figürler|Figürler) " +
                @"\((?<n>\d+)(?:\s*\+\s*(?<m>\d+)\s*etkileşimli)?\)");

            baslik.Success.Should().BeTrue(
                $"'{ders}' künyesi figür başlığı taşımıyor. Üç biçimden biri olmalı: " +
                "`**Figür planı (N):**`, `**Figürler (N):**` ya da `**Mevcut figürler (N):**`. " +
                "Başlıksız künye M12'nin TAMAMININ dışında kalır ve figür sayısı sessizce " +
                "kayabilir — ilk hâlinde 35 bloğun 17'si böyle atlanıyordu (RV-066).");

            var beyan = int.Parse(baslik.Groups["n"].Value)
                      + (baslik.Groups["m"].Success ? int.Parse(baslik.Groups["m"].Value) : 0);
            // "Mevcut figürler" = ders YAYINDA; anahtarlarının kayıtta olması şart.
            var yayinda = baslik.Groups["tur"].Value == "Mevcut figürler";

            // Plan paragrafı: başlıktan sonraki İLK boş satıra kadar (CRLF de olabilir).
            var kalan = block[baslik.Index..];
            var bosSatir = Regex.Match(kalan, @"\r?\n\r?\n");
            var plan = bosSatir.Success ? kalan[..bosSatir.Index] : kalan;

            // Kalemler `·` ile ayrılır; her anahtar KENDİ kaleminde değerlendirilir,
            // böylece komşu kalemin açıklaması yanlışlıkla okunmaz (RV-072).
            var kalemler = plan.Split('·');
            var hamAnahtarlar = new List<string>();
            var kalemiyle = new List<(string Key, string Item)>();
            foreach (var kalem in kalemler)
            {
                foreach (var mm in Regex.Matches(kalem, @"`(?<k>[a-z0-9-]+)`").Cast<Match>())
                {
                    hamAnahtarlar.Add(mm.Groups["k"].Value);
                    kalemiyle.Add((mm.Groups["k"].Value, kalem));
                }
            }
            var anahtarlar = hamAnahtarlar.Distinct().ToList();

            // RV-079: yineleme `.Distinct()` tarafından maskeleniyordu.
            hamAnahtarlar.Count.Should().Be(anahtarlar.Count,
                $"'{ders}' figür planı aynı anahtarı birden fazla listeliyor " +
                $"({string.Join(", ", hamAnahtarlar.GroupBy(k => k).Where(g => g.Count() > 1).Select(g => g.Key))}) — " +
                "yinelenen kalem başlıktaki sayacı doğru gösterip planı yanlış gösterir (M12)");

            hamAnahtarlar.Count.Should().Be(beyan,
                $"'{ders}' künyesinin başlığı {beyan} figür ilan ediyor ama figür planı " +
                $"{hamAnahtarlar.Count} anahtar listeliyor — başlıktaki sayaç eskimiş olabilir (M12)");

            // RV-067'nin kapatılabilen yarısı: "bu figür zaten var" diyen künye
            // doğru söylemeli. Yazılmamış dersler bu iddiada bulunmaz, kırılmaz.
            foreach (var (k, kalem) in kalemiyle)
            {
                var mevcutIddiasi = yayinda || kalem.Contains("*(mevcut)*", StringComparison.Ordinal);
                if (!mevcutIddiasi) continue;
                kayitliFigurler.Should().Contain(k,
                    $"'{ders}' künyesi `{k}` figürünü MEVCUT diye işaretliyor ama web kayıt " +
                    "defterinde (LessonFigure.tsx) böyle bir figür yok. Künye var olmayan bir " +
                    "figürü varmış gibi gösteriyor; sonraki GD onu yazılmış sayıp atlar (M12)");
            }

            // Aşama planındaki 🖼 anahtarları: başlıktan ÖNCEKİ "Aşama planı" bölümü.
            var asamaBas = block.IndexOf("**Aşama planı", StringComparison.Ordinal);
            if (asamaBas < 0 || asamaBas > baslik.Index)
            {
                // RV-073: sessizce atlamak yerine sayılır; aşağıdaki eşitlik tutar.
                asamasizlar.Add(ders);
                continue;
            }
            asamaTasiyan++;

            var asama = block[asamaBas..baslik.Index];
            var asamaAnahtar = Regex.Matches(asama, @"🖼\s*`(?<k>[a-z0-9-]+)`")
                .Select(m => m.Groups["k"].Value).Distinct().ToList();

            foreach (var k in asamaAnahtar)
            {
                anahtarlar.Should().Contain(k,
                    $"'{ders}' aşama planı `{k}` figürünü gösteriyor ama figür planı listesinde yok — " +
                    "aşama figürsüz kalır ya da plan eksik kalır (M12)");
            }

            // Listede olup aşamada olmayan anahtar YALNIZ işaretliyse kabul edilir:
            // 🎛 (etkileşimli, kendi satırı yok) ya da *(fallback)* (başka figürün yerine geçer).
            foreach (var k in anahtarlar.Except(asamaAnahtar))
            {
                // Anahtarın KENDİ kalemi — 26 karakterlik pencere değil (RV-072).
                var kalem = kalemiyle.First(p => p.Key == k).Item;
                // 🎛 BMP dışı bir karakter (surrogate çifti) — char sabiti olamaz, string aranır.
                var isaretli = kalem.Contains("🎛", StringComparison.Ordinal)
                               || kalem.Contains("fallback", StringComparison.OrdinalIgnoreCase)
                               || kalem.Contains("etkileşimli", StringComparison.OrdinalIgnoreCase);
                if (!isaretli) bagsizlar.Add($"{ders}:{k}");
            }
        }

        // RV-066'nın genişletmesi Set 1/3/4/5 künyelerinde DOKUZ birikmiş plan borcu
        // ortaya çıkardı: her birinde planın SON figürü hiçbir aşamaya bağlı değil.
        // Hepsi YAZILMAMIŞ derslerde ve çoğunda aşama metni figürü tarif ediyor ama
        // 🖼 işareti eksik (örn. S4-L1'in "fırsat maliyeti köprüsü" tuzağı tam olarak
        // `opportunity-of-waiting`). Hangi aşamaya bağlanacakları EDİTÖRYAL bir karar;
        // ajan onu tek başına veremez (yanlış bağlama kanonik kaynağa yanlış plan
        // gömer — RV-067'nin uyardığı hata). Bu yüzden liste SAYILI bir istisna:
        // mevcut dokuzu belgeler, YENİ bir bağsız figür kırmızıya düşer.
        // → Borç: TD-005. Çözüldükçe bu listeden silinir.
        bagsizlar.Should().BeEquivalentTo(BilinenBagsizPlanFigurleri,
            "aşamaya bağlanmamış figür listesi DEĞİŞTİ. Yeni bir kalem eklendiyse: " +
            "figürü bir aşamaya 🖼 ile bağla, ya da 🎛/*(fallback)* diye işaretle. " +
            "Bir kalem çözüldüyse BilenenBagsizPlanFigurleri listesinden de sil — " +
            "liste daralan bir kıskaçtır, genişlemez (M12 · TD-005)");

        // Eşitlik, "en az" değil: içerik eşiği geçtiğinde koruma bitmesin (RV-080).
        blocks.Should().HaveCount(35,
            "müfredatta 35 ders künyesi var. Ders eklendiğinde/çıkarıldığında bu sayı " +
            "BİLİNÇLİ olarak güncellenir; eşitlik, yeni künyelerin sessizce denetimsiz " +
            "kalmasını engeller (künye biçimi değiştiyse bu testin regex'i de güncellenmeli)");

        asamaTasiyan.Should().Be(33,
            "35 künyenin 33'ü aşama planı taşır ve aşama ↔ figür planı çapraz kontrolüne " +
            $"girer. Aşama planı olmayanlar: {string.Join(", ", asamasizlar)} (S1-L1/S1-L2 " +
            "yayında olan ve farklı biçimde belgelenen iki ders). Bu sayı düşerse bir künye " +
            "kuralın YARISINI sessizce kaybetmiş demektir (RV-073)");
    }

    /// <summary>
    /// RV-064 (REVIEW-005): ders kaydı yalnız "yok ise ekle" ile yazıldığı sürece metin
    /// düzeltmeleri çalışan kurulumlara inmiyordu — S0-L10'un özeti, gövde düzeltildikten
    /// sonra bile eski hâliyle ekranda duruyordu. Bu test mutabakatı kilitler: seed ikinci
    /// turda yazılı içeriği tazeler, ama <b>işletme kararı olan alanları</b> geri almaz.
    /// </summary>
    [Fact]
    public async Task Reseeding_refreshes_authored_lesson_text_but_not_operational_fields()
    {
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        var lesson = await db.Lessons.SingleAsync(l => l.Slug == "bir-alim-nasil-gerceklesir");
        var authoredSummary = lesson.Summary;
        var authoredTitle = lesson.Title;
        var createdAt = lesson.CreatedAtUtc;

        // Bir kurulumun "eskimiş" hâlini taklit et: metin eski sürümde kalmış,
        // ayrıca işletme tarafı dersi yayından almış.
        lesson.Summary = "ESKİ ÖZET — piyasa emri gerçekleşmeyi garanti eder";
        lesson.Title = "ESKİ BAŞLIK";
        lesson.EstimatedMinutes = 99;
        lesson.IsPublished = false;
        await db.SaveChangesAsync();

        await SeedData.SeedAsync(db);

        var after = await db.Lessons.SingleAsync(l => l.Slug == "bir-alim-nasil-gerceklesir");
        after.Summary.Should().Be(authoredSummary, "yazılı içerik seed'den tazelenmeli (RV-064)");
        after.Title.Should().Be(authoredTitle, "başlık da yazılı içeriktir — T6.7'de atlanmış ve sessizce eskimişti");
        after.EstimatedMinutes.Should().Be(7, "süre tahmini de künyeden gelir");

        after.IsPublished.Should().BeFalse(
            "yayın durumu bir İŞLETME kararıdır; seed onu sessizce geri almamalı (UpsertLesson bilerek dışarıda bırakır)");
        after.CreatedAtUtc.Should().Be(createdAt, "kaydın kendi geçmişi seed turlarıyla değişmemeli");

        // Ders sayısı artmadı: mutabakat yeni satır üretmez.
        (await db.Lessons.CountAsync(l => l.Slug == "bir-alim-nasil-gerceklesir")).Should().Be(1);
    }

    /// <summary>
    /// RV-062 (REVIEW-005) · <c>16</c> §9.1 M7a/M7b + §12-K3a: S0-L10'un künyesi üç
    /// 🔴 kural koyuyordu (aracı kurum adı/tarife yasağı · "kurgusal" etiketi ·
    /// Nirengi cümlesi) ve bunların <b>hiçbirinin makine karşılığı yoktu</b> — koruma
    /// tamamen insan okumasına dayanıyordu. Bu test o boşluğu kapatır.
    /// </summary>
    [Fact]
    public async Task Brokerage_naming_and_fictional_rate_labels_are_machine_checked()
    {
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        var sections = await db.LessonSections.ToListAsync();
        var lessons = await db.Lessons.ToListAsync();
        var corpus = string.Join("\n", sections.Select(s => s.BodyMarkdown));

        // ── M7b: aracı kurum / şirket adı yasağı ─────────────────────────────
        // Tam bir kurum listesi tutulamaz; onun yerine Türkçe aracı kurum ve şirket
        // adlarının TAŞIYICI kalıpları yasaklanır. §12-K3a muafiyeti düzenleyici,
        // borsa ve takas kuruluşu adlarını (SPK · Borsa İstanbul · Takasbank) serbest
        // bırakır — bu kalıplar onları yakalamaz, çünkü ders onları ünvansız anar.
        foreach (var pattern in new[] { "Menkul Değerler", "Yatırım Menkul", "A.Ş.", "A.Ş ", "Aracı Kurumu " })
        {
            corpus.Should().NotContainEquivalentOf(pattern,
                $"eğitim içeriği tek bir şirketi/aracı kurumu adlandırmaz (M7b · §12-K3a muafiyeti ünvan " +
                $"taşımayan düzenleyici/borsa/takas kuruluşu adlarıyla sınırlıdır) — bulunan kalıp: {pattern}");
        }

        // ── M7a: grafik/tahmin kalıpları — ANLATIMDA yasak, Trap'te serbest ───
        // Trap blokları ve quiz çeldiricileri yanılgının kendisini öğretir (§9.1 M7a).
        var narrative = string.Join("\n", sections
            .Where(s => s.Kind != SectionKind.Trap)
            .Select(s => s.BodyMarkdown));
        foreach (var pattern in new[] { "hedef fiyat", "sinyal verdi", "formasyon", "destek kırıldı", "direnç kırıldı" })
        {
            narrative.Should().NotContainEquivalentOf(pattern,
                $"grafik/tahmin kalıbı anlatım metninde geçmez (M7a; Trap bloğunda serbesttir) — " +
                $"bulunan kalıp: {pattern}");
        }

        // ── S0-L10 künyesi · 🔴 kural 1: Nirengi'nin sınırı AÇIKÇA yazılı ─────
        // Kullanıcı beklentisini yanlış kurmak bu derste doğrudan yasal risktir:
        // uygulama emir iletmez ve bunu derste söylemek zorundadır (künye yasal notu).
        var s0l10 = lessons.Single(l => l.Slug == "bir-alim-nasil-gerceklesir");
        var s0l10Body = string.Join("\n", sections
            .Where(s => s.LessonId == s0l10.Id)
            .OrderBy(s => s.OrderIndex)
            .Select(s => s.BodyMarkdown));

        s0l10Body.Should().ContainEquivalentOf("Nirengi bir aracı kurum değildir, emir iletmez",
            "S0-L10 künyesi bu cümlenin derste AÇIKÇA geçmesini şart koşuyor (13. aşama, yasal not)");

        // ── 🔴 kural 2: komisyon oranı geçen her yerde "kurgusal" etiketi ─────
        // Etiketsiz bir oran gerçek bir tarife sanılabilir → reklam/yanıltma riski.
        foreach (var s in sections.Where(s => s.LessonId == s0l10.Id && s.BodyMarkdown.Contains("%0,2")))
        {
            s.BodyMarkdown.Should().ContainEquivalentOf("kurgusal",
                $"'{s.Heading}' aşaması komisyon oranı veriyor ama kurgusal olduğunu söylemiyor " +
                "(künye 🔴 kuralı: oran kurgusal VE etiketli)");
        }
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

        (await db.LessonSections.CountAsync()).Should().Be(187);
        (await db.Quizzes.CountAsync()).Should().Be(15);
        (await db.Lessons.CountAsync()).Should().Be(15); // dersler çoğaltılmadı
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
        live.Should().HaveCount(13);
        db.LessonSections.RemoveRange(live);
        await db.SaveChangesAsync();
        (await db.LessonSections.CountAsync()).Should().Be(174); // diğer bloklar yerinde

        await SeedData.SeedAsync(db); // "bir sonraki açılış"

        // Eksik blok tipi geriye dönük geldi, var olanlar çoğaltılmadı.
        (await db.LessonSections.CountAsync(s => s.Kind == SectionKind.LiveContext)).Should().Be(13);
        (await db.LessonSections.CountAsync()).Should().Be(187);
    }

    [Fact]
    public async Task Section_seed_reconciles_edited_content()
    {
        // REGRESYON (T6.7): içerik DÜZELTİLDİĞİNDE (metin/katman/figür) çalışan
        // kurulumlar da almalı. Salt "eksikse ekle" yaklaşımı bunu kaçırıyordu —
        // aynı Id'li blok var sayılıp güncelleme atlanıyordu.
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        // Figürsüz bir tuzak seç: reconcile FigureKey'i null'a geri çekmeli
        // (bazı Set 0 tuzakları figür taşır — o durumda "BeNull" beklentisi yanıltıcı olur).
        var section = await db.LessonSections.FirstAsync(s => s.Kind == SectionKind.Trap && s.FigureKey == null);
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
        (await db.LessonSections.CountAsync()).Should().Be(187); // çoğaltma yok
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

        // Figür anahtarları DERS İÇİNDE tekil — aynı görsel bir derste iki yerde
        // çizilmemeli. Farklı dersler figür PAYLAŞABİLİR (16 §8.4); global tekillik
        // dayatmak Set 0'ın paylaşılan öğe stratejisini (window-selection vb.) kırardı.
        foreach (var group in sections.GroupBy(s => s.LessonId))
            group.Where(s => s.FigureKey != null).Select(s => s.FigureKey)
                .Should().OnlyHaveUniqueItems();

        // Tuzak blokları Core katmanda → başlangıç seviyesinde KATLANMAZ.
        sections.Where(s => s.Kind == SectionKind.Trap)
            .Should().OnlyContain(s => s.DepthTier == DepthTier.Core);

        // NOT (T6.16): Set 1'de figür yalnız örnek bloklarındaydı; Set 0'da figür
        // ANLATIM aşamalarında da olur (16 §6.1 "her anlatım aşamasında görsel hedeftir").
        // Bu yüzden "Explain görselsiz" kısıtı KALDIRILDI — figür her blok türünde olabilir.
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

        live.Should().HaveCount(13); // 5 (Set 1) + S0-L1..L6 + S0-L8 + S0-L9
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

    /// <summary>
    /// M11 (AC-2) — Token'ın <see cref="ContextKeys"/>'te TANIMLI olması yetmez:
    /// seed portföyünde <b>karşılığı</b> da olmalı.
    ///
    /// <para><c>LessonContextService.AddTypeWeight</c> bir varlık türü portföyde hiç
    /// yoksa token'ı sözlüğe <b>eklemez</b> — ve çözülemeyen token'ın satırı tamamen
    /// düşer. Bu bilinçli bir tasarım ("uydurma %0 yazmayız"), ama yan etkisi
    /// <b>sessizdir</b>: ders eksik bir "Senin portföyünde" bloğuyla yayına çıkar ve
    /// ne seed testleri ne de yapısal sayımlar bunu görür.</para>
    ///
    /// <para>GD-003'ün AC-2 kriteri ("demo ve gerçek portföyde çözülüyor") tam olarak
    /// bunu soruyordu ve makine karşılığı yoktu — bu test o boşluğu kapatır.</para>
    /// </summary>
    [Fact]
    public async Task Type_weight_tokens_have_a_matching_holding_in_the_seed_portfolio()
    {
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        // Tür bazlı ağırlık token'ları: token → gerektirdiği varlık türü.
        var typeTokens = new Dictionary<string, AssetType>
        {
            [ContextKeys.CashWeight] = AssetType.Cash,
            [ContextKeys.StockWeight] = AssetType.Stock,
        };

        var seededTypes = await db.Holdings
            .Join(db.Assets, h => h.AssetId, a => a.Id, (h, a) => a.Type)
            .Distinct()
            .ToListAsync();

        var live = await db.LessonSections
            .Where(s => s.Kind == SectionKind.LiveContext).ToListAsync();

        foreach (var s in live)
        {
            var tokens = System.Text.RegularExpressions.Regex
                .Matches(s.BodyMarkdown, @"\{\{\s*(?<key>[a-z0-9_]+)\s*\}\}")
                .Select(m => m.Groups["key"].Value.ToLowerInvariant())
                .Distinct();

            foreach (var token in tokens)
            {
                if (!typeTokens.TryGetValue(token, out var needed))
                    continue;

                seededTypes.Should().Contain(needed,
                    $"'{{{{{token}}}}}' token'ı {needed} türünde en az bir kalem gerektirir; "
                    + "seed portföyünde yoksa o satır kullanıcıya hiç görünmez (sessiz kayıp)");

                // Kalem var olması YETMEZ: PortfolioCalculationService dağılım dilimlerini
                // `Value > 0` ile süzer (Value = güncel değer ?? maliyet, SC-40). Değeri
                // sıfır olan bir kalem Allocation'a hiç girmez → token yine çözülmez.
                var effective = await db.Holdings
                    .Join(db.Assets, h => h.AssetId, a => a.Id, (h, a) => new { h, a.Type })
                    .Where(x => x.Type == needed)
                    .Select(x => x.h.Quantity * (x.h.CurrentPrice ?? x.h.AvgCost))
                    .ToListAsync();

                effective.Should().Contain(v => v > 0m,
                    $"{needed} türünde DEĞERİ SIFIRDAN BÜYÜK bir kalem olmalı; aksi hâlde "
                    + "dilim Allocation'dan süzülür ve '{{" + token + "}}' yine çözülmez");
            }
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

    // ── T6.20: yapısal sözleşme (16 §9.1) ─────────────────────────────────────
    // NOT (16 §9.3): testler İÇERİKTEN SONRA yazılır, kırmızı bırakılmaz. Mevcut
    // seed'de yalnız "Temeller" (Set 1) var; Ders 3-5 içerik turunu (T6.11c)
    // bekliyor → 1'er figür, 3'er soru. Bu yüzden M3'ün "diğer set ≥2" ve M5'in
    // "9 soru" SAYISAL eşikleri bugün GLOBAL uygulanamaz (kırmızıya düşerdi).
    // T6.20 bu turda: M4 (figür kayıt defteri mutabakatı) · M6 (boşta kavram yok) ·
    // M7 (tavsiye/tahmin/enstrüman-sıralama, Source dahil) + M3/M5'in bugün
    // yeşil olan DEĞİŞMEZLERİ. Set bazlı sayısal eşikler, her setin içeriği
    // indikçe (T6.11c/T6.16/T6.22) bu dosyada açılır.

    /// <summary>
    /// Repo kökü. RV-088: iki ayrı uygulama vardı, <see cref="RepoPaths"/> olarak
    /// tek yere çıkarıldı. Bu sarmalayıcı çağrı yerlerini olduğu gibi bırakır.
    /// </summary>
    private static string RepoRoot() => RepoPaths.Root;

    /// <summary>
    /// <c>LessonFigure.tsx</c> içindeki <c>FIGURES</c> kayıt defterinin anahtarları.
    /// Anahtar ya tırnaklı (<c>"real-vs-nominal":</c>) ya düz tanımlayıcıdır
    /// (<c>concentration:</c>) — ikisi de okunur.
    /// </summary>
    private static IReadOnlySet<string> WebFigureRegistryKeys()
    {
        var path = Path.Combine(RepoRoot(), "web", "src", "components", "LessonFigure.tsx");
        var tsx = File.ReadAllText(path);

        // `const FIGURES ... = { ... };` — tip ek açıklamasındaki `=>` oku değil,
        // gerçek `= {` yakalanır (lazy `.*?` + `=\s*\{`).
        var block = Regex.Match(tsx, @"const FIGURES\b.*?=\s*\{(?<body>.*?)\n\};",
            RegexOptions.Singleline);
        block.Success.Should().BeTrue("LessonFigure.tsx içinde FIGURES kayıt defteri bulunmalı");

        var keys = Regex.Matches(block.Groups["body"].Value,
                @"(?:""(?<q>[a-z0-9-]+)""|(?<b>[A-Za-z_][A-Za-z0-9_]*))\s*:")
            .Select(m => m.Groups["q"].Success ? m.Groups["q"].Value : m.Groups["b"].Value)
            .ToHashSet();
        keys.Should().NotBeEmpty("kayıt defteri en az bir figür tanımlamalı");
        return keys;
    }

    /// <summary>
    /// M4 — <b>figür anahtarı ↔ LessonFigure kayıt defteri mutabakatı.</b> Seed'de
    /// kullanılan her <c>FigureKey</c>, web'deki kayıt defterinde TANIMLI olmalı;
    /// aksi hâlde figür ekranda <b>sessizce düşer</b> (bilinmeyen anahtar → null)
    /// ve ders görselsiz kalır — testler yeşilken içerik bozulur.
    /// </summary>
    [Fact]
    public async Task Every_seed_figure_key_is_defined_in_the_web_registry()
    {
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        var registry = WebFigureRegistryKeys();
        var used = await db.LessonSections
            .Where(s => s.FigureKey != null)
            .Select(s => s.FigureKey!)
            .Distinct()
            .ToListAsync();

        used.Should().NotBeEmpty();
        var missing = used.Where(k => !registry.Contains(k)).ToList();
        missing.Should().BeEmpty(
            "seed'deki figür anahtarları LessonFigure.tsx'te tanımlı olmalı (yoksa görsel sessizce düşer)");
    }

    /// <summary>
    /// M3 (Set 0) — <b>figür eşiği.</b> "İlk Adımlar" setinin her dersi <b>≥6 figür</b>
    /// taşır (16 §9.1-M3, §6.1). T6.20'de sayısal eşik içerik gelene dek ertelenmişti;
    /// Set 0 içerik turu (T6.16→) indikçe bu guard devreye girer. Çok panelli olma
    /// koşulu (≥1) makineyle okunamaz (TSX ayrıntısı) → İ6 insan gözden geçirmesinde.
    /// </summary>
    [Fact]
    public async Task Set0_lessons_meet_the_figure_threshold()
    {
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        var set0 = await db.LearningTracks.SingleAsync(t => t.Slug == "ilk-adimlar");
        var lessons = await db.Lessons.Where(l => l.TrackId == set0.Id && l.IsPublished).ToListAsync();
        var sections = await db.LessonSections.ToListAsync();

        lessons.Should().NotBeEmpty("Set 0 içerik turu en az bir ders indirmeli");
        foreach (var lesson in lessons)
        {
            sections.Count(s => s.LessonId == lesson.Id && s.FigureKey != null)
                .Should().BeGreaterThanOrEqualTo(6,
                    $"'{lesson.Slug}' Set 0 figür eşiğini (≥6) karşılamalı (16 §9.1-M3)");
        }
    }

    /// <summary>
    /// M6 — <b>boşta kavram yok.</b> Her ders en az bir <c>ConceptTag</c> taşır ve
    /// her <c>ConceptTag</c> en az bir derste kullanılır. Kavram haritası (16 §3)
    /// sözlüğün (T6.3) omurgasıdır; bağsız kavram ölü tanım demektir.
    /// </summary>
    [Fact]
    public async Task Every_lesson_is_tagged_and_no_concept_is_orphaned()
    {
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        var lessonIds = await db.Lessons.Select(l => l.Id).ToListAsync();
        var tags = await db.ConceptTags.ToListAsync();
        var links = await db.LessonConceptTags.ToListAsync();

        // Her ders ≥1 kavrama bağlı.
        foreach (var lessonId in lessonIds)
            links.Should().Contain(l => l.LessonId == lessonId,
                "her ders en az bir kavram etiketi taşımalı (16 §9.1-M6)");

        // Her kavram ≥1 derste kullanılır (boşta kavram yok).
        foreach (var tag in tags)
            links.Should().Contain(l => l.ConceptTagId == tag.Id,
                $"'{tag.Key}' kavramı en az bir derste tanıtılmalı (boşta kavram yok)");
    }

    /// <summary>
    /// M7 — <b>tavsiye/tahmin/enstrüman-sıralama taraması, Source blokları DAHİL.</b>
    /// Kaynak bloğu da kullanıcıya görünür; oraya kaçan bir yönlendirme de tavsiye
    /// sayılır (CLAUDE.md §2). Enstrüman sıralaması ("X, Y'den iyi getirdi") zımni
    /// yönlendirmedir (15 §3.4).
    /// </summary>
    [Fact]
    public async Task No_block_including_sources_predicts_or_ranks_instruments()
    {
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        var sections = await db.LessonSections.ToListAsync();

        // Kaynak blokları gerçekten taranıyor (regresyon güvencesi).
        sections.Should().Contain(s => s.Kind == SectionKind.Source);

        // Gelecek/tahmin ve garanti kalıpları hiçbir blokta olmamalı.
        string[] predictions =
        {
            "yükselecek", "düşecek", "artacak", "kazandıracak", "getirecek",
            "garanti getiri", "garantili getiri", "kesin kâr",
        };
        // Enstrüman sıralaması: "…'den/dan daha iyi/çok getiri/performans".
        // ⚠ `-d[ae]n` ayrılma ekini arar ama kelime sınırı yoktur — bu yüzden sonu
        // "den/dan" ile biten KELİMELERE de düşebiliyordu. RV-001 taraması ders
        // özetlerine açılınca ilk kurban "**Ne**den yüksek getiri…" oldu (sıralama
        // değil, meşru eğitim cümlesi). Çözüm: ekin ayrılma eki olması için soldaki
        // yaygın çarpışmalar dışlanır; gerçek sıralama ("altın**dan** daha yüksek
        // getiri") yakalanmaya devam eder.
        var ranking = new Regex(
            @"(?<!\b(?:ne|he))(?:'?d[ae]n)\s+(?:daha\s+)?(?:iyi|çok|yüksek)\s+(?:getiri|performans|kazan)",
            RegexOptions.IgnoreCase);

        foreach (var s in sections)
        {
            foreach (var banned in predictions)
                s.BodyMarkdown.Should().NotContainEquivalentOf(banned,
                    $"'{s.Kind}' bloğu tahmin/garanti içermez (yasak: {banned})");

            ranking.IsMatch(s.BodyMarkdown).Should().BeFalse(
                $"'{s.Kind}' bloğu enstrüman sıralaması yapmamalı (15 §3.4)");
        }

        // REVIEW-001 · RV-001: tarama önceden YALNIZ bölüm gövdelerini görüyordu.
        // Oysa `Lesson.Summary` ders listesinde, `Lesson.BodyMarkdown` ise bölümsüz
        // derslerde doğrudan kullanıcıya gösteriliyor — ikisi de D-002 kapsamında.
        var lessons = await db.Lessons.ToListAsync();
        lessons.Should().NotBeEmpty();

        foreach (var l in lessons)
        {
            foreach (var field in new[] { ("Summary", l.Summary), ("BodyMarkdown", l.BodyMarkdown) })
            {
                foreach (var banned in predictions)
                    field.Item2.Should().NotContainEquivalentOf(banned,
                        $"'{l.Slug}' dersinin {field.Item1} alanı tahmin/garanti içermez (yasak: {banned})");

                ranking.IsMatch(field.Item2).Should().BeFalse(
                    $"'{l.Slug}' dersinin {field.Item1} alanı enstrüman sıralaması yapmamalı (15 §3.4)");
            }
        }
    }

    /// <summary>
    /// M3/M5 değişmezleri (bugün yeşil olan kısım). Sayısal set eşikleri içerik
    /// indikçe açılır; bu test şimdiden iki şeyi kilitler: (a) 9 soruluk bir quiz
    /// varsa zorluklar DENGELİ olmalı (bozuk zengin quiz yakalanır); (b) içerik
    /// turundan geçmiş dersler (1-2) zenginliğini KAYBETMEMELİ (regresyon guard'ı).
    /// </summary>
    [Fact]
    public async Task Rich_quizzes_stay_balanced_and_toured_lessons_keep_their_richness()
    {
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        var quizzes = await db.Quizzes.ToListAsync();
        var questions = await db.QuizQuestions.ToListAsync();

        // (a) M5 değişmezi: 9+ soruluk her quiz her zorluktan ≥3 taşır.
        foreach (var quiz in quizzes)
        {
            var own = questions.Where(q => q.QuizId == quiz.Id).ToList();
            if (own.Count < 9) continue;
            foreach (var diff in new[] { QuizDifficulty.Easy, QuizDifficulty.Medium, QuizDifficulty.Hard })
                own.Count(q => q.Difficulty == diff).Should().BeGreaterThanOrEqualTo(3,
                    $"9+ soruluk quiz '{diff}' zorluğundan en az 3 taşımalı (16 §9.1-M5)");
        }

        // (b) İçerik turundan geçmiş dersler (T6.11a/b) zenginliğini korumalı.
        async Task AssertToured(string slug, int minQuestions, int minFigures)
        {
            var lesson = await db.Lessons.SingleAsync(l => l.Slug == slug);
            var quiz = quizzes.Single(q => q.LessonId == lesson.Id);
            questions.Count(q => q.QuizId == quiz.Id).Should().BeGreaterThanOrEqualTo(minQuestions,
                $"'{slug}' içerik turundan geçti — {minQuestions} soru zenginliğini korumalı");
            (await db.LessonSections.CountAsync(s => s.LessonId == lesson.Id && s.FigureKey != null))
                .Should().BeGreaterThanOrEqualTo(minFigures,
                    $"'{slug}' en az {minFigures} figür taşımalı (regresyon guard'ı)");
        }

        await AssertToured("enflasyon-ve-reel-getiri", 9, 5);
        await AssertToured("cesitlendirme-neden-onemli", 9, 3);
    }

    /// <summary>
    /// T6.21 — <b>güven kritik:</b> seed'lenen enflasyon oranı PLACEHOLDER'dır;
    /// gerçek TÜFE beslenmesi bağlanana dek kurumsal bir kaynakla (TÜİK/TCMB…)
    /// etiketlenemez. Uydurma bir oranı "TÜİK" diye göstermek eğitimin
    /// "kaynak daima görünür" iddiasını (16 §6.4) çürütür.
    /// </summary>
    [Fact]
    public async Task Seeded_inflation_is_labeled_example_not_an_institution()
    {
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        var rates = await db.InflationRates.ToListAsync();
        rates.Should().NotBeEmpty();

        string[] institutions = { "TÜİK", "TUIK", "TCMB", "SPK", "BDDK" };
        foreach (var rate in rates)
        {
            rate.Source.Should().NotBeNullOrWhiteSpace();
            institutions.Should().NotContain(rate.Source,
                "placeholder oran kurumsal kaynakla etiketlenemez (gerçek veri gelene dek 'örnek')");
        }
    }
}

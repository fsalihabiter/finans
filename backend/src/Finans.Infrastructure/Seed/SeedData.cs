using System.Security.Cryptography;
using System.Text;
using Finans.Domain.Education;
using Finans.Domain.Enums;
using Finans.Domain.Identity;
using Finans.Domain.Portfolio;
using Finans.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Finans.Infrastructure.Seed;

/// <summary>
/// Kapsamlı, tutarlı ve idempotent seed (03 §12). 7 çeşitli pozisyon (altın, döviz
/// USD+EUR, USD-fiyatlı hisse, zarardaki fon, BES, nakit). Baz TRY toplamları:
/// maliyet 603.770, değer 839.213, kâr +235.443 (+%39,0; reel ~+%0,7 @enflasyon 0,38).
/// Bu set aynı zamanda integration test fixture'ıdır (09 §2). USD-fiyatlı AAPL
/// summary'de gerçek kur çevrimini (USD→TRY ×48) tetikler.
/// Deterministik Id'ler (anahtar→GUID) sayesinde tekrar çalışınca çoğaltmaz.
/// Eğitim içeriği (03 §12.5, T5E.2) ayrı, bağımsız idempotent bölümde eklenir —
/// böylece portföyü zaten seed'lenmiş mevcut DB'ler bir sonraki açılışta eğitimi de alır.
/// </summary>
public static class SeedData
{
    /// <summary>Anahtardan deterministik GUID üretir (idempotent seed için).</summary>
    public static Guid Id(string key) =>
        new(MD5.HashData(Encoding.UTF8.GetBytes("finans-seed:" + key)));

    public static async Task SeedAsync(FinansDbContext db, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await SeedPortfolioAsync(db, now, ct);
        await SeedEducationAsync(db, now, ct);
        // AYRI kapı (T6.1): SeedEducationAsync "track var mı?" ile korunur, dolayısıyla
        // eğitimi ZATEN almış DB'ler katmanlı içeriği ondan alamaz. Bu adım kendi
        // kapısıyla çalışır → çalışan kurulumlar da bir sonraki açılışta bölümleri alır.
        await SeedEducationSectionsAsync(db, ct);
        await SeedRemainingQuizzesAsync(db, ct);
    }

    private static async Task SeedPortfolioAsync(FinansDbContext db, DateTime now, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(ct))
            return; // portföy zaten seed'lenmiş

        var purchase = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc); // alış dönemi

        // ── Roller & kullanıcılar (12.1) ──────────────────────────────────────
        var roleUser = new Role { Id = Id("role-user"), Name = UserRole.User };
        var roleAdmin = new Role { Id = Id("role-admin"), Name = UserRole.Admin };

        var user = new User
        {
            Id = Id("user-1"),
            DisplayName = "Yatırımcı",
            BaseCurrency = CurrencyCode.TRY,
            IsActive = true,
            CreatedAtUtc = now,
        };
        var admin = new User
        {
            Id = Id("admin-1"),
            DisplayName = "Yönetici",
            BaseCurrency = CurrencyCode.TRY,
            IsActive = true,
            CreatedAtUtc = now,
        };

        db.Roles.AddRange(roleUser, roleAdmin);
        db.Users.AddRange(user, admin);
        db.UserRoles.AddRange(
            new UserRoleAssignment { UserId = user.Id, RoleId = roleUser.Id },
            new UserRoleAssignment { UserId = admin.Id, RoleId = roleAdmin.Id });

        // ── Kur & enflasyon (12.2) ────────────────────────────────────────────
        db.FxRates.AddRange(
            new FxRate { Id = Id("fx-usd-try-now"), FromCurrency = CurrencyCode.USD, ToCurrency = CurrencyCode.TRY, Rate = 48.000000m, Source = "Manual", AsOfUtc = now, CreatedAtUtc = now },
            new FxRate { Id = Id("fx-eur-try-now"), FromCurrency = CurrencyCode.EUR, ToCurrency = CurrencyCode.TRY, Rate = 52.000000m, Source = "Manual", AsOfUtc = now, CreatedAtUtc = now },
            new FxRate { Id = Id("fx-usd-try-old"), FromCurrency = CurrencyCode.USD, ToCurrency = CurrencyCode.TRY, Rate = 43.270000m, Source = "Manual", AsOfUtc = purchase, CreatedAtUtc = now });

        db.InflationRates.Add(new InflationRate
        {
            Id = Id("inflation-2024"),
            PeriodStartUtc = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            PeriodEndUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            AnnualRate = 0.380000m, // örnek/placeholder — TÜİK, prod'da gerçek veri
            Source = "TÜİK",
            CreatedAtUtc = now,
        });

        // ── Varlık kataloğu (12.3) ────────────────────────────────────────────
        // Çeşitli türler + para birimleri. AAPL USD cinsinden fiyatlı → summary'de
        // gerçek kur çevrimi (USD→TRY) tetikler. Fon kasıtlı ZARAR'da (eğitici örnek).
        var gold = new Asset { Id = Id("asset-gold"), Type = AssetType.Gold, Name = "Altın (gram)", Symbol = "XAU", Unit = "gram", PricingCurrency = CurrencyCode.TRY, CreatedAtUtc = now };
        var usd = new Asset { Id = Id("asset-usd"), Type = AssetType.Fx, Name = "ABD Doları", Symbol = "USD", Unit = "USD", PricingCurrency = CurrencyCode.TRY, CreatedAtUtc = now };
        var eur = new Asset { Id = Id("asset-eur"), Type = AssetType.Fx, Name = "Euro", Symbol = "EUR", Unit = "EUR", PricingCurrency = CurrencyCode.TRY, CreatedAtUtc = now };
        var bes = new Asset { Id = Id("asset-bes"), Type = AssetType.Bes, Name = "Bireysel Emeklilik", Unit = "birim", PricingCurrency = CurrencyCode.TRY, CreatedAtUtc = now };
        var cash = new Asset { Id = Id("asset-cash"), Type = AssetType.Cash, Name = "Nakit (TL)", Unit = "TRY", PricingCurrency = CurrencyCode.TRY, CreatedAtUtc = now };
        var aapl = new Asset { Id = Id("asset-aapl"), Type = AssetType.Stock, Name = "Apple Inc.", Symbol = "AAPL", Unit = "adet", PricingCurrency = CurrencyCode.USD, Exchange = "NASDAQ", CreatedAtUtc = now };
        var fund = new Asset { Id = Id("asset-fund"), Type = AssetType.Fund, Name = "Teknoloji Fonu", Symbol = "TEKFON", Unit = "adet", PricingCurrency = CurrencyCode.TRY, CreatedAtUtc = now };

        db.Assets.AddRange(gold, usd, eur, bes, cash, aapl, fund);

        // ── Fiyat geçmişi (12.4) — reel getiri/senaryo için en az iki nokta ────
        db.PriceSnapshots.AddRange(
            new PriceSnapshot { Id = Id("ps-gold-now"), AssetId = gold.Id, Price = 6500.000000m, Source = "Manual", AsOfUtc = now, CreatedAtUtc = now },
            new PriceSnapshot { Id = Id("ps-gold-old"), AssetId = gold.Id, Price = 4546.275000m, Source = "Manual", AsOfUtc = purchase, CreatedAtUtc = now },
            new PriceSnapshot { Id = Id("ps-usd-now"), AssetId = usd.Id, Price = 48.000000m, Source = "Manual", AsOfUtc = now, CreatedAtUtc = now },
            new PriceSnapshot { Id = Id("ps-usd-old"), AssetId = usd.Id, Price = 43.270000m, Source = "Manual", AsOfUtc = purchase, CreatedAtUtc = now },
            new PriceSnapshot { Id = Id("ps-eur-now"), AssetId = eur.Id, Price = 52.000000m, Source = "Manual", AsOfUtc = now, CreatedAtUtc = now },
            new PriceSnapshot { Id = Id("ps-aapl-now"), AssetId = aapl.Id, Price = 210.000000m, Source = "Manual", AsOfUtc = now, CreatedAtUtc = now },
            new PriceSnapshot { Id = Id("ps-fund-now"), AssetId = fund.Id, Price = 23.500000m, Source = "Manual", AsOfUtc = now, CreatedAtUtc = now });

        // ── Pozisyonlar (12.4) — TUTARLI (baz TRY: maliyet 603.770 · değer 839.213 · +%39,0) ──
        // Altın: 40 gr @ 4.546,275 → maliyet 181.851 · güncel 6.500 → 260.000 (+%43,0)
        var goldHolding = new Holding { Id = Id("holding-gold"), UserId = user.Id, AssetId = gold.Id, Quantity = 40m, AvgCost = 4546.275000m, CurrentPrice = 6500.000000m, CreatedAtUtc = now };
        // Dolar: 2.000 $ @ 43,27 → maliyet 86.540 · güncel 48 → 96.000 (+%10,9)
        var usdHolding = new Holding { Id = Id("holding-usd"), UserId = user.Id, AssetId = usd.Id, Quantity = 2000m, AvgCost = 43.270000m, CurrentPrice = 48.000000m, CreatedAtUtc = now };
        // Euro: 800 € @ 47,50 → maliyet 38.000 · güncel 52 → 41.600 (+%9,5)
        var eurHolding = new Holding { Id = Id("holding-eur"), UserId = user.Id, AssetId = eur.Id, Quantity = 800m, AvgCost = 47.500000m, CurrentPrice = 52.000000m, CreatedAtUtc = now };
        // Apple (USD fiyatlı): 12 @ 175 $ → güncel 210 $; TRY'ye ×48 → maliyet 100.800 · değer 120.960 (+%20)
        var aaplHolding = new Holding { Id = Id("holding-aapl"), UserId = user.Id, AssetId = aapl.Id, Quantity = 12m, AvgCost = 175.000000m, CurrentPrice = 210.000000m, CreatedAtUtc = now };
        // Teknoloji Fonu: 1.500 @ 28,00 → maliyet 42.000 · güncel 23,50 → 35.250 (−%16,1) ZARAR
        var fundHolding = new Holding { Id = Id("holding-fund"), UserId = user.Id, AssetId = fund.Id, Quantity = 1500m, AvgCost = 28.000000m, CurrentPrice = 23.500000m, CreatedAtUtc = now };
        // BES: maliyet = CEPTEN ödenen = kendi katkı 120.000 (devlet katkısı 28.554 maliyet DEĞİL, getiriye
        // dahil). Güncel fon değeri 279.378 → getiri (279.378−120.000)/120.000 ≈ +%132,8.
        var besHolding = new Holding { Id = Id("holding-bes"), UserId = user.Id, AssetId = bes.Id, Quantity = 1m, AvgCost = 120000.000000m, CurrentPrice = 279378.000000m, CreatedAtUtc = now };
        // Nakit: 6.025 ₺
        var cashHolding = new Holding { Id = Id("holding-cash"), UserId = user.Id, AssetId = cash.Id, Quantity = 6025m, AvgCost = 1.000000m, CurrentPrice = 1.000000m, CreatedAtUtc = now };

        db.Holdings.AddRange(goldHolding, usdHolding, eurHolding, aaplHolding, fundHolding, besHolding, cashHolding);

        // İşlemler (DOĞRULUK KAYNAĞI) — her alış Buy; AvgCost/Quantity bunlardan türer.
        db.Transactions.AddRange(
            new Transaction { Id = Id("tx-gold-buy"), HoldingId = goldHolding.Id, Type = TransactionType.Buy, Quantity = 40m, UnitPrice = 4546.275000m, Fee = 0m, TransactedAtUtc = purchase, CreatedAtUtc = now },
            new Transaction { Id = Id("tx-usd-buy"), HoldingId = usdHolding.Id, Type = TransactionType.Buy, Quantity = 2000m, UnitPrice = 43.270000m, Fee = 0m, TransactedAtUtc = purchase, CreatedAtUtc = now },
            new Transaction { Id = Id("tx-eur-buy"), HoldingId = eurHolding.Id, Type = TransactionType.Buy, Quantity = 800m, UnitPrice = 47.500000m, Fee = 0m, TransactedAtUtc = purchase, CreatedAtUtc = now },
            new Transaction { Id = Id("tx-aapl-buy"), HoldingId = aaplHolding.Id, Type = TransactionType.Buy, Quantity = 12m, UnitPrice = 175.000000m, Fee = 0m, TransactedAtUtc = purchase, CreatedAtUtc = now },
            new Transaction { Id = Id("tx-fund-buy"), HoldingId = fundHolding.Id, Type = TransactionType.Buy, Quantity = 1500m, UnitPrice = 28.000000m, Fee = 0m, TransactedAtUtc = purchase, CreatedAtUtc = now });

        // BES detayı — devlet katkısı AYRI; getiri tabanına dahil (03 §A).
        db.BesDetails.Add(new BesDetails
        {
            Id = Id("bes-details"),
            HoldingId = besHolding.Id,
            OwnContribution = 120000.000000m,
            StateContribution = 28554.000000m,
            VestingState = VestingState.PartiallyVested,
            ProviderName = "Örnek BES",
            JoinedAtUtc = purchase,
            BirthYear = 1985,
        });
        // Açılış bakiyesi (T-BES.8): toplamlar artık katkı satırlarından türetilir; tek "Opening" kaydı
        // birikmiş kendi/devlet katkıyı taşır. 2024 tarihli → yatırılmış sayılır (own 120.000, devlet 28.554).
        db.BesContributions.Add(new BesContribution
        {
            Id = Id("bes-opening"),
            HoldingId = besHolding.Id,
            OwnAmount = 120000.000000m,
            StateAmount = 28554.000000m,
            PaidAtUtc = purchase,
            Source = "Opening",
            CreatedAtUtc = now,
        });

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Eğitim içeriği seed'i (03 §12.5, T5E.2) — taslaktaki "Temeller" track'i + 5 ders
    /// (her biri bir öncekini ön-koşul ister) + Ders 1'e bağlı 3 soruluk mini test +
    /// örnek ilerleme (User#1: 1-3 Tamamlandı, 4 Devam ediyor, 5 türetilmiş Kilitli).
    /// Portföyden BAĞIMSIZ idempotent (LearningTracks var mı?) → mevcut DB'ler de alır.
    /// Ders gövdeleri kısa eğitici metin; derinleştirme T6.1'de. Tavsiye YOK (CLAUDE.md §2).
    /// </summary>
    private static async Task SeedEducationAsync(FinansDbContext db, DateTime now, CancellationToken ct)
    {
        if (await db.LearningTracks.AnyAsync(ct))
            return; // eğitim zaten seed'lenmiş

        var userId = Id("user-1"); // portföy seed'inin tekil kullanıcısı (ilerleme sahibi)

        // ── Kavram etiketleri (12.5) — Analiz/Hisse kartından derse derin bağlantı ──
        var tagRealReturn = new ConceptTag { Id = Id("tag-real-return"), Key = "real-return", Label = "Reel Getiri" };
        var tagDiversification = new ConceptTag { Id = Id("tag-diversification"), Key = "diversification", Label = "Çeşitlendirme" };
        var tagPe = new ConceptTag { Id = Id("tag-pe-ratio"), Key = "pe-ratio", Label = "F/K Oranı" };
        var tagPb = new ConceptTag { Id = Id("tag-pb-ratio"), Key = "pb-ratio", Label = "PD/DD Oranı" };
        var tagRiskReturn = new ConceptTag { Id = Id("tag-risk-return"), Key = "risk-return", Label = "Risk ve Getiri" };
        var tagCompound = new ConceptTag { Id = Id("tag-compound"), Key = "compound", Label = "Bileşik Getiri" };
        db.ConceptTags.AddRange(tagRealReturn, tagDiversification, tagPe, tagPb, tagRiskReturn, tagCompound);

        // ── Track "Temeller" (12.5) ──────────────────────────────────────────────
        var track = new LearningTrack
        {
            Id = Id("track-temeller"),
            Slug = "temeller",
            Title = "Temeller",
            Description = "Yatırımın temel kavramları — enflasyon, çeşitlendirme, hisse okuma, risk ve bileşik getiri. Temellerden ileri seviyeye, kendi hızında.",
            Level = LessonLevel.Beginner,
            OrderIndex = 1,
            IsPublished = true,
            CreatedAtUtc = now,
        };
        db.LearningTracks.Add(track);

        // ── Dersler (12.5) — Summary metinleri taslakla birebir ──────────────────
        var lesson1 = new Lesson
        {
            Id = Id("lesson-enflasyon"),
            TrackId = track.Id,
            Slug = "enflasyon-ve-reel-getiri",
            OrderIndex = 1,
            Title = "Enflasyon ve Reel Getiri",
            Summary = "\"Param büyüdü mü, yoksa sadece rakam mı?\" sorusunun cevabı.",
            BodyMarkdown =
                "## Nominal mi, reel mi?\n\n" +
                "Paran bir yılda 100.000 ₺'den 140.000 ₺'ye çıktıysa **nominal** getirin %40'tır. " +
                "Ama aynı dönemde fiyatlar %38 arttıysa, elindeki parayla neredeyse aynı şeyleri alabilirsin.\n\n" +
                "**Reel getiri**, enflasyondan arındırılmış gerçek kazançtır:\n\n" +
                "> reel getiri = (1 + nominal) / (1 + enflasyon) − 1\n\n" +
                "Örnekte: (1,40 / 1,38) − 1 ≈ **%1,4**. Yani rakam %40 büyüdü ama alım gücün yalnızca ~%1,4 arttı.\n\n" +
                "Basit çıkarma (%40 − %38 = %2) kaba bir yaklaşımdır; enflasyon yükseldikçe formülle arasındaki fark açılır. " +
                "Yatırımın \"kârda\" görünmesi her zaman \"zenginleştim\" demek değildir — asıl soru **alım gücün** ne oldu.",
            EstimatedMinutes = 4,
            Level = LessonLevel.Beginner,
            IsPublished = true,
            CreatedAtUtc = now,
        };
        var lesson2 = new Lesson
        {
            Id = Id("lesson-cesitlendirme"),
            TrackId = track.Id,
            Slug = "cesitlendirme-neden-onemli",
            OrderIndex = 2,
            Title = "Çeşitlendirme Neden Önemli?",
            Summary = "Tüm yumurtaları tek sepete koymamanın matematiği.",
            BodyMarkdown =
                "## Tüm yumurtalar tek sepette\n\n" +
                "Bir portföyün değeri tek bir varlığa bağlıysa, o varlık düştüğünde portföyün tümü birlikte düşer. " +
                "Farklı davranan varlıkları bir arada tutmak, biri kötü giderken diğerlerinin dengelemesine olan şansı artırır.\n\n" +
                "**Yoğunlaşma** = değerin az sayıda kalemde toplanması. Örneğin portföyünün %84'ü iki varlıktaysa, " +
                "bu iki varlığın ortak kaderi senin de kaderin olur.\n\n" +
                "Çeşitlendirme riski **yok etmez**, farklı kaynaklara **yayar**. Amaç, varlıkların aynı anda aynı yöne " +
                "hareket etme ihtimalini azaltmaktır.\n\n" +
                "Bu bir \"şu kadar varlık iyi\" kuralı değil, bir farkındalıktır: ağırlığın nerede toplandığını bilmek.",
            EstimatedMinutes = 5,
            Level = LessonLevel.Beginner,
            IsPublished = true,
            CreatedAtUtc = now,
        };
        var lesson3 = new Lesson
        {
            Id = Id("lesson-fk-pddd"),
            TrackId = track.Id,
            Slug = "fk-pddd-nedir",
            OrderIndex = 3,
            Title = "F/K, PD/DD Nedir?",
            Summary = "Bir hisseyi okumanın en temel üç rakamı.",
            BodyMarkdown =
                "## Bir hisseyi okumanın rakamları\n\n" +
                "**F/K (Fiyat / Kazanç)**: hisse fiyatının, şirketin hisse başına kârına oranı. \"Şirketin 1 liralık kârı " +
                "için kaç lira ödüyorum?\" sorusunu yanıtlar. Yüksek F/K, piyasanın gelecekten çok şey beklediğini (prim ödediğini) gösterebilir.\n\n" +
                "**PD/DD (Piyasa Değeri / Defter Değeri)**: şirketin borsadaki değerinin, muhasebe defterindeki öz kaynağına oranı. " +
                "1'in üzerinde olması, piyasanın şirkete defter değerinden fazla değer biçtiği anlamına gelir.\n\n" +
                "**Temettü verimi**: şirketin dağıttığı kâr payının fiyata oranı. Büyümeye yatırım yapan şirketlerde bu düşük olabilir.\n\n" +
                "Bu oranların hiçbiri tek başına bir hisseyi \"iyi\" ya da \"kötü\" yapmaz — sana **neye bakman gerektiğini** " +
                "ve rakamların hikâyesini anlatır. Karşılaştırma genelde aynı sektör içinde anlamlıdır.",
            EstimatedMinutes = 6,
            Level = LessonLevel.Beginner,
            IsPublished = true,
            CreatedAtUtc = now,
        };
        var lesson4 = new Lesson
        {
            Id = Id("lesson-risk-getiri"),
            TrackId = track.Id,
            Slug = "risk-ve-getiri-iliskisi",
            OrderIndex = 4,
            Title = "Risk ve Getiri İlişkisi",
            Summary = "Neden yüksek getiri her zaman yüksek risk demektir.",
            BodyMarkdown =
                "## Yüksek getiri, yüksek risk\n\n" +
                "Bir yatırımın yüksek getiri \"vaat etmesi\", aynı zamanda o getirinin gerçekleşmeme (hatta zarar) ihtimalinin " +
                "de yüksek olması demektir. Risk ve beklenen getiri genelde birlikte hareket eder.\n\n" +
                "**Risk** burada \"kötü bir şey olma ihtimali\" değil, sonucun **ne kadar oynak/belirsiz** olduğudur. " +
                "Mevduat düşük oynaklık–düşük getiri; hisse yüksek oynaklık–yüksek potansiyel getiri ucundadır.\n\n" +
                "\"Garantili yüksek getiri\" ifadesi bir çelişkidir — birileri riski üstleniyorsa bir yerde saklıdır.\n\n" +
                "Doğru soru \"en yüksek getiri hangisi?\" değil, \"bu getiriye ulaşmak için ne kadar oynaklığa katlanabilirim?\" sorusudur.",
            EstimatedMinutes = 5,
            Level = LessonLevel.Beginner,
            IsPublished = true,
            CreatedAtUtc = now,
        };
        var lesson5 = new Lesson
        {
            Id = Id("lesson-bilesik"),
            TrackId = track.Id,
            Slug = "bilesik-getirinin-gucu",
            OrderIndex = 5,
            Title = "Bileşik Getirinin Gücü",
            Summary = "Zamanın yatırımcının en büyük dostu olması.",
            BodyMarkdown =
                "## Zaman senin dostun\n\n" +
                "Bileşik getiri, kazancının da kazanç getirmesidir. Sadece ana paran değil, geçmiş getirilerin de üzerine " +
                "getiri biner — ve bu etki zamanla hızlanır.\n\n" +
                "100.000 ₺ yılda %20 büyürse: 1. yıl sonu 120.000, 2. yıl sonu 144.000 (sadece +20.000 değil, +24.000). " +
                "Fark, önceki kârın da çalışmasından gelir.\n\n" +
                "**Erken başlamak** ve **kazancı yeniden yatırmak** (dağıtmamak), bileşik etkinin en güçlü iki bileşenidir. " +
                "Küçük ama düzenli katkılar, uzun vadede tek seferlik büyük tutarları geçebilir.\n\n" +
                "Bu yüzden bileşik getiriye çoğu zaman \"zamanın armağanı\" denir — en büyük değişkeni **süre**dir.",
            EstimatedMinutes = 5,
            Level = LessonLevel.Beginner,
            IsPublished = true,
            CreatedAtUtc = now,
        };
        db.Lessons.AddRange(lesson1, lesson2, lesson3, lesson4, lesson5);

        // ── Ön-koşul zinciri (12.5) — her ders bir öncekini ister (kilit türetimi) ──
        db.LessonPrerequisites.AddRange(
            new LessonPrerequisite { LessonId = lesson2.Id, PrerequisiteLessonId = lesson1.Id },
            new LessonPrerequisite { LessonId = lesson3.Id, PrerequisiteLessonId = lesson2.Id },
            new LessonPrerequisite { LessonId = lesson4.Id, PrerequisiteLessonId = lesson3.Id },
            new LessonPrerequisite { LessonId = lesson5.Id, PrerequisiteLessonId = lesson4.Id });

        // ── Ders ↔ kavram etiketi (12.5) ─────────────────────────────────────────
        db.LessonConceptTags.AddRange(
            new LessonConceptTag { LessonId = lesson1.Id, ConceptTagId = tagRealReturn.Id },
            new LessonConceptTag { LessonId = lesson2.Id, ConceptTagId = tagDiversification.Id },
            new LessonConceptTag { LessonId = lesson3.Id, ConceptTagId = tagPe.Id },
            new LessonConceptTag { LessonId = lesson3.Id, ConceptTagId = tagPb.Id },
            new LessonConceptTag { LessonId = lesson4.Id, ConceptTagId = tagRiskReturn.Id },
            new LessonConceptTag { LessonId = lesson5.Id, ConceptTagId = tagCompound.Id });

        // ── Ders 1 mini testi (12.5) — 3 soru, her birinde eğitici Explanation ────
        var quiz = new Quiz
        {
            Id = Id("quiz-enflasyon"),
            LessonId = lesson1.Id,
            Title = "Enflasyon ve Reel Getiri — Mini Test",
            PassingScore = 60,
        };
        db.Quizzes.Add(quiz);

        var q1 = new QuizQuestion
        {
            Id = Id("quiz-enflasyon-q1"),
            QuizId = quiz.Id,
            OrderIndex = 1,
            Type = QuizQuestionType.SingleChoice,
            Prompt = "Nominal getirin %40, enflasyon %38 ise reel getirin yaklaşık kaçtır?",
            Explanation = "Reel getiri = (1 + nominal) / (1 + enflasyon) − 1. Basit çıkarma (%40 − %38) kaba bir tahmindir; " +
                          "doğru formül (1,40 / 1,38) − 1 ≈ %1,4 verir. Enflasyon yükseldikçe iki yöntem arasındaki fark büyür.",
        };
        var q2 = new QuizQuestion
        {
            Id = Id("quiz-enflasyon-q2"),
            QuizId = quiz.Id,
            OrderIndex = 2,
            Type = QuizQuestionType.TrueFalse,
            Prompt = "Paran bir yılda %20 arttı ama enflasyon %25 olduysa, alım gücün artmıştır.",
            Explanation = "Nominal olarak paran büyüdü ama fiyatlar daha hızlı arttığı için aynı parayla daha az şey alabilirsin — " +
                          "reel getirin negatif. \"Rakam büyüdü\" her zaman \"zenginleştim\" anlamına gelmez.",
        };
        var q3 = new QuizQuestion
        {
            Id = Id("quiz-enflasyon-q3"),
            QuizId = quiz.Id,
            OrderIndex = 3,
            Type = QuizQuestionType.SingleChoice,
            Prompt = "Reel getiri neyi ölçer?",
            Explanation = "Reel getiri kazancını enflasyona göre düzeltir; \"param gerçekte ne kadar değer kazandı ya da kaybetti?\" " +
                          "sorusunu yanıtlar. Ham lira artışı ise nominal getiridir.",
        };
        db.QuizQuestions.AddRange(q1, q2, q3);

        db.QuizOptions.AddRange(
            new QuizOption { Id = Id("q1-o1"), QuestionId = q1.Id, OrderIndex = 1, Text = "%78 — ikisini toplarsın", IsCorrect = false },
            new QuizOption { Id = Id("q1-o2"), QuestionId = q1.Id, OrderIndex = 2, Text = "%2 — ikisini çıkarırsın", IsCorrect = false },
            new QuizOption { Id = Id("q1-o3"), QuestionId = q1.Id, OrderIndex = 3, Text = "Yaklaşık %1,4 — (1 + 0,40) / (1 + 0,38) − 1", IsCorrect = true },
            new QuizOption { Id = Id("q1-o4"), QuestionId = q1.Id, OrderIndex = 4, Text = "%40 — enflasyon getiriyi etkilemez", IsCorrect = false },

            new QuizOption { Id = Id("q2-o1"), QuestionId = q2.Id, OrderIndex = 1, Text = "Doğru", IsCorrect = false },
            new QuizOption { Id = Id("q2-o2"), QuestionId = q2.Id, OrderIndex = 2, Text = "Yanlış", IsCorrect = true },

            new QuizOption { Id = Id("q3-o1"), QuestionId = q3.Id, OrderIndex = 1, Text = "Paranın kaç lira arttığını", IsCorrect = false },
            new QuizOption { Id = Id("q3-o2"), QuestionId = q3.Id, OrderIndex = 2, Text = "Enflasyondan arındırılmış, gerçek alım gücü değişimini", IsCorrect = true },
            new QuizOption { Id = Id("q3-o3"), QuestionId = q3.Id, OrderIndex = 3, Text = "Bankanın uyguladığı faiz oranını", IsCorrect = false },
            new QuizOption { Id = Id("q3-o4"), QuestionId = q3.Id, OrderIndex = 4, Text = "Döviz kurundaki değişimi", IsCorrect = false });

        // ── İlerleme: HİÇ KAYIT YOK (karar 2026-07-19) ───────────────────────────
        // Önceden 1-3 "Tamamlandı" seed'leniyordu; bu, kullanıcıya hiç okumadığı
        // dersleri bitirmiş gibi gösteriyordu ve ilerleme çubuğunu yalanlıyordu.
        // Artık herkes sıfırdan başlar: Ders 1 açık, 2-5 ön-koşuldan TÜRETİLMİŞ kilitli.
        // Ders ancak mini testi geçilince tamamlanır (öğrenme kapısı — EducationService).
        _ = userId;

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Katmanlı ders içeriği seed'i (T6.1, 15 §2) — 5 dersin L1/L2/L3 + örnek + tuzak
    /// blokları. İçerik <see cref="EducationContent"/> dosyasında (ayrı tutuldu ki
    /// topluluk katkısına açılabilsin — 14 §4-D2).
    /// </summary>
    /// <remarks>
    /// <b>BLOK BAZINDA idempotent</b> — "hiç bölüm var mı?" değil, <b>"bu bölüm var mı?"</b>
    /// diye bakar (bölüm Id'leri deterministik). İki sebep:
    /// <list type="number">
    /// <item><see cref="SeedEducationAsync"/> "track var mı?" ile korunduğu için eğitimi
    ///   zaten almış DB'ler oradan hiç bölüm alamaz.</item>
    /// <item>İçeriğe <b>sonradan blok eklenebilir</b> (T6.2'nin <c>LiveContext</c>'i gibi);
    ///   kaba "hiç bölüm var mı?" kapısı bu eklemeleri mevcut kurulumlara indiremezdi.</item>
    /// </list>
    /// Ders bulunamazsa (özel/eksik kurulum) sessizce atlanır — seed hiçbir zaman çökmez.
    /// </remarks>
    private static async Task SeedEducationSectionsAsync(FinansDbContext db, CancellationToken ct)
    {
        // Ders kimlikleri deterministik (Id(...)) — slug'a değil kimliğe bağlanmak,
        // içerik ile ders eşleşmesini yeniden adlandırmalara karşı korur.
        var builders = new (Guid LessonId, Func<Guid, IEnumerable<LessonSection>> Build)[]
        {
            (Id("lesson-enflasyon"), EducationContent.Lesson1),
            (Id("lesson-cesitlendirme"), EducationContent.Lesson2),
            (Id("lesson-fk-pddd"), EducationContent.Lesson3),
            (Id("lesson-risk-getiri"), EducationContent.Lesson4),
            (Id("lesson-bilesik"), EducationContent.Lesson5),
        };

        var existingLessons = await db.Lessons.Select(l => l.Id).ToListAsync(ct);
        var existingSections = await db.LessonSections.Select(s => s.Id).ToListAsync(ct);
        var known = existingSections.ToHashSet();
        var added = 0;

        foreach (var (lessonId, build) in builders)
        {
            if (!existingLessons.Contains(lessonId))
                continue; // beklenmedik kurulum — bu dersi atla, seed'i çökertme

            foreach (var section in build(lessonId))
            {
                if (known.Contains(section.Id))
                    continue; // bu blok zaten var → çoğaltma

                db.LessonSections.Add(section);
                added++;
            }
        }

        if (added > 0)
            await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// 2-5. derslerin mini testleri (T6.1). Ders 1'inki T5E.2'de gelmişti; bu adım
    /// kalan dörde 3'er soru ekler. <b>Kendi kapısı:</b> hedef derste quiz var mı?
    /// (Derse en fazla bir quiz — <c>Quizzes.LessonId</c> UNIQUE.)
    /// </summary>
    private static async Task SeedRemainingQuizzesAsync(FinansDbContext db, CancellationToken ct)
    {
        foreach (var (lessonKey, quizKey, title, questions) in EducationContent.RemainingQuizzes())
        {
            var lessonId = Id(lessonKey);
            if (!await db.Lessons.AnyAsync(l => l.Id == lessonId, ct))
                continue; // ders yoksa atla
            if (await db.Quizzes.AnyAsync(q => q.LessonId == lessonId, ct))
                continue; // bu dersin testi zaten var → idempotent

            var quiz = new Quiz
            {
                Id = Id(quizKey),
                LessonId = lessonId,
                Title = title,
                PassingScore = 60,
            };
            db.Quizzes.Add(quiz);

            var qOrder = 1;
            foreach (var q in questions)
            {
                var question = new QuizQuestion
                {
                    Id = Id($"{quizKey}-q{qOrder}"),
                    QuizId = quiz.Id,
                    OrderIndex = qOrder,
                    Type = q.Type,
                    Prompt = q.Prompt,
                    Explanation = q.Explanation,
                };
                db.QuizQuestions.Add(question);

                var oOrder = 1;
                foreach (var (text, isCorrect) in q.Options)
                {
                    db.QuizOptions.Add(new QuizOption
                    {
                        Id = Id($"{quizKey}-q{qOrder}-o{oOrder}"),
                        QuestionId = question.Id,
                        OrderIndex = oOrder,
                        Text = text,
                        IsCorrect = isCorrect,
                    });
                    oOrder++;
                }

                qOrder++;
            }
        }

        await db.SaveChangesAsync(ct);
    }
}

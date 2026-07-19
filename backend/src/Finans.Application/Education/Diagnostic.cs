using Finans.Domain.Enums;

namespace Finans.Application.Education;

// Tanılama testi (T6.6, 15 §4): eğitime başlamadan ÖNCE sorulan 8 soru.
// 4 bilgi sorusu → LiteracyLevel (içerik DERİNLİĞİ)
// 4 senaryo sorusu → RiskAttitude (ders SIRASI/vurgusu — kullanıcıya GÖSTERİLMEZ)
//
// ⚠ SPK sınırı (15 §1.1): bu bir yerindelik/uygunluk testi DEĞİLDİR. Çıktı hiçbir
// dağılım önerisi üretmez; risk tutumu arayüze de çıkmaz. "Utandırmayan" ilke
// gereği yanlış cevapta puan/kırmızı gösterilmez (14 §4-A2).

/// <summary>Tanılama sorusu türü — puanlanır mı, yoksa tutum mu ölçer?</summary>
public enum DiagnosticKind
{
    /// <summary>Nesnel, tek doğru cevap → <see cref="LessonLevel"/> hesabına girer.</summary>
    Knowledge,

    /// <summary>Doğru cevabı YOK; yalnız tutum sinyali → ders sırası.</summary>
    Scenario,
}

/// <summary>Tanılama sorusu şıkkı (istemciye <c>IsCorrect</c> sızmaz).</summary>
public sealed record DiagnosticOptionDto(string Key, string Text);

/// <summary>Tanılama sorusu (istemciye giden hâli — cevap anahtarı YOK).</summary>
public sealed record DiagnosticQuestionDto(
    string Key, DiagnosticKind Kind, string Prompt, IReadOnlyList<DiagnosticOptionDto> Options);

/// <summary>Kullanıcının verdiği cevap.</summary>
public sealed record DiagnosticAnswerInput(string QuestionKey, string OptionKey);

/// <summary>Tanılama gönderimi. Boş liste = "atla" (profil ölçülmedi sayılır).</summary>
public sealed record SubmitDiagnosticRequest(IReadOnlyList<DiagnosticAnswerInput> Answers);

/// <summary>
/// Tanılama sonucu — <b>yalnızca</b> bilgi seviyesi ve yönlendirme mesajı döner.
/// <see cref="Domain.Enums.RiskAttitude"/> BİLİNÇLİ olarak bu DTO'da YOKTUR (15 §1.1).
/// </summary>
public sealed record DiagnosticResultDto(LessonLevel LiteracyLevel, string Message);

/// <summary>Kullanıcının profil durumu (onboarding'in gösterilip gösterilmeyeceği).</summary>
public sealed record LiteracyProfileDto(LessonLevel? LiteracyLevel, bool Profiled);

/// <summary>Tanılama testi: soruları ver, cevapları değerlendir, profili yaz.</summary>
public interface IDiagnosticService
{
    IReadOnlyList<DiagnosticQuestionDto> GetQuestions();

    Task<LiteracyProfileDto> GetProfileAsync(CancellationToken ct = default);

    Task<DiagnosticResultDto> SubmitAsync(SubmitDiagnosticRequest request, CancellationToken ct = default);
}

/// <summary>
/// Tanılama soru bankası + puanlama (saf, deterministik — test edilebilir).
/// </summary>
public static class DiagnosticContent
{
    /// <summary>Bir şıkkın iç karşılığı: doğru mu (bilgi) / hangi tutum sinyali (senaryo).</summary>
    internal sealed record Option(string Key, string Text, bool IsCorrect = false, int RiskPoints = 0);

    internal sealed record Question(string Key, DiagnosticKind Kind, string Prompt, Option[] Options);

    /// <summary>4 bilgi + 4 senaryo. Sıra sabit (istemci ilerleme çubuğu çizer).</summary>
    internal static readonly Question[] Bank =
    [
        // ── Bilgi (nesnel) ───────────────────────────────────────────────────
        new("real-return", DiagnosticKind.Knowledge,
            "100.000 ₺'n var. Yıllık faiz %40, enflasyon %50. Yıl sonunda alım gücün ne olur?",
            [
                new("increased", "Arttı"),
                new("decreased", "Azaldı", IsCorrect: true),
                new("same", "Aynı kaldı"),
                new("unknown", "Emin değilim"),
            ]),
        new("concentration", DiagnosticKind.Knowledge,
            "Tüm birikimin tek bir varlıkta duruyor. Bu neyi artırır?",
            [
                new("return", "Beklenen getiriyi"),
                new("concentration", "Tek bir olaydan etkilenme riskini", IsCorrect: true),
                new("liquidity", "Paraya çevirme kolaylığını"),
                new("unknown", "Emin değilim"),
            ]),
        new("pe-ratio", DiagnosticKind.Knowledge,
            "Bir şirketin F/K oranı sektör ortalamasının çok altında. Bu tek başına ne söyler?",
            [
                new("cheap", "Hisse kesinlikle ucuzdur"),
                new("context", "Tek başına yeterli değildir; sebebi araştırılmalı", IsCorrect: true),
                new("bad", "Şirket kesinlikle zarar ediyordur"),
                new("unknown", "Emin değilim"),
            ]),
        new("compound", DiagnosticKind.Knowledge,
            "Yılda %20 getiren 100.000 ₺, iki yıl sonunda yaklaşık kaç olur?",
            [
                new("140", "140.000 ₺"),
                new("144", "144.000 ₺", IsCorrect: true),
                new("120", "120.000 ₺"),
                new("unknown", "Emin değilim"),
            ]),

        // ── Senaryo (doğru cevap YOK — yalnız tutum sinyali) ─────────────────
        new("drawdown", DiagnosticKind.Scenario,
            "Portföyün bir ayda %20 değer kaybetti. İlk tepkin ne olur?",
            [
                new("sell", "Daha da düşmeden satarım", RiskPoints: 0),
                new("wait", "Beklerim, zaman tanırım", RiskPoints: 1),
                new("buy", "Fırsat görür, eklerim", RiskPoints: 2),
            ]),
        new("fomo", DiagnosticKind.Scenario,
            "Bir tanıdığın 3 ayda %200 kazandığı bir yatırımdan bahsediyor. Ne hissedersin?",
            [
                new("suspicious", "Şüphelenirim", RiskPoints: 0),
                new("curious", "Merak eder, araştırırım", RiskPoints: 1),
                new("missed", "Kaçırdığımı düşünürüm", RiskPoints: 2),
            ]),
        new("horizon", DiagnosticKind.Scenario,
            "Bu parayı ne zaman kullanmayı düşünüyorsun?",
            [
                new("short", "1 yıl içinde", RiskPoints: 0),
                new("mid", "1-5 yıl arası", RiskPoints: 1),
                new("long", "5 yıldan uzun", RiskPoints: 2),
            ]),
        new("volatility", DiagnosticKind.Scenario,
            "Yatırımının değeri günlük olarak dalgalanıyor. Bu seni ne kadar rahatsız eder?",
            [
                new("much", "Çok — her gün kontrol ederim", RiskPoints: 0),
                new("some", "Biraz, ama alışırım", RiskPoints: 1),
                new("little", "Az — uzun vadeye bakarım", RiskPoints: 2),
            ]),
    ];

    /// <summary>Soru anahtarı bilinen mi? (girdi doğrulaması — 11 §4)</summary>
    public static bool IsKnownQuestion(string questionKey) =>
        Bank.Any(q => q.Key == questionKey);

    /// <summary>Şık, o soruya ait geçerli bir seçenek mi? (girdi doğrulaması — 11 §4)</summary>
    public static bool IsKnownOption(string questionKey, string optionKey) =>
        Bank.FirstOrDefault(q => q.Key == questionKey)?.Options.Any(o => o.Key == optionKey) ?? false;

    /// <summary>İstemciye giden hâl — cevap anahtarı ve risk puanları SIZMAZ.</summary>
    public static IReadOnlyList<DiagnosticQuestionDto> Questions() =>
        Bank.Select(q => new DiagnosticQuestionDto(
                q.Key, q.Kind, q.Prompt,
                q.Options.Select(o => new DiagnosticOptionDto(o.Key, o.Text)).ToList()))
            .ToList();

    /// <summary>
    /// Bilgi skoru → seviye. 0-1 doğru: Başlangıç · 2-3: Gelişen · 4: İleri.
    /// Cevaplanmamış soru yanlış sayılır (atlamak kullanıcıyı üst seviyeye taşımaz).
    /// </summary>
    public static LessonLevel ScoreLiteracy(IReadOnlyDictionary<string, string> answers)
    {
        var correct = Bank
            .Where(q => q.Kind == DiagnosticKind.Knowledge)
            .Count(q => answers.TryGetValue(q.Key, out var picked)
                        && q.Options.Any(o => o.Key == picked && o.IsCorrect));

        return correct switch
        {
            >= 4 => LessonLevel.Advanced,
            >= 2 => LessonLevel.Intermediate,
            _ => LessonLevel.Beginner,
        };
    }

    /// <summary>
    /// Senaryo puanları → tutum. Toplam 0-8; eşikler eşit üçe böler.
    /// Cevaplanmamış senaryo 1 (nötr) sayılır — atlayan "Dengeli" çıkar.
    /// </summary>
    public static RiskAttitude ScoreRiskAttitude(IReadOnlyDictionary<string, string> answers)
    {
        var scenarios = Bank.Where(q => q.Kind == DiagnosticKind.Scenario).ToList();
        var total = scenarios.Sum(q =>
            answers.TryGetValue(q.Key, out var picked)
                ? q.Options.FirstOrDefault(o => o.Key == picked)?.RiskPoints ?? 1
                : 1);

        var max = scenarios.Count * 2;
        return total <= max / 3 ? RiskAttitude.Temkinli
            : total >= max * 2 / 3 ? RiskAttitude.Atilgan
            : RiskAttitude.Dengeli;
    }

    /// <summary>
    /// Seviyeye göre yönlendirme mesajı — <b>utandırmaz</b>, puan/yanlış sayısı vermez
    /// ve risk tutumunu ASLA anmaz (15 §1.1, §4).
    /// </summary>
    public static string Message(LessonLevel level) => level switch
    {
        LessonLevel.Advanced =>
            "Temeller sende sağlam görünüyor. Dersleri derin katmanlarıyla açacağız; " +
            "istersen özet kısımları atlayabilirsin.",
        LessonLevel.Intermediate =>
            "İyi bir zeminin var. Dersleri ana anlatım + biraz derinlikle göstereceğiz; " +
            "daha fazlasını istediğinde derine inebilirsin.",
        _ =>
            "Baştan başlayalım. Dersleri sade ve kısa tutacağız; hazır hissettiğinde " +
            "her dersin derin katmanını açabilirsin.",
    };
}

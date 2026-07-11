using System.Text.RegularExpressions;

namespace Finans.Application.Llm;

/// <summary>
/// Dil saflığı bekçisi (T3.11 — 2026-07-11 canlı gözlem). Ücretsiz katman LLM'leri Türkçe
/// metne başka dillerden sızıntı yapabiliyor: İngilizce kelimeler ("invested olduğunda",
/// "%5 faiz means 100 TL becomes 105 TL"), hatta CJK karakterleri ("yediğini意味します").
/// Ayrıca girdi JSON'unun alan adları ("ownShare", "stateShare") metne aynen sızabiliyor.
///
/// <para>İki deterministik savunma:</para>
/// <list type="number">
///   <item><see cref="TranslateFieldNames"/> — bilinen alan adlarını Türkçe karşılığıyla
///     DEĞİŞTİRİR (kart kurtarılır; içerik başka türlü sağlıklıysa düşürmek israf).</item>
///   <item><see cref="IsForeign"/> — Latin dışı alfabe (CJK/Kiril/Arap…) veya bariz İngilizce
///     kelime sızıntısı içeren kartı İŞARETLER → servis kartı düşürür (deterministik olarak
///     düzeltilemez; yarım çeviri kullanıcı güvenini kırar).</item>
/// </list>
///
/// <para><b>Yanlış pozitif disiplini:</b> İngilizce kelime listesi yalnız Türkçe'de sözcük
/// olarak BULUNMAYAN kelimelerden kurulur ("on", "at", "an", "but", "is" gibi Türkçe eşsesliler
/// listeye alınmaz) ve kelime sınırıyla aranır. Emoji ve semboller (₺, %, …) serbesttir.</para>
/// </summary>
public static class CommentaryLanguageGuard
{
    private const RegexOptions Opt =
        RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase;

    /// <summary>
    /// Girdi JSON alan adları → Türkçe okunuş. Kart metnine sızarsa değiştirilir
    /// (anonim yükün alan adlarıyla senkron tutulmalı — <see cref="AnonymizedPortfolioSummary"/>).
    /// </summary>
    private static readonly (Regex Pattern, string Turkish)[] FieldNames =
    [
        (new Regex(@"\bownShare\b", Opt), "kendi katkı payı"),
        (new Regex(@"\bstateShare\b", Opt), "devlet katkısı payı"),
        (new Regex(@"\breturnRatio\b", Opt), "getiri oranı"),
        (new Regex(@"\brealReturnRatio\b", Opt), "reel getiri oranı"),
        (new Regex(@"\bcashWeight\b", Opt), "nakit oranı"),
        (new Regex(@"\bconcentrationTop2\b", Opt), "ilk iki kalem yoğunlaşması"),
        (new Regex(@"\bholdingCount\b", Opt), "kalem sayısı"),
        (new Regex(@"\bitemCount\b", Opt), "kalem sayısı"),
        (new Regex(@"\btotalValue\b", Opt), "toplam değer"),
        (new Regex(@"\btotalCost\b", Opt), "toplam maliyet"),
        (new Regex(@"\bnetProfit\b", Opt), "net kâr"),
        (new Regex(@"\bbaseCurrency\b", Opt), "baz para birimi"),
    ];

    /// <summary>
    /// Türkçe'de sözcük olarak bulunmayan, sık sızan İngilizce kelimeler (kelime sınırlı).
    /// Kısa/eşsesli kelimeler ("on", "at", "an", "but", "is", "bin"…) BİLEREK dışarıda.
    /// </summary>
    private static readonly Regex EnglishLeak = new(
        @"\b(the|and|with|from|your|this|that|these|those|which|while|where|there|their|" +
        @"when|then|will|would|should|could|have|has|been|being|because|however|therefore|" +
        @"although|means|become|becomes|became|invested|investment|investor|portfolio|" +
        @"increase|increases|decreased?|decreases|money|value|about|after|before|more|less|than|" +
        @"also|only|just|very|much|many|some|other|you|are|was|were|it's)\b",
        Opt);

    /// <summary>Bilinen alan adı sızıntılarını Türkçe karşılığıyla değiştirir (kartı kurtarır).</summary>
    public static string TranslateFieldNames(string text)
    {
        foreach (var (pattern, turkish) in FieldNames)
            text = pattern.Replace(text, turkish);
        return text;
    }

    /// <summary>
    /// Kart metni (başlık + gövde + detail) Türkçe dışı içerik taşıyor mu?
    /// </summary>
    /// <param name="reason">"foreign_script" (Latin dışı alfabe) | "english_leak" — log/metrik için.</param>
    public static bool IsForeign(string title, string body, string? detail, out string reason)
    {
        var text = detail is null ? title + "\n" + body : title + "\n" + body + "\n" + detail;

        if (ContainsForeignScript(text))
        {
            reason = "foreign_script";
            return true;
        }

        if (EnglishLeak.IsMatch(text))
        {
            reason = "english_leak";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    /// <summary>
    /// Latin dışı yazı sistemi karakteri var mı? (CJK, Hiragana/Katakana, Hangul, Kiril, Arap,
    /// İbrani, Tay, Devanagari + tam genişlik formlar.) Emoji ve semboller bu aralıkların
    /// DIŞINDA kalır → serbest.
    /// </summary>
    private static bool ContainsForeignScript(string text)
    {
        foreach (var ch in text)
        {
            int c = ch;
            if ((c >= 0x0400 && c <= 0x04FF) ||   // Kiril
                (c >= 0x0590 && c <= 0x05FF) ||   // İbrani
                (c >= 0x0600 && c <= 0x06FF) ||   // Arap
                (c >= 0x0900 && c <= 0x097F) ||   // Devanagari
                (c >= 0x0E00 && c <= 0x0E7F) ||   // Tay
                (c >= 0x1100 && c <= 0x11FF) ||   // Hangul Jamo
                (c >= 0x2E80 && c <= 0x9FFF) ||   // CJK radikaller + Hiragana/Katakana + Han
                (c >= 0xAC00 && c <= 0xD7AF) ||   // Hangul heceleri
                (c >= 0xFF00 && c <= 0xFFEF))     // Tam genişlik formlar (｡ ､ ｢ vb.)
                return true;
        }
        return false;
    }
}

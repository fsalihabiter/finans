using System.Text;
using System.Text.RegularExpressions;

namespace Finans.Application.Llm;

/// <summary>
/// Çıktı güvenlik filtresi (T3.5 — 07 §7, CLAUDE.md §2). Prompt korkuluğuna (kuşak-1) ek
/// <b>kuşak-2 savunma derinliği</b>: LLM bir kartta yasaklı <b>yönlendirme</b> ("al/sat/şuna gir/
/// şundan çık") veya <b>gelecek tahmini</b> ("yükselecek/düşecek/önümüzdeki ay yükselir") kalıbı
/// üretirse o kart yakalanır → servis katmanı kartı düşürür (hepsi düşerse fallback'e iner).
///
/// <para>
/// <b>Tasarım ilkesi (07 §7):</b> filtre <b>yönlendirme bağlamına</b> odaklanır, kelime avına değil.
/// Meşru eğitim metni kesilmemeli:
/// <list type="bullet">
///   <item>"satın alma gücü" / "alım gücü" → temiz (al/sat kökü var ama yönlendirme yok).</item>
///   <item>"enflasyon yükselirse" / "değer kaybedebilir" → temiz (koşul/olasılık, kesin tahmin değil).</item>
///   <item>"Altından çıkıp hisseye geçmelisin" → yakalanır (öneri eki -meli).</item>
///   <item>"BES eklemek mantıklı olur" → yakalanır (öneri çerçevesi).</item>
///   <item>"Önümüzdeki ay USD/TRY yükselir" → yakalanır (zaman imi + kesin yön).</item>
/// </list>
/// </para>
///
/// <para>
/// <b>Türkçe normalizasyon:</b> LLM çıktısı her zaman tam diyakritikli gelmez (ör. "almalisin").
/// Bu yüzden metin ASCII'ye katlanır (ç→c, ş→s, ı/İ→i, ö→o, ü→u, ğ→g, â→a…) ve kalıplar katlanmış
/// biçimde yazılır → diyakritik olsa da olmasa da yakalanır.
/// </para>
///
/// <para>
/// <b>Kapsam dışı:</b> "yeni rakam uydurma" (CLAUDE.md §2) kalıp taramasıyla güvenilir saptanamaz
/// (kartlar girdideki yüzdeleri meşru biçimde anar); bu kural kuşak-1 prompt + parse katmanına bırakıldı.
/// Bu filtre yalnız <b>yönlendirme/tahmin</b> kalıbına odaklanır.
/// </para>
/// </summary>
public static class CommentaryOutputGuard
{
    private const RegexOptions Opt =
        RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase;

    // ── Yönlendirme (öneri/komut) — kesin yakala ──
    private static readonly Regex[] Directive =
    [
        // Alım-satım fiili + öneri eki (-mali/-meli[+kişi]): almalisin, satmali, gecmelisin,
        // girmeli, cikmali, eklemeli, tutmali, yatirmali. "alma"/"satin" (isim) eşleşmez.
        new(@"\b(al|sat|gec|gir|cik|ekle|tut|yatir)(mali|meli)(sin|siniz|yiz)?\b", Opt),
        // Açık tavsiye fiilleri.
        new(@"\b(tavsiye\s+ed(er|iyor|il)|tavsiyem|tavsiye\s+olunur|oneririm|oneriyorum|oneri\s+olarak)\b", Opt),
        // Öneri çerçevesi: "… mantikli/iyi/dogru/yerinde/akillica olur/olabilir".
        new(@"\b(mantikli|akillica|yerinde|dogru|iyi)\s+(olur|olabilir|olacak)\b", Opt),
        // Aciliyet + çıplak emir: "hemen al", "simdi sat", "bu seviyeden gir/ekle".
        new(@"\b(hemen|simdi|bu\s+seviyeden|su\s+anda?)\s+(al|sat|gir|cik|ekle)\w*", Opt),
        // "fırsatı kaçırma" tipi dürtme.
        new(@"\bfirsat(i|ini)?\s+kacir\w*", Opt),
    ];

    // ── Gelecek tahmini (kesin yön) — zaman imi gerekmeden yasak ──
    private static readonly Regex[] Prediction =
    [
        new(@"\b(yuksel|dus|uc)(ecek|acak)\b", Opt),                       // yukselecek/dusecek/ucacak
        new(@"\bpatlayacak\b", Opt),
        new(@"\b(deger\s+(kazan|kaybed)(ecek|acak)|kazandiracak|kaybettirecek|kar\s+(getirecek|ettirecek)|zarar\s+ettirecek)\b", Opt),
    ];

    // ── Zaman imi + kesin yön (aorist dahil) → tahmin. "yükselirse"/"-ebilir" KAPSANMAZ. ──
    private static readonly Regex ForwardMarker =
        new(@"\b(onumuzdeki|gelecek\s+(ay|hafta|yil|donem|gunler)|yakinda|ilerleyen\s+(gun|hafta|ay))\b", Opt);

    private static readonly Regex DirectionalDefinite =
        new(@"\b(yuksel(ir|ecek)|dus(er|ecek)|art(ar|acak)|azal(ir|acak)|deger\s+kaza(nir|nacak)|deger\s+kaybede(r|cek))\b", Opt);

    /// <summary>
    /// Bir kartın metninde (başlık + gövde + etiketler) yasaklı yönlendirme/tahmin kalıbı var mı?
    /// </summary>
    /// <param name="reason">Eşleşme türü ("directive" | "prediction" | "forecast") — log/metrik için.</param>
    public static bool IsForbidden(string title, string body, IReadOnlyList<string>? tags, out string reason)
    {
        var sb = new StringBuilder(title).Append('\n').Append(body);
        if (tags is not null)
            foreach (var t in tags) sb.Append('\n').Append(t);

        var text = Fold(sb.ToString());

        foreach (var rx in Directive)
            if (rx.IsMatch(text)) { reason = "directive"; return true; }
        foreach (var rx in Prediction)
            if (rx.IsMatch(text)) { reason = "prediction"; return true; }
        if (ForwardMarker.IsMatch(text) && DirectionalDefinite.IsMatch(text)) { reason = "forecast"; return true; }

        reason = string.Empty;
        return false;
    }

    /// <summary>Türkçe metni küçük-harf ASCII'ye katlar (diyakritik duyarsız eşleştirme için).</summary>
    private static string Fold(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            sb.Append(ch switch
            {
                'ç' or 'Ç' => 'c',
                'ğ' or 'Ğ' => 'g',
                'ı' or 'İ' => 'i',
                'ö' or 'Ö' => 'o',
                'ş' or 'Ş' => 's',
                'ü' or 'Ü' => 'u',
                'â' or 'Â' => 'a',
                'î' or 'Î' => 'i',
                'û' or 'Û' => 'u',
                _ => char.ToLowerInvariant(ch),
            });
        }
        return sb.ToString();
    }
}

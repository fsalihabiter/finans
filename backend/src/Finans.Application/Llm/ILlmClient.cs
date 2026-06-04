namespace Finans.Application.Llm;

/// <summary>
/// LLM sağlayıcı soyutlaması (T3.1 — 07 §2). Provider-neutral kontrat: <see cref="ILlmClient"/>
/// üzerindeki tipler Anthropic/OpenAI/Gemini fark etmeksizin aynı kalır; sağlayıcı değişirse yalnız
/// <c>Infrastructure</c>'daki uygulama değişir, üst katman dokunulmaz.
///
/// <para>
/// <b>KVKK çerçevesi (CLAUDE.md §2, §3.1; 11 §1):</b> bu kontrat aracılığıyla LLM'e gönderilen her
/// içerikte <b>kişisel veri YASAK</b>: <c>UserId</c>, isim, e-posta, hesap numarası, açık metin
/// pozisyon kimliği vb. <b>anonim portföy özeti</b> (oranlar, kategoriler, toplam değer/getiri) yeterli
/// ve gönderilebilen tek şey. Servis katmanı (T3.3 <c>LlmCommentaryService</c>) anonimleştirmeden
/// sorumludur; <see cref="ILlmClient"/> bunu varsayar.
/// </para>
///
/// <para>
/// <b>Tek cümlelik kural (CLAUDE.md §3.1):</b> LLM hesap YAPMAZ, KODUN hazırladığı sayıları
/// <b>eğitici dille yorumlar</b>. Bu yüzden <see cref="LlmRequest.UserPrompt"/> içinde verilen veri
/// "doğruluk kaynağı" sayılır; LLM çıktısında yeni rakam görülürse parse aşamasında reddedilir.
/// </para>
/// </summary>
public interface ILlmClient
{
    /// <summary>
    /// Bir LLM çağrısı yapar. Sağlayıcı/transport hatasında <see cref="LlmResult.Success"/> = false
    /// dönülür (exception fırlatılmaz — 07 §5 fallback'in alt katmanı). <see cref="LlmRequest.JsonSchema"/>
    /// verilirse sağlayıcının yapılandırılmış çıktı / tool-use özelliğiyle JSON zorlanmaya çalışılır;
    /// yine de üst katman parse'ı güvenli yapmak zorunda (sözleşme: "olabildiğince JSON").
    /// </summary>
    Task<LlmResult> CompleteAsync(LlmRequest request, CancellationToken ct = default);
}

/// <summary>
/// LLM isteği: sistem talimatı + kullanıcı mesajı + (opsiyonel) JSON şema. KVKK: <see cref="UserPrompt"/>
/// içeriği <b>anonim</b> olmalıdır (yukarı bakın). <see cref="MaxOutputTokens"/> maliyet/kesinti sınırı.
/// </summary>
public sealed record LlmRequest(
    string SystemPrompt,
    string UserPrompt,
    string? JsonSchema = null,
    int MaxOutputTokens = 1024,
    decimal Temperature = 0.2m);

/// <summary>
/// LLM sonucu. Başarılı: <see cref="Text"/> doludur (sağlayıcı JSON şema verilmişse JSON dener),
/// <see cref="InputTokens"/>/<see cref="OutputTokens"/> maliyet metriği için (T3.9'da
/// Prometheus'a yansıyacak). Hata: <see cref="Success"/>=false + <see cref="ErrorReason"/>.
/// Üst katman (parse + fallback, 07 §5) hatada son başarılı cache'i ya da düz fallback metnini döner.
/// </summary>
public sealed record LlmResult(
    bool Success,
    string Text,
    int InputTokens,
    int OutputTokens,
    string? ErrorReason = null)
{
    public static LlmResult Ok(string text, int inputTokens, int outputTokens) =>
        new(true, text, inputTokens, outputTokens);

    public static LlmResult Fail(string reason) =>
        new(false, string.Empty, 0, 0, reason);
}

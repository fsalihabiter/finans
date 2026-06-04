namespace Finans.Infrastructure.Llm;

/// <summary>
/// LLM yapılandırması (T3.1). Sağlayıcı seçimi <see cref="Provider"/> üzerinden; <see cref="ApiKey"/>
/// env/User Secrets'tan gelir, repoda yer ALMAZ (11 §6). <see cref="Model"/> sağlayıcının kabul ettiği
/// model adı. <see cref="TimeoutSeconds"/> dış çağrı budget'ı (10 §2).
/// </summary>
public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    /// <summary>
    /// "Anthropic" | "OpenRouter" | (boş/diğer) → Noop (dev/test güvenli). Soyutlama sayesinde ileride
    /// "Gemini" / "Groq" / "Ollama" dalları eklenebilir, sözleşme değişmez.
    /// </summary>
    public string Provider { get; set; } = "Anthropic";

    /// <summary>Sağlayıcı API anahtarı. <b>Repoda olamaz</b> — env/User Secrets (11 §6).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Varsayılan model. Anthropic için <c>claude-sonnet-4-6</c>; OpenRouter için kullanıcı kendi
    /// modelini env ile yazar (örn. <c>meta-llama/llama-3.3-70b-instruct:free</c>). Provider'a göre
    /// kendi DI dalı uygun default'u kullanır.
    /// </summary>
    public string Model { get; set; } = "claude-sonnet-4-6";

    /// <summary>Anthropic API sürümü (header). Yalnızca Anthropic için anlamlı.</summary>
    public string AnthropicVersion { get; set; } = "2023-06-01";

    /// <summary>Tek çağrı için maksimum bekleme; sınırı aşan çağrı fallback'e düşer.</summary>
    public int TimeoutSeconds { get; set; } = 20;

    /// <summary>
    /// Sağlayıcı taban URL'i. Anthropic varsayılan <c>api.anthropic.com</c>; OpenRouter
    /// <c>openrouter.ai/api/</c>. Kendi gateway / Azure / test stub için override edilebilir.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.anthropic.com/";

    // ── OpenRouter'a özgü opsiyonel meta header'lar (sağlayıcı analytics + öncelik için)──
    /// <summary>OpenRouter <c>HTTP-Referer</c> header'ı — uygulama URL'i.</summary>
    public string OpenRouterAppUrl { get; set; } = "https://localhost";

    /// <summary>OpenRouter <c>X-Title</c> header'ı — uygulama adı.</summary>
    public string OpenRouterAppName { get; set; } = "Finans";
}

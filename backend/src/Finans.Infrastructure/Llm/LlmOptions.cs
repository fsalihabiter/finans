namespace Finans.Infrastructure.Llm;

/// <summary>
/// LLM yapılandırması (T3.1). Sağlayıcı seçimi <see cref="Provider"/> üzerinden; <see cref="ApiKey"/>
/// env/User Secrets'tan gelir, repoda yer ALMAZ (11 §6). <see cref="Model"/> sağlayıcının kabul ettiği
/// model adı. <see cref="TimeoutSeconds"/> dış çağrı budget'ı (10 §2).
/// </summary>
public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    /// <summary>"Anthropic" | "Noop" (yapılandırılmamışsa otomatik). İleride: OpenAI/Gemini.</summary>
    public string Provider { get; set; } = "Anthropic";

    /// <summary>Anthropic Messages API anahtarı. <b>Repoda olamaz</b> — env/User Secrets.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Varsayılan model. Faz 3 için Sonnet 4.6 (Türkçe yorum + tool-use kalitesi); maliyet sıkışırsa
    /// Haiku 4.5 ile değiştirilebilir (NFR-9 cache disiplini bunu rahat tutar).
    /// </summary>
    public string Model { get; set; } = "claude-sonnet-4-6";

    /// <summary>Anthropic API sürümü (header).</summary>
    public string AnthropicVersion { get; set; } = "2023-06-01";

    /// <summary>Tek çağrı için maksimum bekleme; sınırı aşan çağrı fallback'e düşer.</summary>
    public int TimeoutSeconds { get; set; } = 20;

    /// <summary>Sağlayıcı taban URL'i (test edilebilirlik / Azure proxy / kendi gateway).</summary>
    public string BaseUrl { get; set; } = "https://api.anthropic.com/";
}

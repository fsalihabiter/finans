using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Finans.Application.Llm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Finans.Infrastructure.Llm;

/// <summary>
/// OpenRouter (OpenAI-uyumlu) sağlayıcı (T3.1 alternatifi — geliştirme aşamasında ücretsiz model
/// erişimi için). Tek anahtarla DeepSeek/Llama/Qwen/Mistral free varyantlarına geçilebilir;
/// ileride aynı sözleşmeyle ücretliye geçiş kolay (modeli değiştirmek yeter).
///
/// <para>
/// KVKK çerçevesi <see cref="ILlmClient"/> sözleşmesindeki ile aynı: gönderilen içerik anonim
/// portföy özetidir; UserId/PII gönderilmez (07 §2). OpenRouter de tüm sağlayıcılar gibi prompt
/// içeriğini sağlayıcılarına iletir — anonimleştirme bu yüzden TEK güvenlik halkası değil ama
/// servis katmanında zorunlu.
/// </para>
///
/// <para>
/// <b>JSON şema:</b> OpenRouter modellerinin hepsi <c>json_schema</c> response_format'ını
/// desteklemez; daha geniş uyum için <c>json_object</c> moduna düşeriz ve şemayı sistem promptuna
/// ekleyerek modele dayatırız. Üst katmandaki güvenli parse (T3.4) zaten şema sınırlarını ikinci
/// kez uygular — model şemayı tam tutmasa da kart düşer veya kırpılır.
/// </para>
///
/// <para>
/// <b>Hata yönetimi:</b> HTTP/network/parse hatalarında exception fırlatılmaz —
/// <see cref="LlmResult.Fail"/> dönülür (07 §5 fallback'in alt katmanı).
/// </para>
/// </summary>
public sealed class OpenRouterLlmClient(
    HttpClient httpClient,
    IOptions<LlmOptions> options,
    ILogger<OpenRouterLlmClient> logger) : ILlmClient
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<LlmResult> CompleteAsync(LlmRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var opts = options.Value;

        if (string.IsNullOrWhiteSpace(opts.ApiKey))
            return LlmResult.Fail("llm_not_configured");

        // Şema verilmişse sistem promptuna ek talimat yaz + json_object zorla. Anthropic'in tool_use
        // dayatması olmadığı için modelin "düz JSON dön" buyruğunu doğrudan görmesi gerekir.
        var systemPrompt = request.JsonSchema is { } schema
            ? request.SystemPrompt + "\n\nÇıktın AŞAĞIDAKİ JSON şemasına KESİNLİKLE uyan tek bir JSON " +
              "objesi olsun. Şemanın dışında alan üretme, başka metin yazma.\n\nJSON şeması:\n" + schema
            : request.SystemPrompt;

        var body = new ChatCompletionsRequest(
            Model: opts.Model,
            MaxTokens: request.MaxOutputTokens,
            Temperature: (double)request.Temperature,
            Messages:
            [
                new MessageDto("system", systemPrompt),
                new MessageDto("user", request.UserPrompt),
            ],
            ResponseFormat: request.JsonSchema is not null
                ? new ResponseFormatDto("json_object")
                : null);

        try
        {
            using var http = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
            {
                Content = JsonContent.Create(body, options: JsonOpts),
            };
            http.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opts.ApiKey);
            // OpenRouter analytics/öncelik için (zorunlu değil ama önerilir).
            http.Headers.TryAddWithoutValidation("HTTP-Referer", opts.OpenRouterAppUrl);
            http.Headers.TryAddWithoutValidation("X-Title", opts.OpenRouterAppName);
            http.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var resp = await httpClient.SendAsync(http, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                logger.LogWarning("OpenRouter API hata: {Status} {Reason}", (int)resp.StatusCode, resp.ReasonPhrase);
                return LlmResult.Fail($"http_{(int)resp.StatusCode}");
            }

            return ParseResponse(raw);
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested)
        {
            return LlmResult.Fail("cancelled");
        }
        catch (TaskCanceledException)
        {
            return LlmResult.Fail("timeout");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "OpenRouter API erişilemedi.");
            return LlmResult.Fail("network");
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "OpenRouter yanıtı parse edilemedi.");
            return LlmResult.Fail("parse");
        }
    }

    /// <summary>
    /// OpenAI-uyumlu yanıt: <c>choices[0].message.content</c> — JSON moduyla bu zaten düz JSON
    /// stringidir. <c>usage</c> alanı varsa token sayıları (T3.9 metriği için hazır).
    /// </summary>
    private static LlmResult ParseResponse(string raw)
    {
        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        var inputTokens = 0;
        var outputTokens = 0;
        if (root.TryGetProperty("usage", out var usage) && usage.ValueKind == JsonValueKind.Object)
        {
            if (usage.TryGetProperty("prompt_tokens", out var pt)) inputTokens = pt.GetInt32();
            if (usage.TryGetProperty("completion_tokens", out var ct)) outputTokens = ct.GetInt32();
        }

        if (!root.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array)
            return LlmResult.Fail("empty_choices");

        foreach (var choice in choices.EnumerateArray())
        {
            if (!choice.TryGetProperty("message", out var msg)) continue;
            if (!msg.TryGetProperty("content", out var content)) continue;
            var text = content.GetString();
            if (!string.IsNullOrEmpty(text))
                return LlmResult.Ok(text, inputTokens, outputTokens);
        }

        return LlmResult.Fail("empty_text");
    }

    // ── OpenAI-uyumlu Chat Completions DTO'ları ──
    private sealed record ChatCompletionsRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("messages")] IReadOnlyList<MessageDto> Messages,
        [property: JsonPropertyName("response_format")] ResponseFormatDto? ResponseFormat);

    private sealed record MessageDto(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ResponseFormatDto(
        [property: JsonPropertyName("type")] string Type);
}

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Finans.Application.Llm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Finans.Infrastructure.Llm;

/// <summary>
/// Anthropic Messages API uygulaması (T3.1 — 07 §2 sağlayıcı kararı). Doğrudan REST (resmi SDK
/// bağımlılığı eklemeden — küçük yüzey: tek endpoint, az alan). KVKK: <see cref="ILlmClient"/>
/// sözleşmesi gereği gönderilen metinler anonim varsayılır.
///
/// <para>
/// <b>JSON şema (yapılandırılmış çıktı):</b> <see cref="LlmRequest.JsonSchema"/> verilirse Anthropic
/// "tool use" özelliği üzerinden şema zorlanır — model tek bir `tool_use` üretir, içeriği talep edilen
/// şemaya uyar; <see cref="LlmResult.Text"/> içine bu tool input'unun JSON'u konur (T3.3 parse eder).
/// Şema yoksa düz metin yanıt döner.
/// </para>
///
/// <para>
/// <b>Hata yönetimi (07 §5):</b> HTTP/network/parse hatalarında exception fırlatılmaz —
/// <see cref="LlmResult.Fail"/> dönülür. Üst katman cache veya düz metin fallback ile devam eder.
/// </para>
/// </summary>
public sealed class AnthropicLlmClient(
    HttpClient httpClient,
    IOptions<LlmOptions> options,
    ILogger<AnthropicLlmClient> logger) : ILlmClient
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

        // Body: Messages API sözleşmesi. `system` ayrı; mesajlar `user` rolünde.
        // JSON şema verilirse tool use ile yapılandırılmış çıktı zorla.
        var body = new MessagesRequest(
            Model: opts.Model,
            MaxTokens: request.MaxOutputTokens,
            Temperature: (double)request.Temperature,
            System: request.SystemPrompt,
            Messages: [new MessageDto("user", request.UserPrompt)],
            Tools: request.JsonSchema is { } schema
                ? [new ToolDto("structured_output",
                    "Yapılandırılmış JSON çıktısı; yanıtı bu araç çağrısı içinde ver.",
                    ParseSchema(schema))]
                : null,
            ToolChoice: request.JsonSchema is not null
                ? new ToolChoiceDto("tool", "structured_output")
                : null);

        try
        {
            using var http = new HttpRequestMessage(HttpMethod.Post, "v1/messages")
            {
                Content = JsonContent.Create(body, options: JsonOpts),
            };
            http.Headers.Add("x-api-key", opts.ApiKey);
            http.Headers.Add("anthropic-version", opts.AnthropicVersion);
            http.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var resp = await httpClient.SendAsync(http, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                // İçerik sızdırmadan, sınıflandırılmış sebep ver (12 §3 — log'da ham metin değil özet).
                logger.LogWarning("Anthropic API hata: {Status} {Reason}", (int)resp.StatusCode, resp.ReasonPhrase);
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
            logger.LogWarning(ex, "Anthropic API erişilemedi.");
            return LlmResult.Fail("network");
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Anthropic yanıtı parse edilemedi.");
            return LlmResult.Fail("parse");
        }
    }

    /// <summary>
    /// Yanıtı çöz: tool_use varsa input JSON'unu, yoksa text block'ları yan yana getir.
    /// Token sayıları <c>usage</c>'tan (T3.9 metrik için).
    /// </summary>
    private static LlmResult ParseResponse(string raw)
    {
        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        var inputTokens = root.TryGetProperty("usage", out var usage) && usage.TryGetProperty("input_tokens", out var it)
            ? it.GetInt32() : 0;
        var outputTokens = usage.ValueKind == JsonValueKind.Object && usage.TryGetProperty("output_tokens", out var ot)
            ? ot.GetInt32() : 0;

        if (!root.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            return LlmResult.Fail("empty_content");

        // tool_use bloğu varsa onun input'unu döndür (yapılandırılmış JSON).
        foreach (var block in content.EnumerateArray())
        {
            if (block.TryGetProperty("type", out var t) && t.GetString() == "tool_use" &&
                block.TryGetProperty("input", out var input))
                return LlmResult.Ok(input.GetRawText(), inputTokens, outputTokens);
        }

        // Aksi halde text bloklarını birleştir.
        var sb = new System.Text.StringBuilder();
        foreach (var block in content.EnumerateArray())
        {
            if (block.TryGetProperty("type", out var t) && t.GetString() == "text" &&
                block.TryGetProperty("text", out var text))
                sb.Append(text.GetString());
        }

        var combined = sb.ToString();
        return string.IsNullOrEmpty(combined)
            ? LlmResult.Fail("empty_text")
            : LlmResult.Ok(combined, inputTokens, outputTokens);
    }

    private static JsonNode ParseSchema(string schemaJson) =>
        JsonNode.Parse(schemaJson)
        ?? throw new JsonException("LlmRequest.JsonSchema geçerli JSON değil.");

    // ── Anthropic Messages API DTO'ları (sözleşmenin tüm alanlarını taşımıyor — yalnız kullanılanlar) ──
    private sealed record MessagesRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("system")] string System,
        [property: JsonPropertyName("messages")] IReadOnlyList<MessageDto> Messages,
        [property: JsonPropertyName("tools")] IReadOnlyList<ToolDto>? Tools,
        [property: JsonPropertyName("tool_choice")] ToolChoiceDto? ToolChoice);

    private sealed record MessageDto(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ToolDto(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("input_schema")] JsonNode InputSchema);

    private sealed record ToolChoiceDto(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("name")] string Name);
}

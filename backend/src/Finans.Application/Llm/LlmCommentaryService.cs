using System.Text.Json;
using System.Text.Json.Serialization;
using Finans.Application.Portfolio;
using Microsoft.Extensions.Logging;

namespace Finans.Application.Llm;

/// <summary>
/// <see cref="ILlmCommentaryService"/> uygulaması (T3.3). Sorumluluk zinciri:
/// <list type="number">
///   <item><b>Anonimleştir</b> — <see cref="PortfolioAnonymizer"/> ile PII'siz özet (07 §2 KVKK).</item>
///   <item><b>User prompt JSON</b> — anonim özeti deterministik şekilde serileştir (T3.6 cache key dostu).</item>
///   <item><b>LLM çağrısı</b> — <see cref="CommentaryPrompts.SystemPrompt"/> + JSON şema +
///     <see cref="ILlmClient"/>; başarısızsa fallback.</item>
///   <item><b>Parse</b> — şemaya göre <see cref="CommentaryResponse"/> üret; parse hatasında fallback.</item>
/// </list>
/// </summary>
public sealed class LlmCommentaryService(
    ILlmClient llm,
    ILogger<LlmCommentaryService> logger,
    TimeProvider time) : ILlmCommentaryService
{
    /// <summary>Anonim özet → user-prompt JSON. Alan adları/sıralamaları deterministik (cache friendly).</summary>
    private static readonly JsonSerializerOptions PromptJsonOpts = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = false,
    };

    /// <summary>Şema kıracak ufak sapmaları kabul edecek esnek parse: camelCase.</summary>
    private static readonly JsonSerializerOptions ParseJsonOpts = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task<CommentaryResponse> GetCommentaryAsync(
        PortfolioSummaryDto summary, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var anon = PortfolioAnonymizer.Anonymize(summary);
        var userPrompt = JsonSerializer.Serialize(anon, PromptJsonOpts);

        var req = new LlmRequest(
            SystemPrompt: CommentaryPrompts.SystemPrompt,
            UserPrompt: userPrompt,
            JsonSchema: CommentaryPrompts.CommentaryJsonSchema,
            MaxOutputTokens: 1024,
            Temperature: 0.2m);

        var result = await llm.CompleteAsync(req, ct);
        if (!result.Success)
        {
            logger.LogInformation("LLM yorumu üretilemedi ({Reason}); fallback kartı dönülüyor.", result.ErrorReason);
            return Fallback();
        }

        if (TryParseCards(result.Text, out var cards) && cards.Count > 0)
            return new CommentaryResponse(cards, Source: "llm", GeneratedAtUtc: time.GetUtcNow().UtcDateTime);

        logger.LogWarning("LLM yanıtı şemayı tutmadı; fallback kartı dönülüyor.");
        return Fallback();
    }

    /// <summary>
    /// LLM erişilemediğinde / şema bozulduğunda gösterilecek düz metin kartı (07 §5). UI tarafında
    /// "Yorum şu an üretilemedi" durumu için belirli bir kart. Hesap/yönlendirme içermez.
    /// </summary>
    private CommentaryResponse Fallback() => new(
        Cards: new[]
        {
            new CommentaryCard(
                Emoji: "💬",
                Title: "Yorum şu an üretilemedi",
                Body: "Eğitici yorum servisine şu an ulaşılamıyor. Portföy sayıların doğru ve "
                    + "güncel — bu kart yenilenebilir, biraz sonra tekrar deneyebilirsin.",
                Tags: new[] { "fallback" }),
        },
        Source: "fallback",
        GeneratedAtUtc: time.GetUtcNow().UtcDateTime);

    /// <summary>
    /// Minimum güvenli parse: tam JSON parse → cards array → her kart normalize. Parse / şema hatası
    /// yutar (fallback'e düşülmesi için). T3.4 bu mantığı zenginleştirecek (eksik alan, fazla alan,
    /// type coercion, çıktı güvenlik filtresi T3.5).
    /// </summary>
    private static bool TryParseCards(string json, out IReadOnlyList<CommentaryCard> cards)
    {
        cards = Array.Empty<CommentaryCard>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("cards", out var cardsEl) ||
                cardsEl.ValueKind != JsonValueKind.Array)
                return false;

            var list = new List<CommentaryCard>();
            foreach (var c in cardsEl.EnumerateArray())
            {
                if (c.ValueKind != JsonValueKind.Object) continue;
                var emoji = ReadString(c, "emoji");
                var title = ReadString(c, "title");
                var body = ReadString(c, "body");
                if (string.IsNullOrWhiteSpace(emoji) || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
                    continue;

                CommentaryMeter? meter = null;
                if (c.TryGetProperty("meter", out var m) && m.ValueKind == JsonValueKind.Object &&
                    m.TryGetProperty("value", out var mv) && mv.TryGetDecimal(out var mvDec))
                {
                    meter = new CommentaryMeter(
                        Value: mvDec,
                        LowLabel: ReadString(m, "lowLabel") ?? string.Empty,
                        HighLabel: ReadString(m, "highLabel") ?? string.Empty);
                }

                IReadOnlyList<string>? tags = null;
                if (c.TryGetProperty("tags", out var t) && t.ValueKind == JsonValueKind.Array)
                    tags = t.EnumerateArray()
                        .Where(x => x.ValueKind == JsonValueKind.String)
                        .Select(x => x.GetString()!)
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList();

                list.Add(new CommentaryCard(emoji!, title!, body!, meter, tags));
            }

            cards = list;
            return list.Count > 0;
        }
        catch (JsonException)
        {
            return false;
        }

        static string? ReadString(JsonElement el, string name) =>
            el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    }
}

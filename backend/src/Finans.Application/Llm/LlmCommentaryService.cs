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
            // 2048 — OpenRouter free reasoning modelleri (Laguna/Nemotron) 1024'ün büyük kısmını
            // gizli düşünmede tüketip content'i yarım bırakıyordu. 2048 + OpenRouterLlmClient'taki
            // reasoning.exclude/enabled=false ikilisi content'in tamamlanmasını garantiler. Anthropic
            // için fazlalık değil — 5 kart × ~150 char ≈ 750 token; rahat marj.
            MaxOutputTokens: 2048,
            Temperature: 0.2m);

        var result = await llm.CompleteAsync(req, ct);
        if (!result.Success)
        {
            logger.LogInformation("LLM yorumu üretilemedi ({Reason}); fallback kartı dönülüyor.", result.ErrorReason);
            return Fallback();
        }

        var parsed = TryParseCards(result.Text, out var cards, out var guardBlocked);
        if (guardBlocked > 0)
            // Kuşak-2 koruma devreye girdi (T3.5 / 07 §7): prompt korkuluğunun kaçırdığı yönlendirme/
            // tahmin kalıbı çıktıda yakalandı. Görünür kıl ki model kalitesi sapması fark edilsin.
            logger.LogWarning(
                "LLM yorumunda {Count} kart çıktı güvenlik filtresine (T3.5) takıldı ve düşürüldü.", guardBlocked);

        if (parsed && cards.Count > 0)
            return new CommentaryResponse(cards, Source: "llm", GeneratedAtUtc: time.GetUtcNow().UtcDateTime);

        // Tanılama: ham yanıt anonim portföy yorumu (PII içermez). Hangi şema kuralında düştüğünü
        // (JSON parse / cards yok / per-kart filtre / güvenlik filtresi) görmek için ilk 400 char'ı logluyoruz.
        var preview = result.Text.Length > 400 ? result.Text[..400] + "…" : result.Text;
        logger.LogWarning("LLM yanıtı şemayı tutmadı; fallback kartı dönülüyor. Ham yanıt önizleme: {Preview}", preview);
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
    /// Güvenli parse (T3.4): tam JSON parse → cards → her kart normalize+sınırla. Şema sınırlarını
    /// (07 §4) modeli "tutamadığında" bile dayatır:
    /// <list type="bullet">
    ///   <item>Cards üst sınırı <see cref="CommentaryParseConstraints.MaxCards"/> — fazlası kırpılır.</item>
    ///   <item>Title <see cref="CommentaryParseConstraints.MaxTitle"/>'i aşarsa kırpılır; çok kısaysa kart düşer.</item>
    ///   <item>Body min/max — kısaysa kart düşer (anlamlı yorum değil), uzunsa kırpılır.</item>
    ///   <item>Meter value [0,1]'e clamp; eksik alanlı meter null.</item>
    ///   <item>Tags non-string elemanlar atılır; ilk <see cref="CommentaryParseConstraints.MaxTags"/> alınır.</item>
    ///   <item>Bilinmeyen alanlar yutulur (forward compat).</item>
    /// </list>
    /// Bütün JSON parse hatası yutulur → fallback'e düşülür (T3.4 birim testleriyle korunur). Üstüne
    /// T3.5 çıktı güvenlik filtresi (<see cref="CommentaryOutputGuard"/>) gelir: yasaklı yönlendirme/
    /// tahmin kalıbı içeren kart düşürülür (07 §7). <paramref name="guardBlocked"/> kaç kartın bu
    /// filtreyle düştüğünü döndürür (log/metrik için).
    /// </summary>
    private static bool TryParseCards(string json, out IReadOnlyList<CommentaryCard> cards, out int guardBlocked)
    {
        cards = Array.Empty<CommentaryCard>();
        guardBlocked = 0;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("cards", out var cardsEl) ||
                cardsEl.ValueKind != JsonValueKind.Array)
                return false;

            var list = new List<CommentaryCard>(CommentaryParseConstraints.MaxCards);
            foreach (var c in cardsEl.EnumerateArray())
            {
                if (list.Count >= CommentaryParseConstraints.MaxCards) break; // kırpma
                if (c.ValueKind != JsonValueKind.Object) continue;

                var emoji = ReadString(c, "emoji")?.Trim();
                var title = ReadString(c, "title")?.Trim();
                var body = ReadString(c, "body")?.Trim();

                // Zorunlu alan eksik / çok kısa → kart düşer (kısmi başarı).
                if (string.IsNullOrWhiteSpace(emoji)) continue;
                if (string.IsNullOrWhiteSpace(title) || title!.Length < CommentaryParseConstraints.MinTitle) continue;
                if (string.IsNullOrWhiteSpace(body) || body!.Length < CommentaryParseConstraints.MinBody) continue;

                // Üst sınırı aşarsa kırp (kartı koru; LLM'in çok az çok kıldığı sınırı bizim için sertleştir).
                if (title.Length > CommentaryParseConstraints.MaxTitle) title = title[..CommentaryParseConstraints.MaxTitle];
                if (body.Length > CommentaryParseConstraints.MaxBody) body = body[..CommentaryParseConstraints.MaxBody];

                CommentaryMeter? meter = null;
                if (c.TryGetProperty("meter", out var m) && m.ValueKind == JsonValueKind.Object &&
                    m.TryGetProperty("value", out var mv) && mv.TryGetDecimal(out var mvDec))
                {
                    var clamped = Math.Clamp(mvDec, 0m, 1m);
                    var low = (ReadString(m, "lowLabel") ?? string.Empty).Trim();
                    var high = (ReadString(m, "highLabel") ?? string.Empty).Trim();
                    // Etiketler tamamen boşsa meter'ı bırakma — UI'da anlamsız çubuk olmasın.
                    if (!string.IsNullOrEmpty(low) || !string.IsNullOrEmpty(high))
                    {
                        if (low.Length > CommentaryParseConstraints.MaxMeterLabel) low = low[..CommentaryParseConstraints.MaxMeterLabel];
                        if (high.Length > CommentaryParseConstraints.MaxMeterLabel) high = high[..CommentaryParseConstraints.MaxMeterLabel];
                        meter = new CommentaryMeter(clamped, low, high);
                    }
                }

                IReadOnlyList<string>? tags = null;
                if (c.TryGetProperty("tags", out var t) && t.ValueKind == JsonValueKind.Array)
                {
                    var tagList = new List<string>(CommentaryParseConstraints.MaxTags);
                    foreach (var x in t.EnumerateArray())
                    {
                        if (tagList.Count >= CommentaryParseConstraints.MaxTags) break;
                        if (x.ValueKind != JsonValueKind.String) continue;
                        var s = x.GetString()?.Trim();
                        if (string.IsNullOrEmpty(s)) continue;
                        if (s.Length > CommentaryParseConstraints.MaxTagLength) s = s[..CommentaryParseConstraints.MaxTagLength];
                        tagList.Add(s);
                    }
                    if (tagList.Count > 0) tags = tagList;
                }

                // T3.5 çıktı güvenlik filtresi (07 §7): yönlendirme/tahmin kalıbı içeren kartı düşür.
                // Kuşak-1 prompt korkuluğunun kaçırdığı çıktıyı kullanıcıya ulaşmadan eler (CLAUDE.md §2).
                if (CommentaryOutputGuard.IsForbidden(title!, body!, tags, out _))
                {
                    guardBlocked++;
                    continue;
                }

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

/// <summary>
/// <see cref="LlmCommentaryService"/> parse aşamasının sınırları (07 §4 ile aynı). LLM şemayı tam
/// tutmadığında istemci tarafında dayatılır.
/// </summary>
internal static class CommentaryParseConstraints
{
    public const int MaxCards = 5;
    public const int MinTitle = 2;
    public const int MaxTitle = 40;
    public const int MinBody = 60;
    public const int MaxBody = 220;
    public const int MaxTags = 4;
    public const int MaxTagLength = 24;
    public const int MaxMeterLabel = 24;
}

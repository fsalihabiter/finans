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
    TimeProvider time,
    // T3.9 metrik portu opsiyonel: yapılandırılmamışsa (testler/dev) no-op — servis çalışmaya devam eder.
    ILlmMetrics? metrics = null) : ILlmCommentaryService
{
    private readonly ILlmMetrics _metrics = metrics ?? NoopLlmMetrics.Instance;

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
        PortfolioSummaryDto summary,
        IReadOnlyList<HoldingDto>? holdings = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var anon = PortfolioAnonymizer.Anonymize(summary, holdings);
        var userPrompt = JsonSerializer.Serialize(anon, PromptJsonOpts);

        var req = new LlmRequest(
            SystemPrompt: CommentaryPrompts.SystemPrompt,
            UserPrompt: userPrompt,
            JsonSchema: CommentaryPrompts.CommentaryJsonSchema,
            // 6144 (T3.10) — derin yorum: 6 kart × (body ≤600 + detail ≤500 char) ≈ 4-5k token
            // + JSON overhead. OpenRouter free reasoning modellerinin gizli düşünme payı da
            // (reasoning.exclude/enabled=false'a rağmen bazı modellerde sızıyor) bu marja sığar.
            MaxOutputTokens: 6144,
            Temperature: 0.2m);

        // T3.12: kalite düşerse (parse başarısız / bekçi kart düşürdü) sessizce eksik kart
        // göstermek yerine BİR kez yeniden üret; denemelerin en iyisi (en çok kart) kullanılır.
        // Sağlayıcı hatasında (429/timeout) tekrar denenmez — kotayı kötüleştirmeyelim.
        IReadOnlyList<CommentaryCard> best = Array.Empty<CommentaryCard>();
        const int maxAttempts = 2;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var result = await llm.CompleteAsync(req, ct);
            if (!result.Success)
            {
                _metrics.RecordCall(success: false, inputTokens: 0, outputTokens: 0, guardBlocked: 0);
                logger.LogInformation("LLM yorumu üretilemedi ({Reason}).", result.ErrorReason);
                break;
            }

            var parsed = TryParseCards(result.Text, out var cards, out var guardBlocked);
            _metrics.RecordCall(success: true, result.InputTokens, result.OutputTokens, guardBlocked);
            if (guardBlocked > 0)
                // Kuşak-2 koruma devreye girdi (T3.5/T3.11): yönlendirme/tahmin ya da dil sızıntısı
                // yakalandı. Görünür kıl ki model kalitesi sapması fark edilsin.
                logger.LogWarning(
                    "LLM yorumunda {Count} kart çıktı filtrelerine takıldı ve düşürüldü (deneme {Attempt}).",
                    guardBlocked, attempt);

            if (parsed && cards.Count > best.Count) best = cards;

            // Tam tur (parse tamam + filtre devreye girmedi + şemanın istediği TAM 6 kart) →
            // yeniden üretim gereksiz. Eksik kart da yeniden üretim sebebidir (kullanıcı
            // beklentisi: kart sayısı üretimden üretime değişmesin).
            if (parsed && guardBlocked == 0 && cards.Count >= CommentaryParseConstraints.MaxCards) break;

            if (attempt < maxAttempts)
                logger.LogInformation(
                    "LLM yorumu eksik/kusurlu ({Cards} kart, {Blocked} düştü) — yeniden üretiliyor.",
                    best.Count, guardBlocked);
            else if (!parsed)
            {
                // Tanılama: ham yanıt anonim portföy yorumu (PII içermez); ilk 400 char loglanır.
                var preview = result.Text.Length > 400 ? result.Text[..400] + "…" : result.Text;
                logger.LogWarning("LLM yanıtı şemayı tutmadı. Ham yanıt önizleme: {Preview}", preview);
            }
        }

        if (best.Count > 0)
            return new CommentaryResponse(best, Source: "llm", GeneratedAtUtc: time.GetUtcNow().UtcDateTime);

        logger.LogWarning("LLM yorumundan kullanılabilir kart çıkmadı; fallback kartı dönülüyor.");
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

                // Üst sınırı aşarsa kırp (kartı koru; LLM'in çok az çok kıldığı sınırı bizim için
                // sertleştir). Gövde/detail cümle sınırından kesilir — kelime ortasında bitmesin.
                if (title.Length > CommentaryParseConstraints.MaxTitle) title = title[..CommentaryParseConstraints.MaxTitle];
                body = SmartTruncate(body!, CommentaryParseConstraints.MaxBody);

                // Opsiyonel detail (T3.10): kavram eğitimi paragrafı. Çok kısaysa gürültü → null;
                // uzunsa kırp. Kart detail'siz de geçerli (zorunlu alan değil).
                var detail = ReadString(c, "detail")?.Trim();
                if (string.IsNullOrWhiteSpace(detail) || detail!.Length < CommentaryParseConstraints.MinDetail)
                    detail = null;
                else if (DetailFinanceNumber.IsMatch(detail))
                    // Kural 8: detail'de FİNANSAL sayı (yüzde / TL tutarı) olamaz — model orada
                    // girdiyle tutarsız örnek yüzdeler uydurabiliyor (canlı gözlem: %67/%33)
                    // → detail atılır, kart gövdeyle yaşar. Masum sayılar ("10 kilo elma")
                    // serbesttir — benzetmelerin doğal parçası (T3.12 yumuşatması).
                    detail = null;
                else
                    detail = SmartTruncate(detail, CommentaryParseConstraints.MaxDetail);

                // T3.11 dil bekçisi: (1) sızan JSON alan adlarını Türkçe karşılığıyla kurtar,
                // (2) Latin dışı alfabe / bariz İngilizce sızıntı → kart düşer (yarım çeviri
                // gösterilmez — kullanıcı güveni). Ücretsiz model kalitesine karşı deterministik savunma.
                title = CommentaryLanguageGuard.TranslateFieldNames(title);
                body = CommentaryLanguageGuard.TranslateFieldNames(body!);
                if (detail is not null) detail = CommentaryLanguageGuard.TranslateFieldNames(detail);
                if (CommentaryLanguageGuard.IsForeign(title, body, detail, out _))
                {
                    guardBlocked++;
                    continue;
                }

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
                // detail de taranır (T3.10) — hangi alanda olursa olsun yasaklı kalıp kartı düşürür.
                var scanBody = detail is null ? body : body + "\n" + detail;
                if (CommentaryOutputGuard.IsForbidden(title!, scanBody, tags, out _))
                {
                    guardBlocked++;
                    continue;
                }

                list.Add(new CommentaryCard(emoji!, title!, body, meter, tags, detail));
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

    /// <summary>
    /// Detail içinde finansal sayı: "%67", "67 %", "₺", "100 TL", "100 lira(lık)".
    /// Kavram paragrafı portföy rakamı taşımamalı (tutarsız uydurma riski); benzetmelerdeki
    /// masum sayılar ("10 kilo") yakalanmaz.
    /// </summary>
    private static readonly System.Text.RegularExpressions.Regex DetailFinanceNumber = new(
        @"%\s*\d|\d\s*%|₺|\d[\d.,]*\s*(TL|lira)\b",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant |
        System.Text.RegularExpressions.RegexOptions.IgnoreCase |
        System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>
    /// Üst sınırı aşan metni cümle sınırından (yoksa kelime sınırından) kırpar — kelime ortasında
    /// biten kart gövdesi okuyucuya "bozuk" görünür (T3.10 canlı gözlem). Cümle sonu limitin ilk
    /// yarısından önceyse cümle feda edilmez, kelime sınırı + "…" kullanılır; hiç boşluk yoksa düz kesim.
    /// </summary>
    private static string SmartTruncate(string text, int max)
    {
        if (text.Length <= max) return text;
        var cut = text[..max];
        var lastSentence = cut.LastIndexOfAny(['.', '!', '?']);
        if (lastSentence >= max / 2) return cut[..(lastSentence + 1)];
        var lastSpace = cut.LastIndexOf(' ');
        return lastSpace > 0 ? cut[..lastSpace] + "…" : cut;
    }
}

/// <summary>
/// <see cref="LlmCommentaryService"/> parse aşamasının sınırları (07 §4 ile aynı). LLM şemayı tam
/// tutmadığında istemci tarafında dayatılır.
/// </summary>
internal static class CommentaryParseConstraints
{
    // T3.10 derinleştirme: kart sayısı 6'ya, gövde 120-600'e çıktı; opsiyonel detail eklendi.
    public const int MaxCards = 6;
    public const int MinTitle = 2;
    public const int MaxTitle = 48;
    public const int MinBody = 120;
    public const int MaxBody = 600;
    public const int MinDetail = 40;
    public const int MaxDetail = 500;
    public const int MaxTags = 4;
    public const int MaxTagLength = 24;
    public const int MaxMeterLabel = 24;
}

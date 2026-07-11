using System.Text.Json;
using System.Text.Json.Serialization;
using Finans.Application.Common;
using Finans.Application.Llm;
using Microsoft.Extensions.Logging;

namespace Finans.Application.Stocks;

/// <summary>
/// <see cref="ILlmStockExplainService"/> uygulaması (T4.3). Akış: sembol bazlı cache →
/// LLM (statik sistem promptu + metrik JSON'u + şema) → paylaşılan güvenli parse hattı
/// (<see cref="LlmCommentaryService.TryParseCards"/>: T3.4 sınırlar + T3.5 tavsiye bekçisi +
/// T3.11 dil bekçisi) → kalite düşükse bir kez yeniden üretim (T3.12 deseni).
///
/// <para><b>Maliyet disiplini (NFR-9, T3.15 dersleri):</b> açıklama sembol başına 24 saat
/// cache'lenir (piyasa verisi ortak — UserId'siz anahtar); LLM başarısızsa son başarılı
/// açıklama gösterilir; hiçbiri yoksa fallback kartı. Girdi halka açık piyasa verisi — PII yok.</para>
/// </summary>
public sealed class LlmStockExplainService(
    ILlmClient llm,
    IAppCache cache,
    ILogger<LlmStockExplainService> logger,
    TimeProvider time,
    ILlmMetrics? metrics = null) : ILlmStockExplainService
{
    private readonly ILlmMetrics _metrics = metrics ?? NoopLlmMetrics.Instance;

    private static readonly TimeSpan FreshTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan LastSuccessTtl = TimeSpan.FromDays(30);

    private static readonly JsonSerializerOptions PromptJsonOpts = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public async Task<CommentaryResponse> ExplainAsync(StockMetricsDto stock, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stock);

        var key = $"stock:explain:{stock.Symbol}";
        var lastKey = $"stock:explain-last:{stock.Symbol}";

        var cached = await cache.GetAsync<CommentaryResponse>(key, ct);
        if (cached is not null)
        {
            _metrics.RecordServed("cache");
            return cached;
        }

        return await cache.SingleFlightAsync(key, async innerCt =>
        {
            var again = await cache.GetAsync<CommentaryResponse>(key, innerCt);
            if (again is not null)
            {
                _metrics.RecordServed("cache");
                return again;
            }

            var resp = await GenerateAsync(stock, innerCt);
            if (resp.Source == "llm")
            {
                await cache.SetAsync(key, resp, FreshTtl, innerCt);
                await cache.SetAsync(lastKey, resp, LastSuccessTtl, innerCt);
                _metrics.RecordServed("llm");
                return resp;
            }

            // Üretilemedi → son başarılı açıklama (07 §5-a); o da yoksa düz fallback.
            var last = await cache.GetAsync<CommentaryResponse>(lastKey, innerCt);
            if (last is not null)
            {
                _metrics.RecordServed("cache_last");
                logger.LogInformation("Hisse açıklaması üretilemedi; son başarılı gösteriliyor ({Symbol}).", stock.Symbol);
                return last with { Source = "cache" };
            }

            _metrics.RecordServed("fallback");
            return resp;
        }, ct);
    }

    private async Task<CommentaryResponse> GenerateAsync(StockMetricsDto stock, CancellationToken ct)
    {
        // Girdi: halka açık metrik özeti (PII yok). Sayılar KODDAN gelir; LLM yalnız açıklar.
        var userPrompt = JsonSerializer.Serialize(new
        {
            symbol = stock.Symbol,
            name = stock.Name,
            currency = stock.Currency,
            price = stock.Price,
            changeRatio = stock.ChangeRatio,
            metrics = stock.Metrics,
            sectorContext = stock.SectorContext,
        }, PromptJsonOpts);

        var req = new LlmRequest(
            SystemPrompt: StockExplainPrompts.SystemPrompt,
            UserPrompt: userPrompt,
            JsonSchema: StockExplainPrompts.ExplainJsonSchema,
            // 6 kart × (body ≤600 + detail ≤500) ≈ 4-5k token + JSON overhead.
            MaxOutputTokens: 8192,
            Temperature: 0.2m);

        IReadOnlyList<CommentaryCard> best = [];
        var bestScore = -1;
        const int maxAttempts = 2;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var result = await llm.CompleteAsync(req, ct);
            if (!result.Success)
            {
                _metrics.RecordCall(success: false, inputTokens: 0, outputTokens: 0, guardBlocked: 0);
                logger.LogInformation("Hisse açıklaması üretilemedi ({Reason}).", result.ErrorReason);
                break;
            }

            var parsed = LlmCommentaryService.TryParseCards(result.Text, out var cards, out var guardBlocked);
            _metrics.RecordCall(success: true, result.InputTokens, result.OutputTokens, guardBlocked);
            if (guardBlocked > 0)
                logger.LogWarning(
                    "Hisse açıklamasında {Count} kart filtrelere takıldı (deneme {Attempt}).", guardBlocked, attempt);

            var detailCount = cards.Count(c => c.Detail is not null);
            var score = cards.Count * 100 + detailCount;
            if (parsed && score > bestScore)
            {
                best = cards;
                bestScore = score;
            }

            // Tam tur: parse tamam + filtre girmedi + en az 3 kart (genel bakış + ≥2 metrik) +
            // her kartta kavram. Eksikse bir şans daha (T3.12 deseni).
            if (parsed && guardBlocked == 0 && cards.Count >= 3 && detailCount == cards.Count) break;
        }

        if (best.Count > 0)
            return new CommentaryResponse(best, Source: "llm", GeneratedAtUtc: time.GetUtcNow().UtcDateTime);

        return new CommentaryResponse(
            Cards:
            [
                new CommentaryCard(
                    Emoji: "💬",
                    Title: "Açıklama şu an üretilemedi",
                    Body: "Metrik açıklama servisine şu an ulaşılamıyor. Yukarıdaki sayılar doğru ve "
                        + "güncel — açıklamayı biraz sonra yeniden deneyebilirsin.",
                    Tags: ["fallback"]),
            ],
            Source: "fallback",
            GeneratedAtUtc: time.GetUtcNow().UtcDateTime);
    }
}

using System.Text.Json;
using Finans.Application.Common;
using Finans.Application.Llm;
using Finans.Application.Stocks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Finans.Application.Tests.Stocks;

/// <summary>
/// T4.3 (SC-29) — Hisse açıklama servisi: mutlu yol (paylaşılan parse hattı), statik
/// prompt + şema dayatılır, girdide yalnız verilen sayılar, tavsiye içeren kart bekçiyle
/// düşer, LLM erişilemezse fallback, sembol bazlı cache ikinci çağrıda LLM'e gitmez.
/// </summary>
public class LlmStockExplainServiceTests
{
    private static StockMetricsDto Aapl() => new(
        "AAPL", "Apple Inc", "NASDAQ", "USD", 315.32m, -0.0028m,
        new StockMetricValues(37.78m, 43.49m, 0.0034m, 0.29m),
        new StockSectorContext("above", "high", "low", "positive"),
        new DateTime(2026, 7, 12, 0, 0, 0, DateTimeKind.Utc), "finnhub");

    private const string Detail =
        "Kavramı günlük hayattan bir benzetmeyle anlatan, yeterince uzun ve rakamsız eğitici bir paragraf metni.";

    private static string Body() => new('a', 150);

    private static string CardsJson(int count) =>
        "{\"cards\":[" + string.Join(",", Enumerable.Range(1, count).Select(i =>
            $"{{\"emoji\":\"⚖️\",\"title\":\"Kart {i}\",\"body\":\"" + Body() +
            $"\",\"detail\":\"{Detail}\"}}")) + "]}";

    private sealed class StubLlmClient(Func<LlmRequest, LlmResult> responder) : ILlmClient
    {
        public int Calls { get; private set; }
        public LlmRequest? LastRequest { get; private set; }
        public Task<LlmResult> CompleteAsync(LlmRequest request, CancellationToken ct = default)
        {
            Calls++;
            LastRequest = request;
            return Task.FromResult(responder(request));
        }
    }

    private sealed class FakeAppCache : IAppCache
    {
        private readonly Dictionary<string, object> _store = new();
        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
            => Task.FromResult(_store.TryGetValue(key, out var v) ? (T?)v : null);
        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class
        { _store[key] = value; return Task.CompletedTask; }
        public async Task<T> GetOrCreateAsync<T>(string key, TimeSpan ttl, Func<CancellationToken, Task<T>> f, CancellationToken ct = default) where T : class
        { if (_store.TryGetValue(key, out var v)) return (T)v; var c = await f(ct); _store[key] = c!; return c; }
        public Task<T> SingleFlightAsync<T>(string key, Func<CancellationToken, Task<T>> f, CancellationToken ct = default) => f(ct);
    }

    private static LlmStockExplainService Build(StubLlmClient llm) =>
        new(llm, new FakeAppCache(), NullLogger<LlmStockExplainService>.Instance, TimeProvider.System);

    [Fact]
    public async Task Happy_path_parses_cards_and_uses_static_prompt_with_schema()
    {
        var stub = new StubLlmClient(_ => LlmResult.Ok(CardsJson(5), 200, 400));

        var resp = await Build(stub).ExplainAsync(Aapl());

        Assert.Equal("llm", resp.Source);
        Assert.Equal(5, resp.Cards.Count);
        Assert.All(resp.Cards, c => Assert.NotNull(c.Detail));

        var req = stub.LastRequest!;
        Assert.Equal(StockExplainPrompts.SystemPrompt, req.SystemPrompt);
        Assert.Equal(StockExplainPrompts.ExplainJsonSchema, req.JsonSchema);
        // Girdi: KODUN çektiği sayılar + bant etiketleri (LLM hesap yapmaz — CLAUDE.md §3.1).
        Assert.Contains("37.78", req.UserPrompt);
        Assert.Contains("\"above\"", req.UserPrompt);
        Assert.Contains("AAPL", req.UserPrompt);
    }

    [Fact]
    public async Task Second_call_for_same_symbol_is_served_from_cache()
    {
        var stub = new StubLlmClient(_ => LlmResult.Ok(CardsJson(4), 100, 200));
        var svc = Build(stub);

        await svc.ExplainAsync(Aapl());
        var r2 = await svc.ExplainAsync(Aapl());

        Assert.Equal(1, stub.Calls); // 24 saat cache — LLM maliyeti sembol/gün başına ≤1
        Assert.Equal("llm", r2.Source);
    }

    [Fact]
    public async Task Advice_card_is_dropped_by_shared_guard()
    {
        var withAdvice = "{\"cards\":[" +
            $"{{\"emoji\":\"⚖️\",\"title\":\"Temiz\",\"body\":\"{Body()}\",\"detail\":\"{Detail}\"}}," +
            "{\"emoji\":\"🚀\",\"title\":\"Al\",\"body\":\"Bu fiyat seviyesinden bakınca bence hiç düşünmeden almalısın çünkü kısa sürede ciddi biçimde yükselecek; böyle bir fırsatı sakın kaçırma derim, piyasa ikinci şans vermez.\"}" +
            "]}";
        var stub = new StubLlmClient(_ => LlmResult.Ok(withAdvice, 100, 200));

        var resp = await Build(stub).ExplainAsync(Aapl());

        Assert.All(resp.Cards, c => Assert.NotEqual("Al", c.Title)); // tavsiye kartı düştü
        Assert.Contains(resp.Cards, c => c.Title == "Temiz");
    }

    [Fact]
    public async Task Falls_back_when_llm_not_available()
    {
        var stub = new StubLlmClient(_ => LlmResult.Fail("llm_not_configured"));

        var resp = await Build(stub).ExplainAsync(Aapl());

        Assert.Equal("fallback", resp.Source);
        Assert.Single(resp.Cards);
        Assert.Equal("Açıklama şu an üretilemedi", resp.Cards[0].Title);
    }

    [Fact]
    public async Task Retries_once_when_first_attempt_is_thin()
    {
        var calls = 0;
        var stub = new StubLlmClient(_ => ++calls == 1
            ? LlmResult.Ok(CardsJson(2), 100, 100)   // 3'ün altında → kusurlu tur
            : LlmResult.Ok(CardsJson(5), 100, 300));

        var resp = await Build(stub).ExplainAsync(Aapl());

        Assert.Equal(2, stub.Calls);
        Assert.Equal(5, resp.Cards.Count); // en iyi tur kullanıldı
    }

    [Fact]
    public void Prompts_regression_gate()
    {
        var p = StockExplainPrompts.SystemPrompt;
        Assert.Contains("EĞİTMEN", p);
        Assert.Contains("YÖNLENDİRME YAPMA", p);
        Assert.Contains("TAHMİN ETME", p);
        Assert.Contains("TAMAMEN TÜRKÇE", p);
        Assert.Contains("iki yönlü", p);          // dengeli çerçeve kuralı
        Assert.Contains("uydurma", p);            // şirket bilgisi uydurma yasağı

        using var doc = JsonDocument.Parse(StockExplainPrompts.ExplainJsonSchema);
        var cards = doc.RootElement.GetProperty("properties").GetProperty("cards");
        Assert.Equal(3, cards.GetProperty("minItems").GetInt32());
        Assert.Equal(6, cards.GetProperty("maxItems").GetInt32());
        var required = cards.GetProperty("items").GetProperty("required")
            .EnumerateArray().Select(e => e.GetString()).ToHashSet();
        Assert.Contains("detail", required);
    }
}

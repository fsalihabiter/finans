using Finans.Application.Common;
using Finans.Application.Llm;
using Finans.Application.Portfolio;
using Finans.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Finans.Application.Tests.Llm;

/// <summary>
/// T3.9 — LLM metrik kayıt davranışı. İç servis çağrı/token/guard sayar; dekoratör istek başına
/// sunulan kaynağı (llm/cache/cache_last/fallback) kaydeder. (Meter→Prometheus tarafı Infrastructure'da.)
/// </summary>
public class LlmMetricsTests
{
    private static PortfolioSummaryDto Summary(decimal totalValue) => new(
        BaseCurrency: CurrencyCode.TRY,
        TotalValue: totalValue,
        TotalCost: 400_000m,
        NetProfit: totalValue - 400_000m,
        ReturnRatio: 0.2m,
        RealReturnRatio: 0.05m,
        Allocation: [new AllocationDto(AssetType.Gold, "G", totalValue, 1m)],
        AsOf: new DateTime(2026, 6, 18, 0, 0, 0, DateTimeKind.Utc));

    // Gövdeler ≥120 char (T3.10 MinBody) — yasaklı kart uzunluk filtresine değil GUARD'a takılmalı.
    private static string CleanCard => "{\"emoji\":\"✅\",\"title\":\"Kart\",\"body\":\"" + new string('a', 150) + "\"}";
    private const string ForbiddenCard =
        "{\"emoji\":\"🚀\",\"title\":\"Al\",\"body\":\"Bence şimdi hiç beklemeden altın almalısın, kısa vadede kesin toparlar diye gerçekten düşünüyorum; bu fırsat bir daha eline geçmez, herkes alırken sen de kaçırmamalısın bence.\"}";

    private sealed class RecordingMetrics : ILlmMetrics
    {
        public readonly List<(bool success, int input, int output, int guard)> Calls = [];
        public readonly List<string> Served = [];
        public void RecordCall(bool success, int inputTokens, int outputTokens, int guardBlocked)
            => Calls.Add((success, inputTokens, outputTokens, guardBlocked));
        public void RecordServed(string source) => Served.Add(source);
    }

    private sealed class StubLlmClient(Func<int, LlmResult> responder) : ILlmClient
    {
        private int _n;
        public Task<LlmResult> CompleteAsync(LlmRequest request, CancellationToken ct = default)
            => Task.FromResult(responder(_n++));
    }

    private sealed class FixedCurrentUser(Guid id) : ICurrentUser { public Guid UserId { get; } = id; }

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

    private static LlmCommentaryService Inner(StubLlmClient client, RecordingMetrics metrics) =>
        new(client, NullLogger<LlmCommentaryService>.Instance, TimeProvider.System, metrics);

    private static CachedLlmCommentaryService Decorated(StubLlmClient client, RecordingMetrics metrics) =>
        new(Inner(client, metrics), new FakeAppCache(), new FixedCurrentUser(Guid.NewGuid()),
            metrics, NullLogger<CachedLlmCommentaryService>.Instance);

    [Fact]
    public async Task Inner_records_successful_call_with_token_counts()
    {
        var metrics = new RecordingMetrics();
        var svc = Inner(new StubLlmClient(_ => LlmResult.Ok("{\"cards\":[" + CleanCard + "]}", 120, 45)), metrics);

        await svc.GetCommentaryAsync(Summary(641_403m));

        var call = Assert.Single(metrics.Calls);
        Assert.True(call.success);
        Assert.Equal(120, call.input);
        Assert.Equal(45, call.output);
        Assert.Equal(0, call.guard);
    }

    [Fact]
    public async Task Inner_records_guard_blocked_count()
    {
        var metrics = new RecordingMetrics();
        var svc = Inner(new StubLlmClient(_ => LlmResult.Ok("{\"cards\":[" + CleanCard + "," + ForbiddenCard + "]}", 100, 40)), metrics);

        await svc.GetCommentaryAsync(Summary(641_403m));

        // T3.12: bekçi kart düşürünce servis BİR kez yeniden üretir → iki çağrı kaydı,
        // her ikisinde de 1 kart filtreye takıldı (stub aynı yanıtı döner).
        Assert.Equal(2, metrics.Calls.Count);
        Assert.All(metrics.Calls, c => Assert.Equal(1, c.guard));
    }

    [Fact]
    public async Task Inner_records_failed_call_with_zero_tokens()
    {
        var metrics = new RecordingMetrics();
        var svc = Inner(new StubLlmClient(_ => LlmResult.Fail("http_503")), metrics);

        await svc.GetCommentaryAsync(Summary(641_403m));

        var call = Assert.Single(metrics.Calls);
        Assert.False(call.success);
        Assert.Equal(0, call.input);
    }

    [Fact]
    public async Task Decorator_records_llm_then_cache_for_repeated_request()
    {
        var metrics = new RecordingMetrics();
        var svc = Decorated(new StubLlmClient(_ => LlmResult.Ok("{\"cards\":[" + CleanCard + "]}", 100, 40)), metrics);
        var summary = Summary(641_403m);

        await svc.GetCommentaryAsync(summary);
        await svc.GetCommentaryAsync(summary);

        Assert.Equal(new[] { "llm", "cache" }, metrics.Served);
    }

    [Fact]
    public async Task Decorator_records_cache_last_when_llm_fails_after_a_success()
    {
        var metrics = new RecordingMetrics();
        var svc = Decorated(new StubLlmClient(i => i == 0 ? LlmResult.Ok("{\"cards\":[" + CleanCard + "]}", 100, 40) : LlmResult.Fail("http_503")), metrics);

        await svc.GetCommentaryAsync(Summary(641_403m));
        await svc.GetCommentaryAsync(Summary(700_000m));

        Assert.Equal(new[] { "llm", "cache_last" }, metrics.Served);
    }

    [Fact]
    public async Task Decorator_records_fallback_when_llm_fails_with_no_prior_success()
    {
        var metrics = new RecordingMetrics();
        var svc = Decorated(new StubLlmClient(_ => LlmResult.Fail("http_503")), metrics);

        await svc.GetCommentaryAsync(Summary(641_403m));

        Assert.Equal(new[] { "fallback" }, metrics.Served);
    }
}

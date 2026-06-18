using System.Text.Json;
using Finans.Application.Common;
using Finans.Application.Llm;
using Finans.Application.Portfolio;
using Finans.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Finans.Application.Tests.Llm;

/// <summary>
/// T3.6 — Cache + "son başarılı" fallback dekoratörü (07 §6). Doğrulanan davranışlar:
/// aynı portföy 24s içinde tekrar sorulunca LLM'e gidilmez; portföy değişince yeni çağrı; LLM
/// başarısızsa son başarılı yorum (<c>Source="cache"</c>) gösterilir; hiç başarılı yoksa düz fallback.
/// </summary>
public class CachedLlmCommentaryServiceTests
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

    // Temiz (filtreden geçen) tek kart.
    private static LlmResult OkCards() => LlmResult.Ok(
        "{\"cards\":[{\"emoji\":\"✅\",\"title\":\"Kart\",\"body\":\"" + new string('a', 80) + "\"}]}", 100, 40);

    private static LlmResult Failed() => LlmResult.Fail("http_503");

    private sealed class CountingLlmClient(Func<int, LlmResult> responder) : ILlmClient
    {
        public int Calls { get; private set; }
        public Task<LlmResult> CompleteAsync(LlmRequest request, CancellationToken ct = default)
            => Task.FromResult(responder(Calls++));
    }

    private sealed class FixedCurrentUser(Guid id) : ICurrentUser
    {
        public Guid UserId { get; } = id;
    }

    /// <summary>Serileştirmesiz, referans saklayan in-memory cache (cache mantığını izole test eder).</summary>
    private sealed class FakeAppCache : IAppCache
    {
        private readonly Dictionary<string, object> _store = new();

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
            => Task.FromResult(_store.TryGetValue(key, out var v) ? (T?)v : null);

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class
        {
            _store[key] = value;
            return Task.CompletedTask;
        }

        public async Task<T> GetOrCreateAsync<T>(
            string key, TimeSpan ttl, Func<CancellationToken, Task<T>> factory, CancellationToken ct = default)
            where T : class
        {
            if (_store.TryGetValue(key, out var v)) return (T)v;
            var created = await factory(ct);
            _store[key] = created!;
            return created;
        }

        public Task<T> SingleFlightAsync<T>(
            string key, Func<CancellationToken, Task<T>> factory, CancellationToken ct = default)
            => factory(ct);
    }

    private static (CachedLlmCommentaryService svc, CountingLlmClient client) Build(Func<int, LlmResult> responder)
    {
        var client = new CountingLlmClient(responder);
        var inner = new LlmCommentaryService(client, NullLogger<LlmCommentaryService>.Instance, TimeProvider.System);
        var svc = new CachedLlmCommentaryService(
            inner, new FakeAppCache(), new FixedCurrentUser(Guid.NewGuid()),
            NullLogger<CachedLlmCommentaryService>.Instance);
        return (svc, client);
    }

    [Fact]
    public async Task Second_identical_request_is_served_from_cache_without_calling_llm()
    {
        var (svc, client) = Build(_ => OkCards());
        var summary = Summary(641_403m);

        var r1 = await svc.GetCommentaryAsync(summary);
        var r2 = await svc.GetCommentaryAsync(summary);

        Assert.Equal("llm", r1.Source);
        Assert.Equal("llm", r2.Source);
        Assert.Equal(1, client.Calls); // ikinci istek LLM'e gitmedi
    }

    [Fact]
    public async Task Changed_portfolio_produces_a_new_key_and_calls_llm_again()
    {
        var (svc, client) = Build(_ => OkCards());

        await svc.GetCommentaryAsync(Summary(641_403m));
        await svc.GetCommentaryAsync(Summary(700_000m)); // farklı hash → cache miss

        Assert.Equal(2, client.Calls);
    }

    [Fact]
    public async Task Falls_back_to_last_successful_commentary_when_llm_fails()
    {
        // 1. çağrı başarılı (son başarılıyı saklar), 2. çağrı (farklı portföy) başarısız.
        var (svc, client) = Build(i => i == 0 ? OkCards() : Failed());

        var r1 = await svc.GetCommentaryAsync(Summary(641_403m));
        var r2 = await svc.GetCommentaryAsync(Summary(700_000m));

        Assert.Equal("llm", r1.Source);
        Assert.Equal("cache", r2.Source);                        // son başarılı gösterildi
        Assert.Equal(r1.Cards[0].Title, r2.Cards[0].Title);      // ilk başarılı yorumun içeriği
        Assert.Equal(2, client.Calls);
    }

    [Fact]
    public async Task Plain_fallback_when_llm_fails_and_no_prior_success()
    {
        var (svc, _) = Build(_ => Failed());

        var resp = await svc.GetCommentaryAsync(Summary(641_403m));

        Assert.Equal("fallback", resp.Source);
        Assert.Equal("Yorum şu an üretilemedi", resp.Cards[0].Title);
    }

    [Fact]
    public void CommentaryResponse_round_trips_through_web_json()
    {
        // IAppCache değerleri JSON serileştirir → cache'lenen tip güvenle round-trip etmeli.
        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var resp = new CommentaryResponse(
            new[]
            {
                new CommentaryCard("✅", "T", new string('a', 80),
                    new CommentaryMeter(0.5m, "Az", "Çok"), new[] { "etiket" }),
            },
            Source: "llm",
            GeneratedAtUtc: new DateTime(2026, 6, 18, 0, 0, 0, DateTimeKind.Utc));

        var json = JsonSerializer.Serialize(resp, opts);
        var back = JsonSerializer.Deserialize<CommentaryResponse>(json, opts);

        Assert.NotNull(back);
        Assert.Single(back!.Cards);
        Assert.Equal("T", back.Cards[0].Title);
        Assert.Equal(0.5m, back.Cards[0].Meter!.Value);
        Assert.Equal("etiket", back.Cards[0].Tags![0]);
    }
}

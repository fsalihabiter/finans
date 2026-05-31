using FluentAssertions;
using Finans.Application.Common;
using Finans.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Finans.Integration.Tests.Caching;

/// <summary>
/// `DistributedAppCache` (T2.7): GetOrCreate ilk çağrıdan sonra cache'ler; single-flight
/// eşzamanlı çağrıda factory'i bir kez koşar (stampede koruması); miss→null, set→değer.
/// Altta in-memory distributed cache (Redis'siz; yerel/CI deseni).
/// </summary>
public sealed class DistributedAppCacheTests
{
    private static IAppCache NewCache() =>
        new DistributedAppCache(
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
            new CacheMetrics());

    private sealed record Box(int Value);

    [Fact]
    public async Task GetOrCreate_caches_after_first_call()
    {
        var cache = NewCache();
        var calls = 0;
        Task<Box> Factory(CancellationToken _) { calls++; return Task.FromResult(new Box(42)); }

        var first = await cache.GetOrCreateAsync("box:1", TimeSpan.FromMinutes(5), Factory);
        var second = await cache.GetOrCreateAsync("box:1", TimeSpan.FromMinutes(5), Factory);

        first.Value.Should().Be(42);
        second.Value.Should().Be(42);
        calls.Should().Be(1); // ikinci çağrı cache'ten geldi
    }

    [Fact]
    public async Task Single_flight_runs_factory_once_under_concurrency()
    {
        var cache = NewCache();
        var calls = 0;
        async Task<Box> Factory(CancellationToken _)
        {
            Interlocked.Increment(ref calls);
            await Task.Delay(50);
            return new Box(7);
        }

        var tasks = Enumerable.Range(0, 8)
            .Select(_ => cache.GetOrCreateAsync("box:concurrent", TimeSpan.FromMinutes(5), Factory))
            .ToArray();
        var results = await Task.WhenAll(tasks);

        results.Should().OnlyContain(r => r.Value == 7);
        calls.Should().Be(1); // 8 eşzamanlı çağrı → factory bir kez (single-flight)
    }

    [Fact]
    public async Task Get_returns_null_on_miss_and_value_after_set()
    {
        var cache = NewCache();

        (await cache.GetAsync<Box>("box:none")).Should().BeNull();

        await cache.SetAsync("box:x", new Box(9), TimeSpan.FromMinutes(5));
        (await cache.GetAsync<Box>("box:x"))!.Value.Should().Be(9);
    }
}

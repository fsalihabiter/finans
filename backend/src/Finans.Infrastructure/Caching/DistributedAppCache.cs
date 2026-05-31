using System.Collections.Concurrent;
using System.Text.Json;
using Finans.Application.Common;
using Microsoft.Extensions.Caching.Distributed;

namespace Finans.Infrastructure.Caching;

/// <summary>
/// <see cref="IAppCache"/>'in <see cref="IDistributedCache"/> tabanlı uygulaması (T2.7):
/// JSON serileştirme + per-anahtar <b>single-flight</b> (in-process <see cref="SemaphoreSlim"/>)
/// + hit/miss metriği. Altta Redis (yapılandırılmışsa) ya da in-memory distributed cache.
/// <para>Not: Single-flight aynı süreç içinde stampede'i önler (yük altında en sık durum);
/// çoklu-replika için dağıtık kilit ileride eklenebilir — kısa TTL + idempotent yazımlar
/// (snapshot/fxrate dedupe) bu arada koruma sağlar.</para>
/// </summary>
public sealed class DistributedAppCache(IDistributedCache cache, CacheMetrics metrics) : IAppCache
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        var bytes = await cache.GetAsync(key, ct);
        if (bytes is null || bytes.Length == 0)
        {
            metrics.Record(key, hit: false);
            return null;
        }

        metrics.Record(key, hit: true);
        return JsonSerializer.Deserialize<T>(bytes, Json);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, Json);
        return cache.SetAsync(
            key, bytes, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, ct);
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key, TimeSpan ttl, Func<CancellationToken, Task<T>> factory, CancellationToken ct = default)
        where T : class
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached is not null)
            return cached;

        return await SingleFlightAsync(key, async innerCt =>
        {
            // Kilidi beklerken başka bir çağrı doldurmuş olabilir → tekrar bak.
            var again = await GetAsync<T>(key, innerCt);
            if (again is not null)
                return again;

            var created = await factory(innerCt);
            await SetAsync(key, created, ttl, innerCt);
            return created;
        }, ct);
    }

    public async Task<T> SingleFlightAsync<T>(
        string key, Func<CancellationToken, Task<T>> factory, CancellationToken ct = default)
    {
        var gate = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            return await factory(ct);
        }
        finally
        {
            gate.Release();
        }
    }
}

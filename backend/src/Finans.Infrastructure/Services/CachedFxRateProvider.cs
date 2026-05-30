using Finans.Application.Portfolio;
using Finans.Infrastructure.Persistence;
using Microsoft.Extensions.Caching.Memory;

namespace Finans.Infrastructure.Services;

/// <summary>
/// <see cref="IFxRateProvider"/> için in-memory cache decorator'ı (10 §3-4 / 13 §13:
/// dış çağrı/DB cache'lenir). Kurlar kullanıcı-bağımsız ve seyrek değişir → kısa TTL'li
/// global anahtar. <see cref="CurrencyConverter"/> değişmez, paylaşımı güvenli.
/// </summary>
public sealed class CachedFxRateProvider(EfFxRateProvider inner, IMemoryCache cache) : IFxRateProvider
{
    internal const string CacheKey = "fx:converter";
    internal static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    public async Task<CurrencyConverter> GetConverterAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheKey, out CurrencyConverter? cached) && cached is not null)
            return cached;

        var converter = await inner.GetConverterAsync(ct);
        cache.Set(CacheKey, converter, Ttl);
        return converter;
    }
}

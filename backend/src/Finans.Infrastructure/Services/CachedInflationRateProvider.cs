using Finans.Application.Portfolio;
using Finans.Infrastructure.Persistence;
using Microsoft.Extensions.Caching.Memory;

namespace Finans.Infrastructure.Services;

/// <summary>
/// <see cref="IInflationRateProvider"/> için in-memory cache decorator'ı (10 §3-4).
/// Enflasyon kullanıcı-bağımsız, seyrek değişir → kısa TTL'li global anahtar.
/// Null oran da geçerli sonuçtur; sarmalayıcıyla (Holder) "yok" ile karışmaz.
/// </summary>
public sealed class CachedInflationRateProvider(EfInflationRateProvider inner, IMemoryCache cache)
    : IInflationRateProvider
{
    internal const string CacheKey = "inflation:annual";
    internal static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    private sealed record Holder(decimal? Rate);

    public async Task<decimal?> GetAnnualRateAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheKey, out Holder? cached) && cached is not null)
            return cached.Rate;

        var rate = await inner.GetAnnualRateAsync(ct);
        cache.Set(CacheKey, new Holder(rate), Ttl);
        return rate;
    }
}

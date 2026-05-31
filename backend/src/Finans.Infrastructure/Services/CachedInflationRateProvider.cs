using Finans.Application.Common;
using Finans.Application.Portfolio;
using Finans.Infrastructure.Persistence;

namespace Finans.Infrastructure.Services;

/// <summary>"Yok" (cache miss) ile "cache'li null oran"ı ayırmak için serileştirilebilir sarmalayıcı.</summary>
internal sealed record InflationHolder(decimal? Rate);

/// <summary>
/// <see cref="IInflationRateProvider"/> için cache decorator'ı (10 §3-4 / T2.7). Enflasyon
/// kullanıcı-bağımsız, seyrek değişir → kısa TTL'li global anahtar. Null oran da geçerli
/// sonuçtur; <see cref="InflationHolder"/> ile "yok"tan ayrılır.
/// </summary>
public sealed class CachedInflationRateProvider(EfInflationRateProvider inner, IAppCache cache)
    : IInflationRateProvider
{
    internal const string CacheKey = "inflation:annual";
    internal static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    public async Task<decimal?> GetAnnualRateAsync(CancellationToken ct = default)
    {
        var holder = await cache.GetOrCreateAsync(
            CacheKey, Ttl, async c => new InflationHolder(await inner.GetAnnualRateAsync(c)), ct);
        return holder.Rate;
    }
}

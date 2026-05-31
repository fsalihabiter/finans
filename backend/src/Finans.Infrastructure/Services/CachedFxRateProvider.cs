using Finans.Application.Common;
using Finans.Application.Portfolio;
using Finans.Infrastructure.Persistence;

namespace Finans.Infrastructure.Services;

/// <summary>
/// <see cref="IFxRateProvider"/> için cache decorator'ı (10 §3-4 / T2.7: dağıtık cache +
/// single-flight). Kurlar kullanıcı-bağımsız ve seyrek değişir → kısa TTL'li global anahtar.
/// <b>Serileştirilebilir</b> tırnaklar (<see cref="FxQuote"/>) cache'lenir; saf
/// <see cref="CurrencyConverter"/> her çağrıda bunlardan kurulur (ucuz, paylaşımı güvenli).
/// </summary>
public sealed class CachedFxRateProvider(EfFxRateProvider inner, IAppCache cache) : IFxRateProvider
{
    internal const string CacheKey = "fx:quotes";
    internal static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    public async Task<CurrencyConverter> GetConverterAsync(CancellationToken ct = default)
    {
        var quotes = await cache.GetOrCreateAsync(CacheKey, Ttl, inner.GetQuotesAsync, ct);
        return new CurrencyConverter(quotes);
    }
}

using Finans.Application.Common;
using Finans.Application.Pricing;
using Finans.Application.Portfolio;
using Finans.Infrastructure.Caching;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Pricing;
using Finans.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Finans.Infrastructure;

/// <summary>Infrastructure katmanının DI kaydı (02 §2.1).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        Action<PricingOptions>? configurePricing = null,
        string? redisConnectionString = null)
    {
        services.AddDbContext<FinansDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Dağıtık cache (T2.7, 10 §3-4): Redis (yapılandırılmışsa) ya da in-memory (yerel dev —
        // Redis kurulu değil). IAppCache single-flight (stampede koruması) + hit/miss metriği
        // sağlar; FX/enflasyon/fiyat decorator'ları bunu kullanır.
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
            services.AddStackExchangeRedisCache(o => o.Configuration = redisConnectionString);
        else
            services.AddDistributedMemoryCache();
        services.AddSingleton<CacheMetrics>();
        services.AddSingleton<IAppCache, DistributedAppCache>();

        // Kur/enflasyon sağlayıcılar DbContext'e bağlı → scoped; cache decorator'ı (IAppCache)
        // ile sarılır. IAppCache singleton, decorator scoped.
        services.AddScoped<EfFxRateProvider>();
        services.AddScoped<IFxRateProvider, CachedFxRateProvider>();
        services.AddScoped<EfInflationRateProvider>();
        services.AddScoped<IInflationRateProvider, CachedInflationRateProvider>();
        services.AddSingleton<PortfolioCalculationService>();

        // Use-case servisleri (DbContext + ICurrentUser'a bağlı) → scoped.
        services.AddScoped<IHoldingService, HoldingService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<ISettingsService, SettingsService>();

        // Eğitici notlar (T2.5): saf kural motoru (singleton) + per-user servis (scoped).
        services.AddSingleton<NudgeRuleEngine>();
        services.AddScoped<INudgeService, NudgeService>();

        // Fiyat sağlayıcılar (Faz 2, T2.1): anahtarsız dış kaynaklar → typed HttpClient.
        // Frankfurter = döviz (ECB), Truncgil = gram altın. Üst katman (T2.2) IEnumerable
        // ile çözüp CanQuote'a göre yönlendirir; cache/fallback orada.
        var pricing = new PricingOptions();
        configurePricing?.Invoke(pricing);

        services.AddSingleton(TimeProvider.System);
        services.AddHttpClient<FrankfurterPriceProvider>(c =>
        {
            c.BaseAddress = new Uri(pricing.FrankfurterBaseUrl);
            c.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddHttpClient<TruncgilGoldPriceProvider>(c =>
        {
            c.BaseAddress = new Uri(pricing.TruncgilBaseUrl);
            c.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddTransient<IPriceProvider>(sp => sp.GetRequiredService<FrankfurterPriceProvider>());
        services.AddTransient<IPriceProvider>(sp => sp.GetRequiredService<TruncgilGoldPriceProvider>());

        // Orkestrasyon (T2.2): sağlayıcıları yönlendir + cache + snapshot/fxrate/CurrentPrice yaz.
        services.AddScoped<IPriceFetchService, PriceFetchService>();

        return services;
    }
}

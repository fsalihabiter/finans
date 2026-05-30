using Finans.Application.Pricing;
using Finans.Application.Portfolio;
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
        Action<PricingOptions>? configurePricing = null)
    {
        services.AddDbContext<FinansDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Kur/enflasyon sağlayıcılar DbContext'e bağlı → scoped; in-memory cache decorator'ı
        // ile sarılır (10 §3-4: dış çağrı/DB cache'lenir). Cache singleton, decorator scoped.
        services.AddMemoryCache();
        services.AddScoped<EfFxRateProvider>();
        services.AddScoped<IFxRateProvider, CachedFxRateProvider>();
        services.AddScoped<EfInflationRateProvider>();
        services.AddScoped<IInflationRateProvider, CachedInflationRateProvider>();
        services.AddSingleton<PortfolioCalculationService>();

        // Use-case servisleri (DbContext + ICurrentUser'a bağlı) → scoped.
        services.AddScoped<IHoldingService, HoldingService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<ISettingsService, SettingsService>();

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

        return services;
    }
}

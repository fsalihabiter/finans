using Finans.Application.Portfolio;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Finans.Infrastructure;

/// <summary>Infrastructure katmanının DI kaydı (02 §2.1).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
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

        return services;
    }
}

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

        // Kur/enflasyon sağlayıcılar DbContext'e bağlı → scoped. Saf hesap servisi durumsuz → singleton.
        services.AddScoped<IFxRateProvider, EfFxRateProvider>();
        services.AddScoped<IInflationRateProvider, EfInflationRateProvider>();
        services.AddSingleton<PortfolioCalculationService>();

        // Use-case servisleri (DbContext + ICurrentUser'a bağlı) → scoped.
        services.AddScoped<IHoldingService, HoldingService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<ISettingsService, SettingsService>();

        return services;
    }
}

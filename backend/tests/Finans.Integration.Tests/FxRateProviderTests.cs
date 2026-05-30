using FluentAssertions;
using Finans.Application.Portfolio;
using Finans.Domain.Enums;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Finans.Integration.Tests;

/// <summary>
/// EfFxRateProvider + CurrencyConverter uçtan uca (SC-03 integration): seed'lenmiş
/// kurlardan her çift için EN GÜNCEL tırnak seçilir ve dönüşüm doğru hesaplanır.
/// Seed: USD→TRY güncel 48 (eski 43,27 vardır → seçilmemeli), EUR→TRY 52.
/// </summary>
public sealed class FxRateProviderTests : IClassFixture<SqliteWebApplicationFactory>, IAsyncLifetime
{
    private readonly SqliteWebApplicationFactory _factory;

    public FxRateProviderTests(SqliteWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedData.SeedAsync(db);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Provider_uses_latest_quote_and_converts_usd_to_try()
    {
        using var scope = _factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IFxRateProvider>();

        var converter = await provider.GetConverterAsync();

        // Güncel kur 48 kullanılmalı (eski 43,27 değil): 2.000 $ → 96.000 ₺.
        converter.Convert(2000m, CurrencyCode.USD, CurrencyCode.TRY).Should().Be(96000m);
        converter.RateFor(CurrencyCode.USD, CurrencyCode.TRY).Should().Be(48m);
    }

    [Fact]
    public async Task Provider_supports_cross_rate_eur_to_usd()
    {
        using var scope = _factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IFxRateProvider>();

        var converter = await provider.GetConverterAsync();

        // EUR→USD doğrudan seed'de yok → TRY pivotu: 96 € × 52 / 48 = 104 $.
        converter.Convert(96m, CurrencyCode.EUR, CurrencyCode.USD).Should().Be(104m);
    }
}

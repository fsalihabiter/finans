using FluentAssertions;
using Finans.Application.Portfolio;
using Finans.Domain.Enums;
using Finans.Domain.Portfolio;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Finans.Integration.Tests;

/// <summary>
/// EfInflationRateProvider + PortfolioCalculationService uçtan uca (SC-05 binding):
/// seed'lenmiş enflasyon (0,38) yüklenir ve reel getiri summary'de doğru hesaplanır.
/// Nominal getiri +%51,6 → reel ≈ (1,51643)/1,38 − 1 ≈ %9,89 (enflasyon sonrası gerçek artış).
/// </summary>
public sealed class InflationRealReturnTests : IClassFixture<SqliteWebApplicationFactory>, IAsyncLifetime
{
    private readonly SqliteWebApplicationFactory _factory;

    public InflationRealReturnTests(SqliteWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedData.SeedAsync(db);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Provider_loads_seeded_annual_rate()
    {
        using var scope = _factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IInflationRateProvider>();

        var rate = await provider.GetAnnualRateAsync();

        rate.Should().Be(0.380000m);
    }

    [Fact]
    public async Task Summary_real_return_uses_loaded_inflation()
    {
        using var scope = _factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IInflationRateProvider>();
        var calc = scope.ServiceProvider.GetRequiredService<PortfolioCalculationService>();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();

        var inflation = await provider.GetAnnualRateAsync();
        var entities = await db.Holdings.Include(h => h.Asset).ToListAsync();

        // Birim fiyatları baz TRY'ye çevir (USD-fiyatlı kalem ×48, seed FX'i sabit) —
        // PortfolioService'in yaptığı çevrimi taklit eder (T1.3).
        decimal toTry(Holding h, decimal v) =>
            h.Asset.PricingCurrency == CurrencyCode.USD ? v * 48m : v;
        var inputs = entities
            .Select(h => new HoldingInput(
                h.Asset.Type, h.Asset.Name, h.Quantity,
                toTry(h, h.AvgCost),
                h.CurrentPrice is { } cp ? toTry(h, cp) : null))
            .ToList();

        var summary = calc.CalculateSummary(inputs, inflation);

        summary.ReturnRatio.Should().NotBeNull();
        Math.Round(summary.ReturnRatio!.Value, 3).Should().Be(0.459m); // BES maliyeti = kendi katkı (cepten)
        summary.RealReturnRatio.Should().NotBeNull();
        Math.Round(summary.RealReturnRatio!.Value, 4).Should().Be(0.0572m);
    }
}

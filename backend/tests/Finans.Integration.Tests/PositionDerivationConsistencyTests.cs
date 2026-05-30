using FluentAssertions;
using Finans.Application.Portfolio;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Finans.Integration.Tests;

/// <summary>
/// SC-06 integration: seed'lenmiş <c>Transactions</c>'tan türetilen pozisyon, saklanan
/// (türetilmiş ama cache'lenmiş) <c>Holding.Quantity/AvgCost</c> ile BİREBİR tutar.
/// Doğruluk kaynağı işlemlerdir (03 §11); cache'in onlardan sapmadığını kanıtlar.
/// </summary>
public sealed class PositionDerivationConsistencyTests : IClassFixture<SqliteWebApplicationFactory>, IAsyncLifetime
{
    private readonly SqliteWebApplicationFactory _factory;

    public PositionDerivationConsistencyTests(SqliteWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedData.SeedAsync(db);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Derived_position_matches_stored_holding_for_transacted_assets()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();

        // Yalnızca işlemi olan kalemler (altın, dolar); BES/Nakit notional → işlemsiz.
        var holdings = await db.Holdings
            .Include(h => h.Transactions)
            .Where(h => h.Transactions.Any())
            .ToListAsync();

        holdings.Should().NotBeEmpty();

        foreach (var holding in holdings)
        {
            var derived = PortfolioCalculationService.DerivePosition(
                holding.Transactions.Select(t => new TransactionInput(t.Type, t.Quantity, t.UnitPrice, t.Fee)));

            derived.Quantity.Should().Be(holding.Quantity);
            derived.AvgCost.Should().Be(holding.AvgCost);
        }
    }
}

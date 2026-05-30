using System.Net;
using FluentAssertions;
using Finans.Domain.Enums;
using Finans.Domain.Portfolio;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Finans.Integration.Tests;

/// <summary>
/// Sqlite fixture'ının uçtan uca çalıştığını kanıtlar (T0.11): relational sağlayıcıda
/// şema kurulur, seed işler, /health/ready DB'ye bağlanır. Faz 1'de portföy
/// endpoint'leri bu fixture üzerinden test edilecek (SC-01..06, SC-13 IDOR).
/// </summary>
public sealed class SqliteIntegrationTests : IClassFixture<SqliteWebApplicationFactory>, IAsyncLifetime
{
    private readonly SqliteWebApplicationFactory _factory;

    public SqliteIntegrationTests(SqliteWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
        await db.Database.EnsureCreatedAsync(); // Npgsql migration yerine model'den şema
        await SeedData.SeedAsync(db);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Readiness_health_is_healthy_against_sqlite_db()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/ready");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Seed_is_consistent_on_relational_provider()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();

        // Baz TRY toplamı: USD-fiyatlı kalem (AAPL) seed FX'iyle ×48 çevrilir.
        var holdings = await db.Holdings.Include(h => h.Asset).ToListAsync();
        decimal toTry(Holding h, decimal amount) =>
            h.Asset.PricingCurrency == CurrencyCode.USD ? amount * 48m : amount;

        holdings.Sum(h => toTry(h, h.Quantity * h.AvgCost)).Should().Be(603770.00m);
        holdings.Sum(h => toTry(h, h.Quantity * (h.CurrentPrice ?? 0m))).Should().Be(839213.00m);
    }

    [Fact]
    public async Task Check_constraint_rejects_negative_quantity()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();

        // AAPL seed'de pozisyonsuz → unique (UserId,AssetId) çakışmaz.
        var user = await db.Users.FirstAsync();
        var aapl = await db.Assets.FirstAsync(a => a.Symbol == "AAPL");
        db.Holdings.Add(new Holding { UserId = user.Id, AssetId = aapl.Id, Quantity = -5m, AvgCost = 0m });

        // CK_Holdings_Quantity ("Quantity" >= 0) relational şemada da uygulanır.
        var act = async () => await db.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }
}

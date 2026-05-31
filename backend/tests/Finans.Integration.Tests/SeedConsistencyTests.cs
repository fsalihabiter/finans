using FluentAssertions;
using Finans.Domain.Enums;
using Finans.Domain.Portfolio;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;

namespace Finans.Integration.Tests;

/// <summary>
/// Seed fixture doğrulaması (03 §12, 09 §2). Baz TRY toplamları (USD-fiyatlı kalem
/// ×48 çevrilir): maliyet 603.770, değer 839.213, kâr +235.443, getiri +%39,0.
/// Yanlış rakam kabul edilemez (NFR-1). EF InMemory ile sağlayıcısız koşar.
/// </summary>
public sealed class SeedConsistencyTests
{
    private static FinansDbContext NewContext() =>
        new(new DbContextOptionsBuilder<FinansDbContext>()
            .UseInMemoryDatabase($"seed-{Guid.CreateVersion7()}")
            .Options);

    /// <summary>Seed FX'i sabit (USD→TRY=48); USD-fiyatlı kalemi baz TRY'ye çevir.</summary>
    private static decimal ToTry(Holding h, decimal amount) =>
        h.Asset.PricingCurrency == CurrencyCode.USD ? amount * 48m : amount;

    [Fact]
    public async Task Seed_totals_match_draft_figures()
    {
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        var holdings = await db.Holdings.Include(h => h.Asset).ToListAsync();
        var totalCost = holdings.Sum(h => ToTry(h, h.Quantity * h.AvgCost));
        var totalValue = holdings.Sum(h => ToTry(h, h.Quantity * (h.CurrentPrice ?? 0m)));
        var profit = totalValue - totalCost;
        var returnPct = profit / totalCost;

        // BES maliyeti = kendi katkı (cepten); devlet katkısı maliyet değil → 603.770→575.216.
        totalCost.Should().Be(575216.00m);
        totalValue.Should().Be(839213.00m);
        profit.Should().Be(263997.00m);
        Math.Round(returnPct * 100m, 1).Should().Be(45.9m);
    }

    [Fact]
    public async Task Seed_is_idempotent()
    {
        await using var db = NewContext();
        await SeedData.SeedAsync(db);
        await SeedData.SeedAsync(db); // ikinci çağrı çoğaltmamalı

        (await db.Users.CountAsync()).Should().Be(2);
        (await db.Holdings.CountAsync()).Should().Be(7);
        (await db.Assets.CountAsync()).Should().Be(7);
        (await db.Transactions.CountAsync()).Should().Be(5);
    }

    [Fact]
    public async Task Bes_state_contribution_is_tracked_separately()
    {
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        var bes = await db.BesDetails.SingleAsync();
        bes.OwnContribution.Should().Be(120000m);
        bes.StateContribution.Should().Be(28554m); // devlet katkısı AYRI (FR-1.5)
    }
}

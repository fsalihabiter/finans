using FluentAssertions;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;

namespace Finans.Integration.Tests;

/// <summary>
/// SC-01..06 fixture doğrulaması (03 §12, 09 §2): seed sayıları taslakla BİREBİR
/// tutar — toplam maliyet 422.970, değer 641.403, kâr +218.433, getiri +%51,6.
/// Yanlış rakam kabul edilemez (NFR-1). EF InMemory ile sağlayıcısız koşar.
/// </summary>
public sealed class SeedConsistencyTests
{
    private static FinansDbContext NewContext() =>
        new(new DbContextOptionsBuilder<FinansDbContext>()
            .UseInMemoryDatabase($"seed-{Guid.CreateVersion7()}")
            .Options);

    [Fact]
    public async Task Seed_totals_match_draft_figures()
    {
        await using var db = NewContext();
        await SeedData.SeedAsync(db);

        var holdings = await db.Holdings.ToListAsync();
        var totalCost = holdings.Sum(h => h.Quantity * h.AvgCost);
        var totalValue = holdings.Sum(h => h.Quantity * (h.CurrentPrice ?? 0m));
        var profit = totalValue - totalCost;
        var returnPct = profit / totalCost;

        totalCost.Should().Be(422970.00m);
        totalValue.Should().Be(641403.00m);
        profit.Should().Be(218433.00m);
        Math.Round(returnPct * 100m, 1).Should().Be(51.6m);
    }

    [Fact]
    public async Task Seed_is_idempotent()
    {
        await using var db = NewContext();
        await SeedData.SeedAsync(db);
        await SeedData.SeedAsync(db); // ikinci çağrı çoğaltmamalı

        (await db.Users.CountAsync()).Should().Be(2);
        (await db.Holdings.CountAsync()).Should().Be(4);
        (await db.Assets.CountAsync()).Should().Be(5);
        (await db.Transactions.CountAsync()).Should().Be(2);
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

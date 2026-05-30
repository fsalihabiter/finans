using FluentAssertions;
using Finans.Application.Pricing;
using Finans.Domain.Enums;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Pricing;
using Finans.Infrastructure.Seed;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Finans.Integration.Tests.Pricing;

/// <summary>
/// `PriceFetchService` (T2.2, SC-18): canlı tırnakları yazar (snapshot/fxrate +
/// <c>Holding.CurrentPrice</c>), TTL içinde cache'ten döner (dış çağrı tekrar yok),
/// bir sağlayıcı çökse diğeri sürer. İzole Sqlite + seed; sağlayıcılar stub (ağsız).
/// </summary>
public sealed class PriceFetchServiceTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 5, 31, 10, 0, 0, TimeSpan.Zero);

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private FinansDbContext _db = null!;

    public async Task InitializeAsync()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<FinansDbContext>().UseSqlite(_connection).Options;
        _db = new FinansDbContext(options);
        await _db.Database.EnsureCreatedAsync();
        await SeedData.SeedAsync(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private PriceFetchService Build(params IPriceProvider[] providers) =>
        new(_db, providers, new MemoryCache(new MemoryCacheOptions()),
            new FixedTimeProvider(Now), NullLogger<PriceFetchService>.Instance);

    private static StubPriceProvider GoldStub(decimal price) =>
        new("truncgil-test",
            i => i.Kind == PriceInstrumentKind.Gold,
            ins => ins.Select(i => new PriceQuote(i, price, CurrencyCode.TRY, Now.UtcDateTime, "truncgil-test")));

    private static StubPriceProvider FxStub(decimal usd, decimal eur) =>
        new("frankfurter-test",
            i => i.Kind == PriceInstrumentKind.Currency && i.Currency != CurrencyCode.TRY,
            ins => ins.Select(i => new PriceQuote(
                i, i.Currency == CurrencyCode.USD ? usd : eur, CurrencyCode.TRY, Now.UtcDateTime, "frankfurter-test")));

    [Fact]
    public async Task Refresh_writes_snapshots_fxrates_and_updates_holding_currentprice()
    {
        var result = await Build(GoldStub(7000m), FxStub(50m, 55m)).RefreshAsync();

        result.FromCache.Should().BeFalse();
        result.Quotes.Should().HaveCount(3); // gold + USD + EUR
        result.FailedSources.Should().BeEmpty();

        // Holding.CurrentPrice güncellendi (seed: altın 6500, USD 48 → canlı).
        var gold = await _db.Holdings.SingleAsync(h => h.Asset.Symbol == "XAU");
        gold.CurrentPrice.Should().Be(7000m);
        var usd = await _db.Holdings.SingleAsync(h => h.Asset.Symbol == "USD");
        usd.CurrentPrice.Should().Be(50m);

        // FxRate yazıldı → CurrencyConverter en güncel USD→TRY = 50.
        var latestUsd = await _db.FxRates
            .Where(r => r.FromCurrency == CurrencyCode.USD && r.ToCurrency == CurrencyCode.TRY)
            .OrderByDescending(r => r.AsOfUtc).ThenByDescending(r => r.CreatedAtUtc)
            .FirstAsync();
        latestUsd.Rate.Should().Be(50m);
        latestUsd.Source.Should().Be("frankfurter-test");

        // PriceSnapshot (geçmiş) eklendi — altın için yeni satış fiyatı.
        var latestGoldSnap = await _db.PriceSnapshots
            .Where(p => p.Asset.Symbol == "XAU")
            .OrderByDescending(p => p.AsOfUtc).ThenByDescending(p => p.CreatedAtUtc)
            .FirstAsync();
        latestGoldSnap.Price.Should().Be(7000m);
        latestGoldSnap.Source.Should().Be("truncgil-test");
    }

    [Fact]
    public async Task Second_refresh_within_ttl_is_served_from_cache()
    {
        var gold = GoldStub(7000m);
        var fx = FxStub(50m, 55m);
        var service = Build(gold, fx);

        var first = await service.RefreshAsync();
        var second = await service.RefreshAsync();

        first.FromCache.Should().BeFalse();
        second.FromCache.Should().BeTrue();
        // TTL içinde dış sağlayıcı tekrar çağrılmadı.
        gold.Calls.Should().Be(1);
        fx.Calls.Should().Be(1);
    }

    [Fact]
    public async Task One_provider_failure_does_not_block_others()
    {
        // Döviz sağlayıcı çöküyor; altın yine de yazılmalı, çökme olmamalı.
        var result = await Build(
            GoldStub(7000m),
            new ThrowingPriceProvider("frankfurter-test", i => i.Kind == PriceInstrumentKind.Currency))
            .RefreshAsync();

        result.FailedSources.Should().Contain("frankfurter-test");
        result.Quotes.Should().OnlyContain(q => q.Instrument.Kind == PriceInstrumentKind.Gold);

        var gold = await _db.Holdings.SingleAsync(h => h.Asset.Symbol == "XAU");
        gold.CurrentPrice.Should().Be(7000m);
        // Döviz çöktü → USD CurrentPrice seed değerinde kaldı (48).
        var usd = await _db.Holdings.SingleAsync(h => h.Asset.Symbol == "USD");
        usd.CurrentPrice.Should().Be(48m);
    }
}

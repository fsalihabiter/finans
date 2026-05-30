using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Finans.Application.Pricing;
using Finans.Application.Portfolio;
using Finans.Domain.Enums;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Seed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Finans.Integration.Tests.Pricing;

/// <summary>
/// `GET /api/prices` uçtan uca (T2.4, SC-18/SC-08): canlı tırnakları döner + `Holding.CurrentPrice`
/// tazeler (summary/holdings besleme); bir kaynak çökünce `stale:true` yüzeye çıkar. Sağlayıcılar
/// stub (ağ yok). Her test KENDİ factory'sini kurar → izole Sqlite + seed (testler arası sızma yok).
/// </summary>
public sealed class PricesApiTests : IAsyncLifetime
{
    private static readonly DateTime AsOf = new(2026, 5, 31, 8, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Investor = SeedData.Id("user-1");

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private SqliteWebApplicationFactory _factory = null!;

    public async Task InitializeAsync()
    {
        _factory = new SqliteWebApplicationFactory();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedData.SeedAsync(db);
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>Gerçek sağlayıcılar (ağ) yerine verilen stub'larla istemci.</summary>
    private HttpClient ClientWith(params IPriceProvider[] stubs)
    {
        var client = _factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPriceProvider>();
            foreach (var stub in stubs)
                services.AddSingleton(stub);
        })).CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", Investor.ToString());
        return client;
    }

    private static IPriceProvider GoldStub(decimal price) =>
        new StubPriceProvider("truncgil-test",
            i => i.Kind == PriceInstrumentKind.Gold,
            ins => ins.Select(i => new PriceQuote(i, price, CurrencyCode.TRY, AsOf, "truncgil-test")));

    private static IPriceProvider FxStub(decimal usd, decimal eur) =>
        new StubPriceProvider("frankfurter-test",
            i => i.Kind == PriceInstrumentKind.Currency && i.Currency != CurrencyCode.TRY,
            ins => ins.Select(i => new PriceQuote(
                i, i.Currency == CurrencyCode.USD ? usd : eur, CurrencyCode.TRY, AsOf, "frankfurter-test")));

    [Fact]
    public async Task Get_prices_returns_live_quotes_and_feeds_holdings()
    {
        var client = ClientWith(GoldStub(7000m), FxStub(50m, 55m));

        var resp = await client.GetAsync("/api/prices");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await resp.Content.ReadFromJsonAsync<PricesResponse>(Json))!;

        body.FromCache.Should().BeFalse();
        body.HasStale.Should().BeFalse();
        body.FailedSources.Should().BeEmpty();
        body.Prices.Should().HaveCount(3);

        var usd = body.Prices.Single(p => p.Kind == PriceInstrumentKind.Currency && p.Currency == CurrencyCode.USD);
        usd.Price.Should().Be(50m);
        usd.Stale.Should().BeFalse();
        usd.Source.Should().Be("frankfurter-test");
        usd.QuoteCurrency.Should().Be(CurrencyCode.TRY);
        body.Prices.Should().Contain(p => p.Kind == PriceInstrumentKind.Gold && p.Price == 7000m);

        // "summary'i canlı fiyatla besle": refresh CurrentPrice'ı yazdı → holdings yansıtır.
        var holdings = (await client.GetFromJsonAsync<List<HoldingDto>>("/api/holdings", Json))!;
        holdings.Single(h => h.AssetType == AssetType.Gold).CurrentPrice.Should().Be(7000m);
        holdings.Single(h => h.AssetType == AssetType.Fx && h.Symbol == "USD").CurrentPrice.Should().Be(50m);
    }

    [Fact]
    public async Task Get_prices_surfaces_stale_when_a_source_is_down()
    {
        var client = ClientWith(
            GoldStub(7000m),
            new ThrowingPriceProvider("frankfurter-test", i => i.Kind == PriceInstrumentKind.Currency));

        var body = (await client.GetFromJsonAsync<PricesResponse>("/api/prices", Json))!;

        body.HasStale.Should().BeTrue();
        body.FailedSources.Should().Contain("frankfurter-test");

        var usd = body.Prices.Single(p => p.Kind == PriceInstrumentKind.Currency && p.Currency == CurrencyCode.USD);
        usd.Stale.Should().BeTrue();
        usd.Price.Should().Be(48m);        // seed son-bilinen
        usd.Source.Should().Be("Manual");  // seed kaynağı

        // Altın taze sürdü (sağlam kaynak), çökme yok.
        body.Prices.Should().Contain(p => p.Kind == PriceInstrumentKind.Gold && p.Price == 7000m && !p.Stale);
    }
}

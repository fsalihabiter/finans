using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Finans.Application.Portfolio;
using Finans.Application.Pricing;
using Finans.Domain.Enums;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Seed;
using Finans.Integration.Tests.Pricing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Finans.Integration.Tests;

/// <summary>
/// SC-42 — ilk yükleme FX yarışı: kur satırları fiyat tazeleme turunda yazılır; /history
/// ve /scenario istekleri kur commit edilmeden gelirse (paralel /prices ile yarış) servis
/// 500 atmak yerine (1) tazelemeyi kendisi tetikleyip kendini iyileştirir, (2) kur yine
/// yoksa sözleşmeli 502 UPSTREAM_ERROR döner. Sağlayıcılar stub (ağ yok); her test kendi
/// factory'siyle izole.
/// </summary>
public sealed class PortfolioHistoryFxRaceTests : IAsyncLifetime
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

        // Yarış anını kur: kur satırları HENÜZ yazılmamış (tazeleme turu commit etmedi).
        // Seed'de USD fiyatlı AAPL pozisyonu var → seri USD→TRY kuru ister.
        db.FxRates.RemoveRange(db.FxRates);
        await db.SaveChangesAsync();
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

    [Fact]
    public async Task History_self_heals_by_triggering_price_refresh_when_fx_missing()
    {
        // Döviz stub'u: tazeleme turu USD/EUR→TRY kurlarını yazar (PriceFetchService.Persist).
        var fxStub = new StubPriceProvider("frankfurter-test",
            i => i.Kind == PriceInstrumentKind.Currency && i.Currency != CurrencyCode.TRY,
            ins => ins.Select(i => new PriceQuote(
                i, i.Currency == CurrencyCode.USD ? 48m : 52m, CurrencyCode.TRY, AsOf, "frankfurter-test")));
        var client = ClientWith(fxStub);

        var resp = await client.GetAsync("/api/portfolio/history?period=all");

        // Kur yokken 500 değil: servis tazelemeyi tetikledi, kur yazıldı, seri hesaplandı.
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await resp.Content.ReadFromJsonAsync<PortfolioHistoryDto>(Json);
        history!.Points.Should().NotBeEmpty();
        history.Points[^1].Value.Should().BePositive();
        fxStub.Calls.Should().Be(1); // tazeleme gerçekten koştu (tek tur — single-flight)
    }

    [Fact]
    public async Task History_returns_contract_502_when_fx_still_unavailable_after_refresh()
    {
        // Sağlayıcı tamamen çökük + DB'de son bilinen kur da yok → tazeleme kur yazamaz.
        var client = ClientWith(new ThrowingPriceProvider("down-test", _ => true));

        var resp = await client.GetAsync("/api/portfolio/history?period=all");

        // 500 (iç hata) DEĞİL — sözleşmeli 502 zarfı (NFR-5; web isError dalı bunu gösterir).
        resp.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("error").GetProperty("code").GetString()
            .Should().Be("UPSTREAM_ERROR");
    }

    // ── Aynı yarış /scenario'da (ScenarioService aynı saf servisi kullanır) ──

    [Fact]
    public async Task Scenario_self_heals_by_triggering_price_refresh_when_fx_missing()
    {
        var fxStub = new StubPriceProvider("frankfurter-test",
            i => i.Kind == PriceInstrumentKind.Currency && i.Currency != CurrencyCode.TRY,
            ins => ins.Select(i => new PriceQuote(
                i, i.Currency == CurrencyCode.USD ? 48m : 52m, CurrencyCode.TRY, AsOf, "frankfurter-test")));
        var client = ClientWith(fxStub);

        // USD fiyatlı AAPL pozisyonu → seri USD→TRY kuru ister (kur seed'den silindi).
        var resp = await client.GetAsync($"/api/portfolio/scenario/{SeedData.Id("holding-aapl")}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var scenario = await resp.Content.ReadFromJsonAsync<ScenarioComparisonDto>(Json);
        scenario!.Points.Should().NotBeEmpty();
        scenario.Summary.CurrentValue.Should().BePositive();
        fxStub.Calls.Should().Be(1); // tazeleme gerçekten koştu (tek tur — single-flight)
    }

    [Fact]
    public async Task Scenario_returns_contract_502_when_fx_still_unavailable_after_refresh()
    {
        var client = ClientWith(new ThrowingPriceProvider("down-test", _ => true));

        var resp = await client.GetAsync($"/api/portfolio/scenario/{SeedData.Id("holding-aapl")}");

        resp.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("error").GetProperty("code").GetString()
            .Should().Be("UPSTREAM_ERROR");
    }
}

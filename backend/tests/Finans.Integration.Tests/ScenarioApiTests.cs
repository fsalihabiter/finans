using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Finans.Application.Portfolio;
using Finans.Domain.Enums;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace Finans.Integration.Tests;

/// <summary>
/// Senaryo v1 API uçtan uca (T5.4, SC-37): tek pozisyon "nakitte dursaydı" karşılaştırması —
/// seri + özet pozisyon detayıyla tutarlı, enflasyon eşiği, IDOR 404, bilinmeyen id 404.
/// </summary>
public sealed class ScenarioApiTests : IClassFixture<SqliteWebApplicationFactory>, IAsyncLifetime
{
    private readonly SqliteWebApplicationFactory _factory;

    private static readonly Guid Investor = SeedData.Id("user-1");
    private static readonly Guid Admin = SeedData.Id("admin-1");
    private static readonly Guid GoldHolding = SeedData.Id("holding-gold");
    private static readonly Guid BesHolding = SeedData.Id("holding-bes");

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public ScenarioApiTests(SqliteWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedData.SeedAsync(db);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient ClientAs(Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", userId.ToString());
        return client;
    }

    [Fact]
    public async Task Gold_scenario_matches_holding_metrics_and_includes_inflation_threshold()
    {
        var client = ClientAs(Investor);

        var scenario = await client.GetFromJsonAsync<ScenarioComparisonDto>(
            $"/api/portfolio/scenario/{GoldHolding}", Json);

        scenario.Should().NotBeNull();
        scenario!.AssetType.Should().Be(AssetType.Gold);
        scenario.BaseCurrency.Should().Be(CurrencyCode.TRY);

        // Özet pozisyon detayıyla tutarlı (seed altın: 40 gr @4.546,275 → maliyet 181.851;
        // güncel 6.500 → değer 260.000; fark +78.149).
        scenario.Summary.CurrentValue.Should().Be(260000m);
        scenario.Summary.Invested.Should().Be(181851m);
        scenario.Summary.Difference.Should().Be(78149m);
        Math.Round(scenario.Summary.DifferenceRatio!.Value, 2).Should().Be(0.43m);

        // Enflasyon eşiği: seed enflasyonu 0,38 → eşik yatırılandan BÜYÜK (2024'ten beri işler).
        scenario.Summary.AnnualInflationRate.Should().Be(0.38m);
        scenario.Summary.InflationAdjustedInvested.Should().NotBeNull();
        scenario.Summary.InflationAdjustedInvested!.Value.Should().BeGreaterThan(scenario.Summary.Invested);

        // Seri: alış gününde başlar, bugünde biter; son nokta = özet; eşik her noktada dolu.
        scenario.FirstDate.Should().Be(new DateOnly(2024, 6, 1));
        scenario.Points.Should().NotBeEmpty();
        scenario.Points.Count.Should().BeLessThanOrEqualTo(500);
        var last = scenario.Points[^1];
        last.Date.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
        last.Value.Should().Be(scenario.Summary.CurrentValue);
        last.Cost.Should().Be(scenario.Summary.Invested);
        last.InflationAdjustedCost.Should().Be(scenario.Summary.InflationAdjustedInvested);
    }

    [Fact]
    public async Task Bes_scenario_uses_own_contribution_as_invested()
    {
        var client = ClientAs(Investor);

        var scenario = await client.GetFromJsonAsync<ScenarioComparisonDto>(
            $"/api/portfolio/scenario/{BesHolding}", Json);

        // BES: değer = fon (279.378), yatırılan = CEPTEN ödenen kendi katkı (120.000).
        scenario!.Summary.CurrentValue.Should().Be(279378m);
        scenario.Summary.Invested.Should().Be(120000m);
    }

    [Fact]
    public async Task Idor_other_users_holding_returns_404()
    {
        var admin = ClientAs(Admin);

        var resp = await admin.GetAsync($"/api/portfolio/scenario/{GoldHolding}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound); // varlığı bile sızdırma (11 §3)
    }

    [Fact]
    public async Task Unknown_holding_returns_404()
    {
        var client = ClientAs(Investor);

        var resp = await client.GetAsync($"/api/portfolio/scenario/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

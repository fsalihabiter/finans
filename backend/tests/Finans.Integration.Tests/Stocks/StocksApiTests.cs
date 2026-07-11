using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Finans.Application.Stocks;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Seed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Finans.Integration.Tests.Stocks;

/// <summary>
/// `GET /api/stocks/{symbol}/metrics` uçtan uca (T4.2, SC-28). Varsayılan factory'de
/// Finnhub anahtarı YOK → NotConfigured sağlayıcı → anlamlı 502 (uygulama çökmez, NFR-5).
/// Stub sağlayıcıyla 200/404/400 sözleşme yanıtları doğrulanır (ağ yok).
/// </summary>
public sealed class StocksApiTests : IClassFixture<SqliteWebApplicationFactory>, IAsyncLifetime
{
    private readonly SqliteWebApplicationFactory _factory;
    private static readonly Guid Investor = SeedData.Id("user-1");

    public StocksApiTests(SqliteWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedData.SeedAsync(db);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private sealed class StubProvider(Func<string, StockMetricsDto?> responder) : IStockDataProvider
    {
        public string Source => "stub";
        public Task<StockMetricsDto?> GetMetricsAsync(string symbol, CancellationToken ct = default) =>
            Task.FromResult(responder(symbol));
    }

    private static StockMetricsDto Aapl() => new(
        "AAPL", "Apple Inc.", "NASDAQ", "USD", 201.40m, 0.012m,
        new StockMetricValues(28.4m, 44.1m, 0.0052m, 0.091m),
        new StockSectorContext("above", "high", "low", "positive"),
        new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc), "stub");

    private HttpClient ClientWith(Func<string, StockMetricsDto?> responder)
    {
        var client = _factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
        {
            services.RemoveAll<IStockDataProvider>();
            services.AddSingleton<IStockDataProvider>(new StubProvider(responder));
        })).CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", Investor.ToString());
        return client;
    }

    private HttpClient DefaultClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", Investor.ToString());
        return client;
    }

    [Fact]
    public async Task Returns_contract_shaped_payload_for_known_symbol()
    {
        var resp = await ClientWith(_ => Aapl()).GetAsync("/api/stocks/aapl/metrics");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        root.GetProperty("symbol").GetString().Should().Be("AAPL"); // normalize edildi
        root.GetProperty("name").GetString().Should().Be("Apple Inc.");
        root.GetProperty("metrics").GetProperty("peRatio").GetDecimal().Should().Be(28.4m);
        root.GetProperty("sectorContext").GetProperty("peRatio").GetString().Should().Be("above");
    }

    [Fact]
    public async Task Unknown_symbol_returns_404_with_contract_envelope()
    {
        var resp = await ClientWith(_ => null).GetAsync("/api/stocks/YOKBU/metrics");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task Invalid_symbol_returns_400_validation()
    {
        var resp = await ClientWith(_ => Aapl()).GetAsync("/api/stocks/AAPL%21%21/metrics"); // "AAPL!!"

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task Not_configured_returns_meaningful_502_and_does_not_crash()
    {
        // Varsayılan factory: Stocks:ApiKey boş → NotConfiguredStockDataProvider (NFR-5).
        var resp = await DefaultClient().GetAsync("/api/stocks/AAPL/metrics");

        resp.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var error = doc.RootElement.GetProperty("error");
        error.GetProperty("code").GetString().Should().Be("UPSTREAM_ERROR");
        error.GetProperty("message").GetString().Should().Contain("yapılandırılmamış");
    }

    // ── T4.3: /explain (SC-29) ──

    [Fact]
    public async Task Explain_returns_200_with_fallback_card_when_llm_not_configured()
    {
        // Metrik sağlayıcı stub + LLM anahtarı yok (NoopLlmClient) → 200 + fallback kartı
        // (uygulama çökmez, sayılar etkilenmez — NFR-5; 07 §5).
        var resp = await ClientWith(_ => Aapl()).GetAsync("/api/stocks/AAPL/explain");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("source").GetString().Should().Be("fallback");
        doc.RootElement.GetProperty("cards").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Explain_propagates_404_for_unknown_symbol()
    {
        var resp = await ClientWith(_ => null).GetAsync("/api/stocks/YOKBU/explain");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── T4.5: /history (SC-30) ──

    private HttpClient ClientWithHistory(Func<string, IReadOnlyList<StockPricePoint>?> responder)
    {
        var client = _factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
        {
            services.RemoveAll<IStockHistoryProvider>();
            services.AddSingleton<IStockHistoryProvider>(new StubHistoryProvider(responder));
        })).CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", Investor.ToString());
        return client;
    }

    private sealed class StubHistoryProvider(Func<string, IReadOnlyList<StockPricePoint>?> responder) : IStockHistoryProvider
    {
        public string Source => "stub";
        public Task<IReadOnlyList<StockPricePoint>?> GetDailyHistoryAsync(string symbol, CancellationToken ct = default) =>
            Task.FromResult(responder(symbol));
    }

    [Fact]
    public async Task History_returns_range_sliced_series_with_change_ratio()
    {
        var points = Enumerable.Range(0, 40)
            .Select(i => new StockPricePoint(
                DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(i - 39), 100m + i))
            .ToList();
        var resp = await ClientWithHistory(_ => points).GetAsync("/api/stocks/AAPL/history?range=1w");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        root.GetProperty("range").GetString().Should().Be("1w");
        root.GetProperty("points").GetArrayLength().Should().BeInRange(7, 9);
        root.GetProperty("changeRatio").GetDecimal().Should().BeGreaterThan(0);
        root.GetProperty("source").GetString().Should().Be("stub");
    }

    [Fact]
    public async Task History_invalid_range_returns_400()
    {
        var resp = await ClientWithHistory(_ => null).GetAsync("/api/stocks/AAPL/history?range=2saat");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

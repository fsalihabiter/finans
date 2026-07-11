using FluentAssertions;
using Finans.Infrastructure.Stocks;
using Finans.Integration.Tests.Pricing;

namespace Finans.Integration.Tests.Stocks;

/// <summary>
/// T4.2 (SC-28) — Finnhub ayrıştırma/eşleme, stub HTTP ile (ağ yok): üç ucun birleşimi,
/// yüzde→oran normalizasyonu (Finnhub 0-100 ölçeği), alan fallback'leri (peTTM→peNormalized),
/// eksik alan → null ("veri yok"), bilinmeyen sembol → null.
/// </summary>
public sealed class FinnhubStockDataProviderTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 11, 12, 0, 0, TimeSpan.Zero);

    private const string MetricJson = """
        { "metric": { "peTTM": 28.4, "pb": 44.1,
                      "dividendYieldIndicatedAnnual": 0.52, "epsGrowthTTMYoy": 9.1 } }
        """;
    private const string QuoteJson = """{ "c": 201.40, "dp": 1.2 }""";
    private const string ProfileJson = """{ "name": "Apple Inc.", "exchange": "NASDAQ NMS - GLOBAL MARKET", "currency": "USD" }""";

    private static FinnhubStockDataProvider Build(
        string metric = MetricJson, string quote = QuoteJson, string profile = ProfileJson)
    {
        var handler = new StubHttpMessageHandler(req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            var body = path.Contains("/stock/metric") ? metric
                : path.Contains("/stock/profile2") ? profile
                : path.Contains("/quote") ? quote
                : throw new InvalidOperationException($"Beklenmeyen istek: {path}");
            return StubHttpMessageHandler.Json(body);
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://finnhub.test/api/v1/") };
        return new FinnhubStockDataProvider(http, new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task Maps_three_endpoints_into_the_contract_shape()
    {
        var dto = await Build().GetMetricsAsync("AAPL");

        dto.Should().NotBeNull();
        dto!.Symbol.Should().Be("AAPL");
        dto.Name.Should().Be("Apple Inc.");
        dto.Currency.Should().Be("USD");
        dto.Price.Should().Be(201.40m);
        dto.ChangeRatio.Should().Be(0.012m);              // dp 1.2 (%) → 0,012 (oran)
        dto.Metrics.PeRatio.Should().Be(28.4m);           // düz oran — bölünmez
        dto.Metrics.PbRatio.Should().Be(44.1m);
        dto.Metrics.DividendYield.Should().Be(0.0052m);   // 0.52 (%) → 0,0052 (oran)
        dto.Metrics.EarningsGrowth.Should().Be(0.091m);   // 9.1 (%) → 0,091 (oran)
        dto.SectorContext.PeRatio.Should().Be("above");
        dto.AsOfUtc.Should().Be(Now.UtcDateTime);
        dto.Source.Should().Be("finnhub");
    }

    [Fact]
    public async Task Falls_back_to_normalized_pe_when_ttm_missing()
    {
        var metric = """{ "metric": { "peNormalizedAnnual": 18.2, "pb": 3.0 } }""";

        var dto = await Build(metric: metric).GetMetricsAsync("AAPL");

        dto!.Metrics.PeRatio.Should().Be(18.2m);
        dto.Metrics.DividendYield.Should().BeNull();      // veri yok → null (etiket de null)
        dto.SectorContext.DividendYield.Should().BeNull();
    }

    [Fact]
    public async Task Unknown_symbol_returns_null()
    {
        // Finnhub bilinmeyen sembole 200 + boş gövdeler döner.
        var dto = await Build(
            metric: """{ "metric": {} }""",
            quote: """{ "c": 0, "dp": null }""",
            profile: "{}").GetMetricsAsync("YOKBU");

        dto.Should().BeNull();
    }

    [Fact]
    public async Task Missing_profile_name_still_returns_dto_when_price_exists()
    {
        // Profil eksik ama fiyat var (bazı OTC semboller) → sembol adıyla devam, 404 değil.
        var dto = await Build(profile: "{}").GetMetricsAsync("XYZ");

        dto.Should().NotBeNull();
        dto!.Name.Should().Be("XYZ");
        dto.Currency.Should().Be("USD"); // varsayılan
    }
}

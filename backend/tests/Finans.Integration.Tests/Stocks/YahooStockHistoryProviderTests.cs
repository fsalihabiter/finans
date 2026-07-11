using System.Net;
using FluentAssertions;
using Finans.Infrastructure.Stocks;
using Finans.Integration.Tests.Pricing;

namespace Finans.Integration.Tests.Stocks;

/// <summary>
/// T4.5 (SC-30) — Yahoo chart API ayrıştırma, stub HTTP ile (ağ yok): timestamp+close eşleşmesi,
/// null kapanış atlama, ondalık kırpma, sembol dönüşümü (BRK.B → BRK-B), 404 → null.
/// </summary>
public sealed class YahooStockHistoryProviderTests
{
    // 2026-07-09/10/11 UTC gün başları + bir null kapanış.
    private const string ChartJson = """
        { "chart": { "result": [ {
            "meta": { "symbol": "AAPL" },
            "timestamp": [1783036800, 1783123200, 1783209600, 1783296000],
            "indicators": { "quote": [ { "close": [101.5, null, 102.25, 315.32000732421875] } ] }
        } ], "error": null } }
        """;

    private static (YahooStockHistoryProvider provider, List<string> urls) Build(
        string body = ChartJson, HttpStatusCode status = HttpStatusCode.OK)
    {
        var urls = new List<string>();
        var handler = new StubHttpMessageHandler(req =>
        {
            urls.Add(req.RequestUri!.PathAndQuery);
            return StubHttpMessageHandler.Json(body, status);
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://yahoo.test/") };
        return (new YahooStockHistoryProvider(http), urls);
    }

    [Fact]
    public async Task Parses_timestamps_and_closes_skipping_nulls()
    {
        var (provider, urls) = Build();

        var points = await provider.GetDailyHistoryAsync("AAPL");

        points.Should().HaveCount(3);                       // null kapanış atlandı
        points![^1].Close.Should().Be(315.32m);             // ondalık gürültüsü kırpıldı (4 basamak)
        points[0].Close.Should().Be(101.5m);
        urls.Single().Should().Contain("interval=1d").And.Contain("period1=0");
    }

    [Fact]
    public async Task Maps_dotted_symbols_to_yahoo_dash_format()
    {
        var (provider, urls) = Build();

        await provider.GetDailyHistoryAsync("BRK.B");

        urls.Single().Should().Contain("/v8/finance/chart/BRK-B?");
    }

    [Fact]
    public async Task Http_404_means_unknown_symbol_returns_null()
    {
        var (provider, _) = Build("""{ "chart": { "result": null, "error": { "code": "Not Found" } } }""",
            HttpStatusCode.NotFound);

        var points = await provider.GetDailyHistoryAsync("YOKBU");

        points.Should().BeNull();
    }

    [Fact]
    public async Task Empty_result_returns_null()
    {
        var (provider, _) = Build("""{ "chart": { "result": [], "error": null } }""");

        var points = await provider.GetDailyHistoryAsync("BOS");

        points.Should().BeNull();
    }
}

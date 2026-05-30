using FluentAssertions;
using Finans.Application.Pricing;
using Finans.Domain.Enums;
using Finans.Infrastructure.Pricing;

namespace Finans.Integration.Tests.Pricing;

/// <summary>
/// Truncgil gram altın sağlayıcısı (T2.1, SC-17): <c>today.json</c>'dan "GRA" satış
/// fiyatını TRY olarak okur, sayı ve string biçimli alanları güvenle ayrıştırır, gram
/// altın yoksa istisna fırlatır.
/// </summary>
public sealed class TruncgilGoldPriceProviderTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 31, 9, 0, 0, TimeSpan.Zero);

    private static TruncgilGoldPriceProvider Build(string json) =>
        new(
            new HttpClient(StubHttpMessageHandler.Always(json))
            {
                BaseAddress = new Uri("https://finans.truncgil.com/"),
            },
            new FixedTimeProvider(Now));

    [Fact]
    public async Task Parses_gram_gold_selling_price()
    {
        const string json = """
            {
              "USD": { "Buying": 45.7333, "Selling": 45.9165, "Type": "Currency", "Change": -0.1 },
              "GRA": { "Buying": 6686.84, "Selling": 6687.67, "Type": "Gold", "Name": "GRAMALTIN", "Change": 0.85 }
            }
            """;

        var quotes = await Build(json).GetQuotesAsync([PriceInstrument.GramGold()]);

        var q = quotes.Should().ContainSingle().Subject;
        q.Price.Should().Be(6687.67m);
        q.QuoteCurrency.Should().Be(CurrencyCode.TRY);
        q.Instrument.Kind.Should().Be(PriceInstrumentKind.Gold);
        q.AsOfUtc.Should().Be(Now.UtcDateTime);
        q.Source.Should().Be("truncgil");
    }

    [Fact]
    public async Task Parses_string_formatted_numbers()
    {
        const string json = """{ "GRA": { "Buying": "6686.84", "Selling": "6687.67", "Type": "Gold" } }""";

        var quotes = await Build(json).GetQuotesAsync([PriceInstrument.GramGold()]);

        quotes.Should().ContainSingle().Which.Price.Should().Be(6687.67m);
    }

    [Fact]
    public async Task Skips_when_no_gold_requested()
    {
        var quotes = await Build("{}").GetQuotesAsync([PriceInstrument.ForCurrency(CurrencyCode.USD)]);

        quotes.Should().BeEmpty();
    }

    [Fact]
    public async Task Throws_when_gram_gold_missing()
    {
        const string json = """{ "USD": { "Buying": 1, "Selling": 1, "Type": "Currency" } }""";

        var act = () => Build(json).GetQuotesAsync([PriceInstrument.GramGold()]);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}

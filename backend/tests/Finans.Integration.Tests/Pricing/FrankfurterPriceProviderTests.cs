using System.Net;
using FluentAssertions;
using Finans.Application.Pricing;
using Finans.Domain.Enums;
using Finans.Infrastructure.Pricing;

namespace Finans.Integration.Tests.Pricing;

/// <summary>
/// Frankfurter döviz sağlayıcısı (T2.1, SC-17): ECB JSON'ını doğru ayrıştırır, her
/// para birimi için <b>doğrudan</b> TRY kurunu verir, döviz olmayan/TRY enstrümanı atlar,
/// HTTP hatasında istisna fırlatır (T2.3 fallback bunu yakalar).
/// </summary>
public sealed class FrankfurterPriceProviderTests
{
    private static FrankfurterPriceProvider Build(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
        new(new HttpClient(new StubHttpMessageHandler(responder))
        {
            BaseAddress = new Uri("https://api.frankfurter.dev/"),
        });

    [Fact]
    public async Task Parses_direct_TRY_rate_per_currency()
    {
        var provider = Build(req =>
        {
            var json = req.RequestUri!.Query.Contains("base=USD")
                ? """{"amount":1.0,"base":"USD","date":"2026-05-29","rates":{"TRY":45.886}}"""
                : """{"amount":1.0,"base":"EUR","date":"2026-05-29","rates":{"TRY":53.5748}}""";
            return StubHttpMessageHandler.Json(json);
        });

        var quotes = await provider.GetQuotesAsync(
            [PriceInstrument.ForCurrency(CurrencyCode.USD), PriceInstrument.ForCurrency(CurrencyCode.EUR)]);

        quotes.Should().HaveCount(2);

        var usd = quotes.Single(q => q.Instrument.Currency == CurrencyCode.USD);
        usd.Price.Should().Be(45.886m);
        usd.QuoteCurrency.Should().Be(CurrencyCode.TRY);
        usd.AsOfUtc.Should().Be(new DateTime(2026, 5, 29, 0, 0, 0, DateTimeKind.Utc));
        usd.Source.Should().Be("frankfurter");

        quotes.Single(q => q.Instrument.Currency == CurrencyCode.EUR).Price.Should().Be(53.5748m);
    }

    [Fact]
    public async Task Skips_non_currency_and_TRY_instruments()
    {
        var provider = Build(_ =>
            StubHttpMessageHandler.Json("""{"date":"2026-05-29","rates":{"TRY":1}}"""));

        var quotes = await provider.GetQuotesAsync(
            [PriceInstrument.GramGold(), PriceInstrument.ForCurrency(CurrencyCode.TRY)]);

        quotes.Should().BeEmpty();
    }

    [Fact]
    public void CanQuote_only_foreign_currencies()
    {
        var provider = Build(_ => StubHttpMessageHandler.Json("{}"));

        provider.CanQuote(PriceInstrument.ForCurrency(CurrencyCode.USD)).Should().BeTrue();
        provider.CanQuote(PriceInstrument.ForCurrency(CurrencyCode.TRY)).Should().BeFalse();
        provider.CanQuote(PriceInstrument.GramGold()).Should().BeFalse();
    }

    [Fact]
    public async Task Throws_on_http_error()
    {
        var provider = Build(_ => StubHttpMessageHandler.Json("error", HttpStatusCode.InternalServerError));

        var act = () => provider.GetQuotesAsync([PriceInstrument.ForCurrency(CurrencyCode.USD)]);

        await act.Should().ThrowAsync<HttpRequestException>();
    }
}

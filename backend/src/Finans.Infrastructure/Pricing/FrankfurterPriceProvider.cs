using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Finans.Application.Pricing;
using Finans.Domain.Enums;

namespace Finans.Infrastructure.Pricing;

/// <summary>
/// Döviz fiyat sağlayıcısı — <b>Frankfurter</b> (ECB verisi, anahtarsız, kotasız). Her
/// yabancı para için <c>GET /v1/latest?base={ccy}&amp;symbols=TRY</c> ile <b>doğrudan</b>
/// kuru alır (ters çevirme yok → tam isabet; finansal hassasiyet, NFR-1). TRY enstrümanı
/// atlanır (baz pivot). Birden çok para birimi paralel çekilir. (T2.1)
/// </summary>
public sealed class FrankfurterPriceProvider(HttpClient http) : IPriceProvider
{
    public const string SourceKey = "frankfurter";

    public string Source => SourceKey;

    public bool CanQuote(PriceInstrument instrument) =>
        instrument.Kind == PriceInstrumentKind.Currency && instrument.Currency != CurrencyCode.TRY;

    public async Task<IReadOnlyList<PriceQuote>> GetQuotesAsync(
        IReadOnlyCollection<PriceInstrument> instruments, CancellationToken ct = default)
    {
        var currencies = instruments.Where(CanQuote)
            .Select(i => i.Currency)
            .Distinct()
            .ToList();

        if (currencies.Count == 0)
            return [];

        var quotes = await Task.WhenAll(currencies.Select(c => FetchAsync(c, ct)));
        return quotes;
    }

    private async Task<PriceQuote> FetchAsync(CurrencyCode currency, CancellationToken ct)
    {
        var dto = await http.GetFromJsonAsync<LatestResponse>(
            $"v1/latest?base={currency}&symbols=TRY", ct)
            ?? throw new InvalidOperationException($"Frankfurter boş yanıt ({currency}).");

        if (dto.Rates is null || !dto.Rates.TryGetValue(nameof(CurrencyCode.TRY), out var rate))
            throw new InvalidOperationException($"Frankfurter yanıtında TRY kuru yok ({currency}).");

        var asOf = DateOnly.ParseExact(dto.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture)
            .ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        return new PriceQuote(
            PriceInstrument.ForCurrency(currency), rate, CurrencyCode.TRY, asOf, SourceKey);
    }

    private sealed record LatestResponse(
        [property: JsonPropertyName("date")] string Date,
        [property: JsonPropertyName("rates")] Dictionary<string, decimal>? Rates);
}

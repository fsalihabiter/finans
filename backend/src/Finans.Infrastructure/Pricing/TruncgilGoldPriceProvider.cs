using System.Globalization;
using System.Text.Json;
using Finans.Application.Pricing;
using Finans.Domain.Enums;

namespace Finans.Infrastructure.Pricing;

/// <summary>
/// Gram altın fiyat sağlayıcısı — <b>Truncgil</b> (TR piyasası, anahtarsız). Tek
/// <c>GET /v4/today.json</c> çağrısından <c>"GRA"</c> (gram altın) <b>satış</b> fiyatını
/// okur — satış, kullanıcının her yerde gördüğü manşet gram fiyatıdır (alış, muhafazakâr
/// realize-değer için ileride seçenek). Fiyat TRY cinsinden. Kaynakta zaman damgası
/// olmadığından <see cref="TimeProvider"/> ile çekim anı kullanılır. (T2.1)
/// </summary>
public sealed class TruncgilGoldPriceProvider(HttpClient http, TimeProvider clock) : IPriceProvider
{
    public const string SourceKey = "truncgil";
    private const string GramGoldKey = "GRA";
    private const string PriceField = "Selling";

    public string Source => SourceKey;

    public bool CanQuote(PriceInstrument instrument) => instrument.Kind == PriceInstrumentKind.Gold;

    public async Task<IReadOnlyList<PriceQuote>> GetQuotesAsync(
        IReadOnlyCollection<PriceInstrument> instruments, CancellationToken ct = default)
    {
        var gold = instruments.Where(CanQuote).Distinct().ToList();
        if (gold.Count == 0)
            return [];

        await using var stream = await http.GetStreamAsync("v4/today.json", ct);
        var root = await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement>>(
            stream, cancellationToken: ct)
            ?? throw new InvalidOperationException("Truncgil geçersiz/boş yanıt.");

        if (!root.TryGetValue(GramGoldKey, out var gra))
            throw new InvalidOperationException($"Truncgil '{GramGoldKey}' (gram altın) bulunamadı.");

        var price = ReadDecimal(gra, PriceField);
        var asOf = clock.GetUtcNow().UtcDateTime;

        return gold
            .Select(i => new PriceQuote(i, price, CurrencyCode.TRY, asOf, SourceKey))
            .ToList();
    }

    /// <summary>Truncgil alanı JSON sayı ya da string olabilir → ikisini de güvenle (invariant) decimal'e çevirir.</summary>
    private static decimal ReadDecimal(JsonElement entry, string field)
    {
        if (!entry.TryGetProperty(field, out var value))
            throw new InvalidOperationException($"Truncgil alanı yok: {field}.");

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.GetDecimal(),
            JsonValueKind.String => decimal.Parse(
                value.GetString()!, NumberStyles.Number, CultureInfo.InvariantCulture),
            _ => throw new InvalidOperationException($"Truncgil alanı sayı değil: {field}."),
        };
    }
}

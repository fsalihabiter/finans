using Finans.Application.Portfolio;
using Finans.Domain.Enums;

namespace Finans.Application.Tests.Portfolio;

/// <summary>
/// CurrencyConverter birim testleri (SC-03, NFR-1: kur dönüşümü deterministik).
/// Kurlar seed ile aynı: USD→TRY 48, EUR→TRY 52.
/// </summary>
public class CurrencyConverterTests
{
    private static readonly FxQuote[] SeedQuotes =
    [
        new(CurrencyCode.USD, CurrencyCode.TRY, 48m),
        new(CurrencyCode.EUR, CurrencyCode.TRY, 52m),
    ];

    private static CurrencyConverter Sut() => new(SeedQuotes);

    [Fact]
    public void Converts_usd_to_try_at_direct_rate()
    {
        // 2.000 $ × 48 = 96.000 ₺ (seed dolar pozisyonunun güncel değeri)
        Assert.Equal(96000m, Sut().Convert(2000m, CurrencyCode.USD, CurrencyCode.TRY));
    }

    [Fact]
    public void Same_currency_is_identity()
    {
        Assert.Equal(1234.56m, Sut().Convert(1234.56m, CurrencyCode.TRY, CurrencyCode.TRY));
    }

    [Fact]
    public void Same_currency_works_even_without_any_quotes()
    {
        var empty = new CurrencyConverter([]);
        Assert.Equal(10m, empty.Convert(10m, CurrencyCode.USD, CurrencyCode.USD));
    }

    [Fact]
    public void Converts_try_to_usd_at_inverse_rate()
    {
        // 96.000 ₺ / 48 = 2.000 $ (ters yön türetilir)
        Assert.Equal(2000m, Sut().Convert(96000m, CurrencyCode.TRY, CurrencyCode.USD));
    }

    [Fact]
    public void Converts_eur_to_usd_via_try_pivot()
    {
        // EUR→USD doğrudan yok → TRY üzerinden çapraz: 96 € × 52 / 48 = 104 $
        Assert.Equal(104m, Sut().Convert(96m, CurrencyCode.EUR, CurrencyCode.USD));
    }

    [Fact]
    public void Converts_usd_to_eur_via_try_pivot()
    {
        // 104 $ × 48 / 52 = 96 € (çapraz, ters yön)
        Assert.Equal(96m, Sut().Convert(104m, CurrencyCode.USD, CurrencyCode.EUR));
    }

    [Fact]
    public void Explicit_inverse_quote_overrides_derived_inverse()
    {
        // Hem USD→TRY hem TRY→USD açıkça verilirse, verilen tırnak türetilene baskındır.
        var converter = new CurrencyConverter(
        [
            new(CurrencyCode.USD, CurrencyCode.TRY, 48m),
            new(CurrencyCode.TRY, CurrencyCode.USD, 0.02m), // 1/50, türetilen 1/48 değil
        ]);
        Assert.Equal(2m, converter.Convert(100m, CurrencyCode.TRY, CurrencyCode.USD));
    }

    [Fact]
    public void RateFor_throws_when_rate_unknown()
    {
        // Hiç kur yokken USD→TRY bilinmez → sessizce yanlış sayı değil, açık hata.
        // Ayrı tip (MissingFxRateException) → üst katman FX yarışını ayırt edebilir (SC-42).
        var empty = new CurrencyConverter([]);
        var ex = Assert.Throws<MissingFxRateException>(() => empty.RateFor(CurrencyCode.USD, CurrencyCode.TRY));
        Assert.Equal(CurrencyCode.USD, ex.From);
        Assert.Equal(CurrencyCode.TRY, ex.To);
    }

    [Fact]
    public void TryConvert_returns_false_when_rate_unknown()
    {
        var empty = new CurrencyConverter([]);
        Assert.False(empty.TryConvert(100m, CurrencyCode.USD, CurrencyCode.TRY, out var result));
        Assert.Equal(0m, result);
    }

    [Fact]
    public void TryConvert_returns_true_and_value_when_known()
    {
        Assert.True(Sut().TryConvert(2000m, CurrencyCode.USD, CurrencyCode.TRY, out var result));
        Assert.Equal(96000m, result);
    }

    [Fact]
    public void Non_positive_quotes_are_ignored()
    {
        // Sıfır/negatif kur güvenli değil → atlanır (bölme-sıfır / saçma sayı olmaz).
        var converter = new CurrencyConverter(
        [
            new(CurrencyCode.USD, CurrencyCode.TRY, 0m),
            new(CurrencyCode.EUR, CurrencyCode.TRY, -5m),
        ]);
        Assert.False(converter.TryConvert(1m, CurrencyCode.USD, CurrencyCode.TRY, out _));
        Assert.False(converter.TryConvert(1m, CurrencyCode.EUR, CurrencyCode.TRY, out _));
    }
}

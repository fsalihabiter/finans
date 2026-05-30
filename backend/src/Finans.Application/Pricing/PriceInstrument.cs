using Finans.Domain.Enums;

namespace Finans.Application.Pricing;

/// <summary>
/// Bir piyasa enstrümanının kaynaktan bağımsız türü (Faz 2: döviz + altın).
/// </summary>
public enum PriceInstrumentKind
{
    /// <summary>Yabancı para birimi (örn. USD/EUR) — fiyatı baz/quote para biriminde.</summary>
    Currency,

    /// <summary>Gram altın (TR piyasası, yerel primli) — fiyatı TRY cinsinden.</summary>
    Gold,
}

/// <summary>
/// Fiyatı istenen enstrümanın kaynaktan bağımsız tanımı (02 §2.2). Sağlayıcılar bunu
/// kendi sembollerine eşler (örn. <c>Currency/USD</c> → Frankfurter <c>base=USD</c>;
/// <c>Gold</c> → Truncgil <c>"GRA"</c>). Faz 2'de altın yalnızca <b>gram</b> birimidir.
/// </summary>
/// <param name="Kind">Enstrüman türü.</param>
/// <param name="Currency">
/// <see cref="PriceInstrumentKind.Currency"/> için fiyatı istenen yabancı para (USD);
/// <see cref="PriceInstrumentKind.Gold"/> için fiyatlama para birimi (TRY).
/// </param>
public readonly record struct PriceInstrument(PriceInstrumentKind Kind, CurrencyCode Currency)
{
    /// <summary>Gram altın enstrümanı (varsayılan TRY fiyatlı).</summary>
    public static PriceInstrument GramGold(CurrencyCode pricingCurrency = CurrencyCode.TRY) =>
        new(PriceInstrumentKind.Gold, pricingCurrency);

    /// <summary>Yabancı para birimi enstrümanı (TRY karşısında fiyatlanır).</summary>
    public static PriceInstrument ForCurrency(CurrencyCode currency) =>
        new(PriceInstrumentKind.Currency, currency);
}

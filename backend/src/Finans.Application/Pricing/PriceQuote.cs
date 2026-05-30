using Finans.Domain.Enums;

namespace Finans.Application.Pricing;

/// <summary>
/// Bir sağlayıcıdan dönen tek fiyat tırnağı. Tüm parasal değerler <see cref="decimal"/>
/// (NFR-1). T2.2'de cache'lenip <c>PriceSnapshots</c> / <c>FxRates</c>'e yazılır.
/// </summary>
/// <param name="Instrument">Fiyatı verilen enstrüman.</param>
/// <param name="Price">
/// Birim fiyat (<paramref name="QuoteCurrency"/> cinsinden): <see cref="PriceInstrumentKind.Gold"/> →
/// 1 gram fiyatı; <see cref="PriceInstrumentKind.Currency"/> → 1 birim fiyatı.
/// </param>
/// <param name="QuoteCurrency">Fiyatın ifade edildiği para birimi (Faz 2: TRY).</param>
/// <param name="AsOfUtc">Fiyatın geçerli olduğu an (kaynaktan; yoksa çekim anı).</param>
/// <param name="Source">Kaynak anahtarı (<c>PriceSnapshot.Source</c>'a yazılır; örn. "frankfurter").</param>
/// <param name="IsStale">
/// Dış kaynak ulaşılamadığında <b>son bilinen</b> fiyattan üretilen fallback tırnağı (T2.3, NFR-5).
/// Sağlayıcılar canlı tırnağı <c>false</c> üretir; üst katman fallback'te <c>true</c> işaretler.
/// Stale tırnak geçmişe (snapshot/fxrate) <b>yazılmaz</b>; yalnız gösterim/uyarı içindir.
/// </param>
public sealed record PriceQuote(
    PriceInstrument Instrument,
    decimal Price,
    CurrencyCode QuoteCurrency,
    DateTime AsOfUtc,
    string Source,
    bool IsStale = false);

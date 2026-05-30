using Finans.Domain.Enums;

namespace Finans.Application.Pricing;

// ── Fiyat API yanıt DTO'ları (04 §5 — GET /api/prices) ───────────────────────

/// <summary>
/// <c>GET /api/prices</c> yanıtı (T2.4). Bir tazeleme turunu özetler: ne zaman tazelendi,
/// cache'ten mi geldi, bayat (son-bilinen) fiyat var mı, hangi kaynaklar bu turda çöktü.
/// Fiyatlar kullanıcı-bağımsızdır (global piyasa).
/// </summary>
public sealed record PricesResponse(
    DateTime RefreshedAtUtc,
    bool FromCache,
    bool HasStale,
    IReadOnlyList<string> FailedSources,
    IReadOnlyList<PriceDto> Prices);

/// <summary>
/// Tek bir enstrümanın güncel fiyatı. <paramref name="Stale"/> ise canlı kaynak
/// ulaşılamadı, değer son-bilinen (UI'da "yaklaşık" gösterilir). Tüm tutarlar
/// <see cref="decimal"/> (NFR-1); enum'lar JSON'da string (allow-list adı).
/// </summary>
public sealed record PriceDto(
    PriceInstrumentKind Kind,
    CurrencyCode Currency,
    decimal Price,
    CurrencyCode QuoteCurrency,
    DateTime AsOfUtc,
    string Source,
    bool Stale);

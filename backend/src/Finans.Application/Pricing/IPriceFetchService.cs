namespace Finans.Application.Pricing;

/// <summary>
/// Canlı fiyat orkestrasyonu (T2.2, 02 §2.2). Fiyatlanabilir varlıkları enstrümana
/// eşler, kayıtlı <see cref="IPriceProvider"/>'lara <c>CanQuote</c>'a göre yönlendirir,
/// sonucu <b>kısa TTL'li</b> cache'ler (dış API en çok TTL'de bir çağrılır) ve kalıcı
/// olarak yazar: <c>PriceSnapshots</c> (geçmiş) + <c>FxRates</c> (converter) +
/// <c>Holding.CurrentPrice</c> (özet/holdings okuma yolu).
/// </summary>
public interface IPriceFetchService
{
    /// <summary>Fiyatları tazeler (cache'liyse dış çağrı/yazma yapmadan cache'ten döner).</summary>
    Task<PriceRefreshResult> RefreshAsync(CancellationToken ct = default);
}

/// <summary>
/// Bir tazeleme turunun sonucu. <paramref name="FromCache"/> ise dış API'ye gidilmedi
/// (TTL içinde). <paramref name="FailedSources"/> bu turda çağrısı başarısız olan sağlayıcı
/// anahtarlarıdır; bunların enstrümanları <see cref="PriceQuote.IsStale"/> son-bilinen
/// tırnaklarla doldurulur (T2.3, NFR-5). <see cref="HasStale"/> en az bir bayat fiyat varsa.
/// </summary>
public sealed record PriceRefreshResult(
    IReadOnlyList<PriceQuote> Quotes,
    DateTime RefreshedAtUtc,
    bool FromCache,
    IReadOnlyList<string> FailedSources)
{
    /// <summary>Sonuçta en az bir bayat (son-bilinen) fiyat var mı?</summary>
    public bool HasStale => Quotes.Any(q => q.IsStale);
}

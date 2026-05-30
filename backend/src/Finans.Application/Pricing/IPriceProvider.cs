namespace Finans.Application.Pricing;

/// <summary>
/// Dış fiyat kaynağı soyutlaması (02 §2.2). Her sağlayıcı tek bir kaynağı
/// (Frankfurter döviz, Truncgil gram altın, ...) temsil eder ve yalnızca
/// <see cref="CanQuote"/> dediği enstrümanları fiyatlar. Orkestrasyon + cache +
/// fallback üst katmanda (T2.2 <c>PriceFetchService</c>, T2.3 son bilinen fiyat).
/// </summary>
public interface IPriceProvider
{
    /// <summary>Kaynak anahtarı — <c>PriceSnapshot.Source</c> ve log için (örn. "frankfurter").</summary>
    string Source { get; }

    /// <summary>Bu sağlayıcı verilen enstrümanı fiyatlayabilir mi? (Üst katman yönlendirmesi için.)</summary>
    bool CanQuote(PriceInstrument instrument);

    /// <summary>
    /// İstenen enstrümanlar için güncel tırnakları dış kaynaktan çeker. Fiyatlayamadığı
    /// enstrümanları sessizce atlar. Taşıma/ayrıştırma hatasında istisna fırlatır —
    /// fallback (son bilinen fiyat + <c>stale</c>) üst katmanın sorumluluğudur (T2.3).
    /// </summary>
    Task<IReadOnlyList<PriceQuote>> GetQuotesAsync(
        IReadOnlyCollection<PriceInstrument> instruments, CancellationToken ct = default);
}

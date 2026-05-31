using Finans.Application.Portfolio;
using Microsoft.EntityFrameworkCore;

namespace Finans.Infrastructure.Persistence;

/// <summary>
/// <see cref="IFxRateProvider"/>'ın EF tabanlı uygulaması: her para birimi çifti
/// için <b>en güncel</b> tırnağı (AsOfUtc max) yükleyip saf
/// <see cref="CurrencyConverter"/> kurar. Faz 1'de kurlar elle girilir/seed'lenir
/// (02 §2.2, 03 §12). Cache T1.15/Faz 2'de (anahtar pb çifti) eklenecek.
/// </summary>
public sealed class EfFxRateProvider(FinansDbContext db) : IFxRateProvider
{
    /// <summary>En güncel (her pb çifti) tırnakları döndürür — <b>serileştirilebilir</b> (cache için, T2.7).</summary>
    public async Task<List<FxQuote>> GetQuotesAsync(CancellationToken ct = default)
    {
        // Sırala + projekte SQL'de; en güncel tırnağı (her pb çifti) bellekte seç.
        // (EF, gruptan entity seçen GroupBy.First()'i SQL'e çeviremez; FxRates tablosu
        // Faz 1'de küçük → bellekte gruplama güvenli ve nettir.)
        var rows = await db.FxRates
            .OrderByDescending(r => r.AsOfUtc)
            .Select(r => new { r.FromCurrency, r.ToCurrency, r.Rate })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => (r.FromCurrency, r.ToCurrency))
            .Select(g => g.First()) // AsOfUtc'a göre azalan sıralı → ilki en güncel
            .Select(r => new FxQuote(r.FromCurrency, r.ToCurrency, r.Rate))
            .ToList();
    }

    public async Task<CurrencyConverter> GetConverterAsync(CancellationToken ct = default) =>
        new(await GetQuotesAsync(ct));
}

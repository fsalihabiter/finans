using Finans.Application.Portfolio;
using Finans.Domain.Enums;
using Finans.Domain.Portfolio;
using Microsoft.EntityFrameworkCore;
using Finans.Infrastructure.Persistence;

namespace Finans.Infrastructure.Services;

/// <summary>
/// Holding entity'lerini saf hesap girdisine/DTO'ya çeviren ortak yardımcılar.
/// Birim fiyatlar varlığın PricingCurrency'sinden baz pb'ye çevrilir (T1.3) ki
/// toplulaştırma ve ağırlık tutarlı olsun; ham birim değerler DTO'da korunur.
/// </summary>
internal static class HoldingMapping
{
    /// <summary>İstenen baz pb yoksa kullanıcının tercihine düş (yoksa TRY).</summary>
    public static async Task<CurrencyCode> ResolveBaseCurrencyAsync(
        FinansDbContext db, Guid userId, CurrencyCode? requested, CancellationToken ct)
    {
        if (requested is { } r)
            return r;

        var pref = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => (CurrencyCode?)u.BaseCurrency)
            .FirstOrDefaultAsync(ct);

        return pref ?? CurrencyCode.TRY;
    }

    /// <summary>Holding'leri baz pb'ye çevrilmiş saf hesap girdilerine dönüştürür (sıra korunur).</summary>
    public static List<HoldingInput> ToInputs(
        IReadOnlyList<Holding> holdings, CurrencyConverter converter, CurrencyCode baseCurrency) =>
        holdings.Select(h => new HoldingInput(
            h.Asset.Type,
            h.Asset.Name,
            h.Quantity,
            converter.Convert(h.AvgCost, h.Asset.PricingCurrency, baseCurrency),
            h.CurrentPrice is { } price
                ? converter.Convert(price, h.Asset.PricingCurrency, baseCurrency)
                : null))
        .ToList();
}

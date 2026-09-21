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
    /// <summary>
    /// TR yerel "şimdi" (UTC+3 sabit; DST yok). BES tarih karşılaştırmaları kullanıcının
    /// pencerede gördüğü güne göre (T-BES.9).
    /// </summary>
    internal static DateTime TrNow() => DateTime.UtcNow.AddHours(3);

    /// <summary>
    /// Okuma anında pozisyonu kaynaktan yeniden türetir (§6, T1.5): miktar/ort. maliyet
    /// saklanan cache alanından DEĞİL, gerçek işlemlerden (BES değilse) ya da YATIRILMIŞ
    /// kendi katkı toplamından (BES — maliyet = cepten ödenen; devlet katkısı ve ileri
    /// tarihli plan katkıları maliyet değil) hesaplanır. Salt okunur — kalıcılaştırılmaz.
    /// Liste, özet ve değer serisi AYNI kuralı kullanır ki üç yüzey birbirini tutsun (NFR-1).
    /// Çağıran <c>Transactions</c> + <c>BesDetails</c> + <c>BesContributions</c> include etmiş olmalı.
    /// </summary>
    internal static void ApplyReadPosition(Holding h)
    {
        if (h.BesDetails is not null)
        {
            // BES nominal hesap; miktar 1 sabit. Yalnız yatırılmış (≤ bugün) kendi katkılar.
            var asOf = TrNow();
            // Tek taban tanımı (BesCalculator.DepositedTotals): kendi katkı ödendiyse fonda;
            // devlet katkısı ancak YATMA tarihi geçtiyse fonda ("yolda" olan sayılmaz).
            var (ownDeposited, stateDeposited) = BesCalculator.DepositedTotals(
                h.BesContributions.Select(c => (c.PaidAtUtc, c.OwnAmount, c.StateAmount)), asOf);
            h.AvgCost = ownDeposited;

            // GD-002 — BES'in portföy DEĞERİ burada türetilir ki liste · özet · değer
            // serisi · senaryo AYNI kuralı kullansın (beş yüzey, tek kaynak):
            //   değer = kendi katkının fon değeri + hak ediş oranı × devlet katkısının fon değeri
            // Fon değerleri girilmemişse havuzlar katkı tutarına eşit sayılır → değer yine
            // hesaplanır (BES artık toplamdan düşmüyor). Hak edilmemiş devlet katkısı
            // değere GİRMEZ: bugün ayrılsan alamayacağın para.
            var fund = BesCalculator.FundReturnFor(
                h.AvgCost, stateDeposited, h.BesDetails.OwnFundValue, h.BesDetails.StateFundValue);
            var vestedRate = BesCalculator.VestedRateFor(
                h.BesDetails.JoinedAtUtc, BesCalculator.AgeFor(h.BesDetails.BirthYear, asOf), asOf);

            h.CurrentPrice = BesCalculator.VestedPortfolioValueFor(fund.OwnValue, fund.StateValue, vestedRate);
            return;
        }

        // İşlem yoksa türetim 0/0 döner — saklanan değerleri SİLMEZ (örn. Nakit: doğrudan miktar
        // tutulur, alış/satış işlemi olmaz). Yalnız işlem varsa kaynaktan yeniden türetilir.
        if (h.Transactions.Count == 0)
            return;

        var pos = PortfolioCalculationService.DerivePosition(
            h.Transactions.Select(t => new TransactionInput(t.Type, t.Quantity, t.UnitPrice, t.Fee)));
        h.Quantity = pos.Quantity;
        h.AvgCost = pos.AvgCost;
    }

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

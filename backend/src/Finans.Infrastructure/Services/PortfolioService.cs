using Finans.Application.Common;
using Finans.Application.Portfolio;
using Finans.Domain.Enums;
using Finans.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Finans.Infrastructure.Services;

/// <summary>
/// <see cref="IPortfolioService"/> EF uygulaması: geçerli kullanıcının pozisyonlarını
/// baz pb'ye çevirip (T1.3) saf hesap servisiyle (T1.1) özetler; enflasyonla reel
/// getiriyi (T1.4) ekler. **Kullanıcıya kapsanır** (11 §3). Tüm sayılar backend'de.
/// </summary>
public sealed class PortfolioService(
    FinansDbContext db,
    ICurrentUser currentUser,
    PortfolioCalculationService calc,
    IFxRateProvider fxRateProvider,
    IInflationRateProvider inflationRateProvider) : IPortfolioService
{
    public async Task<PortfolioSummaryDto> GetSummaryAsync(CurrencyCode? baseCurrency = null, CancellationToken ct = default)
    {
        var userId = currentUser.UserId;
        var baseCcy = await HoldingMapping.ResolveBaseCurrencyAsync(db, userId, baseCurrency, ct);

        var holdings = await db.Holdings
            .Where(h => h.UserId == userId)
            .Include(h => h.Asset)
            .Include(h => h.Transactions)
            .Include(h => h.BesDetails)
            .Include(h => h.BesContributions)
            .ToListAsync(ct);

        // Pozisyonu okuma anında KAYNAKTAN türet (liste ile aynı kural) — saklanan cache alanı
        // bayatsa (örn. ileri tarihli BES plan katkısı AvgCost'a işlenmişse) özet yanlış maliyet
        // göstermesin; özet = liste = değer serisi (T5.2'de yakalanan tutarsızlık düzeltmesi).
        foreach (var holding in holdings)
            HoldingMapping.ApplyReadPosition(holding);

        var converter = await fxRateProvider.GetConverterAsync(ct);
        var inflation = await inflationRateProvider.GetAnnualRateAsync(ct);

        var summary = calc.CalculateSummary(HoldingMapping.ToInputs(holdings, converter, baseCcy), inflation);

        var allocation = summary.Allocation
            .Select(a => new AllocationDto(a.AssetType, a.Name, a.Value, a.Weight))
            .ToList();

        return new PortfolioSummaryDto(
            baseCcy,
            summary.TotalValue,
            summary.TotalCost,
            summary.NetProfit,
            summary.ReturnRatio,
            summary.RealReturnRatio,
            allocation,
            DateTime.UtcNow);
    }
}

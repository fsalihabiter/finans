using Finans.Application.Portfolio;
using Microsoft.EntityFrameworkCore;

namespace Finans.Infrastructure.Persistence;

/// <summary>
/// <see cref="IInflationRateProvider"/>'ın EF tabanlı uygulaması: en güncel dönemin
/// (PeriodEndUtc max) yıllık oranını döner. Faz 1'de enflasyon elle girilir/seed'lenir
/// (TÜİK; 03 §12.2). Dönem-duyarlı seçim (pozisyonun elde tutma ufkuna göre) ileri
/// fazda; cache Faz 2'de.
/// </summary>
public sealed class EfInflationRateProvider(FinansDbContext db) : IInflationRateProvider
{
    public async Task<decimal?> GetAnnualRateAsync(CancellationToken ct = default)
    {
        var latest = await db.InflationRates
            .OrderByDescending(r => r.PeriodEndUtc)
            .Select(r => (decimal?)r.AnnualRate)
            .FirstOrDefaultAsync(ct);

        return latest;
    }
}

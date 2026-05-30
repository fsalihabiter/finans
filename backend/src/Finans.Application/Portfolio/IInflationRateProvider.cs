namespace Finans.Application.Portfolio;

/// <summary>
/// Reel getiri hesabı için geçerli yıllık enflasyon oranını sağlar
/// (CLAUDE.md §6: reel = (1+nominal)/(1+enflasyon)−1). Implementasyon
/// Infrastructure'da (EF + cache). Veri yoksa <c>null</c> → reel getiri null
/// (04 §4: realReturnRatio nullable).
/// </summary>
public interface IInflationRateProvider
{
    /// <summary>Geçerli yıllık enflasyon oranı (ondalık, örn. 0,38); veri yoksa null.</summary>
    Task<decimal?> GetAnnualRateAsync(CancellationToken ct = default);
}

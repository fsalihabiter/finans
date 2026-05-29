using Finans.Domain.Common;

namespace Finans.Domain.Portfolio;

/// <summary>
/// Dönemsel enflasyon oranı — reel getiri hesabı için (03 §A).
/// Reel getiri = (1 + nominal) / (1 + enflasyon) − 1 (CLAUDE.md §6).
/// </summary>
public class InflationRate : Entity
{
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }

    /// <summary>Yıllık oran (ondalık), örn. 0,380000 = %38. numeric(9,6).</summary>
    public decimal AnnualRate { get; set; }

    /// <summary>TÜİK (resmi). Seed'de örnek; prod'da gerçek veri.</summary>
    public string Source { get; set; } = "TÜİK";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

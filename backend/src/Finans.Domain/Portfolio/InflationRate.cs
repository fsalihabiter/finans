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

    /// <summary>
    /// Verinin kaynağı. Gerçek TÜFE beslemesi bağlanana kadar seed <b>"örnek"</b>
    /// yazar — placeholder bir oranı <c>"TÜİK"</c> ile etiketlemek, eğitimin
    /// "kaynak daima görünür" iddiasını (16 §6.4 · 14 §B1) çürütürdü (T6.21).
    /// Gerçek veri geldiğinde kurum adı + veri tarihiyle yazılır.
    /// </summary>
    public string Source { get; set; } = "örnek";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

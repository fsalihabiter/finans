using Finans.Domain.Common;
using Finans.Domain.Enums;

namespace Finans.Domain.Portfolio;

/// <summary>
/// BES'e özel alanlar (Holdings 1—0..1, 03 §A). Getiri maliyet tabanı =
/// OwnContribution + StateContribution; devlet katkısı UI'da AYRI gösterilir
/// ama getiri tabanına dahildir (FR-1.5, 03 §A modelleme kararı).
/// </summary>
public class BesDetails : Entity
{
    public Guid HoldingId { get; set; }

    /// <summary>Kendi katkı payı (maliyet bileşeni).</summary>
    public decimal OwnContribution { get; set; }

    /// <summary>Devlet katkısı — ayrı tutulur.</summary>
    public decimal StateContribution { get; set; }

    public VestingState VestingState { get; set; }
    public string? ProviderName { get; set; }
    public DateTime? JoinedAtUtc { get; set; }

    // ── Düzenli katkı planı (T-BES.6b): "bundan sonraki katkılar için kullan" ──
    /// <summary>Aktif plan tutarı (aylık). Değiştirilene kadar geçerli; gün geldikçe otomatik kayıt.</summary>
    public decimal? MonthlyAmount { get; set; }

    /// <summary>Plan ödeme günü (1–28).</summary>
    public int? ContributionDay { get; set; }

    /// <summary>Düzenli katkı planı aktif mi? (Açıksa eksik aylar görüntülemede otomatik üretilir.)</summary>
    public bool PlanActive { get; set; }

    public Holding Holding { get; set; } = null!;
}

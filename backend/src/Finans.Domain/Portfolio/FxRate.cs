using Finans.Domain.Common;
using Finans.Domain.Enums;

namespace Finans.Domain.Portfolio;

/// <summary>
/// Kur çevrimi (deterministik, NFR-1). 1 FromCurrency = Rate × ToCurrency.
/// Para birimi dönüşümü koddaki hesaba dahildir, LLM'e bırakılmaz (CLAUDE.md §3.2).
/// </summary>
public class FxRate : Entity
{
    public CurrencyCode FromCurrency { get; set; }
    public CurrencyCode ToCurrency { get; set; }
    public decimal Rate { get; set; }
    public string Source { get; set; } = "Manual";
    public DateTime AsOfUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

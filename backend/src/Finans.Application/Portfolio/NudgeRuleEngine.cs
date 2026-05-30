using System.Globalization;
using Finans.Domain.Enums;

namespace Finans.Application.Portfolio;

/// <summary>Bilgilendirici not önem düzeyi (UI stillemesi için; "yap" demez).</summary>
public enum NudgeSeverity
{
    Info,
    Warning,
}

/// <summary>
/// Kural tabanlı eğitici not (FR-2.4, 04 §5). **Yatırım tavsiyesi DEĞİL** (CLAUDE.md §2):
/// mevcut durumu açıklar, çerçeve/farkındalık sunar — "al/sat/yükselir" demez.
/// </summary>
public sealed record Nudge(string Id, string Icon, string Title, string Body, NudgeSeverity Severity);

/// <summary><c>GET /api/portfolio/nudges</c> yanıtı.</summary>
public sealed record NudgesResponse(IReadOnlyList<Nudge> Nudges);

/// <summary>
/// Portföy özetinden (deterministik, hazır sayılar) eğitici notlar üretir (T2.5). **Saf**
/// (yan etkisiz, %100 testli). Sayısal yargılar koddadır; LLM yorumu Faz 3'tedir. Her not
/// durumu betimler ve çerçeve sunar — somut alım-satım yönlendirmesi içermez (CLAUDE.md §2).
/// </summary>
public sealed class NudgeRuleEngine
{
    /// <summary>En büyük iki varlığın toplam ağırlığı bu eşiği aşarsa "yoğunlaşma" notu.</summary>
    internal const decimal ConcentrationTop2 = 0.60m;

    /// <summary>Tek bir varlığın ağırlığı bu eşiği aşarsa "tek varlık ağırlığı" notu.</summary>
    internal const decimal HighSingle = 0.40m;

    /// <summary>Nakit ağırlığı bu eşiğin altındaysa "nakit tamponu" notu.</summary>
    internal const decimal LowCash = 0.05m;

    public IReadOnlyList<Nudge> Evaluate(PortfolioSummaryDto summary)
    {
        var nudges = new List<Nudge>();
        if (summary.TotalValue <= 0m || summary.Allocation.Count == 0)
            return nudges;

        var byWeight = summary.Allocation.OrderByDescending(a => a.Weight).ToList();

        // 1) Yoğunlaşma — en büyük iki varlık birlikte eşiği aşıyor mu?
        if (byWeight.Count >= 2)
        {
            var top2 = byWeight[0].Weight + byWeight[1].Weight;
            if (top2 >= ConcentrationTop2)
                nudges.Add(new Nudge(
                    "concentration", "⚖️", "Yoğunlaşma",
                    $"Portföyünün {Pct(top2)} kadarı iki varlıkta toplanmış: {byWeight[0].Name} ve {byWeight[1].Name}. " +
                    "Yoğunlaşma, portföyün bu varlıkların değer değişimine duyarlılığını artırır.",
                    NudgeSeverity.Warning));
        }

        // 2) Tek varlık ağırlığı — en büyük tek varlık eşiği aşıyor mu?
        var top = byWeight[0];
        if (top.Weight >= HighSingle)
            nudges.Add(new Nudge(
                "high-single-asset", "🎯", "Tek varlık ağırlığı",
                $"{top.Name} tek başına portföyünün {Pct(top.Weight)} kadarı. " +
                "Tek bir varlığın payı yükseldikçe portföy o varlıkla daha çok birlikte hareket eder.",
                NudgeSeverity.Warning));

        // 3) Nakit tamponu — nakit ağırlığı eşiğin altında mı?
        var cashSlices = summary.Allocation.Where(a => a.AssetType == AssetType.Cash).ToList();
        if (cashSlices.Count > 0)
        {
            var cash = cashSlices.Sum(a => a.Weight);
            if (cash < LowCash)
                nudges.Add(new Nudge(
                    "low-cash", "💵", "Nakit tamponu",
                    $"Nakit, portföyünün {Pct(cash)} kadarı. Nakit; ani ihtiyaç ya da fırsatlarda likidite sağlar.",
                    NudgeSeverity.Info));
        }

        return nudges;
    }

    /// <summary>Oranı TR yüzde metnine çevirir (≥%10 tam sayı, altı 1 ondalık virgüllü): 0,6427→"%64", 0,0072→"%0,7".</summary>
    private static string Pct(decimal ratio)
    {
        var p = ratio * 100m;
        string text;
        if (p >= 10m)
        {
            text = Math.Round(p, 0, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture);
        }
        else
        {
            var r = Math.Round(p, 1, MidpointRounding.AwayFromZero);
            text = r == Math.Truncate(r)
                ? ((long)r).ToString(CultureInfo.InvariantCulture)
                : r.ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',');
        }
        return "%" + text;
    }
}

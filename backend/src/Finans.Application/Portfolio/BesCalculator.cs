using Finans.Domain.Enums;

namespace Finans.Application.Portfolio;

/// <summary>
/// BES için deterministik, saf hesaplar (CLAUDE.md §3.1, NFR-1): devlet katkısı tutarı +
/// sistemde kalış süresinden hak ediş durumu. Parametreler <see cref="BesRules"/>'da. Yan etkisiz,
/// %100 testlenebilir. Geleceği tahmin etmez; mevcut kuralı uygular (CLAUDE.md §2).
/// </summary>
public static class BesCalculator
{
    /// <summary>Kendi katkıya karşılık devlet katkısı (oran × tutar, 2 ondalık). Üst sınır T-BES planında.</summary>
    public static decimal StateContributionFor(decimal ownAmount) =>
        ownAmount <= 0m ? 0m : Math.Round(ownAmount * BesRules.StateContributionRate, 2);

    /// <summary>Sistemde tam kalış yılı (JoinedAtUtc → asOf). Tarih yoksa/gelecekse 0.</summary>
    public static int YearsInSystem(DateTime? joinedAtUtc, DateTime asOfUtc)
    {
        if (joinedAtUtc is not { } joined || joined >= asOfUtc)
            return 0;

        var years = asOfUtc.Year - joined.Year;
        if (asOfUtc < joined.AddYears(years))
            years--;
        return years;
    }

    /// <summary>
    /// Kaba hak ediş durumu (kesin yüzde DEĞİL): &lt;3 yıl NotVested · 3–10 yıl PartiallyVested ·
    /// ≥10 yıl Vested. Emeklilik için ayrıca 56 yaş gerekir (yaş verisi yok → 10 yıl proxy'si).
    /// </summary>
    public static VestingState VestingStateFor(DateTime? joinedAtUtc, DateTime asOfUtc)
    {
        var years = YearsInSystem(joinedAtUtc, asOfUtc);
        if (years >= BesRules.FullVestingYears)
            return VestingState.Vested;
        if (years >= BesRules.PartialVestingYears)
            return VestingState.PartiallyVested;
        return VestingState.NotVested;
    }
}

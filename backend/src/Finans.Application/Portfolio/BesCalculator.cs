using Finans.Domain.Enums;

namespace Finans.Application.Portfolio;

/// <summary>
/// BES için deterministik, saf hesaplar (CLAUDE.md §3.1, NFR-1): devlet katkısı tutarı +
/// sistemde kalış süresinden hak ediş durumu. Parametreler <see cref="BesRules"/>'da. Yan etkisiz,
/// %100 testlenebilir. Geleceği tahmin etmez; mevcut kuralı uygular (CLAUDE.md §2).
/// </summary>
public static class BesCalculator
{
    /// <summary>
    /// Kendi katkıya karşılık devlet katkısı (oran × tutar, 2 ondalık). Oran katkının <b>ödendiği
    /// tarihe</b> göredir (geriye dönük değil): 2026 öncesi %30, sonrası %20. Üst sınır T-BES.4'te.
    /// </summary>
    public static decimal StateContributionFor(decimal ownAmount, DateTime contributionDateUtc) =>
        ownAmount <= 0m ? 0m : Math.Round(ownAmount * BesRules.StateContributionRateOn(contributionDateUtc), 2);

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

    /// <summary>
    /// Devlet katkısının yatma tarihi (katkı ayını izleyen ayın sonu, <see cref="BesRules.StateDepositDateOn"/>).
    /// </summary>
    public static DateTime StateDepositDateFor(DateTime contributionDateUtc) =>
        BesRules.StateDepositDateOn(contributionDateUtc);

    /// <summary>
    /// Bir katkının durumu (tarihten): ödeme gelecekteyse <see cref="BesContributionStatus.Future"/>;
    /// ödeme geçti ama devlet yatma tarihi gelmediyse <see cref="BesContributionStatus.StatePending"/>;
    /// devlet yatma tarihi de geçtiyse <see cref="BesContributionStatus.Deposited"/>.
    /// </summary>
    public static BesContributionStatus ContributionStatusFor(DateTime paidAtUtc, DateTime asOfUtc)
    {
        if (paidAtUtc.Date > asOfUtc.Date)
            return BesContributionStatus.Future;
        return StateDepositDateFor(paidAtUtc).Date <= asOfUtc.Date
            ? BesContributionStatus.Deposited
            : BesContributionStatus.StatePending;
    }

    /// <summary>Doğum yılından kaba yaş (asOf yılı − doğum yılı). Yıl yoksa null.</summary>
    public static int? AgeFor(int? birthYear, DateTime asOfUtc) =>
        birthYear is { } y ? asOfUtc.Year - y : null;

    /// <summary>
    /// Kademeli hak ediş oranı (0/0.15/0.35/0.60/1.00) — sistemde kalış süresi + (opsiyonel) yaştan.
    /// </summary>
    public static decimal VestedRateFor(DateTime? joinedAtUtc, int? age, DateTime asOfUtc) =>
        BesRules.VestedRateFor(YearsInSystem(joinedAtUtc, asOfUtc), age);
}

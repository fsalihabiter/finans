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

    /// <summary>
    /// Fonda FİİLEN bulunan katkı toplamları — BES'in tek taban tanımı (GD-002).
    /// Kendi katkı: ödeme tarihi geçmişse. Devlet katkısı: <b>yatma tarihi</b> de geçmişse
    /// (<see cref="BesContributionStatus.Deposited"/>). "Yolda" olan devlet katkısı
    /// (<see cref="BesContributionStatus.StatePending"/>) henüz fonda değildir → getirisi
    /// olamaz, tabana girmez.
    ///
    /// <para>⚠ Değer, oran, bölme ve değer serisi HEPSİ bunu kullanmalı. Ayrı ayrı
    /// "PaidAtUtc ≤ bugün" yazmak devlet tabanını yoldaki parayla şişirir ve iki havuzun
    /// getirisini yapay olarak ayırır (canlı veride %39 ↔ %48 görüldü).</para>
    /// </summary>
    public static (decimal Own, decimal State) DepositedTotals(
        IEnumerable<(DateTime PaidAtUtc, decimal OwnAmount, decimal StateAmount)> contributions,
        DateTime asOfUtc)
    {
        var t = ContributionTotals(contributions, asOfUtc);
        return (t.OwnDeposited, t.StateDeposited);
    }

    /// <summary>
    /// Katkı toplamlarının TEK sınıflandırması (REVIEW-002 · RV-007) — fonda olan + bekleyen.
    /// <list type="bullet">
    ///   <item><b>Deposited:</b> kendi ve devlet katkısı fonda.</item>
    ///   <item><b>StatePending:</b> kendi katkı fonda; devlet katkısı "yolda" → HİÇBİR toplama girmez.</item>
    ///   <item><b>Future:</b> ödeme tarihi gelmedi → yalnız "bekleyen" toplamlara.</item>
    /// </list>
    /// ⚠ Detay DTO'su, değer yolu, seri ve migration hepsi bu sınıflandırmayı kullanır. Aynı
    /// kavramın iki yerde ayrı yazılması canlı veride iki havuzun getirisini %39 ↔ %48 ayırmıştı.
    /// </summary>
    public static BesContributionTotals ContributionTotals(
        IEnumerable<(DateTime PaidAtUtc, decimal OwnAmount, decimal StateAmount)> contributions,
        DateTime asOfUtc)
    {
        decimal ownDep = 0m, stateDep = 0m, ownPend = 0m, statePend = 0m;
        foreach (var (paidAt, ownAmount, stateAmount) in contributions)
        {
            switch (ContributionStatusFor(paidAt, asOfUtc))
            {
                case BesContributionStatus.Deposited:
                    ownDep += ownAmount;
                    stateDep += stateAmount;
                    break;
                case BesContributionStatus.StatePending:
                    ownDep += ownAmount; // kendi katkı fonda; devlet katkısı yolda
                    break;
                case BesContributionStatus.Future:
                    ownPend += ownAmount;
                    statePend += stateAmount;
                    break;
            }
        }
        return new BesContributionTotals(ownDep, stateDep, ownPend, statePend);
    }

    /// <summary>Doğum yılından kaba yaş (asOf yılı − doğum yılı). Yıl yoksa null.</summary>
    public static int? AgeFor(int? birthYear, DateTime asOfUtc) =>
        birthYear is { } y ? asOfUtc.Year - y : null;

    /// <summary>
    /// Kademeli hak ediş oranı (0/0.15/0.35/0.60/1.00) — sistemde kalış süresi + (opsiyonel) yaştan.
    /// </summary>
    public static decimal VestedRateFor(DateTime? joinedAtUtc, int? age, DateTime asOfUtc) =>
        BesRules.VestedRateFor(YearsInSystem(joinedAtUtc, asOfUtc), age);

    /// <summary>
    /// Yıllık devlet katkısı üst sınırını uygular (T-BES.4): önerilen <paramref name="proposedState"/>
    /// ile <paramref name="alreadyContributedInYear"/>'in (aynı takvim yılındaki diğer state katkıları)
    /// toplamı tavanı aşarsa, kalan kotaya kadar keser. Kota tükendiyse 0 döner (negatif olamaz).
    /// </summary>
    /// <param name="proposedState">Bu katkı için hesaplanan ham devlet katkısı (orana göre).</param>
    /// <param name="year">Ödeme tarihinin takvim yılı (cap yıl başına uygulanır).</param>
    /// <param name="alreadyContributedInYear">Aynı yıl içinde mevcut yatırılmış devlet katkısı toplamı.</param>
    public static decimal ApplyAnnualStateCap(decimal proposedState, int year, decimal alreadyContributedInYear)
    {
        if (proposedState <= 0m) return 0m;
        var cap = BesRules.AnnualStateContributionCapFor(year);
        var remaining = Math.Max(0m, cap - alreadyContributedInYear);
        return Math.Min(proposedState, remaining);
    }

    /// <summary>
    /// BES fon getirisi (T-BES.10): fon, hem kendi katkı hem devlet katkısı üzerinde işleyerek büyür;
    /// dolayısıyla her ikisinin AYRI kâr/zararı vardır ve ikisi de **aynı oranla** (r) yansır.
    /// <c>r = fundValue / (own+state) − 1</c>. <paramref name="fundValue"/> yoksa veya taban 0 ise
    /// oran null; değerler katkıların kendisine (kâr/zarar 0). Yuvarlama: tutarlar 2 ondalık (TRY
    /// gösterimi); oran yuvarlanmaz (oransal aritmetik kayıpsız).
    /// </summary>
    public static BesFundReturn FundReturnFor(decimal own, decimal state, decimal? fundValue)
    {
        var costBase = own + state;
        if (fundValue is not { } fv || costBase <= 0m)
            return new BesFundReturn(null, own, 0m, state, 0m);

        var r = fv / costBase - 1m;
        return new BesFundReturn(
            r,
            Math.Round(own * (1m + r), 2),
            Math.Round(own * r, 2),
            Math.Round(state * (1m + r), 2),
            Math.Round(state * r, 2));
    }

    /// <summary>
    /// BES fon getirisi — <b>İKİ AYRI HAVUZ</b> (GD-002). Devlet katkısı ayrı bir fonda
    /// değerlendirildiği için kendi katkının getirisiyle aynı olmak zorunda değildir:
    /// her havuzun oranı kendi fon değerinden çıkar
    /// (<c>rOwn = ownFund/own − 1</c>, <c>rState = stateFund/state − 1</c>).
    ///
    /// <para>Bir havuzun fon değeri girilmemişse o havuz <b>katkı tutarına eşit</b> kabul
    /// edilir (kâr/zarar 0) — eksik veri uydurulmaz. <see cref="BesFundReturn.Rate"/>
    /// birleşik orandır: <c>(ownValue+stateValue)/(own+state) − 1</c>; taban 0 ise null.</para>
    ///
    /// <para>Yuvarlama: tutarlar 2 ondalık (gösterim); oranlar yuvarlanmaz.</para>
    /// </summary>
    public static BesFundReturn FundReturnFor(
        decimal own, decimal state, decimal? ownFundValue, decimal? stateFundValue)
    {
        var ownValue = ownFundValue is { } ofv && own > 0m ? Math.Round(ofv, 2) : own;
        var stateValue = stateFundValue is { } sfv && state > 0m ? Math.Round(sfv, 2) : state;

        var ownRate = own > 0m && ownFundValue is { } o ? o / own - 1m : (decimal?)null;
        var stateRate = state > 0m && stateFundValue is { } s ? s / state - 1m : (decimal?)null;

        var costBase = own + state;
        var combined = costBase > 0m ? (ownValue + stateValue) / costBase - 1m : (decimal?)null;

        return new BesFundReturn(
            combined,
            ownValue,
            Math.Round(ownValue - own, 2),
            stateValue,
            Math.Round(stateValue - state, 2),
            ownRate,
            stateRate);
    }

    /// <summary>
    /// Tek bir TOPLAM fon değerini iki havuza <b>katkı oranında</b> böler (GD-002 geriye
    /// uyumluluk). Kullanıcı yalnız toplamı bildiğinde (eski giriş biçimi) veya eski kayıt
    /// taşınırken kullanılır; iki değer ayrı girildiğinde bu bölme YAPILMAZ.
    /// Taban 0 ise bölünemez → (null, null): veri uydurulmaz.
    /// </summary>
    public static (decimal? OwnFundValue, decimal? StateFundValue) SplitTotalFundValue(
        decimal totalFundValue, decimal own, decimal state)
    {
        var costBase = own + state;
        if (costBase <= 0m)
            return (null, null);

        var ownShare = Math.Round(totalFundValue * own / costBase, 2);
        // Kalan devlet havuzuna — iki parça toplamı girilen değere KURUŞU KURUŞUNA eşit kalır.
        return (ownShare, totalFundValue - ownShare);
    }

    /// <summary>
    /// BES pozisyonunun <b>portföy değeri</b> (GD-002, ürün sahibi kararı 2026-09-20):
    /// kendi katkının fon değeri + <paramref name="vestedRate"/> × devlet katkısının fon değeri.
    ///
    /// <para><b>Neden hak ediş oranı:</b> hak edilmemiş devlet katkısı bugün ayrılsan
    /// alamayacağın paradır; portföy değerinde tam göstermek kullanıcıyı yanıltır.
    /// Hak edildikçe değere girer (kademe: 0 / 0,15 / 0,35 / 0,60 / 1,00).</para>
    ///
    /// <para>⚠ Maliyet tabanı yalnız <b>kendi katkıdır</b> (devlet katkısı cepten çıkmadı),
    /// dolayısıyla hak edilmiş devlet katkısı getiri oranını yükseltir — bu <b>kasıtlıdır</b>:
    /// devlet katkısı gerçek bir kazançtır. Kırılım detay ekranında ayrı gösterilir
    /// (<c>CLAUDE.md</c> §1: devlet katkısı ayrı satır).</para>
    /// </summary>
    public static decimal VestedPortfolioValueFor(decimal ownValue, decimal stateValue, decimal vestedRate)
    {
        if (vestedRate < 0m || vestedRate > 1m)
            throw new ArgumentOutOfRangeException(nameof(vestedRate), vestedRate, "Hak ediş oranı 0–1 aralığında olmalı.");

        return Math.Round(ownValue + vestedRate * stateValue, 2);
    }
}

/// <summary>BES katkı toplamlarının tek sınıflandırması (<see cref="BesCalculator.ContributionTotals"/>).</summary>
public readonly record struct BesContributionTotals(
    decimal OwnDeposited,
    decimal StateDeposited,
    decimal OwnPending,
    decimal StatePending);

/// <summary>
/// BES fonun her bir katkı kalemine yansıyan getirisi (T-BES.10).
/// <see cref="OwnRate"/>/<see cref="StateRate"/> iki havuz ayrı girildiğinde dolar (GD-002);
/// tek fon değerinin orantılı bölündüğü eski yolda null kalır.
/// </summary>
public readonly record struct BesFundReturn(
    decimal? Rate,
    decimal OwnValue,
    decimal OwnProfit,
    decimal StateValue,
    decimal StateProfit,
    decimal? OwnRate = null,
    decimal? StateRate = null);

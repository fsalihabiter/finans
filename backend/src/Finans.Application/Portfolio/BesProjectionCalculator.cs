namespace Finans.Application.Portfolio;

/// <summary>
/// BES eğitici projeksiyon hesabı (T-BES.5): kullanıcının verdiği varsayımlardan (aylık katkı,
/// süre, varsayılan yıllık getiri) **varsayımsal** birikim illüstrasyonu üretir. Yatırım
/// tavsiyesi DEĞİL — yalnız "varsayımlarının sonucu şu olur" çerçevesi (CLAUDE.md §2).
/// <para>
/// Hesap deterministik (NFR-1) ve saf: aylık iterasyon, devlet katkısı oranı her katkının
/// ödeme ayındaki orana göre (2026 öncesi %30, sonrası %20 — geriye dönük değil; T-BES.4
/// üst sınırı bu MVP'de yok, ileride eklenir). Fon büyümesi aylık compound (r_m = (1+r_y)^(1/12)−1).
/// Vergi/komisyon/enflasyon dahil DEĞİL — disclaimer'da belirtilir.
/// </para>
/// </summary>
public static class BesProjectionCalculator
{
    /// <summary>Maksimum projeksiyon süresi (50 yıl) — sayısal saçma değerlerden korur.</summary>
    public const int MaxYears = 50;

    /// <summary>Maksimum yıllık getiri varsayımı (%200) — illüstrasyon olduğu için geniş ama sınırlı.</summary>
    public const decimal MaxAnnualReturn = 2.00m;

    /// <summary>
    /// Aylık iteratif simülasyon. Her ay:
    /// (a) ay başında <paramref name="ownMonthly"/> kendi katkısı yatar; o ayın orana göre
    /// devlet katkısı hesaplanır,
    /// (b) ay sonunda <c>fund_value</c> aylık getiri oranı kadar büyür (önceki bakiye + bu ayın katkıları).
    /// </summary>
    /// <remarks>
    /// Devlet katkısının yatma gecikmesi (~1 ay) bu illüstrasyonda **göz ardı edilir** — kullanıcının
    /// "varsayımlarının sonucunu görme" amacında ayrıntı; ileride istenirse opsiyon olarak eklenir.
    /// </remarks>
    public static BesProjectionResult Project(BesProjectionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.OwnMonthly < 0m)
            throw new ArgumentException("Aylık katkı negatif olamaz.", nameof(input));
        if (input.Years <= 0 || input.Years > MaxYears)
            throw new ArgumentException($"Süre 1-{MaxYears} yıl arasında olmalı.", nameof(input));
        if (input.AnnualReturnRatio < -0.99m || input.AnnualReturnRatio > MaxAnnualReturn)
            throw new ArgumentException("Yıllık getiri varsayımı makul aralıkta olmalı.", nameof(input));

        var n = input.Years * 12;
        // r_m: yıllık → aylık compound dönüşüm. decimal'de Math.Pow yok → double'a düş, geri al.
        // Yaklaşıklık ~15 ondalık (double) — illüstrasyon için fazlasıyla yeterli; gösterimde 2 ondalık.
        var rYearD = (double)input.AnnualReturnRatio;
        var rMonthD = Math.Pow(1.0 + rYearD, 1.0 / 12.0) - 1.0;
        var rMonth = (decimal)rMonthD;

        decimal own = 0m, state = 0m, fund = 0m;
        var yearly = new List<BesProjectionYear>(input.Years);

        var startDate = input.StartDate.Date == default
            ? DateTime.UtcNow.Date
            : input.StartDate.Date;

        for (int month = 1; month <= n; month++)
        {
            var contribDate = startDate.AddMonths(month - 1);
            var stateRate = BesRules.StateContributionRateOn(contribDate);
            var stateThis = Math.Round(input.OwnMonthly * stateRate, 2);

            own += input.OwnMonthly;
            state += stateThis;
            // Ay sonu compound: önceki bakiye + bu ayın katkıları (own + state) hep birlikte büyür.
            // (Devlet katkısının ~1 ay gecikmeli yatması bu modelde göz ardı edilir; disclaimer'da belirtilir.)
            fund = (fund + input.OwnMonthly + stateThis) * (1m + rMonth);

            if (month % 12 == 0)
            {
                var yearNum = month / 12;
                var ownVal = TotalShare(fund, own, state, isOwn: true);
                var stateVal = TotalShare(fund, own, state, isOwn: false);
                yearly.Add(new BesProjectionYear(
                    yearNum,
                    Math.Round(own, 2),
                    Math.Round(state, 2),
                    Math.Round(fund, 2),
                    Math.Round(ownVal, 2),
                    Math.Round(stateVal, 2),
                    Math.Round(ownVal - own, 2),
                    Math.Round(stateVal - state, 2)));
            }
        }

        var finalOwnValue = TotalShare(fund, own, state, isOwn: true);
        var finalStateValue = TotalShare(fund, own, state, isOwn: false);

        return new BesProjectionResult(
            Math.Round(own, 2),
            Math.Round(state, 2),
            Math.Round(fund, 2),
            Math.Round(finalOwnValue, 2),
            Math.Round(finalStateValue, 2),
            Math.Round(finalOwnValue - own, 2),
            Math.Round(finalStateValue - state, 2),
            input.AnnualReturnRatio,
            yearly);
    }

    /// <summary>
    /// Fon değerini own/state tabanlarına oransal böler (her ikisi de aynı r ile büyüdüğünden
    /// payları tabandaki orana eşittir). Taban 0 ise dilim 0 — bölme yok.
    /// </summary>
    private static decimal TotalShare(decimal fund, decimal own, decimal state, bool isOwn)
    {
        var costBase = own + state;
        if (costBase <= 0m) return 0m;
        var share = isOwn ? own : state;
        return fund * share / costBase;
    }
}

/// <summary>BES projeksiyon girdileri — kullanıcı varsayımları (T-BES.5).</summary>
public sealed record BesProjectionInput(
    decimal OwnMonthly,
    int Years,
    decimal AnnualReturnRatio,
    DateTime StartDate);

/// <summary>BES projeksiyon sonucu — varsayımsal birikim illüstrasyonu (T-BES.5).</summary>
public sealed record BesProjectionResult(
    /// <summary>Süre sonunda yatırılan toplam kendi katkı (cebinden).</summary>
    decimal TotalOwnContribution,
    /// <summary>Süre sonunda yatırılan toplam devlet katkısı.</summary>
    decimal TotalStateContribution,
    /// <summary>Süre sonunda fon değeri (own+state birikim × getiri).</summary>
    decimal FundValue,
    /// <summary>Kendi katkının fon getirisiyle birlikte güncel değeri.</summary>
    decimal OwnValue,
    /// <summary>Devlet katkısının fon getirisiyle birlikte güncel değeri.</summary>
    decimal StateValue,
    /// <summary>Kendi katkının fon getiri kâr/zararı.</summary>
    decimal OwnProfit,
    /// <summary>Devlet katkısının fon getiri kâr/zararı.</summary>
    decimal StateProfit,
    decimal AnnualReturnRatio,
    IReadOnlyList<BesProjectionYear> Yearly);

/// <summary>Her yıl sonundaki birikim/değer durumu (büyüme eğrisi için).</summary>
public sealed record BesProjectionYear(
    int Year,
    decimal OwnContribution,
    decimal StateContribution,
    decimal FundValue,
    decimal OwnValue,
    decimal StateValue,
    decimal OwnProfit,
    decimal StateProfit);

namespace Finans.Application.Portfolio;

/// <summary>
/// BES (Bireysel Emeklilik) devlet katkısı + hak ediş kuralları — <b>tek kaynak</b> (03 §A).
/// Sayısal yargı KODDA, parametreler burada (CLAUDE.md §3.1). <b>UYARI:</b> bu oranlar yıllık
/// değişir ve mevzuata tabidir; <b>lansman öncesi EGM/SPK ile doğrulanmalı</b> (CLAUDE.md §2,§10).
///
/// <para>Kaynak (2026): devlet katkısı oranı 2026-01-01'den itibaren <b>%20</b> (önceki %30;
/// Resmî Gazete 2026-01-07). Üst sınır = yıllık brüt asgari ücretin %20'si. Hak ediş kademeleri
/// sistemde kalış süresine bağlıdır (3/6/10 yıl); kesin hak ediş yüzdeleri kaynaklarda farklılık
/// gösterdiğinden burada yalnız <b>kaba durum</b> (NotVested/PartiallyVested/Vested) türetilir,
/// kesin yüzde gösterilmez.</para>
/// </summary>
public static class BesRules
{
    /// <summary>Devlet katkısı oranının %20'ye düştüğü tarih (RG 2026-01-07; 2026-01-01'den geçerli).</summary>
    public static readonly DateTime Rate20EffectiveUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>2026 öncesi devlet katkısı oranı.</summary>
    public const decimal StateRateBefore2026 = 0.30m;

    /// <summary>2026-01-01'den itibaren devlet katkısı oranı.</summary>
    public const decimal StateRateFrom2026 = 0.20m;

    /// <summary>
    /// Devlet katkısı oranı, katkının <b>ödendiği tarihe</b> göredir (oran <b>geriye dönük DEĞİL</b>):
    /// 2026-01-01 öncesi ödenen katkılar %30, o tarihten itibaren %20.
    /// </summary>
    public static decimal StateContributionRateOn(DateTime contributionDateUtc) =>
        contributionDateUtc < Rate20EffectiveUtc ? StateRateBefore2026 : StateRateFrom2026;

    /// <summary>
    /// Yıllık devlet katkısı üst sınırı (2026 ≈ 79.272 ₺ = yıllık brüt asgari ücret × %20).
    /// Yıllık güncellenir. <b>Not:</b> takvim-yılı bazında uygulanır; tam uygulanması için yıl-bazlı
    /// katkı toplaması gerekir (henüz model tutmuyor → şimdilik bilgi amaçlı, T-BES planında).
    /// </summary>
    public const decimal AnnualStateContributionCap2026 = 79_272m;

    /// <summary>Kısmi hak ediş eşiği (yıl): bu süreden önce sistemden çıkışta devlet katkısı alınmaz.</summary>
    public const int PartialVestingYears = 3;

    /// <summary>Tam hak ediş eşiği (yıl): emeklilik (10 yıl + 56 yaş) yaklaşımı; yaş verisi yoksa 10 yıl proxy'si.</summary>
    public const int FullVestingYears = 10;

    // ── Kademeli hak ediş oranları (EGM): devlet katkısı + getirisinin hak edilen kısmı ──
    // Sistemde kalış süresine göre. Kaynak: EGM (egm.org.tr/bireysel-emeklilik/devlet-katkisi).
    // UYARI: mevzuata tabidir, lansman öncesi EGM/SPK doğrulaması şart (CLAUDE.md §2).

    /// <summary>3 yıldan az: hak edilmez.</summary>
    public const decimal VestedRateUnder3Years = 0.00m;

    /// <summary>3–6 yıl arası: %15.</summary>
    public const decimal VestedRate3to6Years = 0.15m;

    /// <summary>6–10 yıl arası: %35.</summary>
    public const decimal VestedRate6to10Years = 0.35m;

    /// <summary>10 yıl ve üzeri (emeklilik yaşı dolmadan): %60.</summary>
    public const decimal VestedRate10PlusYears = 0.60m;

    /// <summary>10 yıl + emeklilik yaşı (56) ya da emeklilik/vefat/maluliyet: %100.</summary>
    public const decimal VestedRateFull = 1.00m;

    /// <summary>Emeklilik yaşı (tam hak ediş için 10 yıl ile birlikte gerekir).</summary>
    public const int RetirementAge = 56;

    /// <summary>
    /// Devlet katkısının fiilen hesaba yatma tarihi: katkının nakden ulaştığı ayı <b>izleyen ayın
    /// son günü</b> (EGM kuralı; kart ödemelerinde pratikte daha geç olabilir — model basitleştirir).
    /// Örn. 01.05 katkı → 30.06'da yatar.
    /// </summary>
    public static DateTime StateDepositDateOn(DateTime contributionDateUtc)
    {
        var d = contributionDateUtc.Date;
        // Ödeme ayının ilk günü → 2 ay ekle → 1 gün geri = izleyen ayın son günü.
        var firstOfMonth = new DateTime(d.Year, d.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return firstOfMonth.AddMonths(2).AddDays(-1);
    }

    /// <summary>
    /// Sistemde kalış yılı + (opsiyonel) yaşa göre kademeli hak ediş oranı (0/0.15/0.35/0.60/1.00).
    /// 10 yıl dolmuş VE yaş ≥ 56 ise %100; yaş bilinmiyorsa %60'ta kalır (kullanıcı yaş girince %100).
    /// </summary>
    public static decimal VestedRateFor(int yearsInSystem, int? age)
    {
        if (yearsInSystem < PartialVestingYears) return VestedRateUnder3Years;
        if (yearsInSystem < 6) return VestedRate3to6Years;
        if (yearsInSystem < FullVestingYears) return VestedRate6to10Years;
        // ≥ 10 yıl
        if (age is { } a && a >= RetirementAge) return VestedRateFull;
        return VestedRate10PlusYears;
    }
}

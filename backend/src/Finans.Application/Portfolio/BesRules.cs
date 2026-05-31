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
    /// <summary>Devlet katkısı oranı (2026: %20). Kendi katkının bu kadarı devlet katkısı olarak eklenir.</summary>
    public const decimal StateContributionRate = 0.20m;

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
}

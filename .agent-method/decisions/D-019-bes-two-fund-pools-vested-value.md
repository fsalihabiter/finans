---
id: D-019
title: BES iki ayrı fon havuzudur; portföy değerine devlet katkısının yalnız hak edilmiş kısmı girer
status: accepted
scope: architecture
created: 2026-09-20
supersedes: null
superseded_by: null
---

# D-019 — BES: iki fon havuzu, hak edişe göre değer

## Context
Eski model BES için **tek** fon değeri tutuyordu (`Holdings.CurrentPrice` = kendi +
devlet toplamının piyasa değeri) ve iki havuzu okuma anında **orantılı** bölüyordu —
yani iki havuzun getirisini eşit varsayıyordu. Türkiye'de devlet katkısı **ayrı bir
fonda** işletilir; getirisi kendi katkınınkiyle aynı olmak zorunda değildir.
Ayrıca değer, **hak edilmemiş** devlet katkısını da içeriyordu: kullanıcının bugün
ayrılsa alamayacağı para portföy toplamında sayılıyordu. (Ürün sahibi kararı 2026-09-20.)

## Decision
- BES fon değeri **iki alan**dır: `BesDetails.OwnFundValue` (kendi katkıların fondaki
  değeri) ve `BesDetails.StateFundValue` (devlet katkısının fondaki değeri). Her havuzun
  getirisi kendi fonundan çıkar.
- BES'in portföy değeri = **kendi fon değeri + hak ediş oranı × devlet fon değeri**
  (`BesCalculator.VestedPortfolioValueFor`; hak ediş `BesCalculator.VestedRateFor`
  kademeleriyle: 0 · 0,15 · 0,35 · 0,60 · 1,00).
- Kural **tek yerde** türetilir (`HoldingMapping.ApplyReadPosition`) ve liste, özet,
  detay, değer serisi, senaryo aynı değeri gösterir. Değer serisinde hak ediş oranı
  **her noktada o günkü** değeriyle uygulanır.
- Fon değeri girilmemiş havuz **katkı tutarına eşit** sayılır (veri uydurulmaz).
- Kullanıcı yalnız toplamı bilirse (eski giriş) toplam **katkı oranında** bölünür
  (`SplitTotalFundValue`); girilen değer asla yok sayılmaz.
- Maliyet tabanı yine **yalnız kendi katkı**dır.

## Consequences
- Hak edilmemiş devlet katkısı olan kullanıcıların toplamı **düşer** (tohum: 839.213 →
  785.513). Bu kasıtlıdır ve detay ekranında gerekçesiyle gösterilir.
- Hak edilmiş devlet katkısı maliyette olmadığı için getiri oranını yükseltir — kasıtlı:
  devlet katkısı gerçek bir kazançtır; kırılım detayda ayrı satırdır (CLAUDE.md §1).
- Hak ediş zamanla arttığı için BES değeri **fon değeri değişmese bile** kademe
  geçişlerinde sıçrar (örn. 3. yıl dolunca %0 → %15). Kullanıcıya açıklanmalı.
- Migration `BesSeparateFundValues` eski tek değeri bir kez kalıcı olarak böler.

## Keywords (for Doctor drift detection)
- OwnFundValue
- StateFundValue
- VestedPortfolioValueFor
- SplitTotalFundValue
- ApplyReadPosition

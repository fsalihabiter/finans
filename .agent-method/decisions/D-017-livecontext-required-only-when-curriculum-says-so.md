---
id: D-017
title: LiveContext bloğu yalnız künyede tanımlıysa zorunludur (müfredat kanonik)
status: accepted
scope: architecture
created: 2026-09-20
supersedes: null
superseded_by: null
---

# D-017 — "Senin portföyünde" bloğunun zorunluluğu künyeden gelir

## Context
`EducationSeedTests` derinlik merdiveni testi **her dersin** bir
`SectionKind.LiveContext` bloğu taşımasını şart koşuyor
(`EducationSeedTests.cs:205`, T6.2/SC-E13'ten kalma). Ancak müfredat
(`16-CURRICULUM.md`) bazı dersler için **bilinçli olarak** "LiveContext — yok
*(karşılığı bir metrik yok)*" diyor; ilk örnek S0-L7 "Risk ne demek?".

Bu ders risk kavramını tanıtıyor; mevcut 9 bağlam token'ının
(`concentration_top2`, `return_ratio`, `real_return`, `cash_weight`,
`stock_weight`, `holding_count`, `asset_class_count`, `total_value`,
`bes_state_share`) hiçbiri dersin kavramına dürüstçe karşılık gelmiyor.

## Decision
Kural tersine çevrilir: **LiveContext bloğu, künyede tanımlandığı derslerde
zorunludur; künye "yok" diyorsa bloksuzluk sözleşmeye uygundur.** Test, künyeyle
uyumlu bir istisna listesi (ders slug'ı + gerekçe) taşır ve listedeki derste
bloğun **bulunmamasını** doğrular — yani istisna sessiz bir boşluk değil, test
edilen bir beyandır.

Reddedilen alternatifler:
- **Mevcut bir token'ı derse uydurmak:** yoğunlaşma/getiri metriğini risk
  dersine iliştirmek kavramı S0-L8/S1'den önce hazırlıksız açar ve künyenin
  açık kararını çiğner.
- **Yeni bir risk metriği türetmek (oynaklık):** fiyat geçmişi plumbing'i
  gerektirir; dersin kapsamını büyütür. İleride istenirse ayrı iş olarak açılır.

## Consequences
- İçerik sözleşmesi **müfredattan** türer, testten değil; künye kanonik kaynaktır.
- Bir derse sonradan uygun bir metrik gelirse istisna listesinden çıkarılır ve
  blok eklenir — test bunu zorunlu kılar.
- İstisna listesi büyürse bu bir uyarı sinyalidir: "Senin portföyünde" vaadi
  (`14` §4-A1) aşınıyor demektir; liste gözden geçirilir.
- `LessonContextService` ve `ContextKeys` değişmez; yalnız test kuralı değişir.

## Keywords (for Doctor drift detection)
- LiveContext
- ContextKeys
- EducationSeedTests
- depth-ladder
- 16-CURRICULUM

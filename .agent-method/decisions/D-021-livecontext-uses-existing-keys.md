---
id: D-021
title: Ders künyesi mevcut LiveContext anahtarlarıyla yazılır; yeni anahtar ayrı iş gerektirir
status: accepted
scope: architecture
created: 2026-11-22
supersedes: null
superseded_by: null
---

# D-021 — Künye LiveContext anahtarları mevcut `ContextKeys` kümesinden seçilir

## Context
S0-L8 "Vade, hedef ve portföy" künyesi (`16-CURRICULUM.md` §S0-L8) LiveContext
bloğu için `asset_class_weights` anahtarını istiyordu. Böyle bir anahtar kodda
yok: `ContextKeys` (`backend/src/Finans.Application/Education/LessonContext.cs`)
dokuz anahtar tanımlıyor — `holding_count`, `asset_class_count`, `total_value`,
`cash_weight`, `stock_weight`, `concentration_top2`, `return_ratio`,
`real_return`, `bes_state_share`.

Künye kanonik kaynaktır (D-017), ama kanonik olması anahtarın var olmasını
sağlamaz: çözülemeyen `{{token}}` satırı render'da sessizce düşer. Yani künyeye
yazılan her anahtar ya kodda vardır ya da yeni bir metrik işi demektir —
sınıf başına ağırlık dağılımı (`asset_class_weights`) mevcut projeksiyonda
hesaplanmıyor.

## Decision
**Ders künyesi LiveContext bloğunu mevcut `ContextKeys` kümesinden seçer.**
Künye var olmayan bir anahtar istiyorsa, dersi yazarken varsayılan yol anahtarı
üretmek değil, **künyeyi mevcut anahtarlarla düzeltmektir.**

S0-L8'de uygulanışı: `asset_class_weights` yerine `asset_class_count` +
`cash_weight` + `stock_weight` (künye satırı 2026-11-14'te düzeltildi).

Yeni bir anahtar gerçekten gerekiyorsa **kendi işidir**: `LessonContextService`
hesabı + `ContextKeys` kaydı + testi + künye güncellemesi birlikte gelir ve
POOL'a kalem olarak açılır. Ders yazımının içine sıkıştırılmaz.

Reddedilen alternatifler:
- **Anahtarı ders yazarken eklemek:** dersin kapsamını hesap işine büyütür;
  parasal/oransal bir metrik testsiz girer (D-015 yeşil kapısına aykırı).
- **Künyeye dokunmamak, token'ı olduğu gibi bırakmak:** satır render'da sessizce
  düşer — kullanıcı eksik bir "Senin portföyünde" bloğu görür, kimse farkı
  anlamaz. Sessiz kayıp, görünür hatadan kötüdür.

## Consequences
- Ders yazımı **deterministik** kalır: künye ↔ `ContextKeys` birebir denetlenir
  (`finans-lesson` kontrol listesi bunu madde olarak taşır).
- Künye, kanonik olmasına rağmen **tek yönlü bağlayıcı değildir**: içerik
  kararlarında kanoniktir, var olmayan bir koda sözleşme dayatamaz. D-017 ile
  birlikte okunur.
- Yeni metrik istekleri POOL'da görünür hale gelir; "ders yazarken eklendi"
  türü kayıtsız genişleme olmaz.

## Keywords (for Doctor drift detection)
- LiveContext
- ContextKeys
- asset_class_weights
- LessonContextService
- 16-CURRICULUM

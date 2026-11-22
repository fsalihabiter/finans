---
id: D-023
title: Kaynak bloklarında düzenleyici, borsa ve takas kuruluşu adları izinlidir
status: accepted
scope: architecture
created: 2026-11-22
supersedes: null
superseded_by: null
---

# D-023 — Kaynak bloklarında düzenleyici, borsa ve takas kuruluşu adları izinlidir

## Context

D-002 ve `CLAUDE.md` §2 gereği eğitim içeriği yatırım tavsiyesi vermez; bunun bir
uzantısı olarak **özel ad yasağı** vardır. Kural `16-CURRICULUM.md` §9.1'de **M7b**
olarak yazılıydı: *"metinde geçebilen tek özel ad geniş endeks adlarıdır; tek şirket ·
fon · aracı kurum adı hâlâ yasak"* — ve M7 taraması **`Source` bloklarını dahil**
ediyordu.

Ne var ki aynı dokümandaki **S0-L9 ve S0-L10 künyeleri**, dersin dayandığı kurumsal
kaynakların adlarını **açıkça istiyordu**: SPK · Borsa İstanbul · Takasbank. İki kural
aynı dosyada birbirini çürütüyordu.

Çelişki **RV-061** (REVIEW-005, S0-L10 uyum incelemesi) ile ortaya çıktı. İçerik künyeye
uygundu, yani bir içerik hatası değildi — **kural metninde bir boşluktu.** AGENTS.md §4.2
gereği sessizce düzeltilmedi, bildirildi; ürün sahibi kararıyla kapatıldı.

Bu bir yazım tercihi değil, bir **teslim sorunu**: adsız kaynak gösterimi diye bir şey
yoktur. İ2 kuralı ("kaynak iddiayı gerçekten destekliyor mu") her ⟨doğrulanacak⟩ işareti
kalkmadan dersin yayımlanmamasını şart koşuyor — ama kaynağı adlandıramıyorsan o kontrol
hiçbir zaman yapılamaz.

## Decision

`Source` bloklarında **düzenleyici · borsa · takas/saklama kuruluşu** adları, yalnızca
**kaynak gösterimi** amacıyla **izinlidir.**

Gerekçe: bu üç tür kurum kullanıcının **seçtiği bir hizmet sağlayıcı değildir** — ikisi
piyasanın altyapısı, biri denetleyicisidir. Kullanıcı "hangi borsayı kullanayım" diye bir
karar vermez; dolayısıyla adlarının geçmesi **reklam ya da yönlendirme riski taşımaz.**
Yasağın koruduğu şey, kullanıcıyı bir sağlayıcıya yönlendirme ihtimalidir ve burada o
ihtimal yoktur.

**Muafiyetin dışında kalanlar — yasak aynen sürüyor, `Source` bloğunda da:**

- **aracı kurum adı** (kullanıcının seçtiği sağlayıcı — yasağın asıl hedefi)
- **fon adı**
- **tek şirket adı**

Ayrıca **tarife ve kurum karşılaştırması** her yerde yasaktır; bu karar yalnızca **adı**
serbest bırakır, kurumun ürününü, fiyatını ya da başka bir kurumla kıyasını değil.

Kayıt yeri: `16-CURRICULUM.md` §9.1 M7b (muafiyet notu) ve §12 **K3a** satırı.
Geniş endeks adlarına ilişkin K3 kararı değişmedi; bu karar onun yanına eklenir.

## Consequences

- S0-L9 ve S0-L10 künyelerinin istediği kaynak gösterimi **kurala uygun** hâle geldi;
  bu iki ders M7b kodlandığında kırmızıya düşmeyecek.
- **M7b kodlanırken muafiyet de kodlanmalı**: tarama `SectionKind.Source` bloklarını bu
  üç kurum türü için ayrı değerlendirmeli. Bugünkü makine kapısı (RV-062,
  `Brokerage_naming_and_fictional_rate_labels_are_machine_checked`) bunu **ünvan
  kalıbıyla** yapıyor: "Menkul Değerler", "Yatırım Menkul", "A.Ş." gibi taşıyıcılar
  yasak; ders kurumları ünvansız andığı için muafiyet kendiliğinden işliyor.
  Bu bir **yaklaşımdır**, tam bir liste değil — gerçek beyaz liste M7b ile gelecek.
- ⚠ **Lansman öncesi hukuk incelemesinde teyit edilecek** (`14` §6). K3 için zaten
  duran bu çekince bu karar için de geçerlidir: muafiyetin gerekçesi hukuki değil,
  ürün mantığıdır ve bir avukat farklı düşünebilir.
- Yeni bir kurum türü (ör. saklama bankası, veri sağlayıcı) muafiyete girecekse
  **bu karar güncellenir**, künye tek başına genişletilemez — iki kanonik kaynak
  olmasın diye (INV-12).

## Keywords (for Doctor drift detection)
- M7b
- M7a
- SectionKind.Source
- Brokerage_naming_and_fictional_rate_labels_are_machine_checked
- 16-CURRICULUM.md §12-K3a
- D-002
- RV-061

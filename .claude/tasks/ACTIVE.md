# ACTIVE — Aktif Görevler (anlık durum)

> Şu an üzerinde çalışılan / sıradaki görevlerin küçük anlık görüntüsü. Oturum
> başında hook bunu otomatik gösterir. Kaynak plan: [`../docs/08-BACKLOG.md`](../docs/08-BACKLOG.md).
> Bir görev bitince buradan çıkar, backlog'da `[x]` işaretle, TASKLOG'a girdi ekle.

**Aktif faz:** ✅ **Faz 0-5 TAMAM** (Faz 5 kapandı 2026-07-12: Değer Seyri + Senaryo v1 canlı) →
🚧 **Dalga 1 finali: Faz 6** (Eğitim MVP + kavram sözlüğü — vizyonun kalbi)

**Strateji (2026-07-11):** [`14-PRODUCT-STRATEGY.md`](../docs/14-PRODUCT-STRATEGY.md) —
finansal okuryazarlık vizyonu fazlara işlendi (Faz 5-8 = Dalga 1-3, backlog'da kırılımlı).
Konumlandırma: *"Nirengi sana ne alacağını söylemez; haritayı okumayı öğretir."*

## 🔵 YÖN DEĞİŞİKLİĞİ (2026-07-21) — müfredat sıfır bilgiden başlıyor
Ürün sahibi tespiti: **ilk basamak fazla yüksekti.** "Enflasyon ve Reel Getiri"
dersi; yatırımın ne olduğunu, varlık türlerini ve getirinin nereden geldiğini
bilinen sayıyordu (`CLAUDE.md` §1 sıfır bilgi ilkesiyle çelişki).
→ **Set 0 "İlk Adımlar" (8 ders)** eklendi; eski "Temeller" → **"Yatırım Kavramları"**.
Anlatım çıtası yükseldi: **6-10 figür/ders**, **çok panelli anlatı figürleri**,
**set başına 1 etkileşimli araç**. Kararlar: [`15` §6, §6.1-6.3, §9.1](../docs/15-EDUCATION-PLAN.md).
⚠ Set 1, Set 0'a **kilitlenmez** — yönlendirme öneri rozetiyle (tavan kapatılmaz).

## 📘 MÜFREDAT HAZIR (2026-07-22) — [`16-CURRICULUM.md`](../docs/16-CURRICULUM.md)
Ders yazmadan önce **künye oradan okunur.** 25 dersin tamamı ders bazında:
öğrenme çıktıları (Bloom fiilleriyle) · **64 kavramlık harita** (her kavramın tek
tanıtım dersi) · aşama planları · figür planları · 9 soruluk test tasarımı ·
**kaynak ve doğruluk politikası** · yapısal sözleşme (M1-M9 makine, İ1-İ5 insan).
⚠ **Bulgu:** `SectionKind.Source` enum'da var, seed'de **hiç kullanılmamış** →
T6.19 devreye alıyor. `14` §B1 zaten "kaynak daima görünür" diyordu.

> ✅ **T6.8 bitti (2026-07-22):** `MiniMarkdown` artık **bağlantı + tablo** işliyor.
> **Şema beyaz-listesi** (`safeHref`): `https/http/mailto` + uygulama içi `/yol`;
> güvensiz hedef (`javascript:`, `data:`, `vbscript:`, protokole göreli `//host`,
> **kontrol karakteriyle gizlenmiş şema**) **bağlantıya çevrilmez** — ham metin kalır.
> Dış bağlantı `rel="noopener noreferrer"`. Tablo **yalnız hizalama satırı varsa**
> tablodur (yoksa düz paragraf — geriye dönük), sütun sayısı başlıktan gelir,
> kendi kabında yatay kaydırılır. SC-E24; web **130/130**.
> ⚠ Tarayıcı doğrulaması yapılamadı (Chrome sekmesi localhost'u açamadı) — görsel
> teyit **T6.19'da** gerçek kaynak bloklarıyla yapılacak.

> ✅ **T6.19 bitti (2026-07-22):** her ders artık **açılış bloğuyla başlıyor**
> ("Bu derste ne öğreneceksin?" — öğrenme çıktıları) ve **kaynak bloğuyla kapanıyor**
> ("Bu bilgiler nereden geliyor?"). Kullanılmayan `SectionKind.Source` nihayet devrede.
> Kaynak bloğu dördü birden söyler: **kurum** (TÜİK/KAP/SPK, tıklanabilir bağlantı),
> **örnek sayıların kurgusal olduğu beyanı**, hesapların kodda yapıldığı, ve
> **"yatırım tavsiyesi değildir"** çerçevesi. 5 derse 10 blok → **44→54 bölüm**,
> mutabakatla canlı Postgres'e indi. SC-E21; Education 16/16, web 130/130;
> **tarayıcıda ekran görüntüsüyle teyit** (T6.8'in ertelenen görsel doğrulaması da
> böylece kapandı — `tuik.gov.tr` bağlantısı canlıda `rel="noopener noreferrer"` ile).
> ⚠ İki hata yakalandı: Ders 2'de açılış bloğu atlanmıştı (sabit bölüm sayısı testi
> yakaladı); kaynak bloğunun **görsel ayrımı yoktu** (canlıda görüldü → kesikli çizgi).
> 🔴 **Yeni bulgu → T6.21:** `InflationRate` seed'i `Source="TÜİK"` etiketiyle
> **sabit %38** yazıyor. Eğitim artık "kaynak daima görünür" iddiası taşıdığı için
> TÜİK etiketli uydurma oran bu iddiayı çürütür.

## Sıradaki (öncelik sırası)
1. 🔴 **T6.20 — Yapısal sözleşme testleri:** `16` §9.1 M3-M9 → `EducationSeedTests`
   (M1/M2 T6.19 ile yeşil). Figür eşiği · **figür anahtarı ↔ `LessonFigure` kayıt
   defteri mutabakatı** · 9 soru/3 zorluk · boşta kavram yok.
2. 🔴 **T6.21 — Enflasyon verisi:** TÜİK etiketli sabit %38 ya gerçek veriyle
   değişmeli ya da açıkça "örnek" etiketlenmeli (güven kritik).
3. **T6.15 — Çok set desteği (web):** `EducationPage` `tracks.data[0]` ile **tek
   track** varsayıyor → set listesi + set başına ilerleme + "Buradan başla" rozeti.
4. **T6.16 — Set 0 iskeleti:** "İlk Adımlar" track'i + 8 ders + ön-koşul zinciri +
   "Temeller" → **"Yatırım Kavramları"** (slug korunur).
5. **T6.17a-h — Set 0 içerik turu (ders ders):** `16` §5 künyelerine göre.
   ⚠ İş yükü: **~62 figür + 72 soru** → önce `16` §8.3 paylaşılan SVG öğeleri.
6. **T6.18** — Enflasyon kaydırıcısı (Set 0 Ders 4)
7. **T6.11c** — Set 1 Ders 3-5 (`16` S1-L3/L4/L5 künyeleri hazır)
8. **T5E.4b** — Kavram derin bağlantı: `ConceptTag` → ilgili ders
9. **T6.9** — `UserConceptMastery` + aralıklı tekrar
10. **T6.3** — Kavram sözlüğü *(omurgası `16` §3 haritası)*
11. OSS kalanı — README ekran görüntüleri tazeleme

> ✅ **T6.11b bitti (2026-07-19):** **Ders 2 (Çeşitlendirme) 6 → 13 aşama** — TR'ye özgü
> **para birimi ekseni** ("dört kalem, tek eksen"), korelasyon sezgisi, kriz anında
> korelasyon artışı, **ağırlık kayması** (işlem yapmadan yoğunlaşma). 3 yeni figür (toplam 12),
> test 3 → 9 soru. **Yol haritası sütunu** `fit-content(35%)`: başlık tek satıra sığana kadar
> genişler, %35'i geçmez (canlıda ~%19). App 291/291, web 117/117.

> ✅ **T6.11a bitti (2026-07-19):** **Ders 1 zenginleştirildi — 6 → 13 aşama**, her kavramın
> ardından işlenmiş örnek (ekmek/alım gücü · kişisel sepet · dönem penceresi), **5 figür**,
> **zorluk kademeli test** (3 → 9 soru; seviyeye göre hem gösterim hem PUANLAMA filtreli).
> **Satır genişliği düzeldi:** iki sütun (solda yapışkan yol haritası). SC-E18; web 117/117.
> ⚠ Tarayıcıda 3 hata yakalandı: uzun başlık taşması · çok satırlı liste öğesinin ikiye
> bölünmesi (MiniMarkdown) · desteklenmeyen italik. **Sıradaki: Ders 2 aynı kalıpla.**

> ✅ **T6.10 bitti (2026-07-19):** ders okuyucusu **aşamalı gezgin** oldu — tek adım/ekran,
> segmentli ilerleme, **yol haritası** (ilerideki başlıklar okunur ama **kilitli**; tür/derinlik
> rozetleriyle "ne geliyor" sezdirilir), **mini test AYRI ve SON adım** (yönlendirmeyle ulaşılır,
> sayfanın devamı değil). Bölüm başlığı gövdeden ayrıştırılıp `Heading`'e taşındı.
> SC-E17; web 116/116; canlıda ekran görüntüsüyle teyit. ⚠ Seed mutabakatı `Heading`'i
> atlıyordu (aynı sınıf hatanın 4.'sü) → alan listesi eksiksizleştirildi + test.

> ✅ **T6.7 bitti (2026-07-19):** seviyeye göre katlama canlı — üst katmanlar `<details>`
> içinde ("Daha derine in"/"Uzman katmanı"), **tavan kapatılmaz**, profil yoksa Başlangıç.
> **Dersler görselleştirildi:** `FigureKey` + 5 elle yazılmış SVG (kütüphanesiz, tema uyumlu,
> erişilebilir); tuzak bloğu Core katmana alındı. SC-E16; App 291/291, web 114/114;
> **tarayıcıda ekran görüntüsüyle teyit**. ⚠ İki hata tarayıcıda yakalandı (testler yeşilken):
> MiniMarkdown çok satırlı alıntıyı 4 kutuya bölüyordu (birleştirildi + regresyon testi);
> figür ortalanınca metinden kopuktu (sola hizalandı). ⚠ **Seed artık MUTABAKAT yapıyor** —
> içerik düzeltmeleri çalışan DB'ye iner (aynı sınıf tuzağın üçüncü turu).

> ✅ **T6.6 + öğrenme kapısı bitti (2026-07-19):** tanılama testi canlı (8 soru, atlanabilir,
> onboarding'de) → `LiteracyLevel`. 🔒 `RiskAttitude` DB'de ama **hiçbir yanıtta/arayüzde yok**.
> **Ders artık testi geçmeden tamamlanamaz** (sunucuda 400 `quiz_not_passed`); testi geçmek
> dersi tamamlar ve yalnız bir sonrakini açar. **Seed hiç ilerleme yazmıyor** — herkes sıfırdan.
> Canlı ilerleme + denemeler **sıfırlandı** (portföy verisine dokunulmadı). SC-E14/E15;
> App 291/291, web 107/107. ⚠ Web doğrulaması artık `npm run build` ile (`tsc --noEmit` yetersiz).

> ✅ **T6.2 bitti (2026-07-19):** "Senin portföyünde" canlı — `{{token}}` şablonları KODDA
> hesaplanan metriklerle çözülüyor (9 anahtar); **3 durum** `Own`/`Demo`/`Stale`; çözülemeyen
> token'ın satırı düşer. **Web artık bölümleri render ediyor** (T6.1 içeriği görünür oldu) +
> bağlam rozeti. **İlerleme akışı:** ders tamamlanınca sonraki kilit açılır, "Sonraki ders →"
> ile doğrudan geçilir, set sonunda kutlama. SC-E13; App 277/277, web 104/104.
> ⚠ Canlı doğrulama iki hata yakaladı (testler yeşilken): imaj *globalization-invariant* →
> `GetCultureInfo("tr-TR")` 500 verdi (açık `NumberFormatInfo`'ya geçildi); seed kapısı
> **blok bazına** çevrildi (deterministik Id) yoksa yeni blok tipi mevcut DB'ye inmiyor.

> ✅ **T6.1 bitti (2026-07-19):** katmanlı ders içeriği canlı — **25 bölüm** (5 ders ×
> L1/L2/L3 + örnek + tuzak) yeni `EducationContent.cs`'te (topluluk katkısına açılabilir);
> 2-5. derslere test eklendi (quiz 1→5, soru 3→15). **Ayrı idempotent kapı** sayesinde
> dersleri zaten olan DB'ler içeriği geriye dönük aldı (canlı Postgres'te teyitli).
> İçerik kısıtları teste bağlı: MiniMarkdown alt kümesi + tavsiye yasağı. SC-E12; 32/32.
> **Kalan:** `LiveContext` bloğu → T6.2 (ContextKey sözleşmesi gerekiyor).

> ✅ **T6.5 bitti (2026-07-19):** katmanlı içerik şeması canlı — `DepthTier{Core,Context,Deep}` +
> `SectionKind{Explain,Example,Trap,LiveContext,Source}`, `LessonSection` artık taşıyıcı
> (varsayılan Core/Explain), 2 CHECK + indeks, migration `20260719210255` **canlı Postgres'te**.
> Servis filtrelemez — katlama T6.7'de. SC-E11; Education 27/27, web 101/101.

> 📘 **Eğitim tasarımı (2026-07-19):** [`15-EDUCATION-PLAN.md`](../docs/15-EDUCATION-PLAN.md) —
> Faz 6 T6.5–T6.14 ile genişletildi (17 ders, katmanlı derinlik, demo bağlam).
> **Üç eksen:** bilgi→derinlik · risk tutumu→vurgu (**görünmez**) · portföy→örnek.
> 🔒 SPK: `RiskAttitude` dağılım/portföy çıktısına girmez (SC-E4); enstrüman
> sıralaması guard'la düşer (SC-E5). Kararlar: demo portföyle başlangıç · risk
> etiketi gizli · vergi dersi kapsam dışı.

> ✅ **T5E.4 çekirdek bitti (2026-07-17):** Eğitim sayfası canlı (ComingSoon kalktı) — "Temeller"
> seti + ilerleme çubuğu + kilit/tamamlandı rozetleri + sayfa-içi ders okuma (**`MiniMarkdown`** —
> güvenli, lib'siz) + mini test (tam-eşleşme; sonuçta doğru şık+açıklama açılır). Shared tipler+istemci
> +hook'lar eklendi. SC-45 web 101/101; **canlı Postgres uçtan uca teyit** (api+caddy rebuild →
> /egitim seed'le birebir). Kalan: ConceptTag derin bağlantı → T5E.4b.

> ✅ **T5E.3 bitti (2026-07-16):** eğitim uçları canlı — `api/education` 6 uç (tracks·track-lessons·
> lesson-detay·progress-upsert·quiz-attempt·by-concept). Kilit ön-koşuldan türetilir; içerik açık,
> durum/ilerleme+deneme `UserId` kapsamlı (per-user izolasyon testli); quiz cevap-anahtarı yalnız
> deneme sonucunda açılır. SC-44 integration 12/12. **Canlı Postgres'te teyit edildi (T5E.4 ile).**

> ✅ **T5E.2 bitti (2026-07-16):** eğitim seed'i canlı — "Temeller" track + 5 ders
> (sıralı ön-koşul zinciri) + 6 kavram etiketi + Ders 1'e 3 soruluk quiz + örnek ilerleme
> (1-3 Tamamlandı·4 Devam·5 türetilmiş Kilitli). **Portföyden bağımsız idempotent** →
> çalışan DB'ler bir sonraki `Database:Seed` açılışında eğitimi de alır (re-migrate gerekmez).
> İçerik kısa/eğitici (derinleştirme T6.1). SC-43 integration 6/6.

> ✅ **T5E.1 bitti (2026-07-12):** eğitim şeması canlı — 11 tablo (track/ders/bölüm/
> ön-koşul/kavram/quiz/ilerleme/deneme), CHECK+unique kısıtları, KVKK kaskadları,
> migration gerçek Postgres'e uygulandı (SC-38 integration 4).

> ✅ **FAZ 5 KAPANDI (2026-07-12, T5.1→T5.4):** günlük değer+maliyet serisi (saf servis) →
> `GET /api/portfolio/history` → web Değer Seyri (pano + Performans, hover tooltip) →
> **Senaryo v1 canlı**: "nakitte dursaydı" karşılaştırması + alım gücü eşiği (üç çizgi,
> tahmin YOK). Bonus (SC-34): özet bayat AvgCost düzeltmesi (özet = liste = seri).

> ✅ **T4.2 + T4.3 bitti (2026-07-11/12):** Finnhub sağlayıcı (3 uç, token başlıkta, kaba bant
> eşikleri KODDA, 1s cache) canlı teyitli (AAPL/MSFT gerçek veriyle; anahtar .env'de). /explain:
> StockExplainPrompts (iki yönlü çerçeve, uydurma yasağı) + paylaşılan parse/bekçi hattı +
> sembol bazlı 24s cache; canlıda Anthropic ile 5 eğitici kart. Faz 4 backend TAMAM — kalan tek iş web UI (T4.4).

## Ortam notu (2026-07-11)
- **Birincil ortam: Docker compose** → `docker compose up -d --build` → **https://localhost**
  (web SPA Caddy imajında; gerçek veri compose Postgres'inde — yerelden taşındı).
- Yerel `dotnet run` + `pnpm dev` yalnız geliştirme sandbox'ı (ayrı DB, eski kopya).
- Web kodu değişince: `docker compose up -d --build caddy`.

## Son kilometre taşları (detay: TASKLOG)
- **2026-07-19:** Planlama — açık bankacılık/ÖHVPS araştırıldı ve **reddedildi**
  (TCMB lisansı ≥1,5M TL + kapsam yatırım verisini içermiyor); yerine **Faz 9:
  ekstre yükleme + gelir-gider** backlog'a eklendi (T9.1–T9.8; kapı: T7.2 kimlik;
  sıra: Faz 6 → T7.2 → Faz 9). Kimlik analizi: `11` §2 kararı teyit (self-host
  JWT+Argon2id), web'de httpOnly cookie önerisi — detay TASKLOG.
- **2026-07-13:** Araç kutusu bakımı (finans ürününe dokunmaz) — CLAUDE.md **§14 model
  yönlendirme tablosu + kuralı** eklendi (Fable5/Opus4.8/Sonnet5/Haiku4.5/yerel nemotron);
  git geçmişindeki **Claude co-author trailer'ı temizlendi** (filter-branch + force-push;
  bundan sonra eklenmiyor — memory); **SC-42 FX yarışı işi commit'lendi** (`046d7e3`).
- **2026-07-12:** Pano Değer Seyri ilk yükleme FX yarışı düzeltmesi (SC-42) — pano kartına
  `isError` dalı (hata boş-veri gibi maskelenmez) + fiyat tazelemesi `portfolio-history`'yi
  de invalidate eder + backend kur yokken tazelemeyi kendisi tetikler / 500 yerine 502
  (`MissingFxRateException`); aynı desen `ScenarioService`'e de uygulandı (/scenario;
  FxRace integration 4/4); backend + web süitleri yeşil ·
  T5.5+T5.6 kenar-durum düzeltmeleri — fiyatsız kalem özete **maliyetiyle**
  girer (sahte −%100 yok; özet = seri) + kronolojik aşırı satış yazmada 400
  (`FirstOversoldDate`; seri negatife düşemez); SC-40/41, backend 394 test yeşil ·
  Yorum gezgini yeni görünüm — solda dikey başlık rayı, ≤720px'te accordion
  (Analiz + Hisse, tek bileşen; web 76/76) · T4.5: fiyat geçmişi grafiği + Faz 4 kapanışı.
- **2026-07-11:** Motion katmanı 3 (sayaç/donut çizimi/sparkline/bar+hover; reduced-motion
  bilinçli kapalı) · BES katkı geçmişi teşhisi + etkin oran rozeti · tek komut Docker +
  veri taşıma · SETUP.md yalnız-Docker sadeleştirme · strateji dokümanı + faz planı işlendi.
- **2026-07-10:** Analiz sayfası canlı (çalışan ücretsiz model: nemotron; iki-backend tuzağı bulundu).
- **2026-06-20:** T4.1 — Finnhub kararı (ayrıntı: 08-BACKLOG Faz 4 notu).

## Devam eden / Bloke
- (bloke yok) — T5E.2 tamam, commit'lenecek.
- **Sıradaki gerçek iş:** T5E.3 — Eğitim endpoint'leri (tracks/lessons/progress/quiz; IDOR).
- ⚠ **Bilinen (görev dışı):** `ObservabilitySecurityTests`'te web host başlatan 3 test bu
  ortamda `WebApplicationFactory<Program>` ayağa kalkmadığı için kırmızı (T5E.2 öncesinden;
  düz factory/DB'siz — seed ile ilgisiz). T5E.3 endpoint testleri SQLite fixture'ıyla koşar.

---
*Güncelleme kuralı: CLAUDE.md §11. Bu dosya kısa kalmalı — detay TASKLOG.md'de,
tam plan 08-BACKLOG.md'de.*

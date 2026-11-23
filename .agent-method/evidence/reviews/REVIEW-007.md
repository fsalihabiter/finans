---
review_id: REVIEW-007
runtime: claude-code
timestamp: "2026-11-23T15:30:00Z"
scope: "GD-008 — Set 2 track iskeleti + S2-L1 dersi (içerik seed + figür katmanı + test kapıları)"
base_ref: 3e306b5
head_ref: 1f88908
active_gd: GD-008
reviewers:
  - content-accuracy
  - correctness
  - ui-regression
  - tests-and-gates
  - compliance-legal
  - architecture-reuse
  - naming
findings:
  - id: RV-089
    severity: high
    evidence: proven
    category: content-accuracy
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    summary: >-
      "Senin portföyünde" bloğu Değer Seyri için "eksende olmayan günler vardır"
      diyor; oysa seri HER takvim günü için nokta üretiyor — iddia uygulamanın
      gerçeğine aykırı.
    rationale: >-
      `PortfolioValueHistoryService` ilk olay gününden bitiş gününe kadar
      `for (var day = start; day <= endDate; day = day.AddDays(1))` ile
      ilerliyor ve her gün için bir `DailyValuePoint` ekliyor; `AssetState`
      fiyat işaretçisini ilerletirken son bilinen fiyatı taşıyor. Yani hafta
      sonunun noktası VARDIR — yeni bir anlaşma değil, TAŞINMIŞ bir değerdir.
      Ders tam da "o nokta nereden geldi?" sorusunu öğretiyor, dolayısıyla bu
      yanlış iddia dersin kendi tezini çürütüyor. Üstelik doğrusu daha
      öğreticidir: piyasa grafiğinde gün yok, Değer Seyri'nde gün var ama
      nokta taşınmış — iki grafiğin noktaları AYNI TÜRDEN ŞEY DEĞİL.
    status: open
  - id: RV-090
    severity: medium
    evidence: proven
    category: tests-and-gates
    file: backend/tests/Finans.Integration.Tests/EducationSeedTests.cs
    rule_or_decision: SC-E25
    summary: >-
      Yeni M7a kalıpları (`boşluk kapan` · `boşluğu kapat`) DOĞRU cümleyi de
      yakalıyor — "boşluk kapanmak zorunda değildir" yasak kalıbı içeriyor.
    rationale: >-
      Kontrol `NotContainEquivalentOf`, yani düz alt dize araması. "boşluk
      kapanmaz", "boşluk kapanmak zorunda değildir", "boşluğu kapatmaz"
      cümlelerinin hepsi yasaklı dizeyi İÇERİR. Bu cümleler guard'ın korumak
      istediği şeyin ta kendisi: kehaneti REDDEDEN anlatım. S2-L4/L5 boşluk
      konusunu işlerken bu cümleyi yazmak zorunda kalacak ve guard kırmızıya
      düşecek — guard o noktada ya gevşetilir (koruma kaybolur) ya da ders
      yanlış yazılır. Olumlu biçimleri hedeflemek gerekir (`boşluk kapanır`,
      `boşluk kapanacak`, `boşluğu kapatır`), olumsuzu serbest bırakarak.
    status: open
  - id: RV-091
    severity: medium
    evidence: proven
    category: correctness
    file: web/src/components/LessonFigure.tsx
    summary: >-
      `priceScale` `lo === hi` durumunda sıfıra bölüyor; düz seri çizen bir
      figür `NaN`/`Infinity` koordinatla boş çizilir.
    rationale: >-
      `(v - lo) / (hi - lo)` paydası sıfır olduğunda sonuç `Infinity` (ya da
      `v === lo` ise `NaN`) olur ve SVG o şekli çizmez. `timeScale` ikiz
      durumu (`n <= 1`) açıkça koruyor, `priceScale` korumuyor — asimetri
      tesadüfi. Risk teorik değil: setin BİR SONRAKİ dersi (S2-L2) "grafik
      yatay görünüyor" konusunu işliyor ve düz/dar aralıklı seri çizmek tam
      oradaki figürlerin işi. Render kapısı da yakalamaz: şekil çizilmediğinde
      `aria-label` ve `figcaption` yerinde kalır, test yeşil geçer.
    status: open
  - id: RV-092
    severity: low
    evidence: strong
    category: correctness
    file: web/src/components/LessonFigure.tsx
    summary: >-
      `ChartFrame` React anahtarlarını tick ETİKETİNDEN üretiyor; yinelenen
      etiket (ör. iki "100 ₺" ya da tekrar eden gün adı) anahtar çakışması yapar.
    rationale: >-
      `key={`y-${t.label}`}` / `key={`x-${t.label}`}` — etiketler bugünkü dokuz
      figürde tekil olduğu için sorun görünmüyor. Ama bu yardımcı bilerek
      PAYLAŞILAN bir iskelet olarak yazıldı (Set 2'nin kalan yedi dersi, ~60
      figür) ve yinelenen eksen etiketi olağandır. Çakışan anahtar React'te
      sessiz yanlış eşleştirme üretir; dizin tabanlı anahtar bu sınıfı tümden
      kapatır.
    status: open
  - id: RV-093
    severity: low
    evidence: proven
    category: compliance-legal
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    rule_or_decision: D-017
    summary: >-
      Kaynak bloğu künyenin adıyla andığı yöntem kaynağını (`GET
      /api/portfolio/history`) yazmıyor; sapma SESSİZCE yapıldı.
    rationale: >-
      Künye (§S2-L1 Kaynaklar) uç noktayı açıkça adlandırıyor; ders metni
      "portföy geçmişi uç noktası" diye genel bir ifade kullanıyor. Sapmanın
      kendisi savunulabilir (kullanıcıya görünen metinde depo-içi doküman
      bağlantısı 404 verirdi) ama AGENTS.md §4.2 "sessizce düzeltme — bildir"
      diyor ve bu sapma ne künyeye ne GD'ye not düşüldü. Karar ya metne ya
      künyeye yazılmalı.
    status: open
  - id: RV-094
    severity: low
    evidence: strong
    category: content-accuracy
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    rule_or_decision: D-017
    summary: >-
      Künye `volatility` kavramını "pekiştirilen" sayıyor; ders kavramı hiçbir
      yerde ADLANDIRMIYOR.
    rationale: >-
      12. aşamanın "hafta içi kapanış aralığı 7 ₺" hesabı tam olarak bir
      dalgalanma ölçüsüdür ama bu bağ kurulmuyor; okur kavramı tanımadan
      geçiyor. "Pekiştirilen kavram" iddiasının hiçbir makine karşılığı yok
      (M6 yalnız TANITILAN etiketleri denetliyor), dolayısıyla künye ile ders
      arasındaki bu yön sessizce eskiyebilir — M12/M13'ün figürler için
      kapattığı boşluğun kavramlardaki karşılığı.
    status: open
  - id: RV-095
    severity: low
    evidence: proven
    category: naming
    file: web/src/components/LessonFigure.tsx
    summary: >-
      `ArrowRight`/`ArrowUp` üçgenlerini `fig-dot-neutral` ("nokta" dolgusu)
      sınıfıyla boyuyor.
    rationale: >-
      Çalışıyor, ama sınıf adı şekli yanlış tarif ediyor ve kayıt defterinin
      "durum biçimle kodlanır" sözleşmesini okumayı zorlaştırıyor. Render
      kapısı yalnız `rect`/`circle` incelediği için bu kayma teste de
      görünmez; `fig-mark-neutral` gibi şekilden bağımsız bir ad ya da ayrı
      bir ok sınıfı doğru olur.
    status: open
  - id: RV-096
    severity: low
    evidence: strong
    category: naming
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    summary: >-
      `ExDeep` **Context** katmanı, yeni `ExL3` **Deep** katmanı — yan yana iki
      ad tam da yanlış seçimi davet ediyor.
    rationale: >-
      `ExDeep` adı GD-008'den önce de yanıltıcıydı; yeni yardımcı eklenince
      ikisi birlikte okunan bir çift oldu. XML özeti uyarıyor ama ders yazarı
      yardımcıyı listeden seçiyor, özeti değil. `ExCtx` + `ExDeep` adlandırması
      (mevcut çağrıları yeniden adlandırarak) belirsizliği tümden kaldırır.
    status: open
  - id: RV-097
    severity: medium
    evidence: proven
    category: architecture-reuse
    file: backend/src/Finans.Infrastructure/Seed/SeedData.cs
    rule_or_decision: RV-064
    summary: >-
      Track kaydında MUTABAKAT yok ("yoksa ekle"); başlık/açıklama düzeltmesi
      çalışan kurulumlara inmez — RV-064'ün ders kaydında kapattığı sınıfın
      aynısı.
    rationale: >-
      `SeedSet2Async` track'i yalnız `AnyAsync(...)` yanlışsa ekliyor. Ders
      kaydı için bu desen REVIEW-005'te bir bulgu olmuş ve `UpsertLesson` ile
      düzeltilmişti; track kaydı aynı derste eski desende bırakıldı. Set
      açıklaması kullanıcıya görünen bir metindir ("Okuma becerisi öğretir;
      sinyal ya da tahmin öğretmez" — yasal çerçeveyi taşıyan bir cümle) ve
      düzeltilmesi gerektiğinde inmeyecek. Üç track de aynı durumda: sorun
      GD-008'de doğmadı ama GD-008 onu üçüncü kez tekrarladı.
    status: open
  - id: RV-098
    severity: low
    evidence: heuristic
    category: ui-regression
    file: web/src/components/LessonFigure.tsx
    summary: >-
      `missing-days` figüründe alt grafiğin dikey ekseni etiketsiz çizilmiş;
      aynı ders "eksenlerin ne gösterdiği yazılı olmalı" diyor.
    rationale: >-
      Figürün amacı yatay eksen olduğu için dikey eksende tick yok, ama çizgi
      duruyor — okur "fiyat ekseni var, ölçeği yok" görüyor. 8. aşamanın dört
      sorusuyla (özellikle "hangi para birimi") hafif bir çelişki. Ya eksen
      etiketlenmeli ya da çizgi hiç çizilmemeli.
    status: open
  - id: RV-099
    severity: low
    evidence: proven
    category: tests-and-gates
    file: backend/tests/Finans.Integration.Tests/EducationSeedTests.cs
    summary: >-
      Yeni satır-içi markdown kapısının 120 karakterlik aralık sınırı ve
      `***üçlü***` boşluğu yorumda YAZILI DEĞİL.
    rationale: >-
      Yorum alt çizgi istisnasını gerekçesiyle anlatıyor (iyi), ama regex'teki
      `{1,120}` üst sınırını ve üç yıldızlı biçimin kapsam dışı kaldığını
      söylemiyor. REVIEW-006'nın (RV-076) dersi tam buydu: kapının SINIRLARI
      yazılı değilse, kapı olduğundan geniş sanılır ve bir sonraki okur ona
      güvenir.
    status: open
---

# REVIEW-007 — GD-008 · Set 2 açılışı + S2-L1

**Taban:** `3e306b5..1f88908` · **Aktif GD:** GD-008 · **Çalıştıran:** claude-code
(sıralı yol — kanonik doğruluk tabanı, INV-15)

## 1. Diff sınıflandırması

Değişiklik karışık: **içerik seed'i** (eğitim metni + quiz, kullanıcıya doğrudan
görünen), **UI** (dokuz yeni SVG figür + paylaşılan yardımcı + CSS), **test
kapıları** (sayımlar, iki yeni kural) ve **ADM kayıtları**. Veritabanı şeması,
kimlik/yetki ve migration **yok** → güvenlik/migration perspektifleri dışarıda.

Seçilen perspektifler: içerik doğruluğu · correctness · UI regresyonu · test
kapıları · yasal uyum (M7 ailesi) · mimari/yeniden kullanım · adlandırma.

## 2. Önceki incelemeden taşınan

REVIEW-006'nın bulgularının tamamı `resolved`; taşınan açık bulgu **yok**.

## 3. Sayılar

| Önem | Adet | Durum |
|---|---|---|
| critical | 0 | — |
| **high** | **1** | RV-089 — **açık** |
| medium | 3 | RV-090 · RV-091 · RV-097 — açık |
| low | 7 | RV-092 · RV-093 · RV-094 · RV-095 · RV-096 · RV-098 · RV-099 — açık |

⚠ **INV-18:** açık `high` bulgu varken GD **tamamlanamaz.**

## 4. Tek bir şey değişecekse

**RV-089.** Ders, uygulamanın kendi grafiği hakkında **yanlış** bir şey
söylüyor ve yanlışlık dersin konusunun tam merkezinde. Doğrusunu yazmak ayrıca
daha iyi bir ders veriyor: piyasa grafiğinde hafta sonunun noktası **yok**,
Değer Seyri'nde **var** — ama o nokta yeni bir anlaşma değil, **taşınmış** bir
değer. Yani iki grafiğin noktaları aynı türden şey değil ve sorulacak soru hep
aynı kalıyor: *bu nokta nereden geldi?*

## 5. İncelendi, temiz

Bulgu çıkmayan ama bakılan yerler — "bakılmadı" ile karışmasın diye yazılı:

- **Aritmetik ↔ figür ↔ metin üçlüsü.** On beş günlük serinin haftalık
  karşılığı (`gunluk[4]`, `gunluk[9]`, `gunluk[14]` = 104 · 110 · 105), 12.
  aşamanın iki çıkarması (110−103 = 7 · 110−104 = 6), beş kapanış (100 · 104 ·
  101 · 107 · 105), 14 Mart/100 ₺, 15 Mart/106 ₺ ve bir günün yedi işlemi —
  metin, figür ve kaynak bloğu **birebir** aynı sayıları taşıyor.
- **Ölçek sınırları.** Dokuz figürün her birinde en küçük ve en büyük veri
  noktası kendi `priceScale` aralığının İÇİNDE; `line-connects-closes`'ın
  kesikli ara yolu da (`y ± 16/10`) çerçeveyi taşmıyor ve x'te monoton.
- **viewBox taşması.** Her figürde en alttaki metnin taban çizgisi `height`
  değerinin altında kalıyor; en geniş metinler (51 karakterlik `missing-days`
  notu, `reading-a-value`'nun iki adım satırı) 320 birimlik tuvale sığıyor.
- **Tavsiye yasağı.** Dersin hiçbir yerinde yön yorumu, örüntü dili, enstrüman
  sıralaması ya da kişiye özel plan yok; örnek seriler adsız ("A varlığı") ve
  kurgusal olduğu **metinde de** yazılı. Quiz çeldiricileri yanılgıyı öğretiyor
  (M7a'nın serbest bıraktığı alan).
- **Ön-koşul kararı.** `LessonPrerequisite` yokluğu M9'a uygun: zincir track
  içinde kalır, setler arası sert kilit yoktur ve S2-L1 setin ilk dersi.
- **Sayım güncellemeleri.** On sayımın onu da gerçek değere çekilmiş,
  hiçbiri `>=`'ye gevşetilmemiş; `Section_seed_backfills_a_newly_added_block_type`
  içindeki 174 → 187 ara değeri de (201 − 14 LiveContext) doğru.
- **İki kapının mutasyon kanıtı.** M7a ve yeni markdown kapısı bilerek
  bozularak kırmızıya düşürüldü, sonra geri alındı.

## 6. Düzeltmelerin açabileceği yeni iş

- RV-090'ın düzeltmesi guard'ı **olumlu kalıplara** daraltır; olumsuz cümle
  serbest kalır. Bu, guard'ın "kehaneti reddeden anlatımı engellememesi"
  kuralını yazılı hâle getirir ve SC-E25'in kapsam notu buna göre güncellenir.
- RV-094 bir boşluğa işaret ediyor: **"pekiştirilen kavram" iddiasının makine
  karşılığı yok.** M12/M13 bunu figürler için kapattı; kavramların aynı
  mutabakatı (künyedeki pekiştirilen liste ↔ ders metni) bugün hiçbir testte
  yok. Bu bir **test borcu adayıdır**, bu incelemenin bulgusu değil.

---
review_id: REVIEW-001
runtime: claude-code
timestamp: "2026-09-20"
scope: "GD-001 — S0-L7 dersi, figürler, D-017 test kuralı, SPIKE-001 ölçüm düzeneği"
base_ref: 30c0bc9
head_ref: 929771a
active_gd: GD-001
reviewers:
  - content-compliance
  - test-integrity
  - correctness
  - ui-accessibility
  - resource-hygiene
findings:
  - id: RV-001
    severity: medium
    evidence: proven
    category: test-coverage
    file: backend/tests/Finans.Integration.Tests/EducationSeedTests.cs
    line: 676
    rule_or_decision: D-002
    summary: "Tavsiye/tahmin taraması (M7) ve markdown güvenlik taraması YALNIZ `LessonSections.BodyMarkdown` üzerinde koşuyor; `Lesson.Summary` ve `Lesson.BodyMarkdown` hiç taranmıyor — oysa ikisi de kullanıcıya görünür."
    rationale: "Ders listesinde her dersin `Summary`'si gösteriliyor (tarayıcıda teyit edildi) ve `Lesson.BodyMarkdown` bölümsüz derslerde geriye dönük içerik olarak render ediliyor. Bu iki alan D-002 kapsamındaki metin ama yasal guard'ın dışında: bugün içerik temiz, korumanın kendisi eksik. Bir sonraki ders yazarı (ya da topluluk katkısı, 14 §4-D2) buraya yönlendirme/tahmin cümlesi koyarsa hiçbir test uyarmaz."
    status: resolved
    resolution:
      resolved_at: "2026-09-20"
      evidence_ref: "a09d089 · EducationSeedTests.No_block_including_sources_predicts_or_ranks_instruments"
      note: "Kullanıcı kararı. Tarama Lesson.Summary ve Lesson.BodyMarkdown alanlarını da kapsıyor. Genişletilince mevcut içerikte bir false positive çıktı (\"Neden yüksek getiri\" — sıralama regexi kelime sınırı yoktu) → regex düzeltildi."
  - id: RV-002
    severity: medium
    evidence: proven
    category: test-integrity
    file: backend/tests/Finans.Integration.Tests/EducationSeedTests.cs
    line: 180
    rule_or_decision: D-017
    summary: "`LessonsWithoutLiveContext` istisna listesindeki slug'ın gerçekten var olduğu doğrulanmıyor; ders yeniden adlandırılır ya da kaldırılırsa istisna ÖLÜ kalır ve kimse fark etmez."
    rationale: "D-017 istisnayı 'sessiz boşluk değil, testle doğrulanan beyan' olarak tanımlıyor. Bugün kural iki yönlü çalışıyor (listedeki derste blok BULUNMAMALI) ama listenin kendisi denetlenmiyor: var olmayan bir slug için `TryGetValue` hiç eşleşmez, döngü o dersi görmez, test yeşil kalır. Liste zamanla gerçeklikten kopabilir — D-017'nin 'liste büyürse uyarı sinyalidir' maddesi de bu yüzden işlemez."
    status: resolved
    resolution:
      resolved_at: "2026-09-20"
      evidence_ref: "a09d089 · EducationSeedTests (LiveContext istisna listesi denetimi)"
      note: "Kullanıcı kararı (\"RV-001 ve RV-002yi şimdi kapat\"). Listedeki her slug için gerçek bir dersin varlığı doğrulanıyor; ders yeniden adlandırılırsa test kırmızıya döner."
  - id: RV-003
    severity: low
    evidence: proven
    category: resource-hygiene
    file: backend/tests/Finans.Integration.Tests/Llm/LocalModelTrial.cs
    line: 150
    summary: "`BuildService` her koşuda yeni `HttpClient` üretiyor ve hiçbiri dispose edilmiyor; ayrıca kapı (`FINANS_LLM_TRIAL=1`) açıkken 600 sn'lik timeout bir CI koşusunu model başına 10 dakika kilitleyebilir."
    rationale: "Manuel düzenek olduğu için etkisi sınırlı: normal koşuda test erken dönüyor, soket sızıntısı süreç ömrüyle sınırlı. Yine de `using` eklemek ve timeout'u ortam değişkeninden okumak ucuz."
    status: open
  - id: RV-004
    severity: low
    evidence: proven
    category: content-consistency
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 2500
    summary: "S0-L7 metni yayında OLMAYAN içeriğe ileri atıf yapıyor: 'bir sonraki dersin konusu olan vade ve hedef' (S0-L8 henüz yok) ve 'davranış setinde ayrıntısıyla ele alınacak' (Set 4 / T6.12 açık)."
    rationale: "Kullanıcı dersi bitirdiğinde işaret edilen yere gidemiyor. S0-L8 sıradaki iş olduğu için kısa ömürlü bir tutarsızlık; ama Set 4 uzak. Emsal var (S0-L6 da S3-L2'ye köprü atıyor), dolayısıyla bu bir desen kararı: ya ileri atıflar 'ileride' diye belirsiz bırakılır ya da yayın sırası buna göre planlanır."
    status: open
---

# REVIEW-001 — GD-001 (S0-L7) · 2026-09-20

**Kapsam:** `30c0bc9..929771a` — 5 dosya, +994/−17.
Değişimin niteliği karışık: **içerik/seed** (ders metni, quiz, kavramlar) ·
**test kuralı değişikliği** (D-017) · **UI/SVG** (9 figür) · **yeni test aracı**
(SPIKE-001 ölçüm düzeneği). Seçilen bakış açıları bu karışıma göre belirlendi;
kimlik/şema/migration dokunuşu olmadığı için güvenlik-yetkilendirme ve
migration-safety bakışları kapsam dışı bırakıldı (diff'te karşılığı yok).

## Özet

| Önem | Sayı |
|---|---|
| critical | 0 |
| high | 0 |
| medium | 2 |
| low | 2 |

**Açık critical/high bulgu yok** → INV-18 gereği GD-001'in tamamlanmasını
engelleyen bir şey bulunmadı. Kabul kararı yine insana ait (INV-06).

## Bulguların okunuşu

İki `medium` bulgunun ortak teması şu: **bu turda yazılan içerik doğru, ama
içeriği koruyan mekanizmanın kapsamı eksik.** RV-001'de yasal guard kullanıcıya
görünen iki alanı hiç görmüyor; RV-002'de D-017'nin istisna listesi kendi
doğruluğunu denetlemiyor. İkisi de "bugün bozuk" değil, "yarın sessizce bozulur"
sınıfı. Topluluk katkısına açılacak bir içerik hattında (14 §4-D2) bu ayrım önemli.

İki `low` bulgudan RV-003 tamamen kozmetik; RV-004 ise bir **desen kararı**
gerektiriyor: dersler yayınlanmamış derslere atıf yapabilir mi? Mevcut emsal
(S0-L6 → S3-L2) "evet" diyor; o zaman bu bulgu kabul edilip kapatılabilir.

## Doğrulanan noktalar (bulgu değil)

- **Yasal çerçeve (D-002):** S0-L7'nin hiçbir bloğunda kurum/kişi/platform/ürün
  adı yok — **çeldiriciler dahil** elle tarandı. Kaynak bloğundaki SPK ve TMSF
  atıfları künyenin açıkça istediği kurumsal referanslar; bağlantılar
  `rel="noopener noreferrer"` + `target=_blank` ile render ediliyor (canlı teyit).
- **D-012:** figürlerin tamamı elle yazılmış SVG, kütüphane eklenmedi,
  `dangerouslySetInnerHTML` yok; anahtarlar `LessonFigure.tsx` kayıt defteriyle
  M4 testinde mutabık.
- **Erişilebilirlik:** dokuz figür de `Figure` sarmalayıcısından geçiyor →
  `role="img"` + açıklayıcı `aria-label`; figür kaybolsa metin eksilmiyor.
- **D-017 uygulaması iki yönlü:** istisnalı derste bloğun *bulunmadığı* iddia
  ediliyor, yani uygun metrik gelip blok eklenirse test kırmızıya döner.
- **Sır/PII:** yeni kodda anahtar yok; ölçüm düzeneği kurgusal portföy kullanıyor
  ve çıktısını gitignore'lu `tmp_diag/` altına yazıyor.
- **Testler:** EducationSeed+Api 48/48 · Application 291/291 · web 141/141 ·
  `tsc -b` temiz. Integration'daki 4 kırmızı INC-001 (ortam, önceden var).

## Sıradaki adım

Dört bulgunun durumu **insan kararı** bekliyor (`open` → `resolved | accepted |
false-positive`). RV-001 ve RV-002 küçük test eklemeleriyle kapanır; istenirse
GD-001 içinde yapılır, istenirse POOL'a ayrı kalem olarak taşınır.

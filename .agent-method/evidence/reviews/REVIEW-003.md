---
review_id: REVIEW-003
runtime: claude-code
timestamp: "2026-11-18"
scope: "GD-003 — S0-L8 'Vade, hedef ve portföy': ders içeriği + test, seed (ders, ön-koşul, 4 kavram), 9 figür, yapısal test sayımları, künye düzeltmesi"
base_ref: df6c2d4
head_ref: "WORKTREE (commit edilmemiş, df6c2d4 üzerinde)"
active_gd: GD-003
reviewers:
  - content-correctness
  - compliance-m7
  - data-integrity
  - ui-regression
  - tests
findings:
  - id: RV-011
    severity: medium
    evidence: proven
    category: tests
    file: backend/tests/Finans.Integration.Tests/EducationSeedTests.cs
    line: 99
    summary: "Concept_tags_link_lessons_to_glossary_keys tam anahtar listesini bekliyor; S0-L8'in 4 yeni anahtarı listeye eklenmemişti → beklenen 4 kırmızıya ek 5. kırmızı."
    rationale: "Sayımlar (29 kavram) güncellenmiş, tam-liste sözleşmesi atlanmıştı. Kullanıcı makinesinde dotnet test 491/496 ile yakalandı."
    status: resolved
    resolution:
      resolved_at: "2026-11-17"
      evidence_ref: "EducationSeedTests.cs:99 · dotnet test 492/496 (yalnız INC-001'in 4 kırmızısı)"
      note: "Listeye time-horizon · portfolio · position · weight eklendi."
  - id: RV-012
    severity: low
    evidence: proven
    category: content-correctness
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3105
    summary: "LiveContext satırında Türkçe ek hatası: '%79,3'sini' (doğrusu '…'ünü'). Değişken sayıya sabit ek yazılamaz."
    rationale: "Tarayıcı kontrolünde canlı portföyle görüldü."
    status: resolved
    resolution:
      resolved_at: "2026-11-18"
      evidence_ref: "EducationContent.cs:3105"
      note: "Kullanıcı onayı ('olur'). Cümle eksiz kuruldu: 'En büyük iki kalemin portföydeki payı: **{{concentration_top2}}**.'"
  - id: RV-013
    severity: low
    evidence: proven
    category: content-correctness
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3087
    summary: "%61,5 için 'neredeyse üçte ikisi' deniyordu; üçte iki %66,7."
    status: resolved
    resolution:
      resolved_at: "2026-11-18"
      evidence_ref: "EducationContent.cs:3087"
      note: "Kullanıcı onayı ('olur'). 'artık **%61,5**'i' olarak sabit sayıyla yazıldı."
  - id: RV-014
    severity: medium
    evidence: proven
    category: content-correctness
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 3091
    summary: "'En çok yükselen kalem, zamanla portföyün en büyük kalemi hâline gelir' genel kural olarak yanlış; en çok yükselen kalemin AĞIRLIĞI artar, en büyük olması gerekmez."
    rationale: "Dersin kendi örneğiyle karşı örnek: C (20.000 ₺) %50 yükselse 30.000 ₺ olur, A (50.000 ₺) hâlâ en büyüktür. Eğitim içeriğinde yanlış genelleme ürünün ana vaadine (doğru kavram) dokunuyor. Öneri: 'en çok yükselen kalemin ağırlığı büyür; yeterince uzun sürerse portföyün en büyük kalemi bile olabilir.'"
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "EducationContent.cs (weights-move-by-themselves bloğu) · EducationSeedTests 22/22"
      note: "Genelleme kaldırıldı: 'en çok yükselen kalemin AĞIRLIĞI büyür' + incelemenin kendi karşı örneği metne eklendi ('C %50 yükselip 30.000 ₺ olsaydı bile A hâlâ önde olurdu') + 'yeterince uzun sürerse en büyüğü hâline de gelebilir' koşullu ifadesi. Yanlış kural yerine doğru sezgi öğretiliyor."
  - id: RV-015
    severity: low
    evidence: heuristic
    category: compliance-m7
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 2921
    rule_or_decision: D-002
    summary: "Tuzak bloğunda 'yirmi beş yıl dokunulmayacak parayı kısa vadeli gibi tutmak da bir uyumsuzluktur — bedeli daha sessiz ödenir' cümlesi, uzun vadeli parayı dalgalı kalemlere yönlendiren örtük bir yönlendirme gibi okunabilir."
    rationale: "Kişiye özel değil ve kavramsal; yine de 'bedel' vurgusu bir dağılım tercihini ima ediyor. Öneri: bedeli adlandırmak (ör. 'enflasyon karşısında alım gücü kaybı riski — Ders 4') ve yönlendirmeyi kaldırmak. İnsan kararı gerekli. || 2026-11-22: önerilen düzeltme UYGULANDI (EducationContent.cs, S0-L8 tuzak bloğu) — 'bedeli daha sessiz ödenir' kalktı; yerine bedel adlandırıldı ('bekleyen paranın ALIM GÜCÜ enflasyon karşısında zamanla erir; bunu daha önceki bir derste görmüştün') ve karar okura iade edildi ('hangisinin senin durumun olduğu, paraya ne zaman ihtiyacın olduğuna bağlıdır'). Ders numarası bilerek yazılmadı (OrderIndex kayabilir). M7 taraması temiz. STATUS AÇIK BIRAKILDI: bulgunun kendisi 'insan kararı gerekli' diyor — yeni metnin D-002 açısından yeterli olduğunu ürün sahibi onaylamalı."
    status: open
  - id: RV-016
    severity: low
    evidence: heuristic
    category: ui-regression
    file: web/src/components/LessonFigure.tsx
    line: 2060
    summary: "three-goals figüründe acil durum fonu kırmızı (fig-bar-neg), emeklilik yeşil (fig-bar-pos); bu renkler uygulamada kazanç/kayıp anlamı taşıyor."
    rationale: "Figür vade uzunluğunu gösteriyor, iyi/kötü değil; acil durum fonunu 'olumsuz' renkle göstermek yanlış anlam yükler. Öneri: nötr ton skalası (fig-seg-lead / fig-seg / fig-bar-muted)."
    status: resolved
    resolution:
      resolved_at: "2026-11-22"
      evidence_ref: "LessonFigure.tsx ThreeGoals · web 147/147 · tsc -b temiz"
      note: "Önerilen nötr skala birebir uygulandı: fig-seg-lead / fig-seg / fig-bar-muted (yakın → uzak). fig-bar-neg ve fig-bar-pos kaldırıldı. Süreyi zaten çubuk UZUNLUĞU anlatıyor; renk artık bilgi taşımıyor. Kod yorumuna gerekçe düşüldü ki aynı hata tekrar yazılmasın."
  - id: RV-017
    severity: low
    evidence: proven
    category: content-correctness
    file: backend/src/Finans.Infrastructure/Seed/EducationContent.cs
    line: 686
    summary: "RV-012'deki ek hatası eski derslerde de var (diff dışı): 686, 860, 1021, 1022, 1600, 1813, 2023. satırlar — {{token}}**'si/'i gibi sabit ekler."
    rationale: "Örn. cash_weight %40 → '%40'i' (doğrusu '%40'ı'), %6 → '%6'i' (doğrusu '%6'sı'). GD-003 kapsamı dışında; aynı eksiz kalıpla toplu düzeltme ya da POOL'a taşıma önerilir."
    status: open
  - id: RV-018
    severity: low
    evidence: proven
    category: authorization
    file: backend/src/Finans.Infrastructure/Services/EducationService.cs
    line: 180
    rule_or_decision: D-013
    summary: "Ön-koşul kilidi yalnız okuma yolunda hesaplanıyor; UpdateProgressAsync ve SubmitQuizAttemptAsync kilitli derse yazmayı reddetmiyor (diff dışı, önceden var)."
    rationale: "Tarayıcı doğrulamasında kilitli S0-L8'e ilerleme yazıldı ve kabul edildi (test kullanıcısında 'Devam ediyor %93'). Kullanıcı izolasyonu korunduğu için güvenlik etkisi yok; öğrenme kapısı istemciye bağımlı. Öneri: kilitliyse 409/400. Ayrı iş."
    status: open
---

# REVIEW-003 — GD-003 (S0-L8 · Vade, hedef ve portföy)

## Kapsam
`df6c2d4` → commit edilmemiş çalışma ağacı. Değişiklik türü: eğitim içeriği + seed verisi +
SVG figürleri + yapısal testler (migration yok). Bakış açıları sırayla yürütüldü (tek ajan).

## Doğrulananlar (bulgu yok)
- **Hesaplar:** kur örneği (100.000 ₺ → 110.000 ₺ +%10; 2.500 $ → 2.200 $ −%12), ağırlıklı toplam
  (0,8×10 − 0,2×15 = +%5), kayma (%61,5 / %23,1 / %15,4) ve ağırlık payı örnekleri elle doğrulandı.
  Figür çubuk oranları metinle tutarlı.
- **Seed:** ders, ön-koşul (S0-L7 → S0-L8), 4 ConceptTag ve bağları idempotent `AnyAsync` korumalı;
  bölümler deterministik Id ile mutabakattan geçiyor. API: 14 bölüm, 9 figür anahtarı, 4 etiket.
- **LiveContext:** 5 token'ın hepsi `ContextKeys`'te; `concentration_top2` kalem bazında
  (`Allocation` kalem listesi) → "en büyük iki kalem" ifadesi doğru. Nakit/hisse satırının tür
  yoksa düşmesi bilinçli tasarım (`AddTypeWeight`).
- **Ekran eşlemesi:** `HoldingsTable` sütunları (Varlık · Miktar · Maliyet · Değer · Ağırlık) ve
  "ort." alt satırı dersin anlattığıyla birebir.
- **Tarayıcı (2026-11-17, compose yığını):** 15 adım, 9 figür doğru adımlarda render, konsol hatası
  yok, ekranda `undefined`/`NaN`/çözülmemiş token yok.
- **Testler:** dotnet test 492/496 (4 = INC-001); web vitest 147/147, `tsc -b` ve eslint temiz.

## Özet
8 bulgu: 0 critical · 0 high · 2 medium · 6 low. 3 resolved, 5 open.
Açık critical/high yok → GD tamamlanmasını bloklayan bulgu yok (INV-18).
Açık: RV-014 (medium), RV-015, RV-016, RV-017, RV-018 (low).

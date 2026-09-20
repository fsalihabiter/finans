# PHASES

> Kanonik faz yapısı. İlk sürüm `adm-analyze` ile `ROADMAP.md`,
> `.claude/docs/08-BACKLOG.md` ve `CLAUDE.md` §4'ten türetildi (2026-09-20);
> durumlar backlog işaret sayımından geldi.
> **ID eşlemesi:** `PHASE-00N` = projenin "Faz N"i. Mobil kol (Faz M) = `PHASE-100`
> (numaralandırma onay bekliyor). İlerleme yüzdesi TUTULMAZ (türetilir).

## PHASE-000 — Hazırlık & İskelet (Faz 0)
- status: completed
- intent_goals: [G-1, G-5]
- summary: Monorepo (pnpm) + `@finans/shared`, .NET 10 çözümü (4 katman), EF Core/Npgsql + ilk migration + idempotent seed, web iskeleti (Vite/Router/Query + tasarım token'ları), `/api/health`, Docker, test altyapısı.
- exit_criteria (gerçekleşen): T0.1-T0.14 `[x]`; `dotnet test` ve web testleri yeşil.
- gd_range: (geçmiş — GD dosyası yok, kayıt TASKLOG'da)

## PHASE-001 — Portföy Takip MVP (Faz 1)
- status: completed
- intent_goals: [G-1, G-4]
- summary: Elle varlık girişi, işlemlerden ortalama maliyet, portföy özeti (değer, kâr, getiri, dağılım, reel getiri), BES devlet katkısı ayrı satır, web panosu.
- gd_range: (geçmiş)

## PHASE-002 — Canlı Fiyat & Bilgilendirme (Faz 2)
- status: completed
- intent_goals: [G-1, G-4, G-5]
- summary: Frankfurter (döviz) + Truncgil (gram altın) anahtarsız sağlayıcılar, `IPriceProvider` soyutlaması, cache + fallback, kural tabanlı eğitici notlar (`NudgeRuleEngine`), cache/metrik gözlemlenebilirliği.

## PHASE-003 — LLM Yorum Katmanı (Faz 3)
- status: completed
- intent_goals: [G-2, G-3]
- summary: Hazır sayıların LLM'e yapılandırılmış çıktı ile yorumlatılması; anonimleştirme, dil ve çıktı guard'ları, cache, fallback.

## PHASE-004 — Hisse Temel Analiz (Faz 4)
- status: completed
- intent_goals: [G-2, G-3]
- summary: Finnhub (ABD) metrikleri + Yahoo fiyat geçmişi; metriklerin LLM ile **açıklanması** (tavsiye yok). BIST ertelendi.

## PHASE-005 — Değer Seyri + Senaryo v1 (Faz 5)
- status: completed
- intent_goals: [G-1, G-2]
- summary: Fiyat geçmişinden günlük değer serisi, Değer Seyri grafiği ve geçmişe dönük senaryo v1. (Kapandı 2026-07-12.)

## PHASE-006 — Eğitim MVP + Kavram Sözlüğü (Faz 6) ← AKTİF
- status: active
- intent_goals: [G-2, G-3, G-4]
- summary: "Portföyünle Öğren" modülü: eğitim şeması + seed içeriği, katmanlı anlatım (L1/L2/L3), tanılama testi ve uyarlanabilir render, çok set desteği, Set 0 "İlk Adımlar" (10 ders) ve Set 2 "Grafik ve Piyasa Okuryazarlığı" (8 ders), kavram sözlüğü.
- entry_criteria: Faz 5 kapalı; eğitim şeması + endpoint'leri hazır.
- exit_criteria (backlog DoD): 35 ders seviyeye uyarlanmış derinlikte okunabiliyor; çok set + "Buradan başla"; "Senin portföyünde" gerçek/etiketli demo veriyle çalışıyor; kullanıcı fiyat ve mum grafiği okuyabiliyor, kalemini ölçütle karşılaştırabiliyor; tanılama seviyeyi belirliyor ve `RiskAttitude` hiçbir yerde görünmüyor; quiz + ilerleme + kavram ustalığı kayıtlı; sözlük aranabilir; her ders kaynak bloğu taşıyor; SC-E1–E10 + SC-E19–E28 yeşil.
- durum notu: 17 görev `[x]`, 2 `[~]` (T6.16 Set 0 ders 6/10, T5E.4 ConceptTag derin bağlantısı), 16 açık.

## PHASE-007 — Kişiselleşme & Erişim (Faz 7)
- status: planned
- intent_goals: [G-2, G-5]
- summary: Onboarding/seviye derinleştirme, kimlik ve çok kullanıcı (JWT), PWA, bildirim, TEFAS, altın kültürü modülü, demo mod.

## PHASE-008 — Ölçek & Etki (Faz 8)
- status: planned
- intent_goals: [G-2, G-3, G-4]
- summary: Davranış aynası, enflasyon paneli, gelir modeli kararı, **SPK/KVKK hukuki onay (lansman kapısı)**.

## PHASE-009 — Nakit Akışı & Harcama Bilinci (Faz 9)
- status: planned
- intent_goals: [G-2, G-5]
- summary: Kullanıcının kendi indirdiği ekstreyi yüklemesi, deterministik kategorileştirme, nakit akışı panosu, geçmişe dönük birikim senaryosu, KVKK sertleştirme. **Kapı: PHASE-007 kimlik.**

## PHASE-100 — Mobil Kol (Faz M)
- status: planned
- intent_goals: [G-1, G-2]
- summary: React Native / Expo; aynı API + `@finans/shared`. Web parası oturduktan sonra.

## Açık sorular

1. ADM `max_active_phases: 1` diyor; öneri PHASE-006'yı aktif tutuyor. Faz 9
   planlaması Faz 6 sürerken ilerlerse bu kural gözden geçirilmeli.
2. Tamamlanmış fazların (PHASE-000..005) kabul kriterleri **geriye dönük**
   yazılsın mı, yoksa "geçmiş faz, kanıt TASKLOG'da" notu yeterli mi? (INV-06
   insan onayı gerektirir; geçmiş için onay yeniden üretilemez.)
3. `PHASE-100` numaralandırması uygun mu, yoksa `PHASE-010` mu olmalı?

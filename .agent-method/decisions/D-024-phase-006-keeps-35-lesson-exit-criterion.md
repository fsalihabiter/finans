---
id: D-024
title: PHASE-006'nın "35 ders" çıkış ölçütü korunur; faz bilinçli olarak büyük kabul edilir
status: accepted
scope: structure
created: 2026-11-23
supersedes: null
superseded_by: null
---

# D-024 — Faz 6'nın çıkış ölçütü 35 derste kalır

## Context

`adm-phase status` (S-010 sonrası) PHASE-006'da bir kapsam uyuşmazlığı buldu ve
sessizce düzeltmek yerine bildirdi (AGENTS.md §4.2):

- **Çıkış ölçütü** "35 ders seviyeye uyarlanmış derinlikte okunabiliyor" diyor.
- **Müfredat künyeleri** (`16-CURRICULUM.md`, D-017 gereği kanonik) tam 35 ders
  planlıyor: S0 10 · S1 5 · S2 8 · S3 4 · S4 4 · S5 4.
- **Seed'de 15 ders var** (doğrulandı: `SeedData.cs`'de 15 `new Lesson`;
  17 slug'dan ikisi track slug'ı). Bunlar S0'ın 10'u + S1'in 5'i.
- Ama fazın kendi **durum notu** kalan işi yalnız "Set 2 (8 ders) + kavram
  sözlüğü" diye çerçeveliyordu → 15 + 8 = **23 ≠ 35**. Set 3 "Portföy Yönetimi"
  (4), Set 4 "Davranışsal" (4) ve Set 5 "TR'ye özgü" (4) — toplam **12 ders**
  hiçbir planlama satırında sayılmıyordu.

Yani faz, kendi ilerleme notunun ima ettiğinden yaklaşık **üç kat** büyüktü ve
bu, "Set 2 bitti → Faz 6 kapanır" beklentisiyle çalışıldığında sonda ortaya
çıkacak bir sürprizdi. İki okuma vardı: ölçütü daraltmak (Faz 6 = Set 0-2) ya da
ölçütü koruyup fazın büyüklüğünü kabul etmek.

⚠ `adm-phase` bu kararı kendi başına vermez — çıkış ölçütünü değiştirmek faz
kapsamı kararıdır (skill: "human approval points — phase close, any GD deferral").

## Decision

**Çıkış ölçütü "35 ders" olarak KORUNUR.** Faz 6, Sets 0-5'in tamamını
kapsayan, bilinçli olarak **büyük** bir faz kabul edilir. Faz bölünmez.

Ürün sahibi kararı (2026-11-23): *"35 kalsın, faz büyük"*.

Bunun pratik karşılığı:

- Faz 6'nın gerçek kalan kapsamı **20 ders + kavram sözlüğü**:
  Set 2 (8) · Set 3 (4) · Set 4 (4) · Set 5 (4). "Set 2 + sözlük" çerçevesi
  eksikti ve `PHASES.md` durum notunda düzeltildi.
- Faz notları ve planlama bundan sonra **20 ders** üzerinden konuşur; 8 ders
  yalnızca sıradaki **dalga**dır, kalan iş değil.
- `config.workflow.phase_gd_warning_threshold` (15) bu fazda hâlâ aşılmadı
  (7 GD). Eşik aşılırsa uyarı bir **bölme önerisi değil**, bu kararın yeniden
  gözden geçirilmesi için bir tetikleyici sayılır.

## Consequences

**İyi:**
- Faz, vizyonun "portföyünle öğren" bütününü tek kapıda tutuyor; eğitim modülü
  yarım bir çıkış ölçütüyle "bitmiş" sayılmıyor.
- Çıkış ölçütü ile kanonik müfredat (D-017) artık **aynı sayıyı** söylüyor.
  İkisinin ayrışması, bu projede tekrar eden hata sınıfının (künye ↔ gerçek
  uyuşmazlığı; M12/M13'ün varlık sebebi) planlama düzeyindeki hâliydi.

**Maliyet / risk:**
- Faz uzun sürecek ve "faz kapanışı" uzak bir ufuk olacak. Motivasyon CLAUDE.md
  §4'te açık bir kriter ("her fazın sonunda çalışan bir ürün olsun"); bu faz o
  tatmini **faz kapanışıyla değil, set kapanışlarıyla** verir — Set 0'ın
  10/10 kapanışı bunun kanıtı.
- GD sayısı eşiği aşabilir. Aşarsa Doctor/adm-phase uyarır; uyarı bu kararın
  gözden geçirilme anıdır, otomatik bölme değildir.
- **TD-002** (künye → seed yönü) Set 2 bitmeden yazılamıyordu; bu karar o borcun
  ömrünü uzatıyor, çünkü "tüm planlı figürler yazıldı" durumu artık Set 5'in
  sonunda geliyor. Borç satırı bunu yansıtmalı.

## Keywords (for Doctor drift detection)
- PHASE-006
- exit_criteria
- 35 ders
- 20 ders + kavram sözlüğü
- Set 2
- Set 3
- Set 4
- Set 5
- phase_gd_warning_threshold
- D-017
- TD-002
- 16-CURRICULUM.md

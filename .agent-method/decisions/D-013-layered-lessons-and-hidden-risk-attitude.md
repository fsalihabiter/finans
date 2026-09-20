---
id: D-013
title: Katmanlı ders derinliği (L1/L2/L3) ve RiskAttitude'un arayüze çıkmaması
status: proposed
scope: architecture
created: 2026-09-20
supersedes: null
superseded_by: null
---

# D-013 — Seviyeye uyarlanan içerik

## Context
Hedef kullanıcı sıfır bilgiyle geliyor; aynı ders hem yeni başlayana hem
ilerleyene hitap etmeli. Risk tutumu ise kişiselleştirilmiş yönlendirmeye
dönüşme riski taşıyor. (15 §2, §4; SC-E4)

## Decision
`LessonSection.DepthTier {Core,Context,Deep}` + `SectionKind
{Explain,Example,Trap,LiveContext,Source}`. Seviyenin üstündeki katman katlanır
ama herkes açabilir (tavan kapatılmaz). Tanılama testi `LiteracyLevel` ve
`RiskAttitude` üretir; **`RiskAttitude` hiçbir DTO/yanıt/arayüzde görünmez**,
yalnız içerik sırasını etkiler. Setler birbirine sert kilitlenmez.

## Consequences
- Quiz soruları da seviyeye göre filtrelenir (Easy/Medium/Hard).
- `RiskAttitude`'un bir yanıt gövdesinde belirmesi doğrudan ihlaldir
  (ham gövde taramasıyla test edilir).

## Keywords (for Doctor drift detection)
- DepthTier
- SectionKind
- LiteracyLevel
- RiskAttitude

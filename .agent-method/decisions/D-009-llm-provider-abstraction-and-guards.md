---
id: D-009
title: LLM sağlayıcı soyutlaması + yapılandırılmış çıktı + guard + cache
status: proposed
scope: architecture
created: 2026-09-20
supersedes: null
superseded_by: null
---

# D-009 — LLM hattı

## Context
Sağlayıcı seçimi netleşmedi; ücretsiz katmanlar kesinti/429 riski taşıyor;
çıktı yasal sınıra uymak zorunda. (CLAUDE.md §10, 07, 14 §2)

## Decision
Tüm LLM erişimi `ILlmClient` arkasından: Anthropic, OpenRouter veya
yapılandırılmamışsa `NoopLlmClient`. Çıktı daima yapılandırılmış (JSON)
istenir, güvenli parse edilir; `CommentaryLanguageGuard` ve
`CommentaryOutputGuard` yasak dili eler; sonuç cache'lenir; portföy verisi
prompt'a `AnonymizedPortfolioSummary` ile girer.

## Consequences
- Parse hatası uygulamayı çökertmez (fallback metin).
- Yeni LLM özelliği bu hattı yeniden kullanır; yeni guard kuralı buraya eklenir.

> ⚠ **D-018 ile gözden geçirildi (2026-09-20).** Bu karardaki soyutlama, guard
> hattı ve sağlayıcı dalları **geçerliliğini korur**; değişen şey LLM'in üründeki
> *rolü*: artık yorumun kaynağı değil, deterministik çekirdeğin üstünde
> **opsiyonel ve varsayılan kapalı** bir zenginleştirmedir. Gerekçe: SPIKE-001
> ölçümü (yerel modeller GPU'suz üretimde bütçeyi karşılamıyor; şema tutturma
> ~%50; guard'ın yakalayamadığı yanlış atıf riski).

## Keywords (for Doctor drift detection)
- ILlmClient
- NoopLlmClient
- CommentaryOutputGuard
- CommentaryLanguageGuard
- AnonymizedPortfolioSummary

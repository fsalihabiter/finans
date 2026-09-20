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

## Keywords (for Doctor drift detection)
- ILlmClient
- NoopLlmClient
- CommentaryOutputGuard
- CommentaryLanguageGuard
- AnonymizedPortfolioSummary

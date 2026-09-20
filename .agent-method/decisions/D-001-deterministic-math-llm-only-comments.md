---
id: D-001
title: Sayısal hesap kodda deterministik, LLM yalnızca yorumlar
status: proposed
scope: architecture
created: 2026-09-20
supersedes: null
superseded_by: null
---

# D-001 — Sayısal hesap kodda, yorum LLM'de

## Context
Finans uygulamasında yanlış rakam en büyük güven kırıcıdır. LLM'ler sayısal
işlemde halüsinasyon üretebilir. (CLAUDE.md §3.1, 02 §1)

## Decision
Tüm parasal/oransal hesap .NET tarafında deterministik formüllerle yapılır.
LLM'e yalnızca **hazır hesaplanmış** sayılar verilir ve yalnızca yorumlaması
istenir; yeni rakam üretmesi guard'larla engellenir. İstemci hesap yapmaz.

## Consequences
- Hesap mantığı Application/Domain katmanında, DB ve dış API'den izole → birim test kolay.
- Her LLM özelliği önce "sayıyı kim hesaplıyor?" sorusunu yanıtlamak zorunda.
- Çıktı guard hattı (D-009) bu kararın uygulayıcısıdır.

## Keywords (for Doctor drift detection)
- PortfolioCalculationService
- CurrencyConverter
- ScenarioCalculationService
- LlmCommentaryService
- CommentaryOutputGuard
- decimal

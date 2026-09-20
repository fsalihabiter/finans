---
id: D-004
title: Para decimal, biçimleme tek yerde ve tr-TR
status: proposed
scope: naming
created: 2026-09-20
supersedes: null
superseded_by: null
---

# D-004 — decimal + TR biçim

## Context
Kayan noktalı tiplerde yuvarlama hatası parasal veride kabul edilemez; kullanıcı
arayüzü Türkçe biçim bekler. (CLAUDE.md §8, NFR-1/NFR-7)

## Decision
Parasal her değer `decimal`. Yuvarlama yalnız gösterimde. Biçimleme
`@finans/shared/format` üzerinden `tr-TR` (binlik nokta, ondalık virgül).
Backend'de kültür bağımsızlığı için açık `NumberFormatInfo` kullanılır.

## Consequences
- `float`/`double` parasal alanlarda drift işaretidir.
- Her yeni hesap = yeni birim testi (zorunlu).
- Üretim imajı globalization-invariant olduğundan `GetCultureInfo("tr-TR")`
  kullanılamaz (T6.2'de canlıda 500 verdi).

## Keywords (for Doctor drift detection)
- decimal
- NumberFormatInfo
- formatCurrency
- tr-TR

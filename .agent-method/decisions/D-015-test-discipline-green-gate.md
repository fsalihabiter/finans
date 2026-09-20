---
id: D-015
title: Senaryo-önce, test-yanında, yeşil olmadan tamam yok
status: proposed
scope: structure
created: 2026-09-20
supersedes: null
superseded_by: null
---

# D-015 — Test disiplini

## Context
Yanlış rakam kabul edilemez; dış bağımlılıklar kırılgan. (CLAUDE.md §12, 09)

## Decision
Her geliştirme görevi için önce senaryo (Given-When-Then, `09` §5 kataloğuna
SC-ID ile) yazılır; sonra hem birim (parasal hesapta zorunlu) hem
olay/entegrasyon testi eklenir; `dotnet test` + `pnpm test` yeşil olmadan görev
kapanmaz. İçerik kuralları makine testleriyle taranır (M1-M9, M7a/M7b).

## Consequences
- Kırmızı test bırakılmaz; uygulanamayan eşik testi yazılmaz, içerik indikçe açılır.
- ADM karşılığı: gerekli doğrulama kırmızıyken GD tamamlanamaz (INV-07);
  kapsam borcu TEST_DEBT'e yazılır (INV-08).

## Keywords (for Doctor drift detection)
- dotnet test
- vitest
- EducationSeedTests
- SC-

---
id: D-005
title: PostgreSQL + EF Core; testlerde SQLite fixture
status: proposed
scope: architecture
created: 2026-09-20
supersedes: null
superseded_by: null
---

# D-005 — Veritabanı ve ORM

## Context
Ücretsiz, prod-dostu, numeric desteği güçlü bir veritabanı gerekiyordu;
migration disiplini isteniyordu. (02 §2.3, 03)

## Decision
PostgreSQL + EF Core (Npgsql). Şema migration'larla yönetilir. Entegrasyon
testleri `SqliteWebApplicationFactory` ile SQLite üzerinde koşar. Seed
idempotenttir ve **mutabakat** yapar (içerik değişikliği çalışan DB'ye iner).

## Consequences
- Şema değişikliği = migration + seed mutabakat kapısı gözden geçirme.
- SQLite ile Postgres arasındaki davranış farkları canlı teyitle kapatılır
  (eğitim seed'inde birden çok kez canlı Postgres doğrulaması yapıldı).

## Keywords (for Doctor drift detection)
- FinansDbContext
- UseNpgsql
- Migrations
- SeedData
- SqliteWebApplicationFactory

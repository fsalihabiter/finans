---
id: D-003
title: Katmanlı .NET backend (Api → Application → Domain, Infrastructure DI ile)
status: proposed
scope: architecture
created: 2026-09-20
supersedes: null
superseded_by: null
---

# D-003 — Katmanlı backend

## Context
Hesap mantığının test edilebilir, dış bağımlılıkların değiştirilebilir olması
gerekiyordu. (02 §2.1)

## Decision
Dört proje: `Finans.Api` (controller/DI/middleware), `Finans.Application`
(iş mantığı, DTO, arayüzler), `Finans.Domain` (saf entity/kural),
`Finans.Infrastructure` (EF Core, dış API client, servis implementasyonları).
Bağımlılık yönü Api → Application → Domain; Infrastructure arayüzleri
implemente eder. Hedef çatı `net10.0`, çözüm `.slnx`.

## Consequences
- Domain hiçbir şeye bağımlı değil; birim test hızlı.
- Yeni dış bağımlılık önce Application'da arayüz olarak tanımlanır.
- Erken mikroservis yok; modüler monolit.

## Keywords (for Doctor drift detection)
- Finans.Api
- Finans.Application
- Finans.Domain
- Finans.Infrastructure
- DependencyInjection

---
id: D-014
title: Dağıtım self-hosted tek VPS + Docker Compose, açık kaynak yığın
status: proposed
scope: architecture
created: 2026-09-20
supersedes: null
superseded_by: null
---

# D-014 — Dağıtım

## Context
Maliyet en düşük tutulmalı, bağımlılık tek satıcıya kilitlenmemeli. (02 §6, 10 §5)

## Decision
Tek VPS üzerinde Docker Compose: reverse proxy (TLS + rate limit) → stateless
API replikaları → PostgreSQL + Redis; izleme Seq + Prometheus + Grafana.
Container non-root ve minimal imaj. Önce dikey ölçek, sonra yatay replika.

## Consequences
- Uygulama stateless kalmalı (oturum durumu container'da tutulmaz).
- Yönetilen bulut servislerine bağımlılık bu kararı bozar.

## Keywords (for Doctor drift detection)
- docker-compose
- non-root
- Traefik
- Seq
- Grafana

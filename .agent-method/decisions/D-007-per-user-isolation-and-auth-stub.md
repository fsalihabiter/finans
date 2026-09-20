---
id: D-007
title: Per-user veri izolasyonu zorunlu; kimlik geçici olarak X-User-Id
status: proposed
scope: architecture
created: 2026-09-20
supersedes: null
superseded_by: null
---

# D-007 — Veri izolasyonu ve geçici kimlik

## Context
Finansal veri; bir kullanıcının başkasının verisini görmesi kabul edilemez.
Kimlik (JWT) ise Faz 7'ye planlandı. (CLAUDE.md §13, 11 §3)

## Decision
Her veri erişimi `UserId` ile kapsanır; başkasının kaydı 404 döner. Cache
anahtarı `UserId` içerir. Yeni her endpoint IDOR testi ile gelir (SC-13).
Kimlik bugün `X-User-Id` başlığıyla temsil edilir
(`ICurrentUser`/`HttpCurrentUser`), rate limit de bu anahtarla bölümlenir.
JWT'ye geçiş PHASE-007'dedir ve hassas veri fazlarının (Faz 9) kapısıdır.

## Consequences
- `X-User-Id` üretim güvenliği sağlamaz — bu kapı kapanmadan hassas veri fazı açılmaz.
- Kimlik değişimi `ICurrentUser` arkasında izole; çağrı yerleri etkilenmez.

## Keywords (for Doctor drift detection)
- ICurrentUser
- HttpCurrentUser
- X-User-Id
- IDOR
- UserId

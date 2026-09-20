---
id: D-006
title: pnpm monorepo, paylaşılan sözleşme paketi, web birincil yüzey
status: proposed
scope: structure
created: 2026-09-20
supersedes: null
superseded_by: null
---

# D-006 — Monorepo ve ön yüz sırası

## Context
İki ön yüz (web, mobil) tek API tüketecek; geliştirici tek kişi. (CLAUDE.md §3, 02 §3)

## Decision
pnpm workspaces: `backend/`, `packages/shared/`, `web/`, (sonra) `mobile/`.
`@finans/shared` yalnız tip, API istemcisi/hook'ları, tasarım token'ı ve
biçimleyici barındırır — **hesap barındırmaz**. Web birincil yüzeydir; mobil
(React Native/Expo) sonra gelir. RN-for-web kullanılmaz.

## Consequences
- Sunum katmanı platforma özel, sözleşme paylaşılır.
- Mobil başlayana kadar `mobile/` klasörü yok; bu eksiklik drift değildir.

## Keywords (for Doctor drift detection)
- pnpm-workspace
- finans/shared
- packages/shared
- web/src

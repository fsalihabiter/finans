---
id: D-020
title: Demo barındırma — web Vercel, API + PostgreSQL Render
status: accepted
scope: architecture
created: 2026-09-22
supersedes: null
superseded_by: null
---

# D-020 — Demo barındırma: web Vercel, API + PostgreSQL Render

## Context
Web ön yüzü Vercel'e deploy edildi (`finans-web-seven.vercel.app`) ama `/api` 404 döndü:
`web/src/lib/api.ts` `baseUrl: ""` ile aynı origin'e istek atar; bu istekleri geliştirmede
Vite proxy'si, compose'da Caddy iletiyordu. Vercel .NET çalıştıramaz. Kalıcı hedef hâlâ
self-hosted VPS + Docker (CLAUDE.md §13); bu karar erken paylaşılabilir bir demo için.

## Decision
- API, `backend/Dockerfile` ile **Render** web servisi olarak çalışır; PostgreSQL de Render'da.
  Tanım `render.yaml`'da (Blueprint). Sırlar `sync: false`, repoda yok.
- Vercel, `web/vercel.json` rewrite kuralıyla `/api/*` isteklerini Render'a iletir. Aynı
  origin korunur: CORS gerekmez ve `baseUrl: ""` değişmez. SPA fallback da buradadır.
- Render bağlantıyı URI olarak verir (`postgresql://...`). `PostgresConnectionString.Normalize`
  bunu Npgsql biçimine çevirir; anahtar=değer biçimli dizeler değişmeden geçer.
- Kimlik gelene kadar (Faz 7) `Auth__DevUserId` = seed kullanıcısı.

## Consequences
- ⚠ Kimlik doğrulama yok: Render adresini bilen herkes veriyi okuyup yazabilir. Faz 7'ye kadar
  yalnız demo/seed verisiyle kullanılır; gerçek portföy verisi girilmez.
- Ücretsiz katman: servis 15 dk boşta kalınca uyur (ilk istek ~1 dk sürer). Ücretsiz
  Postgres 30 gün sonra silinir.
- Redis, Seq, Prometheus ve Caddy bu kurulumda yok: cache in-memory çalışır, log'lar
  Render konsolunda görülür. `/metrics` Render adresinde herkese açıktır (Vercel üzerinden açık değildir).
- VPS'e geçişte `render.yaml` kaldırılır ve Vercel rewrite hedefi değiştirilir.

## Keywords (for Doctor drift detection)
- render.yaml
- web/vercel.json
- PostgresConnectionString
- Auth__DevUserId

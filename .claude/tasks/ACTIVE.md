# ACTIVE — Aktif Görevler (anlık durum)

> Şu an üzerinde çalışılan / sıradaki görevlerin küçük anlık görüntüsü. Oturum
> başında hook bunu otomatik gösterir. Kaynak plan: [`../docs/08-BACKLOG.md`](../docs/08-BACKLOG.md).
> Bir görev bitince buradan çıkar, backlog'da `[x]` işaretle, TASKLOG'a girdi ekle.

**Aktif faz:** Faz 0 — Hazırlık & İskelet · **Frontend: WEB ÖNCELİKLİ** (monorepo)

## Sıradaki (öncelik sırası)
1. **T0.11 kalan** — Sqlite integration fixture + Playwright iskeleti
   → **Faz 0 TAM kapanış**, sonra Faz 1 (Portföy MVP: T1.1 hesap servisi)

> Mobil **FAZ M**'de (web parası sonrası).

## Tamamlanan (bu oturum)
- T0.1-T0.3, T0.7, T0.8, T0.10: monorepo + .NET (net10.0/slnx) + health + web + canlı zincir
- T0.11 (kısmen): backend integration + web Vitest/RTL
- **T0.4-T0.6b**: EF Core veri katmanı + migration + tutarlı seed (422.970/641.403/+%51,6) — `main`
- **T0.9**: tasarım token'ları + fontlar (DESIGN.md → web, görsel doğrulandı) — `main`
- **T0.12+T0.13**: Serilog+correlation+redaksiyon+health/ready, hata maskeleme,
  CORS allow-list, User Secrets — `main`
- **T0.14**: Docker (Dockerfile non-root + compose); `docker compose up --build`
  ile migrate+seed'li API canlı doğrulandı — dal `feat/docker`

## Devam eden
- (yok)

## Bloke
- (yok)

---
*Güncelleme kuralı: CLAUDE.md §11. Bu dosya kısa kalmalı — detay TASKLOG.md'de,
tam plan 08-BACKLOG.md'de.*

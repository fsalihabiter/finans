# ACTIVE — Aktif Görevler (anlık durum)

> Şu an üzerinde çalışılan / sıradaki görevlerin küçük anlık görüntüsü. Oturum
> başında hook bunu otomatik gösterir. Kaynak plan: [`../docs/08-BACKLOG.md`](../docs/08-BACKLOG.md).
> Bir görev bitince buradan çıkar, backlog'da `[x]` işaretle, TASKLOG'a girdi ekle.

**Aktif faz:** Faz 0 — Hazırlık & İskelet · **Frontend: WEB ÖNCELİKLİ** (monorepo)

## Sıradaki (öncelik sırası)
1. **T0.9** — Tasarım token'ları (`DESIGN.md` → `@finans/shared/theme` TS+CSS) +
   web'de uygula + fontlar (Fraunces/Hanken)
2. **T0.12** — Serilog yapılandırılmış log + `/health` & `/health/ready`
3. **T0.13** — Güvenlik temeli (User Secrets/env + hata maskeleme + CORS allow-list)
4. **T0.14** — Docker (API Dockerfile non-root + compose: api + postgres)
5. **T0.11 kalan** — Sqlite integration fixture + Playwright iskeleti

> Bunlar Faz 0'ı kapatır. Mobil **FAZ M**'de (web parası sonrası).

## Tamamlanan (bu oturum)
- T0.1 `.gitignore` · T0.2 monorepo+`@finans/shared` · T0.3 .NET (net10.0/slnx)
- T0.7 `/api/health` · T0.8 web iskeleti · T0.10 web→health canlı zincir
- T0.11 (kısmen): backend integration testleri + web Vitest/RTL
- **T0.4-T0.6b**: EF Core veri katmanı + 12 tablo + migration + tutarlı seed
  (422.970/641.403/+%51,6) — dal `feat/data-layer`

## Devam eden
- (yok)

## Bloke
- (yok)

---
*Güncelleme kuralı: CLAUDE.md §11. Bu dosya kısa kalmalı — detay TASKLOG.md'de,
tam plan 08-BACKLOG.md'de.*

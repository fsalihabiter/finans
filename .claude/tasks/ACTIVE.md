# ACTIVE — Aktif Görevler (anlık durum)

> Şu an üzerinde çalışılan / sıradaki görevlerin küçük anlık görüntüsü. Oturum
> başında hook bunu otomatik gösterir. Kaynak plan: [`../docs/08-BACKLOG.md`](../docs/08-BACKLOG.md).
> Bir görev bitince buradan çıkar, backlog'da `[x]` işaretle, TASKLOG'a girdi ekle.

**Aktif faz:** ✅ Faz 0 BİTTİ → **Faz 1 — Portföy Takip MVP** · **WEB ÖNCELİKLİ**

## Sıradaki (öncelik sırası)
1. **T1.9** — Settings (baz para birimi) endpoint `GET/PUT /api/settings`
2. **T1.10** — `@finans/shared`: API tipleri + TanStack Query hook'ları + `formatCurrency/formatPercent` (tr-TR)
3. **T1.11-14** — Web: AppShell/HeroCard + AllocationDonut + Holdings tablo + "Varlık Ekle" formu
4. (kalan) T1.8 BES ekranı · T1.15 cache (per-user kapsam ✓, cache yok)

> Faz 1 ayrıca: T1.8 BES, T1.9 settings, T1.10 shared API/format hook, T1.11-14 web
> (AppShell/HeroCard/Donut/Holdings/Ekle), T1.15 per-user kapsam. Mobil **FAZ M**'de.

## Tamamlanan (bu oturum)
- T0.1-T0.3, T0.7, T0.8, T0.10: monorepo + .NET (net10.0/slnx) + health + web + canlı zincir
- T0.11 (kısmen): backend integration + web Vitest/RTL
- **T0.4-T0.6b**: EF Core veri katmanı + migration + tutarlı seed (422.970/641.403/+%51,6) — `main`
- **T0.9**: tasarım token'ları + fontlar (DESIGN.md → web, görsel doğrulandı) — `main`
- **T0.12+T0.13**: Serilog+correlation+redaksiyon+health/ready, hata maskeleme,
  CORS allow-list, User Secrets — `main`
- **T0.14**: Docker (Dockerfile non-root + compose); `docker compose up --build`
  ile migrate+seed'li API canlı doğrulandı — `main`
- **T0.11**: test altyapısı — Sqlite integration fixture + Playwright iskeleti
  (sağlayıcı-duyarlı model); dotnet 13 + web 2 + shared 8 + e2e 1 yeşil — dal `feat/test-infra`
- **T1.1+T1.2**: `PortfolioCalculationService` (§6 formülleri, saf/yan etkisiz) + 20 birim
  testi (seed seti birebir, altın +%43, dağılım toplamı 1, reel getiri) — SC-01/02/05/06
- **T1.3**: `CurrencyConverter` (saf, ters/çapraz kur, **bölmeyle tam hassasiyet**) +
  `IFxRateProvider`/`EfFxRateProvider` (en güncel kur) + DI — SC-03 unit+integration
- **T1.4+T1.5**: reel getiri enflasyon bağlama (`IInflationRateProvider`) + ort. maliyet
  türetimi (`DerivePosition`, satış ortalamayı bozmaz) — SC-05/SC-06 unit+integration
- **T1.6+T1.7**: Portföy API — Holdings CRUD + `portfolio/summary`; `ICurrentUser` per-user
  izolasyon (IDOR→404), DTO+validasyon (ApiError), fx/enflasyon entegre. SC-01/04/07/13;
  backend **66 yeşil** + canlı PostgreSQL smoke doğrulandı

## Devam eden
- (yok)

## Bloke
- (yok)

---
*Güncelleme kuralı: CLAUDE.md §11. Bu dosya kısa kalmalı — detay TASKLOG.md'de,
tam plan 08-BACKLOG.md'de.*

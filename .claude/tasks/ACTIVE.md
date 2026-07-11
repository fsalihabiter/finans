# ACTIVE — Aktif Görevler (anlık durum)

> Şu an üzerinde çalışılan / sıradaki görevlerin küçük anlık görüntüsü. Oturum
> başında hook bunu otomatik gösterir. Kaynak plan: [`../docs/08-BACKLOG.md`](../docs/08-BACKLOG.md).
> Bir görev bitince buradan çıkar, backlog'da `[x]` işaretle, TASKLOG'a girdi ekle.

**Aktif faz:** ✅ Faz 0-3 · 🚧 **Faz 4 — Hisse Temel Analiz** (T4.1 ✅ Finnhub kararı)
→ sonra **Dalga 1** (Faz 5-6)

**Strateji (2026-07-11):** [`14-PRODUCT-STRATEGY.md`](../docs/14-PRODUCT-STRATEGY.md) —
finansal okuryazarlık vizyonu fazlara işlendi (Faz 5-8 = Dalga 1-3, backlog'da kırılımlı).
Konumlandırma: *"Nirengi sana ne alacağını söylemez; haritayı okumayı öğretir."*

## Sıradaki (öncelik sırası)
1. **T4.4** — Web: Hisse Analizi sayfası — sembol arama + `MetricGrid` + açıklama
   kartları + disclaimer (backend hattı hazır: /metrics + /explain canlı)
2. **Faz 5 başlangıcı: T5.1** — `PortfolioValueHistoryService` (günlük değer serisi + birim test)
3. OSS kalanı — README Analiz ekran görüntüsü tazeleme (artık Anthropic ile kaliteli)

> ✅ **T4.2 + T4.3 bitti (2026-07-11/12):** Finnhub sağlayıcı (3 uç, token başlıkta, kaba bant
> eşikleri KODDA, 1s cache) canlı teyitli (AAPL/MSFT gerçek veriyle; anahtar .env'de). /explain:
> StockExplainPrompts (iki yönlü çerçeve, uydurma yasağı) + paylaşılan parse/bekçi hattı +
> sembol bazlı 24s cache; canlıda Anthropic ile 5 eğitici kart. Faz 4 backend TAMAM — kalan tek iş web UI (T4.4).

## Ortam notu (2026-07-11)
- **Birincil ortam: Docker compose** → `docker compose up -d --build` → **https://localhost**
  (web SPA Caddy imajında; gerçek veri compose Postgres'inde — yerelden taşındı).
- Yerel `dotnet run` + `pnpm dev` yalnız geliştirme sandbox'ı (ayrı DB, eski kopya).
- Web kodu değişince: `docker compose up -d --build caddy`.

## Son kilometre taşları (detay: TASKLOG)
- **2026-07-11:** Motion katmanı 3 (sayaç/donut çizimi/sparkline/bar+hover; reduced-motion
  bilinçli kapalı) · BES katkı geçmişi teşhisi + etkin oran rozeti · tek komut Docker +
  veri taşıma · SETUP.md yalnız-Docker sadeleştirme · strateji dokümanı + faz planı işlendi.
- **2026-07-10:** Analiz sayfası canlı (çalışan ücretsiz model: nemotron; iki-backend tuzağı bulundu).
- **2026-06-20:** T4.1 — Finnhub kararı (ayrıntı: 08-BACKLOG Faz 4 notu).

## Devam eden / Bloke
- (yok)

---
*Güncelleme kuralı: CLAUDE.md §11. Bu dosya kısa kalmalı — detay TASKLOG.md'de,
tam plan 08-BACKLOG.md'de.*

# ACTIVE — Aktif Görevler (anlık durum)

> Şu an üzerinde çalışılan / sıradaki görevlerin küçük anlık görüntüsü. Oturum
> başında hook bunu otomatik gösterir. Kaynak plan: [`../docs/08-BACKLOG.md`](../docs/08-BACKLOG.md).
> Bir görev bitince buradan çıkar, backlog'da `[x]` işaretle, TASKLOG'a girdi ekle.

**Aktif faz:** ✅ **Faz 0-4 TAMAM** (Faz 4 kapandı 2026-07-12: /hisse canlı) →
🚧 **Dalga 1 devamı: Faz 5** (fiyat geçmişi → Değer Seyri + Senaryo v1)

**Strateji (2026-07-11):** [`14-PRODUCT-STRATEGY.md`](../docs/14-PRODUCT-STRATEGY.md) —
finansal okuryazarlık vizyonu fazlara işlendi (Faz 5-8 = Dalga 1-3, backlog'da kırılımlı).
Konumlandırma: *"Nirengi sana ne alacağını söylemez; haritayı okumayı öğretir."*

## Sıradaki (öncelik sırası)
1. **T5.4** — Senaryo v1 (geçmişe dönük "dursaydı/almasaydım" karşılaştırması; tahmin YOK,
   kalıcı disclaimer) — ScenarioPage ComingSoon → gerçek sayfa; **Faz 5 DoD kapanışı**
2. OSS kalanı — README ekran görüntüleri tazeleme (pano artık gerçek Değer Seyri ile)

> ✅ **T5.1–T5.3 bitti (2026-07-12):** günlük değer+maliyet serisi (saf servis) →
> `GET /api/portfolio/history` (canlı teyit) → **web Değer Seyri canlı**: pano kartı
> (son 1 yıl, compact) + Performans dönem seçicili iki-serili grafik (değer + kesikli
> yatırılan; "geçmiş, tahmin değil" notu). **Bonus (SC-34):** özet bayat AvgCost
> düzeltmesi — pano maliyet KPI'sı 646.635→522.385 (özet = liste = seri).

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
- **2026-07-12:** Yorum gezgini yeni görünüm — solda dikey başlık rayı, ≤720px'te accordion
  (Analiz + Hisse, tek bileşen; web 76/76) · T4.5: fiyat geçmişi grafiği + Faz 4 kapanışı.
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

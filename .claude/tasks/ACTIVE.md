# ACTIVE — Aktif Görevler (anlık durum)

> Şu an üzerinde çalışılan / sıradaki görevlerin küçük anlık görüntüsü. Oturum
> başında hook bunu otomatik gösterir. Kaynak plan: [`../docs/08-BACKLOG.md`](../docs/08-BACKLOG.md).
> Bir görev bitince buradan çıkar, backlog'da `[x]` işaretle, TASKLOG'a girdi ekle.

**Aktif faz:** ✅ **Faz 0-5 TAMAM** (Faz 5 kapandı 2026-07-12: Değer Seyri + Senaryo v1 canlı) →
🚧 **Dalga 1 finali: Faz 6** (Eğitim MVP + kavram sözlüğü — vizyonun kalbi)

**Strateji (2026-07-11):** [`14-PRODUCT-STRATEGY.md`](../docs/14-PRODUCT-STRATEGY.md) —
finansal okuryazarlık vizyonu fazlara işlendi (Faz 5-8 = Dalga 1-3, backlog'da kırılımlı).
Konumlandırma: *"Nirengi sana ne alacağını söylemez; haritayı okumayı öğretir."*

## Sıradaki (öncelik sırası)
1. **T5E.2** — Eğitim seed'i: "Temeller" track'i + 5 ders + ders-1 quiz'i + örnek
   ilerleme (03 §12.5; içerik T6.1 ile birlikte yazılacak)
2. T5E.3 — Eğitim endpoint'leri (tracks/lessons/progress/quiz; `UserId` kapsam + IDOR)
3. T5E.4 — Web Eğitim sayfası (ComingSoon → gerçek: liste + ilerleme + kilit + ders + quiz)
4. OSS kalanı — README ekran görüntüleri tazeleme (pano Değer Seyri + Senaryo artık canlı)

> ✅ **T5E.1 bitti (2026-07-12):** eğitim şeması canlı — 11 tablo (track/ders/bölüm/
> ön-koşul/kavram/quiz/ilerleme/deneme), CHECK+unique kısıtları, KVKK kaskadları,
> migration gerçek Postgres'e uygulandı (SC-38 integration 4).

> ✅ **FAZ 5 KAPANDI (2026-07-12, T5.1→T5.4):** günlük değer+maliyet serisi (saf servis) →
> `GET /api/portfolio/history` → web Değer Seyri (pano + Performans, hover tooltip) →
> **Senaryo v1 canlı**: "nakitte dursaydı" karşılaştırması + alım gücü eşiği (üç çizgi,
> tahmin YOK). Bonus (SC-34): özet bayat AvgCost düzeltmesi (özet = liste = seri).

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

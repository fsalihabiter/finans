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
1. **T5E.4b** — Kavram derin bağlantı: Analiz/Hisse kartından `ConceptTag` → `/egitim` ilgili ders
   (`getLessonsByConcept` istemcisi hazır; kartlardan tetikleme kaldı)
2. T6.1 — İlk müfredat içeriği (5 dersin derin gövdesi + "Senin portföyünde" şablonu)
3. OSS kalanı — README ekran görüntüleri tazeleme (pano + Eğitim artık canlı)

> ✅ **T5E.4 çekirdek bitti (2026-07-17):** Eğitim sayfası canlı (ComingSoon kalktı) — "Temeller"
> seti + ilerleme çubuğu + kilit/tamamlandı rozetleri + sayfa-içi ders okuma (**`MiniMarkdown`** —
> güvenli, lib'siz) + mini test (tam-eşleşme; sonuçta doğru şık+açıklama açılır). Shared tipler+istemci
> +hook'lar eklendi. SC-45 web 101/101; **canlı Postgres uçtan uca teyit** (api+caddy rebuild →
> /egitim seed'le birebir). Kalan: ConceptTag derin bağlantı → T5E.4b.

> ✅ **T5E.3 bitti (2026-07-16):** eğitim uçları canlı — `api/education` 6 uç (tracks·track-lessons·
> lesson-detay·progress-upsert·quiz-attempt·by-concept). Kilit ön-koşuldan türetilir; içerik açık,
> durum/ilerleme+deneme `UserId` kapsamlı (per-user izolasyon testli); quiz cevap-anahtarı yalnız
> deneme sonucunda açılır. SC-44 integration 12/12. **Canlı Postgres'te teyit edildi (T5E.4 ile).**

> ✅ **T5E.2 bitti (2026-07-16):** eğitim seed'i canlı — "Temeller" track + 5 ders
> (sıralı ön-koşul zinciri) + 6 kavram etiketi + Ders 1'e 3 soruluk quiz + örnek ilerleme
> (1-3 Tamamlandı·4 Devam·5 türetilmiş Kilitli). **Portföyden bağımsız idempotent** →
> çalışan DB'ler bir sonraki `Database:Seed` açılışında eğitimi de alır (re-migrate gerekmez).
> İçerik kısa/eğitici (derinleştirme T6.1). SC-43 integration 6/6.

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
- **2026-07-13:** Araç kutusu bakımı (finans ürününe dokunmaz) — CLAUDE.md **§14 model
  yönlendirme tablosu + kuralı** eklendi (Fable5/Opus4.8/Sonnet5/Haiku4.5/yerel nemotron);
  git geçmişindeki **Claude co-author trailer'ı temizlendi** (filter-branch + force-push;
  bundan sonra eklenmiyor — memory); **SC-42 FX yarışı işi commit'lendi** (`046d7e3`).
- **2026-07-12:** Pano Değer Seyri ilk yükleme FX yarışı düzeltmesi (SC-42) — pano kartına
  `isError` dalı (hata boş-veri gibi maskelenmez) + fiyat tazelemesi `portfolio-history`'yi
  de invalidate eder + backend kur yokken tazelemeyi kendisi tetikler / 500 yerine 502
  (`MissingFxRateException`); aynı desen `ScenarioService`'e de uygulandı (/scenario;
  FxRace integration 4/4); backend + web süitleri yeşil ·
  T5.5+T5.6 kenar-durum düzeltmeleri — fiyatsız kalem özete **maliyetiyle**
  girer (sahte −%100 yok; özet = seri) + kronolojik aşırı satış yazmada 400
  (`FirstOversoldDate`; seri negatife düşemez); SC-40/41, backend 394 test yeşil ·
  Yorum gezgini yeni görünüm — solda dikey başlık rayı, ≤720px'te accordion
  (Analiz + Hisse, tek bileşen; web 76/76) · T4.5: fiyat geçmişi grafiği + Faz 4 kapanışı.
- **2026-07-11:** Motion katmanı 3 (sayaç/donut çizimi/sparkline/bar+hover; reduced-motion
  bilinçli kapalı) · BES katkı geçmişi teşhisi + etkin oran rozeti · tek komut Docker +
  veri taşıma · SETUP.md yalnız-Docker sadeleştirme · strateji dokümanı + faz planı işlendi.
- **2026-07-10:** Analiz sayfası canlı (çalışan ücretsiz model: nemotron; iki-backend tuzağı bulundu).
- **2026-06-20:** T4.1 — Finnhub kararı (ayrıntı: 08-BACKLOG Faz 4 notu).

## Devam eden / Bloke
- (bloke yok) — T5E.2 tamam, commit'lenecek.
- **Sıradaki gerçek iş:** T5E.3 — Eğitim endpoint'leri (tracks/lessons/progress/quiz; IDOR).
- ⚠ **Bilinen (görev dışı):** `ObservabilitySecurityTests`'te web host başlatan 3 test bu
  ortamda `WebApplicationFactory<Program>` ayağa kalkmadığı için kırmızı (T5E.2 öncesinden;
  düz factory/DB'siz — seed ile ilgisiz). T5E.3 endpoint testleri SQLite fixture'ıyla koşar.

---
*Güncelleme kuralı: CLAUDE.md §11. Bu dosya kısa kalmalı — detay TASKLOG.md'de,
tam plan 08-BACKLOG.md'de.*

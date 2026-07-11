# TASKLOG — Çalışma Günlüğü

> Append-only, **en yeni en üstte** kronolojik kayıt. Projeyle yapılan her
> anlamlı iş (kod, doküman, karar, düzeltme) buraya bir girdi bırakır. Protokol:
> `CLAUDE.md` §11 ve [`README.md`](README.md). Sohbet/soru-cevap turları kayıt
> gerektirmez — yalnızca **bir şey değiştiğinde** yaz.

**Girdi şablonu** (kopyala, en üste ekle):

```
## YYYY-AA-GG · <kısa başlık>
- **Görev(ler):** <08-BACKLOG ID'leri, örn. T0.2> | (plan dışıysa: ad-hoc)
- **Ne yapıldı:** <1-3 madde, somut>
- **Dokunulan dosyalar:** <yol listesi>
- **Test:** <yazılan/geçen testler + senaryo ID'leri (09 §5), örn. "SC-01 unit+integration ✓ / dotnet test yeşil" | yok (test gerektirmez)>
- **Karar/Not:** <varsa kalıcı karar — ilgili dokümana da işle>
- **Durum:** tamamlandı | devam ediyor | bloke (<sebep>)
- **Sıradaki:** <bir sonraki somut adım>
```

---

## 2026-07-11 (6) · ad-hoc — Strateji planlama dokümanlarına işlendi (Faz 5-8 = Dalga 1-3)
- **Görev(ler):** ad-hoc (kullanıcı: "14-PRODUCT-STRATEGY gerekliliklerine göre fazları/
  taskları/dokümanları düzenle") — strateji önceliklendirmesi böylece onaylanmış oldu.
- **Ne yapıldı:**
  1. **08-BACKLOG:** Eski serbest-liste "Faz 5 — Ötesi" bölümü 4 yapılandırılmış faza
     dönüştü: **FAZ 5** (T5.1-T5.4: değer serisi servisi → history endpoint → Değer Seyri
     grafiği → Senaryo v1), **FAZ 6** (T5E.1-4 korunarak + T6.1-T6.4: müfredat içeriği,
     "Senin portföyünde" bağlam API'si, kavram sözlüğü, ilerleme mekaniği), **FAZ 7**
     (T7.1-T7.9: onboarding/seviye, kimlik, PWA, bildirim, TEFAS, altın modülü, demo mod,
     OSS anlatısı, "neden görüyorum" şeffaflığı), **FAZ 8** (T8.1-T8.8: davranış aynası
     ⚠hukukla birlikte, enflasyon paneli, senaryo tam, mobil, gelir+hukuk kapısı, güvenlik
     tamamlama, işbirlikleri, yeni varlık türleri). Her faza DoD; başlık durumu Dalga
     özetiyle güncellendi. Eski listedeki kimlik/güvenlik/bildirim maddeleri T7.2/T7.4/
     T8.6'ya taşındı (kayıp yok).
  2. **ROADMAP.md:** genel bakış tablosuna Faz 5-8 satırları; "PHASE 5 — Beyond" bölümü
     4 ayrı faz bölümüne (amaç + kapsam + DoD + backlog görev referansı) yeniden yazıldı.
  3. **CLAUDE.md:** §4 faz planına Faz 5-8 dalgaları; §9 "Sıradaki Adım" (proje başından
     kalma bayat "şema tasarımı" metni) güncel duruma çekildi.
  4. **ACTIVE.md:** kendi "kısa kal" kuralına göre sıfırdan (225→~45 satır): sıradaki 5
     görev, ortam notu (Docker birincil), son kilometre taşları. Eski faz özetleri zaten
     TASKLOG'da.
  5. **14-PRODUCT-STRATEGY.md** durum satırı: Taslak → ✅ Onaylandı/işlendi.
- **Dokunulan dosyalar:** `.claude/docs/08-BACKLOG.md`, `ROADMAP.md`, `CLAUDE.md`,
  `.claude/tasks/ACTIVE.md`, `.claude/docs/14-PRODUCT-STRATEGY.md`, `.claude/tasks/TASKLOG.md`
- **Test:** yok (planlama dokümanları).
- **Karar/Not:** Dalga 1 sırası kesinleşti: **T4.2 → T4.3 → T4.4 → T5.1...** T8.1
  (davranış aynası) ve T7.4 (bildirim) hukuk merceğiyle birlikte tasarlanacak (14 §6).
- **Durum:** tamamlandı.
- **Sıradaki:** T4.2 — `StockDataService` + `GET /api/stocks/{symbol}/metrics` (Finnhub).

## 2026-07-11 (5) · ad-hoc — SETUP.md sadeleştirildi: yalnız Docker akışı
- **Görev(ler):** ad-hoc (kullanıcı isteği: "SETUP sadece Docker üzerinden, kalabalık
  olmasın; önce gerekli programlar/versiyonlar/kurulum, sonra PowerShell komutları").
- **Ne yapıldı:** SETUP.md sıfırdan yazıldı (308 → ~120 satır): gereksinim yalnız
  Git + Docker Desktop (winget komutlarıyla), kod alma + `.env`, tek komut ayağa
  kaldırma, günlük komutlar tablosu, servis listesi, sorun giderme tablosu. Dev
  (hot-reload) akışı 06-DEV-PLAYBOOK'a işaret eden tek nota indirildi. README
  "Getting started" yeni akışla hizalandı (pnpm önkoşulu kalktı); `.env.example`
  LLM model örneği çalışanla değiştirildi (nemotron; llama-3.3 kalıcı 429 notu).
- **Dokunulan dosyalar:** `SETUP.md` (yeniden yazım), `README.md`, `.env.example`,
  `.claude/tasks/TASKLOG.md`
- **Test:** yok (doküman). Komutlar bu oturumda canlı doğrulanmış akışın aynısı.
- **Durum:** tamamlandı.
- **Sıradaki:** Strateji Dalga 1 önceliklendirme onayı.

## 2026-07-11 (4) · ad-hoc — Tek komut Docker: web SPA compose'a girdi + gerçek veri taşındı
- **Görev(ler):** ad-hoc (kullanıcı kararı: "her şey Docker'dan kalksın").
- **Ne yapıldı:**
  1. **Web compose'a girdi:** `compose/caddy/Dockerfile` (context: repo kökü) — aşama 1
     node:22-alpine + pnpm@11.5.0 ile `@finans/web` build; aşama 2 caddy:2-alpine'e
     `/srv` olarak gömülür. Caddyfile fallback'i bilgi mesajından **SPA sunumuna** döndü
     (`root /srv` + `try_files → index.html`). Kök `.dockerignore` eklendi (node_modules/
     backend/sırlar bağlam dışı; web/index.html korunur).
  2. **Postgres 17→18-alpine** (yerel PostgreSQL 18 ile sürüm eşleşmesi). 18 imajında
     volume bağlama noktası değişti: `/var/lib/postgresql/data` → `/var/lib/postgresql`
     (eski yol 18'de başlatma hatası veriyor — compose güncellendi).
  3. **Veri taşıma:** yerel DB'den `pg_dump --no-owner --no-privileges` (parola User
     Secrets'ten belleğe, ekrana yazılmadı) → compose volume sıfırlandı (demo veri
     silindi — kullanıcı onaylı) → psql ile geri yükleme. Doğrulama: BesContributions 48,
     Holdings 7, Transactions 10, Users 2, PriceSnapshots 50.
  4. **Doğrulama (Caddy üzerinden):** `https://localhost` → SPA 200 (`<title>Nirengi`),
     derin URL fallback 200, `/health` Healthy, `/api/holdings` 48 katkı + planActive.
     Seed, dolu DB'de atlıyor (Users.Any kontrolü) — migrate idempotent.
  5. Yerel arka plan süreçleri (dotnet 5298 + Vite 5173) durduruldu; **SETUP.md** §0/§4
     yeniden yazıldı (A yolu birincil kullanım, ayrı-veritabanı uyarısı, web rebuild
     talimatı); hafıza `local-dev-database` güncellendi (asıl veri artık compose'da).
- **Dokunulan dosyalar:** `compose/caddy/Dockerfile` (yeni), `compose/caddy/Caddyfile`,
  `docker-compose.yml`, `.dockerignore` (yeni), `SETUP.md`, `.claude/tasks/TASKLOG.md`
- **Test:** kod değişmedi (altyapı); uçtan uca canlı doğrulama yapıldı (yukarıda).
- **Karar/Not:** Birincil ortam = compose (`https://localhost`); yerel dev (dotnet+Vite)
  yalnız kod yazarken, ayrı sandbox DB ile. Web değişikliği sonrası
  `docker compose up -d --build caddy` gerekir. Dump dosyası scratchpad'te (repo dışı).
- **Durum:** tamamlandı.
- **Sıradaki:** Kullanıcı tarayıcıda sertifikayı bir kez kabul edecek; strateji Dalga 1
  önceliklendirmesi bekleniyor.

## 2026-07-11 (3) · ad-hoc — "BES aylık katkılar kayboldu" teşhisi + geçmişe etkin oran rozeti
- **Görev(ler):** ad-hoc (kullanıcı: "aylık ödemeler önceden vardı şimdi yok; geçmişte
  devlet katkısı miktarı ve ORANI da görünmeliydi").
- **Kök neden (veri kaybı DEĞİL):** Vite dev sunucusu dünkü Analiz çalışmasından beri
  `VITE_API_TARGET` ile **compose yığınına** (Caddy, ayrı Postgres) gidiyordu; o DB'de
  yalnız "Açılış" kaydı var. **Yerel dev DB'de her şey yerinde:** 48 katkı, aktif plan
  (7.500 ₺/ay, gün=1), Temmuz katkısı ödeme gününe göre otomatik eklenmiş (StatePending)
  — otomatik ekleme çalışıyor. (Tuzak TASKLOG 2026-07-10 (6)'da öngörülmüştü.)
- **Çözüm:** Compose'a bakan Vite süreci durduruldu, `VITE_API_TARGET`siz yeniden
  başlatıldı (varsayılan http://localhost:5298); yerel API `dotnet run` ile ayağa
  kaldırıldı. Doğrulama: 5173 üzerinden Via başlığı yok + 48 katkı dönüyor; tarayıcıda
  aylık satırlar + durum şeritleri görünür.
- **Yeni özellik (oran):** `BesContributionHistory` devlet sütununa **etkin oran rozeti**
  (`stateAmount/ownAmount`, `%20`/`%30`) satır + ödenmiş toplam için eklendi (`.hist-rate`).
  Oran değişikliği (2026-01: %30→%20) geçmişte satır satır okunuyor — eğitici değer.
- **Dokunulan dosyalar:** `web/src/components/BesContributionHistory.tsx` (+test),
  `web/src/App.css`, `.claude/tasks/TASKLOG.md`
- **Test:** BesContributionHistory 5/5 yeşil (yeni: satır + toplam oran rozeti).
- **Karar/Not:** İki ortam (yerel dev + compose) aynı anda ayaktayken tarayıcının hangi
  DB'ye baktığı Vite hedefine bağlı — kalıcı çözüm için tek ortam kararı/veri taşıma
  ayrı görev olarak önerildi.
- **Durum:** tamamlandı.
- **Sıradaki:** Kullanıcı strateji dokümanındaki Dalga 1 önceliklendirmesini bildirecek.

## 2026-07-11 (2) · ad-hoc — Ürün stratejisi dokümanı: finansal okuryazarlık vizyonu
- **Görev(ler):** ad-hoc (kullanıcı isteği: "uzman olarak incele; neler eklenirse piyasada
  öne çıkar; ulusal düzeyde yön verici, bilinçlendiren, finans okuryazarlığı kazandıran
  bir ilerleyiş").
- **Ne yapıldı:** `.claude/docs/14-PRODUCT-STRATEGY.md` yazıldı: konumlandırma ("Nirengi
  haritayı okumayı öğretir"), mevcut durum güçlü/boşluk analizi, TR rekabet boşluğu,
  4 katmanlı özellik önerisi (A: okuryazarlık çekirdeği — bağlamsal eğitim/onboarding/
  şeffaflık/sözlük/davranış aynası; B: TR'ye özgü — enflasyon/TEFAS/altın kültürü/BES;
  C: ürünleşme — fiyat geçmişi/kimlik/PWA/bildirim/veri dayanıklılığı; D: ulusal etki —
  demo mod/açık kaynak/işbirlikleri/içerik), tarafsızlığı bozmayan gelir modelleri,
  yasal hatırlatmalar, 3 dalgalı öncelik planı, "DAU değil öğrenme" metrik çerçevesi.
  Doküman indeksi (docs/README.md) güncellendi.
- **Dokunulan dosyalar:** `.claude/docs/14-PRODUCT-STRATEGY.md` (yeni),
  `.claude/docs/README.md`, `.claude/tasks/TASKLOG.md`
- **Test:** yok (doküman).
- **Karar/Not:** Öncelik önerisi: Dalga 1 = Faz 4 bitir → fiyat geçmişi/Değer Seyri →
  Eğitim MVP → sözlük. Davranış aynası + bildirimler SPK merceğinde avukat görüşüyle
  tasarlanacak (lansmana bırakılmadan). Kullanıcının dalga önceliklendirmesi bekleniyor.
- **Durum:** tamamlandı (doküman); uygulama görevleri backlog'a kullanıcı onayıyla girecek.
- **Sıradaki:** Kullanıcı Dalga 1 sırasını onaylarsa 08-BACKLOG'a görev kırılımı.

## 2026-07-11 · ad-hoc — Motion katmanı 3: sayaç, donut çizimi, sparkline, bar/hover mikro-animasyonları
- **Görev(ler):** ad-hoc (kullanıcı isteği: 6 maddelik motion listesi).
- **Ne yapıldı:**
  1. **KPI kartları kademeli fade-in:** `.kpis .kpi` sağdan kayma yerine `fade-up`
     keyframe'i (yukarı süzülerek, 40-190ms stagger korunarak).
  2. **Para değerleri sayaç animasyonu:** yeni `useCountUp` hook'u (easeOutCubic,
     ~900ms; reduced-motion/matchMedia'sız ortamda ANINDA hedef → testler deterministik)
     + `CountUpCurrency` bileşeni. Uygulandı: KpiGrid (4 para değeri), varlık detay
     hero'su, BES projeksiyon hero'su. Gösterim-katmanı; hesap yine backend'de (NFR-1).
  3. **Donut dilim çizimi:** `AllocationDonut` dilimlerine `.alloc-seg` + inline
     `--seg-delay/--seg-dur` (paya orantılı) → `stroke-dasharray` keyframe'iyle çevre
     boyunca sırayla çizilir; kapsayıcı `donut-in` sadeleşti (rotate kalktı).
  4. **Sparkline (yeni bileşen):** `Sparkline.tsx` — path draw (`pathLength=1` +
     dashoffset 1→0) + gecikmeli gradyan alan fade. Gerçek veriyle ilk kullanım: BES
     projeksiyonunun yıllık fon değeri serisi (proj-hero). "Değer Seyri" kartı Faz 2
     fiyat geçmişini beklediği için orada veri yok — bileşen hazır.
  5. **Ağırlık barları genişleme:** `bar-grow` (scaleX, origin left) + satır sırasına
     göre inline gecikme (HoldingsTable); performans barlarında merkezden dışa doğru
     (inline `transformOrigin`, `.pb-fill`).
  6. **Satır hover aksiyonları:** düzenle/sil ikonları `@media (hover: hover)`'da
     satır hover/focus-within'e dek opacity 0; dokunmatikte hep görünür (özellik kaybı yok).
  - A11y: reduced-motion bloğuna `animation-delay`/`transition-delay` sıfırlama eklendi
    (backwards-fill stagger'lar delay boyunca içeriği gizliyordu — mevcut sorun da düzeldi).
- **Dokunulan dosyalar:** `web/src/lib/useCountUp.ts` (+test), `web/src/components/CountUpCurrency.tsx`,
  `web/src/components/Sparkline.tsx` (+test), `web/src/components/KpiGrid.tsx`,
  `web/src/components/AllocationDonut.tsx`, `web/src/components/HoldingsTable.tsx`,
  `web/src/components/BesProjectionForm.tsx`, `web/src/routes/HoldingDetailPage.tsx`,
  `web/src/routes/PerformancePage.tsx`, `web/src/App.css`
- **Test:** `useCountUp` (reduced-motion'da anında hedef ×2) + `Sparkline` (path'ler,
  pathLength, <2 nokta → null, sabit seri NaN yok) yeni; toplam **60/60 yeşil**, `tsc -b` temiz.
- **Karar/Not:** jsdom'da `matchMedia` olmaması bilinçli sözleşme olarak kullanıldı —
  sayaç testte animasyonsuz çalışır, para asserterleri beklemez. Sayaç yalnız gösterim;
  ara değerler hiçbir hesaba girmez.
- **Ek (aynı gün, kullanıcı geri bildirimi "sayaç görünmüyor"):** Sayaç KPI'lardaki TÜM
  sayısal değerlere genişletildi: `CountUpPercent` (getiri, reel getiri, hero yüzdesi,
  detay hero yüzdesi) + `CountUpNumber` (pozisyon sayısı). `CountUpCurrency.tsx` →
  `CountUp.tsx` olarak birleştirildi (3 import güncellendi). **Kök neden:** kullanıcının
  Windows'unda "animasyon efektleri" kapalı → `prefers-reduced-motion: reduce` → sayaç
  (tasarım gereği) hedefe anında atlıyor. Tarayıcıda matchMedia geçici ezilerek kanıtlandı:
  ara değerler ₺153.847→₺549.861→₺726.443→₺843.887,52 örneklendi. Test 63/63 yeşil.
  Not: otomasyon sırasında takılı bir View Transition'ın rAF'i dondurduğu gözlendi
  (InvalidStateError; taze yüklemede sağlıklı) — normal kullanıcı akışında görülmedi, izlenecek.
- **Ek 2 (aynı gün, "her yenilemede çalışsın, göremedim"):** `prefers-reduced-motion`
  desteği KALDIRILDI (kullanıcı kararı): App.css/index.css'teki üç medya bloğu silindi,
  `useCountUp`'ta reduced-motion kontrolü çıkarıldı (yalnız jsdom/test → anında hedef
  sözleşmesi korunuyor). DESIGN.md §Motion güncellendi + hafızaya `ui-animations-always-on`
  yazıldı. Animasyonlar artık OS ayarından bağımsız her açılış/yenilemede/rota geçişinde
  oynar. Otomasyon bulgusu: gizli sekmede rAF çalışmaz → sayaç görünene dek 0 kalır,
  görünür olunca son değere tamamlanır (takılma yok — tasarım gereği doğal). Test 63/63.
- **Durum:** tamamlandı.
- **Sıradaki:** Tarayıcıda görsel doğrulama + commit (kullanıcı onayıyla) → T4.2.

## 2026-07-10 (6) · ad-hoc — Analiz sayfası ÇALIŞIR duruma getirildi (LLM yorum canlı)
- **Görev(ler):** ad-hoc (kullanıcı isteği: "Analiz sayfası çalışsın, ne gerekiyorsa yap").
- **Kök neden zinciri (üç katman):**
  1. Yapılandırılmış model `poolside/laguna-m.1:free` 200 dönüp **boş içerik** bırakıyor
     (reasoning token tuzağı); popüler ücretsiz modeller (qwen/gemma) anlık 429.
  2. Metriklerden teşhis (`finans_llm_tokens_total output=5`): nemotron-NANO reasoning
     kapalıyken sadece `{"cards":[]}` üretiyor → parse 0 kart → fallback.
  3. **Asıl sürpriz:** Vite proxy'nin gittiği yer yerel backend değil — yanıttaki
     `via: 1.1 Caddy` başlığı, 16 saattir ayakta olan **Docker compose yığınını** açığa
     çıkardı. Tarayıcıdaki uygulama compose API'sine (ayrı Postgres + .env LLM configi)
     konuşuyor; oradaki model `llama-3.3-70b:free` (Haziran'dan beri kalıcı 429) →
     kullanıcı hep fallback görüyordu. (Pozisyon sayısı tutarsızlıklarının da açıklaması:
     iki ayrı veritabanı.)
- **Çözüm:**
  1. Aday ücretsiz modeller gerçek bayraklarla (reasoning exclude+disabled, json_object)
     canlı test edildi → **`nvidia/nemotron-3-super-120b-a12b:free`** tam JSON + 5 kart üretti.
  2. Yerel dev: User Secrets `Llm:Model` güncellendi. **Compose: `.env` LLM_MODEL güncellendi**
     + yinelenen placeholder `LLM_API_KEY` satırı silindi; `docker compose up -d api` ile
     container yeniden oluşturuldu (DB volume korunur). KOD DEĞİŞMEDİ.
  3. Doğrulama: proxy üzerinden `source: llm | 5 kart`; sayfada "LLM tarafından üretildi"
     rozeti + yoğunlaşma metre'li kartlar; sayılar birebir doğru (842.276 TL, %65,6, %41,8/%2,7).
- **Dokunulan dosyalar:** `.env` (repo dışı), User Secrets (repo dışı), `SETUP.md` (örnek model),
  `README.md` + `docs/assets/analysis.png` (yeni görsel), `.claude/tasks/TASKLOG.md`
- **Test:** yok (kod değişmedi — yapılandırma). Canlı uçtan uca doğrulama yapıldı.
- **Karar/Not:** Ücretsiz katman modelleri kırılgan (429/boş içerik) — kalıcı çözüm için
  Anthropic anahtarı veya OpenRouter kredisi düşünülebilir. Prometheus LLM metrikleri
  (T3.9) teşhiste birebir işe yaradı. Vite hedefi hangi ortama bakıyorsa tarayıcı ORAYI
  kullanır — iki backend aynı anda ayaktayken karışıklığa dikkat.
- **Durum:** tamamlandı.
- **Sıradaki:** README analiz görseli commit → T4.2.

## 2026-07-10 (5) · ad-hoc — Motion katmanı 2: kart giriş/çıkış + toast/modal keyframe'leri
- **Görev(ler):** ad-hoc (kullanıcı isteği: "her kart için giriş/çıkış efekti + notification keyframe + daha hareketli site").
- **Ne yapıldı:**
  1. **Rota çıkış/giriş animasyonu — View Transitions API:** tüm `NavLink`'lere `viewTransition`
     prop'u (8 adet); CSS `::view-transition-old/new(root)` → eski sayfa yukarı süzülüp çıkar
     (220ms), yenisi alttan yükselir (300ms). Desteklemeyen tarayıcıda zarif düşüş (yalnız giriş
     stagger'ı). reduced-motion için ayrı kapatma (global `*` bloğu pseudo'ları kapsamıyordu).
  2. **`lib/viewTransition.ts`:** `withViewTransition(update)` yardımcı — destek varsa
     `document.startViewTransition`, yoksa (eski tarayıcı + jsdom) SENKRON çağrı. Bu sayede
     Modal/Toast testlerinin senkron `onClose` beklentileri kırılmadı.
  3. **Modal giriş/çıkış:** giriş `overlay-in` (perde) + `modal-pop` (yay easing, %60'ta
     hafif taşma) keyframe'leri; kapanış (X/overlay/Escape/Vazgeç) Modal.tsx +
     AddHoldingDialog.tsx'te `withViewTransition`'a sarıldı → tarayıcıda yumuşak çıkış.
  4. **Toast yaşam döngüsü keyframe'leri:** `toast-in` (sağdan yaylanarak, 340ms) +
     `toast-out` (sağa süzülerek, 260ms). Otomatik kapanışta timer sürümlü `leaving` durumu
     (EXIT_MS önce sınıf, sonra silme — jsdom animasyon olayı üretmediği için davranış
     animasyona bağlanmadı); manuel kapatma View Transition'lı.
  5. **Kart canlılığı:** `.card:hover` translateY(-3px) kalkış + `.kpi:hover`; hero kart
     glow'una `glow-breathe` (5.5s nefes) eklendi.
  6. **(Revizyon — kullanıcı isteği) SLIDE düzeni:** rota geçişi dikey süzülme yerine yatay
     kayma (eski sayfa sola -56px çıkar, yeni sağdan +64px girer); kart stagger girişi de
     sağdan kayma (+28px) — toast'ların sağdan giriş/çıkışıyla tutarlı tek yön dili.
  7. **(Revizyon 2 — kullanıcı isteği) Daha yavaş + smooth:** modal pop → alttan slide
     (`modal-slide-up` 560ms, %70'te -4px taşma; perde 340ms), toast-in 340→580ms /
     toast-out 260→440ms (Toast.tsx `EXIT_MS=440` eşitlendi), rota slide 340/480ms +
     mesafeler -72/+88px, kart stagger 540/560ms. Easing sabit: cubic-bezier(0.16,1,0.3,1).
  8. **(Revizyon 3 — kullanıcı kararı) TAM GENİŞLİK gövde:** `.app-content` 1320px sütun
     ortalaması kaldırıldı → `--gutter: clamp(22px, 2.5vw, 44px)` sabit kenar boşluğu;
     `.detail` 1100px sınırı kaldırıldı. 1920px'te canlı doğrulandı (dashboard + BES detay).
  9. **(Revizyon 4 — kullanıcı geri bildirimi) Sakin accent:** parlak indigo #6E7CFF göz
     yoruyordu → desatüre **#8A94DC** (accentSoft #A6AEE8; koyu gradyan uçları #555EB0/#6069BD).
     Kontrast yükseldi (4.9→6.0:1). Sweep: token+test+App.css rgba'ları+index.css (seçim,
     ambient blob)+favicon+banner+DESIGN.md. shared 16 + web 54 ✓, build temiz, canlı doğrulandı.
  10. **README görselleri final tasarımla tazelendi** (kullanıcı isteği): banner.svg zaten
     v2+sakin accent'teydi (görsel doğrulandı); 4 PNG (dashboard, dashboard-notes,
     performance, bes-detail) sakin indigo + tam genişlik düzeniyle yeniden çekildi.
- **Dokunulan dosyalar:** `web/src/lib/viewTransition.ts` (yeni), `web/src/components/{Toast,Modal,AddHoldingDialog}.tsx`,
  `web/src/{App.tsx,App.css}`, `.claude/tasks/TASKLOG.md`
- **Test:** web 54/54 ✓ · build temiz ✓ · canlı doğrulama (kayıt turu: sayfa geçişleri + modal aç/kapa).
- **Karar/Not:** Çıkış animasyonları React unmount'una bağlanmadı (test senkronluğu bozulurdu);
  bunun yerine tarayıcı-native View Transitions kullanıldı — Chromium'da tam efekt, diğerlerinde
  animasyonsuz ama davranış aynı. Ek bağımlılık yine yok.
- **Durum:** tamamlandı (commit onayı bekliyor — 2/3/4/5 turları birlikte).
- **Sıradaki:** commit → T4.2.

## 2026-07-10 (4) · ad-hoc — TEMA v2 "Gece": gece mavisi + indigo + motion katmanı
- **Görev(ler):** ad-hoc (kullanıcı isteği: "animasyonlu/motionlı, mevcut paletten farklı yeni tasarım").
- **Ne yapıldı:** (ui-ux-pro-max `--design-system --motion 7` → "Modern Dark (Cinema)" stili;
  öneriler marka bağlamına uyarlanıp WCAG kontrastları node script'iyle doğrulandı)
  1. **Palet v1→v2** (`packages/shared/src/theme`): kömür+altın → **gece mavisi (#0B0F1E,
     saf siyah değil) + indigo vurgu (#6E7CFF)**. `gold/goldSoft` vurgu token'ları →
     `accent/accentSoft`; `gold` artık yalnız kategorik Altın varlığı rengi. Tüm kategorikler
     soğuk zemine uyarlandı (usd/eur/fx/stock/fund/bes/cash). Metin/accent/muted çiftleri
     doğrulandı (accent 4.9:1, kategorikler ≥6.5:1).
  2. **Tipografi:** Fraunces/Hanken → **Space Grotesk (display) + Inter (gövde)**
     (@fontsource-variable, self-hosted, latin-ext). tabular-nums korunur.
  3. **App.css tam sweep:** 50× var(--gold)→var(--accent), 20× altın rgba→indigo rgba,
     15+ sıcak hex→gece eşdeğeri, mint/coral rgba güncellendi, gradyan uçları (#9c6f2e/
     #a9762f→indigo), skeleton shimmer, modal scrim, topbar camı, takvim ikonu filtresi.
  4. **Motion katmanı (yalnız transform/opacity, reduced-motion güvenli):** sayfa içeriği
     45ms stagger'lı rise-in girişi, KPI mikro-stagger, donut scale+rotate açılışı,
     kart hover yüzey/gölge geçişleri, ambient ışık lekeleri (`body::before`, 26s salınım,
     indigo+turkuaz+mor), nav geçişleri. Easing: cubic-bezier(0.16,1,0.3,1).
  5. **Marka yüzeyleri:** BrandMark glyph + favicon.svg + theme-color meta + manifest +
     README banner.svg v2 paletine geçti. README ekran görüntüleri yeniden çekildi.
  6. `DESIGN.md` §1-3 v2 olarak yeniden yazıldı (felsefe/palet/tipografi + motion ilkeleri).
- **Dokunulan dosyalar:** `packages/shared/src/theme/{index.ts,theme.test.ts}`,
  `web/src/{App.css,index.css,main.tsx}`, `web/src/lib/applyTheme.test.ts`,
  `web/src/components/BrandMark.tsx`, `web/{index.html,public/favicon.svg,public/manifest.webmanifest,package.json}`,
  `DESIGN.md`, `docs/assets/{banner.svg,*.png}`
- **Test:** shared 16/16 ✓ · web 54/54 ✓ (applyTheme assert'i v2'ye güncellendi) · build temiz ✓
  · dashboard + modal canlı görsel doğrulama (indigo CTA, görünür form kenarlıkları, ambient glow).
- **Karar/Not:** v1 sıcak tema git geçmişinde (geri dönüş = bu commit'i revert). GSAP gibi
  ek bağımlılık alınmadı — motion tamamen CSS. Emoji→SVG ikon dönüşümü hâlâ ayrı iş.
- **Durum:** tamamlandı (commit kullanıcı onayı bekliyor — 2/3/4 turları birlikte).
- **Sıradaki:** commit → T4.2.

## 2026-07-10 (3) · ad-hoc — ui-ux-pro-max skill kurulumu + etkileşim turu
- **Görev(ler):** ad-hoc (kullanıcı açık talimatı: "bu skill'i kesin kur").
- **Ne yapıldı:**
  1. **Skill kuruldu:** `npm i -g ui-ux-pro-max-cli` + `uipro init --ai claude` →
     `.claude/skills/` altına 7 klasör geldi (ui-ux-pro-max 2 MB + banner-design/brand/
     design/design-system/slides/ui-styling ~1 MB). Python 3.14.5 mevcut, arama motoru çalışıyor.
  2. **`.gitignore`:** 7 üçüncü-taraf skill klasörü ignore edildi (yerel dev aracı — public
     repoya vendored içerik koymuyoruz; isteyen `uipro init` ile kurar).
  3. **Skill sorguları:** `--design-system` (jenerik slate+Fira önerdi → marka kimliği korundu,
     REDDEDİLDİ); `--domain color "luxury gold dark"` bizim kömür+altın ailesini doğruladı;
     `--domain ux` etkileşim kuralları uygulandı:
  4. **index.css etkileşim temel kuralları:** `button` için global `cursor: pointer`
     (22 dağınık kural yerine temel kural), 120-160ms transition token'ları ve
     `:active` basılı geri bildirimi (`scale(0.98)` + brightness — transform, layout kaydırmaz).
- **Dokunulan dosyalar:** `web/src/index.css`, `.gitignore`, `.claude/skills/*` (ignore),
  `.claude/tasks/TASKLOG.md`
- **Test:** web 54/54 ✓ · build temiz ✓.
- **Karar/Not:** Skill'in jenerik palet önerileri marka için geçersiz; değeri kural/checklist
  motorunda. Sonraki UI işlerinde `python .claude/skills/ui-ux-pro-max/scripts/search.py`
  ile domain sorgusu yapılacak. Emoji→SVG ikon dönüşümü hâlâ bilinçli ertelenmiş durumda.
- **Durum:** tamamlandı (commit onayı bekliyor — (2) turuyla birlikte).
- **Sıradaki:** commit → T4.2.

## 2026-07-10 (2) · ad-hoc — UI/UX renk düzenleme turu (ui-ux-pro-max metodolojisi)
- **Görev(ler):** ad-hoc (kullanıcı isteği: "tasarımı güzelleştir, renkleri düzenle").
- **Ne yapıldı:** (skill kurulamadı — otomatik mod npm global kurulumu engelledi; metodoloji
  GitHub'daki SKILL.md'den okunarak uygulandı: 4.5:1/3:1 kontrast, token tek-kaynak,
  kategorik grafik renkleri ayırt edilebilirlik, placeholder/border görünürlüğü)
  1. **Token paleti genişletildi** (`packages/shared/src/theme`): `lineStrong #55493A`
     (form kenarlığı), `textSoft #E0D6C4` (kart paragrafı), `muted2 #6F6557→#82786B`
     (placeholder 3.1→4.1:1), kategorik `fx/stock/fund/eur` eklendi (assetMeta'daki
     kaçak hex'ler token'a taşındı). DESIGN.md §2 senkron + kontrast kuralı bölümü.
  2. **assetMeta artık token tüketiyor** (bileşende ham hex kalmadı). `CURRENCY_COLOR`
     düzeltmeleri: EUR artık BES moruyla AYNI DEĞİL (yeni `--eur #94A7E8`), USD token
     mavisine (#7FB7D6) oturdu. `shade()` + `sliceColors()` yardımcıları eklendi.
  3. **Donut ayırt edilebilirlik:** aynı türden tekrar eden dilimler (iki Fx: Euro+USD)
     deterministik ton varyantı alıyor — lejant ve dilim aynı diziden beslenir.
  4. **App.css:** form kenarlıkları `--line-strong`; `.quickinfo`/`.nudge-tx` tek
     kullanımlık renkleri `--text-soft`'a; `.proj-vesting` palet dışı yeşili (#6AA84F)
     mint'e. **index.css:** `::placeholder` (muted-2), marka uyumlu `::selection`,
     ince sıcak scrollbar (`scrollbar-color`).
  5. **README ekran görüntüleri yeni tasarımla tazelendi** (docs/assets 4 PNG).
- **Dokunulan dosyalar:** `packages/shared/src/theme/{index.ts,theme.test.ts}`,
  `web/src/lib/assetMeta.ts`, `web/src/components/AllocationDonut.tsx`,
  `web/src/{App.css,index.css}`, `DESIGN.md`, `docs/assets/*.png`
- **Test:** shared 16/16 ✓ (yeni token assert'leri) · web 54/54 ✓ · `vite build` temiz ✓
  · canlı görsel doğrulama (dashboard + Varlık Ekle modalı ekran görüntüsü).
- **Karar/Not:** Kontrast ölçümleri node script'iyle yapıldı; mevcut palet zaten sağlamdı
  (çoğu ≥6:1), zayıf noktalar kapatıldı. Emoji-ikon anti-pattern'i (SVG ikon seti) bilinçli
  ertelendi — ayrı, daha büyük bir tur.
- **Durum:** tamamlandı (commit kullanıcı onayı bekliyor).
- **Sıradaki:** commit → T4.2.

## 2026-07-10 · ad-hoc — ROADMAP.md İngilizceye çevrildi (+ faz durumları)
- **Görev(ler):** ad-hoc (OSS hazırlığı 4. tur — kullanıcı isteği).
- **Ne yapıldı:**
  1. `ROADMAP.md` tamamen İngilizce yeniden yazıldı (aynı dosya adı, aynı yapı/bölümler).
  2. Güncelleme: genel bakış tablosuna **Status** kolonu (Faz 0-3 ✅ · Faz 4 🚧 · Faz 5 🔜) ve
     faz başlıklarına durum işaretleri eklendi; T4.1 Finnhub kararı ve "ort. maliyet
     işlemlerden türetilir" kararı ilgili yerlere italik not düşüldü.
  3. Ayrıca dün: GitHub **secret scanning + push protection** repo ayarı API'den açıldı
     (kullanıcı UI'da bulamadı — yeni yeri Settings → Security → Advanced Security);
     public repo tam geçmiş sır denetimi TEMİZ çıktı (bilinen anahtar formatları, gömülü
     parola, yüksek-entropi, silinen dosyalar dahil — yalnız belgelenmiş dev varsayılanları var).
- **Dokunulan dosyalar:** `ROADMAP.md`, `.claude/tasks/TASKLOG.md`
- **Test:** yok (doküman).
- **Karar/Not:** Kök dokümanlardan İngilizce göçü tamamlananlar: README, SETUP, ROADMAP.
  Türkçe kalanlar: CLAUDE.md, DESIGN.md, `.claude/docs/*` (proje-içi mühendislik dokümanları —
  göç "planlandı" durumunda).
- **Durum:** tamamlandı (OSS turu commit'i hâlâ kullanıcı onayı bekliyor).
- **Sıradaki:** OSS turu commit+push → Analiz ekran görüntüsü (LLM kotası) → T4.2.

## 2026-07-09 (3) · ad-hoc — KURULUM.md → SETUP.md (İngilizce + güncel)
- **Görev(ler):** ad-hoc (OSS hazırlığı 3. tur — kullanıcı isteği).
- **Ne yapıldı:**
  1. **`SETUP.md` (İngilizce)** yazıldı — KURULUM.md'nin çevirisi + güncelleme:
     backend portu netleşti (`5xxx` → **5298**), health yanıtları canlı doğrulandı
     (`/api/health` → `{"status":"ok"}`, Caddy `/health` → `Healthy`), test sayısı
     99 → **246** (156 unit + 90 integration), gerçek repo URL'si, PostgreSQL "17+".
  2. **Yeni §8 "Optional: LLM commentary"** — Anthropic/OpenRouter User Secrets ve compose
     `.env` kurulumu, Noop/fallback davranışı, free-tier 429 notu. Sorun giderme bölümüne
     "Analysis page only shows a fallback card" maddesi eklendi. Yol A'ya gözlem yığını
     URL'leri (Seq 8081 / Prometheus 9090 / Grafana 3001, 127.0.0.1) eklendi.
  3. **`KURULUM.md` silindi** (git rm — Türkçe orijinal git geçmişinde duruyor);
     README bağlantısı SETUP.md'ye çevrildi. Repo genelinde başka KURULUM referansı yok.
- **Dokunulan dosyalar:** `SETUP.md` (yeni), `KURULUM.md` (silindi), `README.md`,
  `.claude/tasks/TASKLOG.md`
- **Test:** yok (doküman — kod değişmedi). Health/URL iddiaları canlı sistemde doğrulandı.
- **Karar/Not:** Kurulum rehberinin kanonik adı artık **SETUP.md** (İngilizce, OSS konvansiyonu).
  Türkçe sürüm istenirse `KURULUM.md` git geçmişinden geri alınabilir.
- **Durum:** tamamlandı (commit kullanıcı onayı bekliyor).
- **Sıradaki:** OSS turu commit'i → Analiz ekran görüntüsü (LLM kotası) → T4.2.

## 2026-07-09 (2) · ad-hoc — README görsel yükseltme: banner + canlı ekran görüntüleri
- **Görev(ler):** ad-hoc (OSS hazırlığı 2. tur — kullanıcı isteği: "daha görsel README").
- **Ne yapıldı:**
  1. **`docs/assets/banner.svg`** — DESIGN.md paletiyle (kömür/altın/nane) el yapımı marka banner'ı:
     Nirengi serif wordmark + nirengi üçgeni motifi + sparkline + varlık sınıfı lejantı.
  2. **Canlı ekran görüntüleri** (backend `dotnet run` + `pnpm dev:web` + Playwright/sistem Chrome,
     1440×860 @2x PNG): `dashboard`, `dashboard-notes` (eğitici notlar + varlık tablosu),
     `performance`, `bes-detail` → `docs/assets/`. İşlemler/Eğitim/Senaryo "yakında" ekranı olduğu
     için elendi. **Analiz sayfası alınamadı:** OpenRouter free havuzu 429 + `laguna-m.1:free`
     boş content dönüyor → LLM yorum ekranı sadece fallback gösterdi (görüntü sonra eklenebilir).
  3. **README yeniden kuruldu (görsel ağırlıklı):** ortalanmış banner + logolu rozetler,
     "A look around" galerisi (tam genişlik 2 görsel + 2'li tablo), Mermaid mimari diyagramı
     (ASCII yerine), GitHub `[!IMPORTANT]` tavsiye-değil bloğu, emoji bölüm başlıkları.
  4. Not: Vite dev sunucusu sandbox içinde 5298'e bağlanamıyordu (proxy ECONNREFUSED) —
     servisler sandbox'sız yeniden başlatıldı; ekran görüntüleri gerçek canlı veriyle alındı.
- **Dokunulan dosyalar:** `README.md`, `docs/assets/banner.svg` (yeni), `docs/assets/*.png` (4 yeni),
  `.claude/tasks/TASKLOG.md`, `.claude/tasks/ACTIVE.md`
- **Test:** yok (doküman/görsel — kod değişmedi).
- **Karar/Not:** README görselleri `docs/assets/` altında yaşar (~2,3 MB). LLM yorum ekranının
  görüntüsü için ya OpenRouter kotası beklenecek ya da geçerli bir Anthropic anahtarıyla alınacak.
- **Durum:** tamamlandı (commit kullanıcı onayı bekliyor).
- **Sıradaki:** Analiz sayfası görüntüsü (LLM kota açılınca) → T4.2.

## 2026-07-09 · ad-hoc — OSS hazırlığı: İngilizce README + MIT LICENSE + sır doğrulaması
- **Görev(ler):** ad-hoc (Claude for OSS başvuru hazırlığı — repo 2026-07-08 civarı public yapıldı; paylaşılan claude.ai sohbetindeki eksik listesi).
- **Ne yapıldı:**
  1. **`README.md` (İngilizce)** yazıldı: Nirengi markası, "not investment advice" ilkesi,
     mimari şema, özellik tablosu (✅/🚧/🔜), monorepo yapısı, Docker/lokal kurulum, test ve
     gözlemlenebilirlik özeti, roadmap linki, MIT + disclaimer.
  2. **`LICENSE` (MIT, © 2026 Fatıma Saliha Biter)** eklendi — lisanssız repo teknik olarak açık kaynak sayılmıyordu.
  3. **Sır taraması:** `.env` git'te hiç izlenmemiş (geçmiş dahil doğrulandı); `appsettings*.json`,
     `.env.example`, `.claude/settings.json` temiz — yalnızca placeholder/doküman metni. Public kalabilir.
  4. Kök `package.json` description İngilizceleştirildi (Nirengi konumlandırması).
- **Dokunulan dosyalar:** `README.md` (yeni), `LICENSE` (yeni), `package.json`,
  `.claude/tasks/TASKLOG.md`, `.claude/tasks/ACTIVE.md`
- **Test:** yok (doküman/lisans — kod değişmedi).
- **Karar/Not:** Lisans **MIT** (sohbetteki öneri doğrultusunda). README'de Türkçe mühendislik
  dokümanları not edildi, İngilizce göçü "planlandı" olarak işaretlendi. OSS başvurusu için kalan
  eksikler: README'ye gerçek ekran görüntüleri, (ileride) yeniden kullanılabilir katmanın paket
  olarak yayınlanması, yıldız/katkıcı biriktirme.
- **Durum:** tamamlandı (commit kullanıcı onayı bekliyor).
- **Sıradaki:** README'ye ekran görüntüsü eklemek (uygulamayı çalıştırıp yakala) → sonra T4.2.

## 2026-06-20 · T4.1 — Hisse veri kaynağı kararı: Finnhub (ABD)
- **Görev(ler):** T4.1 (Faz 4 başlangıcı — veri kaynağı kararı).
- **Ne yapıldı:**
  1. Faz 4 için hisse fundamentals sağlayıcısı **Finnhub** seçildi (ücretsiz katman, ABD hisseleri).
     Doğrulama (web): ücretsiz katman **60 çağrı/dk**, temel fundamentals dahil, `/stock/metric`
     erişilebilir (yalnız "Financials As Reported" premium — ihtiyacımız değil).
  2. 4 metriğimizin eşlemesi belirlendi: F/K=`peTTM`, PD/DD=`pb`, temettü verimi=`dividendYieldIndicatedAnnual`,
     kâr büyümesi=`epsGrowthTTMYoy` — tek `GET /stock/metric?metric=all` çağrısı. Fiyat/ad/borsa için
     `/quote` + `/stock/profile2`. Sektör bağlamı (above/high/low/positive) MVP'de **kaba eşiklerle KODDA**.
  3. Uygulama deseni: Faz 2 `IPriceProvider` ile aynı — `IStockDataProvider` + typed HttpClient + DI +
     stub HTTP testler + sembol/snapshot bazlı cache (07 §6). BIST **ertelendi** (ücretli/zor, CLAUDE.md §3.3).
- **Dokunulan dosyalar:** `.claude/docs/08-BACKLOG.md` (T4.1 [x] + karar notu + üst durum göstergesi),
  `.claude/tasks/ACTIVE.md`, `.claude/tasks/TASKLOG.md`
- **Test:** yok (karar görevi — kod değişmedi). Kesin alan adları T4.2'de canlı yanıtla doğrulanacak;
  null/boş alan → "veri yok" fallback (07 §5).
- **Karar/Not:** Anahtar koda gömülmez → env/User Secrets (§13). Ücretsiz katman ToS dev kullanımı için yeterli.
- **Durum:** tamamlandı.
- **Sıradaki:** T4.2 — `StockDataService` + `GET /api/stocks/{symbol}/metrics` (Finnhub, stub testli).

## 2026-06-18 · Temizlik — derleme uyarıları + pnpm-workspace mobile referansı
- **Görev(ler):** ad-hoc (analiz turu önerisi #6).
- **Ne yapıldı:**
  1. `Program.cs`: `ForwardedHeadersOptions.KnownNetworks` → `KnownIPNetworks` (ASPDEPR005 deprecation).
  2. `PortfolioApiTests.cs` (345/366/384/401): `create.Transactions!`/`goldHolding!.Transactions!` ile
     4× CS8604 (olası null başvuru) uyarısı kapatıldı (test bağlamında değer garantili).
  3. `pnpm-workspace.yaml`: var olmayan `mobile` paketi yorum satırına alındı (Faz M'de açılacak) —
     pnpm "missing package" uyarısını önler.
- **Dokunulan dosyalar:** `backend/src/Finans.Api/Program.cs`,
  `backend/tests/Finans.Integration.Tests/PortfolioApiTests.cs`, `pnpm-workspace.yaml`
- **Test:** Tam backend takımı yeşil — **Application 156/156 · Integration 90/90** · web build temiz.
  Derleme: önceki 5 uyarıdan (deprecation + 4×CS8604) arındı; geriye **2 önceden var olan CS8620**
  (`FinansDbContext` IPAddress converter nullability) kaldı — bu işin kapsamı dışı, ayrı bir kayıtta ele alınmalı.
- **Karar/Not:** CS8620 (IP audit kolonu converter'ı) bilerek dokunulmadı — okumadığım persistence/güvenlik
  koduna kapsam dışı değişiklik yapmaktan kaçınıldı.
- **Durum:** tamamlandı.
- **Sıradaki:** Faz 4 — T4.1 (hisse veri kaynağı kararı).

## 2026-06-18 · T3.9 — LLM maliyet/çağrı metriği + bütçe alarmı
- **Görev(ler):** T3.9 (12 §4, 10 §7). **Faz 3'ün son görevi.**
- **Ne yapıldı:**
  1. `ILlmMetrics` portu (Application) + `NoopLlmMetrics` (test/dev no-op) + `LlmMetrics`
     (Infrastructure, Meter `Finans.Llm`). Sayaçlar: `finans_llm_calls_total{result}`,
     `finans_llm_tokens_total{direction}` (maliyet proxy'si), `finans_llm_guard_blocked_total` (T3.5),
     `finans_llm_served_total{source}` (llm/cache/cache_last/fallback — cache ne kadar işe yarıyor).
  2. İç servis (`LlmCommentaryService`) her LLM çağrısında çağrı+token+guard sayar; dekoratör
     (`CachedLlmCommentaryService`) istek başına sunulan kaynağı sayar.
  3. Program.cs OTel'e `AddMeter(LlmMetrics.MeterName)` → mevcut Prometheus `/metrics` exporter'ı toplar.
  4. `compose/prometheus/rules.yml`: `finans-llm` grubu + 3 alarm — LlmCallBudgetBurn (>60 çağrı/1s),
     LlmTokenBudgetBurn (>200k token/1s), LlmFallbackRateHigh (>%50 fallback/15dk). Eşikler **dev**
     katmanı; ölçek/lansmanla güncellenecek.
- **Dokunulan dosyalar:**
  - `backend/src/Finans.Application/Llm/ILlmMetrics.cs` (yeni)
  - `backend/src/Finans.Infrastructure/Llm/LlmMetrics.cs` (yeni)
  - `backend/src/Finans.Application/Llm/LlmCommentaryService.cs` (opsiyonel metrik + RecordCall)
  - `backend/src/Finans.Application/Llm/CachedLlmCommentaryService.cs` (RecordServed)
  - `backend/src/Finans.Infrastructure/DependencyInjection.cs` (ILlmMetrics singleton + decorator'a geç)
  - `backend/src/Finans.Api/Program.cs` (AddMeter)
  - `compose/prometheus/rules.yml` (3 bütçe alarmı)
  - `backend/tests/Finans.Application.Tests/Llm/LlmMetricsTests.cs` (yeni)
- **Test:** **+6 unit** (iç: başarı+token / guard sayımı / başarısız; dekoratör: llm→cache / cache_last /
  fallback) + commentary integration **2/2** (host OTel meter kaydı doğrulandı). Application **150→156 yeşil**.
- **Karar/Not:** Metrik kaydı katmana göre bölündü — token/çağrı maliyeti iç serviste (LlmResult orada),
  kaynak dağılımı dekoratörde (cache kararı orada). Bütçe eşikleri dev için kaba; Grafana'da panel +
  Alertmanager dağıtımı Faz 5'e (12 §9).
- **Durum:** tamamlandı. **🎉 Faz 3 DoD karşılandı** (kartlar LLM'den; asla al/sat/yükselir [T3.5];
  hata çökertmiyor [fallback]; cache çalışıyor [T3.6]; maliyet görünür [T3.9]).
- **Sıradaki:** küçük temizlik (derleme uyarıları + pnpm-workspace mobile referansı), sonra Faz 4.

## 2026-06-18 · T3.6 — LLM yorum cache + "son başarılı" fallback
- **Görev(ler):** T3.6 (07 §6, 10 §3-4).
- **Ne yapıldı:**
  1. `CachedLlmCommentaryService` dekoratörü (mevcut FX/enflasyon/fiyat decorator deseni, T2.7).
     Cache anahtarı `commentary:{UserId:N}:{anonim özet SHA-256}` — portföy değişince anahtar değişir
     (otomatik tazeleme), değişmezse 24s TTL boyunca cache'ten döner (her ekran açılışında LLM çağrısı
     yok — NFR-9).
  2. **Son başarılı fallback (07 §5-a):** LLM başarısızsa (inner `Source="fallback"`) son başarılı
     yorum `Source="cache"` ile gösterilir (`commentary-last:{UserId}`, 30g). O da yoksa düz fallback
     kartı (07 §5-b). Yalnız başarılı yorum cache'lenir (geçici hata 24s dondurulmaz).
  3. **Per-user izolasyon (CLAUDE.md §13):** cache anahtarı UserId içerir; single-flight stampede koruması.
  4. DI: iç servis `LlmCommentaryService` concrete + dış `CachedLlmCommentaryService` dekoratör.
- **Dokunulan dosyalar:**
  - `backend/src/Finans.Application/Llm/CachedLlmCommentaryService.cs` (yeni)
  - `backend/src/Finans.Infrastructure/DependencyInjection.cs` (decorator kaydı + Logging using)
  - `backend/tests/Finans.Application.Tests/Llm/CachedLlmCommentaryServiceTests.cs` (yeni)
- **Test:** **+5 unit** (cache-hit / portföy değişince yeni çağrı / son-başarılı fallback / düz fallback /
  CommentaryResponse JSON round-trip) + commentary integration **2/2** (yeni DI ile host kurulumu doğrulandı).
  Application **145→150 yeşil**.
- **Karar/Not:** "Günde bir" hem 24s TTL hem hash-anahtarı ile karşılanır (değişmeyen portföy → cache;
  değişen → yeni anahtar). Ayrı tarih damgası gerekmedi.
- **Durum:** tamamlandı.
- **Sıradaki:** T3.9 (LLM maliyet/çağrı metriği + bütçe alarmı).

## 2026-06-18 · T3.5 — Çıktı güvenlik filtresi (kuşak-2 koruma)
- **Görev(ler):** T3.5 (07 §7, CLAUDE.md §2).
- **Ne yapıldı:**
  1. `CommentaryOutputGuard` (saf, public static) — kart metnini ASCII'ye katlayıp (ç→c, ş→s, ı/İ→i…
     diyakritiksiz LLM çıktısına dayanıklı) yasaklı **yönlendirme** (-malı/-meli ekli al/sat/geç/gir/
     çık/ekle/tut/yatır, "tavsiye ederim", "mantıklı olur" çerçevesi, "hemen/şimdi al", "fırsatı
     kaçırma") ve **gelecek tahmini** (yükselecek/düşecek/kazandıracak…) kalıplarını tarar; ayrıca
     "zaman imi (önümüzdeki ay…) + kesin yön (yükselir)" kombinasyonu.
  2. **Bağlam odaklı (07 §7 ilkesi):** meşru eğitim metni kesilmez — "satın alma gücü", "enflasyon
     yükselirse", "değer kaybedebilir" TEMİZ kalır (koşul/olasılık ≠ tahmin).
  3. `LlmCommentaryService.TryParseCards`: her kart eklenmeden önce filtreye sokulur; takılan kart
     düşürülür (`guardBlocked` sayılır), hepsi düşerse fallback'e iner. Düşen kart sayısı `LogWarning`
     ile görünür kılınır.
- **Dokunulan dosyalar:**
  - `backend/src/Finans.Application/Llm/CommentaryOutputGuard.cs` (yeni)
  - `backend/src/Finans.Application/Llm/LlmCommentaryService.cs` (parse'a filtre + log)
  - `backend/tests/Finans.Application.Tests/Llm/CommentaryOutputGuardTests.cs` (yeni)
  - `backend/tests/Finans.Application.Tests/Llm/LlmCommentaryServiceTests.cs` (+2 servis testi)
- **Test:** **+18 unit** (6 temiz/yanlış-pozitif yok + 9 yasak kalıp + 1 başlık/etiket tarama + 2 servis).
  Application **127→145 yeşil**.
- **Karar/Not:** "Yeni rakam uydurma" kalıp taramasıyla güvenilir saptanamadığı için (kartlar girdideki
  yüzdeleri meşru anar) bilinçli kapsam dışı bırakıldı — kuşak-1 prompt + parse katmanına bırakıldı.
- **Durum:** tamamlandı.
- **Sıradaki:** T3.6 (cache + son başarılı fallback).

## 2026-06-18 · OpenRouter yamasını commit'e hazırlama — gizli test hatası + repo temizliği
- **Görev(ler):** ad-hoc (2026-06-08 yamasının kapanışı) — analiz turunda iki integration testinin
  kararlı kırmızı olduğu ve `tmp_diag/` takipsiz dump'ının repoya girebileceği tespit edildi.
- **Ne yapıldı:**
  1. `OpenRouterLlmClientTests` — `Forces_json_object_…` (:97) ve `Sends_reasoning_exclude_…` (:141)
     testleri `captured.Content`'i çağrı bittikten SONRA okuyordu; `OpenRouterLlmClient` `using var http`
     ile request'i (ve Content'ini) dispose ettiği için `ObjectDisposedException` fırlıyordu (gizliydi,
     çünkü integration testleri önceki turlarda koşulmamıştı). Düzeltme: istek gövdesi responder lambda'sı
     **içinde** okunup tampona alınır.
  2. `backend/src/Finans.Api/tmp_diag/laguna.json` (geçici LLM ham yanıt dump'ı) silindi; `.gitignore`'a
     `tmp_diag/` eklendi (gelecekte istemeden commit'i önler).
- **Dokunulan dosyalar:**
  - `backend/tests/Finans.Integration.Tests/Llm/OpenRouterLlmClientTests.cs`
  - `.gitignore` (+`tmp_diag/`)
  - silindi: `backend/src/Finans.Api/tmp_diag/`
- **Test:** OpenRouter testleri **5/5 yeşil** (önceki 3/5). Yeşil-kapı (§12) geri kazanıldı; reasoning
  yaması regresyon koruması artık gerçekten çalışıyor.
- **Karar/Not:** —
- **Durum:** tamamlandı.
- **Sıradaki:** T3.5 (çıktı güvenlik filtresi).

## 2026-06-08 · T3.1/T3.3 düzeltme — OpenRouter free reasoning modelleri + max_tokens
- **Görev(ler):** ad-hoc (T3.1 + T3.3 kapsama). Belirti: kullanıcı Analiz sayfasında hep
  "Yorum şu an üretilemedi" fallback'i görüyordu. Endpoint `200 OK` ama `source="fallback"`.
- **Kök neden (canlı OpenRouter ile doğrulandı):**
  1. Mevcut `meta-llama/llama-3.3-70b-instruct:free` (Venice provider) **sürekli upstream 429**
     (paylaşımlı kota). `Llama→Kimi→Qwen→Gemma` hepsi aynı upstream 429'a düşüyor.
  2. Az kalabalık modeller (Laguna M.1, Nemotron Super 120B) yanıt veriyor **ama** gizli "reasoning"
     tokens harcıyor — `max_tokens=1024`'ün ~700'ünü düşünmede tüketip JSON content'i yarım
     bırakıyor (`finish_reason="length"`). Üst katmandaki `TryParseCards` bozuk JSON'da fail → fallback.
- **Düzeltme (iki dilim):**
  1. `OpenRouterLlmClient`: chat completions request body'sine `"reasoning":{"exclude":true,"enabled":false}`
     ekle. Destekleyen modeller reasoning'i kapatır, desteklemeyenler alanı sessizce yutar — geniş uyum.
  2. `LlmCommentaryService.MaxOutputTokens`: 1024 → 2048. Reasoning bütçesini emen
     modellerde content'in tamamlanmasını garantiler; Anthropic için fazlalık değil (5 kart ≈ 750 token).
  3. User secrets: `Llm:Model = poolside/laguna-m.1:free` (az kalabalık, reasoning artık devre dışı).
- **Yan düzeltme (`appsettings.Development.json`):** `Security:UseHttpsRedirection=false` (dev'de
  Vite proxy düz HTTP'ye konuşurken 307 redirect tüm `/api/*` çağrılarını kesiyordu — "Genel Bakış
  yüklenemedi" sorununun kaynağı buydu). Prod compose'da Caddy TLS sonlandırıyor, dokunulmadı.
- **Tanılama logu (`LlmCommentaryService`):** Parse fail durumunda ham yanıtın ilk 400 char'ı
  `LogWarning` ile basılır artık — gelecekteki LLM kalite sapmalarını sessiz fallback yerine görünür kılar.
- **Dokunulan dosyalar:**
  - `backend/src/Finans.Infrastructure/Llm/OpenRouterLlmClient.cs` (reasoning DTO + alan)
  - `backend/src/Finans.Application/Llm/LlmCommentaryService.cs` (MaxOutputTokens 2048 + tanılama log)
  - `backend/src/Finans.Api/appsettings.Development.json` (HTTPS redirect dev'de kapalı)
  - `backend/tests/Finans.Integration.Tests/Llm/OpenRouterLlmClientTests.cs` (+1 regresyon testi:
    reasoning.exclude/enabled gönderiliyor mu)
- **Test:** Application **127/127 yeşil**. OpenRouter integration testi (+1, toplam 5) VS Api kilidi
  bırakılınca koşulacak (mevcut akış). Build temiz.
- **Karar/Not:** OpenRouter free tier kalıcı çözüm değil — kullanıcıya net sunuldu: (a) bu yama
  (ücretsiz, kırılgan, dev için yeterli), (b) OpenRouter'a $5 kredi → paylaşımlı kotadan çık,
  (c) Anthropic key + Provider'ı geri çevir (CLAUDE.md varsayılan tasarımı). Kullanıcı (a)'yı seçti.
  Eğer Laguna da Venice rate-limit'e takılırsa Nemotron 30B/120B (Nvidia provider, daha sakin) dene.
- **Durum:** tamamlandı (yama). Backend restart sonrası kullanıcı tarafından canlı doğrulanacak.
- **Sıradaki:** T3.5 (çıktı güvenlik filtresi — yasaklı yönlendirme kalıbı). Bu yamanın faydası:
  modelin gerçekten yorum üretebildiği durumlarda T3.5 filtresi anlamlı bir şey üzerinde çalışacak.

## 2026-06-05 · T3.1 ek — OpenRouter sağlayıcı (ücretsiz dev katmanı)
- **Görev(ler):** T3.1'in genişletilmesi. Geliştirme aşamasında Anthropic kredi maliyetinden kaçınmak
  için **ücretsiz** bir sağlayıcı (`Llm:Provider="OpenRouter"`). Soyutlama (`ILlmClient`) zaten
  vardı → yeni dal eklendi, sözleşme değişmedi.
- **Ne yapıldı (Infrastructure — yeni dal):**
  1. `OpenRouterLlmClient` — typed HttpClient, OpenAI-uyumlu `/v1/chat/completions`. Bearer auth +
     `HTTP-Referer`/`X-Title` OpenRouter meta header'ları. JSON şema verilince:
     (a) sistem promptuna "Çıktın AŞAĞIDAKİ JSON şemasına KESİNLİKLE uyan tek bir JSON objesi olsun"
     direktifi + ham şema eklenir, (b) `response_format: { type: "json_object" }` zorlanır.
     `json_schema` mode'unu tüm OpenRouter modelleri desteklemediği için daha geniş uyum sağlayan
     `json_object` tercih edildi; üst katman T3.4 hardening zaten şemayı ikinci kez dayatıyor.
  2. `LlmOptions`: yeni alanlar `OpenRouterAppUrl`, `OpenRouterAppName`. Yorum güncel:
     `Provider` "Anthropic" | "OpenRouter" | (boş→Noop). Sözleşme aynı.
  3. `DependencyInjection`: yeni dal — `Provider="OpenRouter"` ise `AddHttpClient<ILlmClient, OpenRouterLlmClient>`.
     BaseUrl varsayılanı (Anthropic) verilmişse otomatik `openrouter.ai/api/` ile değiştirilir
     (kullanıcı override edebilir).
  4. `appsettings.json`: `_openRouterExample` notu + yeni alanların boş varsayılanları.
  5. Aynı hata akışı: HTTP/network/timeout/parse → `Fail(reason)` (07 §5).
- **Ne yapıldı (test):**
  6. `Finans.Integration.Tests.Llm.OpenRouterLlmClientTests` (+4 stub): API key yokken Fail; Bearer
     auth + meta header'lar + text yanıt parse; JSON şema verilince `response_format=json_object` +
     sistem promptuna şemanın eklenmesi; 5xx → `http_503` Fail (throw değil).
- **Dokunulan dosyalar:** `backend/src/Finans.Infrastructure/Llm/OpenRouterLlmClient.cs` (yeni),
  `backend/src/Finans.Infrastructure/Llm/LlmOptions.cs`,
  `backend/src/Finans.Infrastructure/DependencyInjection.cs`,
  `backend/src/Finans.Api/appsettings.json`,
  `backend/tests/Finans.Integration.Tests/Llm/OpenRouterLlmClientTests.cs` (yeni),
  `.claude/docs/07-LLM-INTEGRATION.md` (§2 sağlayıcı dalları açıklaması).
- **Test:** **Application 127/127 yeşil.** OpenRouter testleri (Integration) VS Api kilidi
  bırakılınca koşacak — bu projenin normal akışı.
- **Karar/Not:** `json_schema` modu yerine `json_object` + sistem prompt şeması seçildi: tüm free
  modeller `json_schema`'yı tam desteklemiyor; `json_object` geniş uyum. T3.4 hardening üst katmanda
  şema sınırlarını ikinci kez dayatıyor → uyumsuz kart düşer, iyi kart kalır.
- **Durum:** tamamlandı.
- **Sıradaki:** Kullanıcının OpenRouter API key alıp `Llm:Provider=OpenRouter` + `Llm:Model` set
  etmesi. Sonra **T3.5** (çıktı güvenlik filtresi — yasaklı yönlendirme kalıbı).

## 2026-06-05 · T3.4 + T3.7 + T3.8 — LLM yorum: parse hardening + endpoint + Web Analiz sayfası
- **Görev(ler):** T3.4 + T3.7 + T3.8 (08-BACKLOG Faz 3). Hedef: "kullanıcı Web'de Analiz sekmesini
  açıp LLM kartlarını (yoksa fallback'i) gerçekten görsün". Üç dilim:
- **T3.4 — Parse hardening:** `LlmCommentaryService.TryParseCards` artık şema sınırlarını **istemci
  tarafında da dayatıyor** (07 §4 ile aynı): cards üst sınır 5 (fazla → kırp); title min 2 / max 40
  (kısa → düş, uzun → kırp); body min 60 / max 220 (kısa → düş, uzun → kırp); meter value [0,1] clamp,
  boş etiketli meter null; tags non-string filtre + ≤4 + ≤24 char; bilinmeyen alanlar yutulur (forward
  compat). `CommentaryParseConstraints` sabit. **+9 unit edge test.**
- **T3.7 — Endpoint:** `PortfolioController.GetCommentary` (`GET /api/portfolio/commentary`),
  per-user özet → `ILlmCommentaryService` → 200 + `CommentaryResponse`. Yeni rate limit politikası
  **"commentary"** (Fixed 10/dk; LLM pahalı). +2 integration: NoopLlmClient ile 200+fallback (NFR-5),
  başka kullanıcı kapsamı (per-user izolasyon, IDOR yok).
- **T3.8 — Web Analiz sayfası:** `@finans/shared` tipleri (`CommentaryResponse`/`CommentaryCard`/
  `CommentaryMeter`) + `getCommentary`; web `useCommentary` hook'u — **manuel tazele**, otomatik
  refetch yok, staleTime 1h (NFR-9 cache disiplini). `AnalysisPage` "ComingSoon"dan gerçek sayfaya:
  başlık + lead + **Disclaimer her durumda** (loading dahil — CLAUDE.md §2 / NFR-2), source rozeti
  ("LLM tarafından üretildi" / "Yorum şu an üretilemedi — sayıların etkilenmedi" / "Önbellekten"),
  "↻ Yenile" butonu, skeleton (3 kart), hata+retry, kart listesi. `CommentaryCardList` komponenti
  (emoji + title + body + opsiyonel meter çubuğu + opsiyonel etiketler). CSS: `.commentary-list/
  .commentary-card/.commentary-meter/.sk-line` + skeleton animasyon.
- **Dokunulan dosyalar:**
  - `backend/src/Finans.Application/Llm/LlmCommentaryService.cs` (hardening + `CommentaryParseConstraints`),
  - `backend/src/Finans.Api/Controllers/PortfolioController.cs` (commentary endpoint),
  - `backend/src/Finans.Api/Program.cs` ("commentary" rate limit policy),
  - `backend/tests/Finans.Application.Tests/Llm/LlmCommentaryHardeningTests.cs` (yeni, +9),
  - `backend/tests/Finans.Integration.Tests/CommentaryApiTests.cs` (yeni, +2),
  - `packages/shared/src/types/index.ts` (`CommentaryResponse/Card/Meter` tipleri),
  - `packages/shared/src/api/index.ts` (`getCommentary`),
  - `web/src/lib/hooks.ts` (`useCommentary`, `queryKeys.commentary`),
  - `web/src/components/CommentaryCardList.tsx` (yeni),
  - `web/src/routes/AnalysisPage.tsx` (ComingSoon → gerçek sayfa),
  - `web/src/routes/AnalysisPage.test.tsx` (3 test: disclaimer her durumda + LLM çıktı + fallback),
  - `web/src/App.css` (analiz/commentary stilleri + skeleton).
- **Test yeşil kapı:** **Application 127/127 · Integration 85/85 · Web 54/54 + build temiz.**
  Hardening regresyon kapısı: yeni LLM şema gevşemesi/UI parse zayıflığı testte yakalanır.
- **Karar/Not:**
  - "Auth zorunlu" testi atıldı: `HttpCurrentUser` X-User-Id veya `Auth:DevUserId` config'ine
    düşüyor; ikisi yoksa exception → 500 (özel 401 yok). Endpoint güvenliği per-user data isolation
    ile test ediliyor (zaten kapsanmış). Faz 5 JWT geldiğinde özel 401 testi gelecek.
  - `useCommentary` otomatik refetch yapmaz (NFR-9 maliyet): kullanıcı "↻ Yenile" ile elle tetikler.
    T3.6'da cache anahtarı portföy hash'ine bağlanacak; aynı portföy → aynı kart, çağrı bile gitmez.
  - API anahtarı yokken (varsayılan dev): backend `NoopLlmClient` → fallback → UI'da bilgilendirme
    kartı görünür; "Bu nasıl çalışır" deneyimi tarayıcıda çalışır.
- **Durum:** tamamlandı. **LLM yorum hattı uçtan uca görünür hâle geldi.**
- **Sıradaki:** **T3.5** (çıktı güvenlik filtresi — yasaklı yönlendirme kalıbı taraması) +
  **T3.6** (cache: portföy hash / günde bir). Sonra T3.9 (LLM maliyet metriği — Prometheus).

## 2026-06-05 · T3.3 — LlmCommentaryService + Portföy anonimleştirme
- **Görev(ler):** T3.3 (08-BACKLOG Faz 3). T3.1 soyutlaması + T3.2 statik promptu birleştirip somut
  bir orkestrasyon servisi: hazır sayı (`PortfolioSummaryDto`) → anonimleştir → LLM → kart listesi.
- **Ne yapıldı (Application — anonimleştirme):**
  1. `Finans.Application.Llm.AnonymizedPortfolioSummary` (yeni) + `AnonymizedAllocationSlice` —
     PII'siz tip: `baseCurrency` (string), `totalValue` (tam sayı), `returnRatio`/`realReturnRatio`
     (3 basamak), `allocation` (sadece tür+ağırlık), `concentrationTop2` (top-2 toplamı).
  2. `PortfolioAnonymizer.Anonymize(PortfolioSummaryDto)` saf statik. Tür-bazlı `GroupBy` (aynı türde
     iki holding tek dilim), `OrderByDescending(weight)`. **Kullanıcı varlık adları sızmaz** (07 §2
     KVKK).
- **Ne yapıldı (Application — orkestrasyon):**
  3. `ILlmCommentaryService` + `LlmCommentaryService` (yeni). Bağımlılık: `ILlmClient`, `ILogger`,
     `TimeProvider`. Zincir: anonimleştir → deterministik JSON user prompt (alan adları sabit, cache
     friendly — T3.6) → `CompleteAsync(CommentaryPrompts.SystemPrompt, user, JsonSchema)` → güvenli
     parse → `CommentaryResponse(cards, Source, GeneratedAtUtc)`.
  4. **Fallback** (07 §5 ilk hat): LLM `Fail` veya JSON parse fail veya 0 kart → tek düz metin kartı
     ("Yorum şu an üretilemedi"), `Source="fallback"`. T3.4 fallback'i sıkılaştıracak (eksik alan
     tipi, çıktı güvenlik filtresi T3.5).
  5. Parse: per-kart zorunlu alanlar (emoji+title+body) kontrolü; eksikse o kart düşer ama diğerleri
     kalır (kısmi başarı). Opsiyonel `meter` (value+lowLabel+highLabel) ve `tags` toplanır.
  6. `CommentaryDtos.cs`: `CommentaryResponse`, `CommentaryCard`, `CommentaryMeter` — Web (T3.8)
     bunu render edecek.
- **Ne yapıldı (DI):**
  7. `services.AddScoped<ILlmCommentaryService, LlmCommentaryService>()` — `ILlmClient` zaten kayıtlı
     (Anthropic veya Noop).
- **Ne yapıldı (test — Application):**
  8. `PortfolioAnonymizerTests` (+5): isim sızmaz; aynı tür birleşir; oranlar yuvarlanır; top-2 yoğunlaşma;
     null oranlar korunur.
  9. `LlmCommentaryServiceTests` (+6): stub `ILlmClient` ile mutlu yol (2 kart parse + meter + tags);
     sistem promptu+şema dayatılır + user prompt anonim; LLM Fail → fallback; geçersiz JSON → fallback;
     sıfır kart → fallback; eksik alanlı kart düşer ama geçerli kalır.
- **Dokunulan dosyalar:** `backend/src/Finans.Application/Llm/AnonymizedPortfolioSummary.cs` (yeni),
  `backend/src/Finans.Application/Llm/CommentaryDtos.cs` (yeni),
  `backend/src/Finans.Application/Llm/ILlmCommentaryService.cs` (yeni),
  `backend/src/Finans.Application/Llm/LlmCommentaryService.cs` (yeni),
  `backend/src/Finans.Application/Finans.Application.csproj` (`Microsoft.Extensions.Logging.Abstractions`),
  `backend/src/Finans.Infrastructure/DependencyInjection.cs` (servis kaydı),
  `backend/tests/Finans.Application.Tests/Llm/PortfolioAnonymizerTests.cs` (yeni),
  `backend/tests/Finans.Application.Tests/Llm/LlmCommentaryServiceTests.cs` (yeni).
- **Test:** **Application 118/118 yeşil** (+11) · **Integration 83/83 yeşil** (regresyon yok).
- **Karar/Not:** `LlmCommentaryService` Application'da kaldı — bağımlılığı yalnız `ILlmClient`
  soyutlaması + DTO'lar. Test ile validation arasındaki "şema disiplini" iki seviyede: (a) Anthropic
  tarafında `tool_use.input_schema` (T3.2'deki şema) modelin çıktısını dayatır, (b) servis tarafında
  güvenli parse + per-kart zorunlu alan kontrolü kötü çıktıyı eleminer. T3.4 bunu ek olarak: type
  coercion (örn. tag stringify), uzunluk doğrulama, çok uzun body kırpma ile sertleştirecek.
- **Durum:** tamamlandı.
- **Sıradaki:** **T3.4 — Güvenli parse + fallback testleri** (07 §5 → bozuk JSON / eksik alan /
  boş yanıt + LLM cache'den son başarılıyı dönmek).

## 2026-06-05 · T3.2 — Portföy yorum sistem promptu + few-shot + JSON şema
- **Görev(ler):** T3.2 (08-BACKLOG Faz 3). 07 §3 iskeletini somut, cache-friendly statik bir modüle
  döktük; T3.3 bunu çağırıp anonim portföy özetiyle birleştirip `ILlmClient.CompleteAsync`'e gönderecek.
- **Ne yapıldı (Application — statik prompt modülü):**
  1. `Finans.Application.Llm.CommentaryPrompts` (yeni, statik) — yan etki yok, dışa bağımlılık yok.
  2. `SystemPrompt` (eğitmen kişiliği + 7 KESİN KURAL):
     (1) verilen sayıların dışında **rakam üretme/hesap yapma**,
     (2) **yönlendirme/tahmin yapma** — "al/sat/yükselir/düşer" yasak,
     (3) mevcut durumu açıkla; genel çerçeveler ver,
     (4) Türkçe + sıfır bilgi varsayımı + terimleri ilk geçişte kısaca tanımla,
     (5) çıktı yalnızca `structured_output` tool çağrısı — düz metin yok,
     (6) kart sayısı 3-5, her kart bir tema, tema tekrarı yok,
     (7) `body` 60-220 char (1-2 cümle, akademik dil yok).
     + 2 doğru few-shot (yoğunlaşma + reel getiri kartları) ve 4 yasak örnek
     ("Altından çıkıp hisseye geçmelisin" / "Bu seviyeden BES eklemek mantıklı" / "Önümüzdeki ay
     USD/TRY yükselir" / "Toplam değerin aslında 650.000").
  3. `CommentaryJsonSchema` (07 §4 kart şeması):
     - `cards` array (`minItems:3`, `maxItems:5`) — maliyet/UX kapısı.
     - kart: `emoji` + `title` (2-40 char) + `body` (60-220 char) zorunlu;
       `meter` (value 0..1 + lowLabel + highLabel) ve `tags` (≤4) opsiyonel.
     - Anthropic `tool_use.input_schema` ile modele dayatılacak (T3.3); ayrıca üst katmanda güvenli parse (T3.4).
- **Ne yapıldı (test — regresyon kapısı):**
  4. `Finans.Application.Tests.Llm.CommentaryPromptsTests` (+5): KESİN KURALLAR bayrak ifadeler
     metinde geçiyor mu (eğitmen + danışman değil + YÖNLENDİRME/TAHMİN YAPMA + "yeni rakam üretme");
     Türkçe + structured_output şartları; doğru/yanlış örnek bloğu mevcut; şema parse edilebilir +
     zorunlu alanlar listede; minItems/maxItems + body uzunluk sınırları.
- **Dokunulan dosyalar:** `backend/src/Finans.Application/Llm/CommentaryPrompts.cs` (yeni),
  `backend/tests/Finans.Application.Tests/Llm/CommentaryPromptsTests.cs` (yeni),
  `.claude/docs/08-BACKLOG.md`, `.claude/tasks/ACTIVE.md`, `.claude/tasks/TASKLOG.md`.
- **Test:** **Application 107/107 yeşil** (+5 unit). Integration etkilenmedi (LLM hâlâ Noop varsayılan).
- **Karar/Not:** Few-shot örnekleri sistem promptuna gömüldü (alternatif: ayrı user/assistant turn'leri).
  Sebep: sistem promptu Anthropic prompt cache'ine alınacak (T3.6) — tek blok hâlinde tutarsa cache
  isabeti yüksek; örnek değişimi nadir, cache invalidation sorun değil. Şema sınırları (3-5 kart, body
  60-220) hem **maliyet** (NFR-9) hem **UX kalitesi** (ne çok az içerik ne tekrar bombardımanı) kapısı.
- **Durum:** tamamlandı.
- **Sıradaki:** **T3.3** — `LlmCommentaryService`: anonimleştirilmiş özet → `CompleteAsync` → kart listesi.

## 2026-06-04 · T3.1 — LLM sağlayıcı kararı + `ILlmClient` soyutlama + Anthropic istemci
- **Görev(ler):** T3.1 (08-BACKLOG Faz 3). Faz 3'ün giriş kapısı: sağlayıcı seç, provider-neutral
  soyutlama kur, KVKK çerçevesini sözleşmeye yaz.
- **Karar — Anthropic Claude** (07 §2 güncellendi): Türkçe kalitesi + talimat takibi + `tool_use`
  ile JSON şema zorlama + prompt caching. Faz 3 başlangıç modeli `claude-sonnet-4-6`; maliyet
  sıkışırsa `claude-haiku-4-5` ile env değişikliği (Llm:Model).
- **Ne yapıldı (Application — sözleşme):**
  1. `Finans.Application.Llm.ILlmClient` — `Task<LlmResult> CompleteAsync(LlmRequest, ct)`.
     `LlmRequest(SystemPrompt, UserPrompt, JsonSchema?, MaxOutputTokens=1024, Temperature=0.2m)`,
     `LlmResult(Success, Text, InputTokens, OutputTokens, ErrorReason?)` + `Ok`/`Fail` factory.
  2. **KVKK kuralı arayüz yorumunda** (CLAUDE.md §2, 11 §1): LLM'e gönderilen içerikte UserId/isim/
     e-posta vb. **YASAK**; yalnız anonim özet. Anonimleştirme servis katmanı sorumluluğu (T3.3).
- **Ne yapıldı (Infrastructure — istemci):**
  3. `Finans.Infrastructure.Llm.AnthropicLlmClient` — typed `HttpClient`; resmi SDK YOK (küçük yüzey,
     tek endpoint `/v1/messages`). Header: `x-api-key` + `anthropic-version=2023-06-01`. Body:
     `model/max_tokens/temperature/system/messages/tools/tool_choice`. `JsonSchema` verilirse
     `tools=[{name:"structured_output", input_schema:<şema>}]` + `tool_choice:tool` → yapılandırılmış
     çıktıyı modele dayatır. Yanıtta `tool_use.input` JSON olarak döner; aksi halde `text` blokları
     birleştirilir. `usage.input_tokens`/`output_tokens` `LlmResult`'a (T3.9 metriği için hazır).
     Hata akışı: HTTP/`HttpRequestException`/`TaskCanceledException`/`JsonException` → exception
     fırlatılmaz, `Fail(reason)` döner (07 §5 fallback'in alt katmanı).
  4. `NoopLlmClient` — API anahtarı yokken dev/test güvenli varsayılan; her çağrı
     `Fail("llm_not_configured")` → üst katman cache/fallback metnine düşer → uygulama çökmez (NFR-5).
  5. `LlmOptions:{Provider,ApiKey,Model,TimeoutSeconds,BaseUrl,AnthropicVersion}`.
  6. `DependencyInjection`: `Llm:ApiKey` doluysa typed HttpClient + `AnthropicLlmClient`; aksi halde
     `NoopLlmClient` singleton.
- **Ne yapıldı (test):**
  7. `Finans.Application.Tests.Llm.LlmContractTests` (+3): `Ok`/`Fail` factory'leri, request default'ları.
  8. `Finans.Integration.Tests.Llm.AnthropicLlmClientTests` (+4 stub): API key yokken Fail; header'lar
     + endpoint + text yanıt parse; `tool_use` JSON parse; 5xx → `http_502` Fail (throw değil).
- **Dokunulan dosyalar:** `backend/src/Finans.Application/Llm/ILlmClient.cs` (yeni),
  `backend/src/Finans.Infrastructure/Llm/LlmOptions.cs` (yeni),
  `backend/src/Finans.Infrastructure/Llm/NoopLlmClient.cs` (yeni),
  `backend/src/Finans.Infrastructure/Llm/AnthropicLlmClient.cs` (yeni),
  `backend/src/Finans.Infrastructure/DependencyInjection.cs`,
  `backend/src/Finans.Api/appsettings.json` (boş ApiKey varsayılan),
  `backend/tests/Finans.Application.Tests/Llm/LlmContractTests.cs` (yeni),
  `backend/tests/Finans.Integration.Tests/Llm/AnthropicLlmClientTests.cs` (yeni),
  `.claude/docs/07-LLM-INTEGRATION.md` (§2 karar gerekçesi + KVKK çerçevesi + yapılandırma).
- **Test:** **Application 102/102 · Integration 83/83 yeşil** (+3 unit + 4 stub HTTP). Stub testler
  ağa çıkmaz (`StubHttpMessageHandler`).
- **Karar/Not:** SDK eklenmedi — Anthropic Messages API'sinin Faz 3'te kullanılan yüzeyi küçük
  (system + messages + tools + tool_choice + usage). Bağımlılık minimum; lansman öncesi maliyet
  takibi ve LLM-side rate limit metrikleri T3.9'da Prometheus'a yansıyacak.
- **Durum:** tamamlandı.
- **Sıradaki:** **T3.2 — Sistem promptu + few-shot** ("tavsiye değil" korkuluk; portföy yorumu için).

## 2026-06-04 · T-BES.6b ileri — Arka plan job: BES plan otomatik devam
- **Görev(ler):** T-BES.6b ileri (08-BACKLOG T-BES epik). Önceki lazy catch-up sayfa açılınca/GET'te
  tetikleniyordu → kullanıcı uygulamayı haftalarca açmazsa plan kaydı oluşmuyordu. Bu turda gerçek
  arka plan job: server süreci ayakta olduğu sürece (compose/VPS) plan periyodik akar.
- **Ne yapıldı (saf çekirdek — runner):**
  1. `Finans.Infrastructure.Services.BesPlanCatchUpRunner` (yeni) — `HoldingService.CatchUpBesPlanAsync`
     core mantığı buraya taşındı. **`ICurrentUser`'a bağlı değil**; verili (önceden yüklenmiş, includes'lı)
     `Holding` üzerinde çalışır → kullanıcı kapsamlı GET hattı *ve* sistem hattı (cron) ortak çekirdek.
     EF v7 tracked koleksiyon tuzağı (memory): `db.BesContributions.Add(...)` korundu → caller `SaveChanges`.
  2. `HoldingService.CatchUpBesPlanAsync` → runner'a delege (60→8 satır). `IsPlanSource` planlı katkı
     katalogu için kullanılmaya devam ediyor.
- **Ne yapıldı (hosted service):**
  3. `BesPlanCatchUpHostedService` (`BackgroundService`) — host başlar başlamaz ilk tik (varsayılan +60s
     gecikme: migrate/seed/healthcheck bitsin); sonra her N saatte bir (varsayılan 6h). Tik içinde
     `Asset.Type=BES + PlanActive + MonthlyAmount + ContributionDay` filtresiyle aktif holding'leri
     **per-holding try/catch + log** ile ilerletir (bir hata diğer holding'leri düşürmez — 11 §4).
  4. `BesPlanCatchUpOptions` (`Bes:PlanCatchUp`): `Enabled` (true), `IntervalHours` (6),
     `InitialDelaySeconds` (60). `appsettings.json`'a varsayılanlar; **integration test factory'sinde
     `Enabled=false`** (testin deterministik kurgusunu arka plan tiki bozmasın).
- **Ne yapıldı (DI):**
  5. `DependencyInjection.AddInfrastructure(... IConfiguration? configuration = null)` — opsiyonel:
     verilirse `Bes:PlanCatchUp` bind edilir; her hâlükârda runner scoped + hosted service kayıtlı.
  6. `Program.cs` → `AddInfrastructure(..., builder.Configuration)`.
  7. NuGet: `Microsoft.Extensions.Hosting.Abstractions 10.0.8` (Infrastructure: `BackgroundService`/
     `IHostedService` için).
- **Dokunulan dosyalar:** `backend/src/Finans.Infrastructure/Services/BesPlanCatchUpRunner.cs` (yeni),
  `backend/src/Finans.Infrastructure/Services/BesPlanCatchUpHostedService.cs` (yeni),
  `backend/src/Finans.Infrastructure/Services/HoldingService.cs` (catch-up delege),
  `backend/src/Finans.Infrastructure/DependencyInjection.cs`,
  `backend/src/Finans.Infrastructure/Finans.Infrastructure.csproj`,
  `backend/src/Finans.Api/Program.cs`, `backend/src/Finans.Api/appsettings.json`,
  `backend/tests/Finans.Integration.Tests/SqliteWebApplicationFactory.cs`
  (`Bes:PlanCatchUp:Enabled=false`),
  `backend/tests/Finans.Integration.Tests/BesPlanCatchUpRunnerTests.cs` (yeni, +2),
  `backend/tests/Finans.Integration.Tests/RateLimitApiTests.cs` (T2.9'dan kalan eksik using fix —
  `Finans.Api.ErrorHandling`).
- **Test:** +2 integration (`Catches_up_missing_plan_months_when_active`,
  `No_op_when_plan_inactive`). Runner doğrudan DI'dan çözülüp çağrılır — sistem hattını simüle eder.
  **Application 99/99 · Integration 79/79 yeşil** (RateLimit testleri de bu kez koştu, VS kilidi yoktu).
- **Karar/Not:** "Uygulama gerçekten kapalıyken bile" katı anlamda **sürecin dışında** zamanlanmış iş
  ister (cron / Windows Task Scheduler / Hangfire ayrı süreç). Bu hâliyle: sunucu (compose/VPS) ayakta
  olduğu sürece plan akar; kullanıcı tarayıcısı kapalıyken bile doğru. Daha sağlam dağıtık cron için
  Hangfire/Quartz değerlendirilmesi gelecek faza ertelendi (12 §9). `RateLimitApiTests.cs` küçük
  using eksiği bu turda fark edildi (T2.9'dan kalan); fixlendi.
- **Durum:** tamamlandı.
- **Sıradaki:** **Faz 3 — LLM yorum katmanı** (T3.1: sağlayıcı seçimi + KVKK çerçevesi).

## 2026-06-04 · T2.8 — Gözlemlenebilirlik yığını (Seq + Prometheus + Grafana + OTel)
- **Görev(ler):** T2.8 (08-BACKLOG Faz 2). 12 §4 (metrik) + §6 (dashboard/alarm). Faz 2'nin dağıtım/gözlem
  kapısı — ürün canlıdaysa "RED + cache + bağımlılık + 5xx/p95 alarmı" baştan olmalı (`12` §1, §9).
- **Ne yapıldı (API — OTel metrik):**
  1. `Finans.Api.csproj` paketler: `OpenTelemetry.Extensions.Hosting 1.15.3`,
     `.Instrumentation.AspNetCore 1.15.2`, `.Instrumentation.Http 1.15.1`,
     `.Instrumentation.Runtime 1.15.1`, `.Exporter.Prometheus.AspNetCore 1.15.3-beta.1` (resmî beta).
  2. `Program.cs` `AddOpenTelemetry().WithMetrics(...)` — `AddAspNetCoreInstrumentation` (RED:
     `http_server_request_duration_seconds_*`), `AddHttpClientInstrumentation`
     (`http_client_request_duration_seconds_*` — Frankfurter/Truncgil bağımlılık metriği),
     `AddRuntimeInstrumentation` (GC/CPU/Thread), `AddMeter(CacheMetrics.MeterName)` (T2.7'de hazırdı:
     `finans_cache_requests_total{result,cache}`). `Resource.AddService("finans-api", version)` → label.
  3. `MapPrometheusScrapingEndpoint().DisableRateLimiting()` — `/metrics` 8080'de scrape için. Rate-limit
     dışında (kendi metriklerimizi limitleyemeyiz). Caddy `/metrics`'i DIŞARI vermez (admin-only, 11 §5).
- **Ne yapıldı (API — Serilog Seq sink, opsiyonel):**
  4. `Serilog.Sinks.Seq 9.1.0`. `Program.cs`: `Serilog:Seq:ServerUrl` doluysa sink eklenir, boşken eklenmez
     (yerel `dotnet run`'ı bozmaz). `appsettings.json`: `Serilog.Seq.ServerUrl: ""` varsayılan.
- **Ne yapıldı (compose):**
  5. `seq` (datalust/seq:2024.3, EULA=Y) — UI `127.0.0.1:8081→80`, ingestion iç ağ 5341.
  6. `prometheus` (prom/prometheus:v2.55.1) — `compose/prometheus/prometheus.yml` (15s scrape api:8080/metrics,
     rule_files), `rules.yml` (3 alarm: 5xx>2%, p95>600ms-10dk, instance down 2dk). 15g retention.
     `127.0.0.1:9090:9090`. Volume `prometheus-data`.
  7. `grafana` (grafana-oss:11.3.1) — provisioning: `datasources/datasource.yml` (Prometheus),
     `dashboards/dashboards.yml` (file provider), `dashboards/finans-overview.json` ("Genel Bakış":
     istek hızı/route, hata oranı 5xx+429, p95 gecikme/route, **cache hit oranı**, dış bağımlılık p95,
     .NET GC heap). `127.0.0.1:3001:3000`. Volume `grafana-data`. Admin parola env (`GRAFANA_ADMIN_PASSWORD`,
     dev varsayılan `admin`).
  8. `api` env: `Serilog__Seq__ServerUrl=http://seq:5341` — compose'da otomatik Seq'e log.
  9. **Tüm admin port'ları `127.0.0.1` bind** — LAN'a açık değil; production'da SSH tüneli + Caddy
     basic-auth + IP whitelist (`11` §5).
- **Dokunulan dosyalar:** `backend/src/Finans.Api/Finans.Api.csproj`, `backend/src/Finans.Api/Program.cs`,
  `backend/src/Finans.Api/appsettings.json`, `docker-compose.yml`, `compose/prometheus/prometheus.yml`,
  `compose/prometheus/rules.yml`, `compose/grafana/provisioning/datasources/datasource.yml`,
  `compose/grafana/provisioning/dashboards/dashboards.yml`, `compose/grafana/dashboards/finans-overview.json`.
- **Test:** yeni testi yok (altyapı/yapılandırma). **Application 99/99 yeşil** (regresyon yok). Integration
  testleri VS Api kilidi bırakılınca koşulacak (her zamanki gibi). Build: 0 hata + mevcut 3 uyarı (T2.8 ile
  alakasız: `KnownNetworks` deprecation + 2 EF nullability). Manuel doğrulama: `docker compose up --build`
  → Grafana http://localhost:3001 (admin/admin), Prometheus :9090 (Status→Targets `finans-api` UP), Seq :8081.
- **Karar/Not:** Prometheus exporter `1.15.3-beta.1` — OTel ekosistemi Prometheus için **hâlâ beta'da**
  ama resmî paket; alternatif (OTLP→OTel Collector→Prometheus remote_write) Faz 5'te değerlendirilir.
  Alertmanager bu turda yok — kurallar Prometheus UI'da görünür, Grafana Alerting de okuyabilir; e-posta/
  Telegram dağıtımı Faz 5 (12 §9). Web tarafı bu turda değişmedi.
- **Durum:** tamamlandı. **Faz 2 (altyapı dahil) BİTTİ.**
- **Sıradaki:** **Faz 3 — LLM yorum katmanı** (T3.1: sağlayıcı seçimi + KVKK çerçevesi) **VEYA** T-BES.6b
  ileri (arka plan zamanlayıcı). Sıra kullanıcıyla netleşecek.

## 2026-06-02 · T2.9 — Caddy reverse proxy + TLS + ASP.NET RateLimiter
- **Görev(ler):** T2.9 (08-BACKLOG Faz 2). Lansman öncesi güvenlik kapısı: TLS, dış servisleri iç ağa
  kapatma, rate limit (Caddy ağ-katı + ASP.NET endpoint-katı), güvenlik başlıkları.
- **Ne yapıldı (Caddy / compose):**
  1. `compose/caddy/Caddyfile` — `localhost` için `tls internal` (Caddy CA, tarayıcı bir kez kabul);
     `/api/*` ve `/health*` → `api:8080` iç ağ. Güvenlik başlıkları (HSTS, XCTO, XFO, Referrer-Policy,
     `-Server`). gzip+zstd encode. Diğer yollar bilgi mesajı (compose'da web servisi yok).
  2. `docker-compose.yml` — Caddy servisi (80, 443; volume Caddyfile + named volume `caddy-data`/
     `caddy-config` sertifika kalıcılığı). **`api` `ports` → `expose: 8080`** (dışarı kapandı);
     `postgres` `ports` kaldırıldı (yerel dev kendi PostgreSQL'i 5432'yi kullanır); `redis` `expose`.
     Healthcheck Caddy için (`wget --no-check-certificate https://localhost/health`).
- **Ne yapıldı (ASP.NET RateLimiter / Program.cs):**
  3. `AddRateLimiter` (built-in, .NET 7+). 429 → sözleşmeli `ApiErrorEnvelope` (`RATE_LIMIT_EXCEEDED`)
     + `Retry-After` header. Partition: kullanıcı (X-User-Id) varsa `user:`, yoksa `ip:`.
  4. **Global limiter:** SlidingWindow 120/dk/partition (6 dilim, smooth).
  5. **Politika "prices":** FixedWindow 10/dk — `PricesController` `[EnableRateLimiting("prices")]`.
     Dış API (Frankfurter/Truncgil) korunsun.
  6. **Politika "nudges":** FixedWindow 30/dk — `PortfolioController.GetNudges`. Web 5dk'da tazeler.
  7. **`/health*` rate limit DIŞINDA** — `MapHealthChecks(...).DisableRateLimiting()`. Uptime/probe
     trafiği kesilmez.
  8. `UseForwardedHeaders` (Caddy arkasında gerçek client IP'sini görmek için; `Security:ForwardedHeaders`
     bayrağı). KnownNetworks/KnownProxies şimdi açık — production'da daraltılır.
  9. CORS allowed origin'lere `https://localhost` eklendi (web Caddy üzerinden API çağrısı yapacaksa).
- **Dokunulan dosyalar:** `compose/caddy/Caddyfile` (yeni), `docker-compose.yml`,
  `backend/src/Finans.Api/Program.cs`, `backend/src/Finans.Api/Controllers/PricesController.cs`,
  `backend/src/Finans.Api/Controllers/PortfolioController.cs`,
  `backend/tests/Finans.Integration.Tests/RateLimitApiTests.cs` (yeni).
- **Test:** +2 integration test (Prices: 10 OK + 11. 429 ApiError; Health: 150 istek 429 yok). Partition
  izolasyonu: her test rastgele `X-User-Id` → sayaçlar paylaşılmaz. **Application 99/99 yeşil.**
  Integration VS Api kilidi bırakılınca koşulacak.
- **Karar/Not:** Web compose'a girmedi — `pnpm dev` ayrı (5173/5174) koşar; production hazırlığı için
  ilerleyen aşamada Caddy `file_server` ile `web/dist` sunulabilir. Localhost TLS için tarayıcı bir kez
  Caddy'nin internal CA'sını "kabul et" diyecek (Chrome: thisisunsafe). Production'da `tls internal`
  yerine gerçek domain + otomatik Let's Encrypt (Caddy varsayılanı).
- **Durum:** tamamlandı (kod). Manuel doğrulama: `docker compose up --build` → `https://localhost/health`.
- **Sıradaki:** **T2.8** — Gözlemlenebilirlik (Seq + Prometheus + Grafana + OTel metrik).

## 2026-06-01 · T-BES.4 — devlet katkısı yıllık üst sınırı (takvim yılı bazlı kesme)
- **Görev(ler):** T-BES.4 (08-BACKLOG T-BES epik) — devlet katkısı oranı %20 uygulanıyordu ama
  **üst sınır kontrolü yoktu**; aylık 50.000 ₺ katkı yapan biri için yıl boyunca 120.000 ₺ devlet
  katkısı hesaplıyorduk, oysa 2026 tavanı 79.272 ₺.
- **Ne yapıldı (saf kurallar/hesap):**
  1. `BesRules` — `AnnualCaps` dictionary (2024: 51.006 · 2025: 66.312 · 2026: 79.272 ₺ — yıllık brüt
     asgari ücret × oran). `AnnualStateContributionCapFor(year)` tablo dışı yıl için **en son bilinen
     yılın değeri**ne düşer (gelecek projeksiyonu için makul; tablo güncellenir). EGM/SPK doğrulaması
     ŞART (CLAUDE.md §2 — kod yorumunda belirtildi).
  2. `BesCalculator.ApplyAnnualStateCap(proposedState, year, alreadyContributedInYear)` — kalan kotaya
     göre kesme; kota dolduysa 0; negatif proposed → 0.
- **Ne yapıldı (servis — 4 katkı metodu):**
  3. `AddBesContributionAsync` — `Include(BesContributions)` eklendi; aynı yıl kümülatif toplama göre kesme.
  4. `UpdateBesContributionAsync` — mevcut katkıyı hariç tutarak (`Id != contributionId`) aynı yıl
     toplamına göre kesme; yıl değişimi doğru ele alınır (eski yıl delta'sı + yeni yıl kotası).
  5. `GenerateBesContributionsAsync` — `Dictionary<int, decimal>` yıllık kümülatif; sıralı geçişte yıl
     içinde birikim doğru artar.
  6. `CatchUpBesPlanAsync` — aynı desen (lazy plan üretimi tavanı dolduktan sonra 0 state ile devam eder).
- **Ne yapıldı (projeksiyon):**
  7. `BesProjectionCalculator.Project` — her ay yıllık kümülatif tutar; yıl değişiminde otomatik sıfırlanır
     (yeni anahtar). Aylık 50.000 ₺ × 12 = 600.000 ₺ own (cap own'u etkilemez) + state cap'te 79.272'de durur.
- **Ne yapıldı (web — bilgi):**
  8. `BesContributionForm` — `ANNUAL_STATE_CAPS` tablosu (backend ile eşlek); tahmini devlet katkısı
     tavanı aşıyorsa **altın renkli uyarı** ("⚠ Bu katkı tavanı aştığı için fazla kısım yatmayacak").
- **Dokunulan dosyalar:** `backend/src/Finans.Application/Portfolio/BesRules.cs`,
  `backend/src/Finans.Application/Portfolio/BesCalculator.cs`,
  `backend/src/Finans.Application/Portfolio/BesProjectionCalculator.cs`,
  `backend/src/Finans.Infrastructure/Services/HoldingService.cs`,
  `backend/tests/Finans.Application.Tests/Portfolio/BesCalculatorTests.cs`,
  `backend/tests/Finans.Application.Tests/Portfolio/BesProjectionCalculatorTests.cs`,
  `backend/tests/Finans.Integration.Tests/BesAndHistoryApiTests.cs`,
  `web/src/components/BesContributionForm.tsx`.
- **Test:** +5 unit (`ApplyAnnualStateCap`: tam geçer / tavana yakın kesme / tükenmiş 0 / non-positive 0 /
  bilinmeyen yıl fallback). +3 unit (`Projection`: tavanda durur / yeni yıl sıfırlanır / tavanın altında
  kesme yok). **Application.Tests 99/99 yeşil.** +3 integration (Add: kesme / kota dolu→0 / yeni yıl
  sıfır) — VS Api kilidi bırakılınca koşulacak. Web 52/52 + build temiz.
- **Karar/Not:** Tavan tablosu **mevzuata tabidir**, lansman öncesi EGM/SPK ile doğrulanmalı. 2025
  değeri (66.312 ₺) varsayım — eski oran %25 brüt yıllık asgari ücret formülü; gerçek sayı kaynaklarda
  tutarsız (EGM ile teyit gerekli). Tablo dışı yıl fallback = son bilinen yıl (gelecek projeksiyonu
  için pragmatik). `DeleteBesContribution` cap'i etkilemez (yalnız düşer). `CreateBes` Opening
  bakiyesi cap'le kontrol edilmez (kullanıcı geçmiş yıllar boyunca birikmiş toplamı bildirir, tek satır).
- **Durum:** tamamlandı (kod). VS Api kilidi bırakılınca integration testleri koşulacak.
- **Sıradaki:** T-BES kapandı. Faz 2 dağıtımı (T2.8 gözlemlenebilirlik + T2.9 reverse proxy + rate limit).

## 2026-06-01 · T-BES.5 ext — sözleşme kademesi süre preset'leri + süre sonu hak ediş
- **Görev(ler):** T-BES.5 uzantısı — kullanıcı: "Sözleşmeye göre 3-6, 10 veya emeklilik zamanı
  diyordu bunları otomatik hesaplasın; ama istediğim yıl değerini de girebileyim."
- **Ne yapıldı (backend):**
  1. `BesProjectionInput` — opsiyonel `JoinedAtUtc` + `BirthYear` parametreleri.
  2. `BesProjectionResult` — `VestedRateAtEnd` (0/0,15/0,35/0,60/1,00) + `VestedStateAmountAtEnd`
     (rate × süre sonu state değeri).
  3. `BesProjectionCalculator.Project` süre sonu hak edişi `BesCalculator.VestedRateFor` ile hesaplar:
     **mevcut sözleşme süresi de hesaba katılır** (joined=2020+4 yıl projeksiyon → süre sonu 10y → %60).
  4. `HoldingService.ProjectBesAsync` Holding'in `BesDetails.JoinedAtUtc`/`BirthYear`'ı calculator'a yedirir.
- **Ne yapıldı (web):**
  5. `BesProjectionForm`: `buildYearPresets(joinedAtUtc, birthYear)` — sözleşme kademeleri
     (3/6/10 yıl noktalarına kalan süre + Emeklilik = max(56'a kalan, 10y'a kalan)). Doğum yılı
     yoksa "Emeklilik" preset'i gizli. **"Özel yıl" inputu** (number, 1-50) kullanıcı kendi yılını
     yazabilir.
  6. Preset chip'i 2 satır: yıl + kademe hak ediş yüzdesi (örn. "3. yıl · 3 yıl" / "Kısmen hak
     ediş %15"). Form prop'ları: `joinedAtUtc`, `birthYear`.
  7. **Süre sonu hak ediş kartı** (yeşil tonlu): hak ediş oranı + hak kazanılan devlet katkısı +
     kademe açıklaması.
  8. CSS: `.proj-years`/`.year-chip`/`.proj-years-custom` (grid auto-fit chip'ler + özel input),
     `.proj-vesting` (yeşilimsi border, vurgu).
- **Dokunulan dosyalar:** `backend/src/Finans.Application/Portfolio/BesProjectionCalculator.cs`,
  `backend/src/Finans.Infrastructure/Services/HoldingService.cs`,
  `backend/tests/Finans.Application.Tests/Portfolio/BesProjectionCalculatorTests.cs`,
  `packages/shared/src/types/index.ts`, `web/src/components/BesProjectionForm.tsx`,
  `web/src/routes/HoldingDetailPage.tsx`, `web/src/App.css`.
- **Test:** +5 unit (VestedRateAtEnd: <3y %0 / 3-6y %15 / 10y (no age) %60 / 10y+age 56+ %100 /
  mevcut sözleşme yılları sayılır) yeşil. Toplam projeksiyon unit: **15/15**. Web 52/52 + build temiz.
- **Karar/Not:** "Emeklilik" preset için doğum yılı şart — yoksa preset gizlenir, kullanıcı
  ayarlardan girebilir. Mevcut sözleşmesi olan kullanıcı için "3. yıl"/"6. yıl"/"10. yıl"
  preset'leri **kalan süre**yi gösterir (1 yıl alt sınır). Sonuç kartında 56 yaş kademesi yalnız
  `BirthYear` set ise %100'e ulaşır.
- **Durum:** tamamlandı.
- **Sıradaki:** T2.8 (Gözlemlenebilirlik) ya da T-BES.4 (devlet katkısı yıllık üst sınır).

## 2026-06-01 · T-BES.5 — BES eğitici projeksiyon (varsayımsal birikim illüstrasyonu)
- **Görev(ler):** T-BES.5 (08-BACKLOG, T-BES epik).
- **Tanı:** Kullanıcının "ne kadar biriktirebilirim?" sorusunu yatırım tavsiyesi vermeden, kendi
  varsayımlarıyla hesaplanmış bir illüstrasyon olarak göster (CLAUDE.md §2).
- **Ne yapıldı (saf hesap):**
  1. `BesProjectionCalculator.Project(input)` — aylık iteratif simülasyon: her ay başında
     `ownMonthly` yatırılır, oranı ödeme ayına göre devlet katkısı hesaplanır
     (`BesRules.StateContributionRateOn` — 2026 öncesi %30, sonrası %20); ay sonu fon `(1+r_m)` ile
     compound. `r_m = (1+r_y)^(1/12)−1` (double → decimal yaklaşıklığı, ~15 ondalık; illüstrasyon
     için yeterli). Final özet + her yıl sonu için snapshot (yıllık seri).
  2. `BesProjectionInput`/`Result`/`Year` record'ları. Validasyon: yıl 1-50, getiri -0,99..+2,0,
     aylık ≥0.
  3. own/state dilimleri tabandaki orana göre (her ikisi aynı r ile büyüdüğünden bu doğru).
- **Ne yapıldı (servis + endpoint):**
  4. `IHoldingService.ProjectBesAsync` + impl: BES değilse 400, IDOR 404; calculator
     `ArgumentException` → `ValidationException` zarfı.
  5. `POST /api/holdings/{id}/bes/projection` — `BesProjectionRequest`/`BesProjectionResult`.
- **Ne yapıldı (shared+web):**
  6. `@finans/shared` tipler + `projectBes` api.
  7. `useBesProjection(id)` mutation — saf hesap, invalidate yok.
  8. `BesProjectionForm` bileşeni: kalıcı **disclaimer**, aylık katkı (plan varsa ön-doldur),
     süre select (1-30 yıl), yıllık getiri input + chip önerileri (%15/25/35/50). Sonuç kartı:
     fon değeri (hero), own/state için ayrı yatırılan/değer/kâr-zarar kartları, yıllık seri
     tablosu. Açıklama dipnotu: vergi/enflasyon/komisyon dahil değil, devlet katkısı gecikmesi
     ihmal edildi, **yatırım tavsiyesi değildir**.
  9. `HoldingDetailPage`: BES aksiyon satırında "📊 Eğitici senaryo" butonu + modal.
  10. CSS: `.bes-proj`, `.proj-disclaimer` (altın dashed border, dikkat çekici), `.proj-hero`,
      `.proj-card`, `.proj-rate-chips`, `.proj-series` (mobilde tek sütun).
- **Dokunulan dosyalar:** `backend/src/Finans.Application/Portfolio/BesProjectionCalculator.cs` (yeni),
  `backend/src/Finans.Application/Portfolio/PortfolioDtos.cs`,
  `backend/src/Finans.Application/Portfolio/IHoldingService.cs`,
  `backend/src/Finans.Infrastructure/Services/HoldingService.cs`,
  `backend/src/Finans.Api/Controllers/HoldingsController.cs`,
  `backend/tests/Finans.Application.Tests/Portfolio/BesProjectionCalculatorTests.cs` (yeni),
  `backend/tests/Finans.Integration.Tests/PortfolioApiTests.cs`,
  `packages/shared/src/types/index.ts`, `packages/shared/src/api/index.ts`,
  `web/src/lib/hooks.ts`, `web/src/components/BesProjectionForm.tsx` (yeni),
  `web/src/routes/HoldingDetailPage.tsx`, `web/src/App.css`.
- **Test:** 10 yeni unit (BesProjectionCalculator: sıfır getiri / pozitif getiri orantısal /
  yıllık seri sayısı / oran tarih yıla göre değişir / sıfır aylık / 5 geçersiz girdi parametrize)
  yeşil. 4 yeni integration (zero_growth / non_bes_400 / IDOR_404 / invalid_400) — VS Api kilidi
  bırakılınca koşulacak. Web 52/52 + vite build temiz.
- **Karar/Not:** Devlet katkısının ~1 ay gecikmeli yatması bu illüstrasyonda göz ardı edildi
  (kullanıcının "varsayımsal sonuç" amacında detay; disclaimer'da belirtildi). Vergi/komisyon/
  enflasyon yok — gerçek getiri farklı olur. T-BES.4 yıllık üst sınır ileride eklenirken bu hesaba
  da iliştirilmeli (ileri tarihli yıllar için brüt asgari ücret bilinmediğinden şu an açık tutuldu).
- **Durum:** tamamlandı (kod). VS Api kilidi bırakılınca integration testleri koşulacak.
- **Sıradaki:** Faz 2 dağıtım altyapısı — **T2.8** (Seq + Prometheus + Grafana) ya da T-BES.4
  (devlet katkısı yıllık üst sınır).

## 2026-06-01 · BES fon getirisi: own + state için ayrı kâr/zarar (T-BES.10)
- **Görev(ler):** T-BES.10 (ad-hoc) — kullanıcı: "Devlet katkısı da fon üzerinden işletiliyor, kâr elde
  ediliyor; katkı payına yapılan fon getirisi gibi devlet katkısı için de fon getirisi hesaplanmalı."
- **Tanı:** Mevcut model "maliyet = own (cebimden ödenen)" idi; devlet katkısı "bonus" sayılıyordu — kendi
  kâr/zararı görünmüyor, yalnız `stateContribution` toplamı vardı. Oysa fon değeri **own + state birikiminin
  toplamı** üzerinde büyüyor; aynı oran her iki katkıya da işliyor.
- **Karar:** Düşük invaziv yol — Hero "yatırım performansım" (own perspektifi) değişmedi; yalnız BES split
  bölümünde **her iki katkı için ayrı güncel değer + kâr/zarar** göster. Fon getirisi `r = fund/(own+state)−1`.
- **Ne yapıldı (saf hesap, BesCalculator):**
  1. `BesFundReturn` (record struct: Rate, OwnValue, OwnProfit, StateValue, StateProfit).
  2. `BesCalculator.FundReturnFor(own, state, fundValue?)` — taban 0 veya fundValue null ise oran null +
     değerler tabana eşit (0 kâr/zarar); aksi halde `r` ve `own*r`/`state*r` (2 ondalık yuvarlama; ratio
     yuvarlanmaz).
- **Ne yapıldı (servis + DTO):**
  3. `BesDto` 5 yeni alan: `FundReturnRatio?`, `OwnValue`, `OwnProfit`, `StateValue`, `StateProfit`.
  4. `HoldingService.ToBesDto` `fundValue` parametresi alır (`Holding.CurrentPrice`); helper'ı çağırır.
- **Ne yapıldı (shared+web):**
  5. `@finans/shared` `Bes` tipine 5 yeni alan.
  6. `HoldingDetailPage` BES split: her iki katkı altında **mini-satır** — "Güncel değer · Kâr/Zarar (renk
     tonlu) · oran". Açıklamalar genişletildi (devlet katkısı da fonda işletilir).
  7. CSS: `.bes-fund-row` — başlığın altına dashed separator + 3 sütun (etiket / değer / kâr-zarar).
- **Dokunulan dosyalar:** `backend/src/Finans.Application/Portfolio/BesCalculator.cs`,
  `backend/src/Finans.Application/Portfolio/PortfolioDtos.cs`,
  `backend/src/Finans.Infrastructure/Services/HoldingService.cs`,
  `backend/tests/Finans.Application.Tests/Portfolio/BesCalculatorTests.cs`,
  `backend/tests/Finans.Integration.Tests/PortfolioApiTests.cs`,
  `packages/shared/src/types/index.ts`, `web/src/routes/HoldingDetailPage.tsx`, `web/src/App.css`.
- **Test:** 4 yeni unit (FundReturnFor: standart kazanç + kayıp + null fund + sıfır taban) yeşil
  (Application.Tests). 1 yeni integration (Bes_holding_exposes_fund_return_for_own_and_state) — VS Api kilidi
  bırakılınca koşulacak. Web 52/52 yeşil + vite build temiz.
- **Karar/Not:** Hero kâr ve portföy TotalCost önceki "maliyet = own" modeliyle devam eder — devlet katkısı
  "bonus" olarak yatırım performansını şişirmeye devam (kullanıcının bilinçli kararı). Yeni alanlar **ek
  görüntü** (split satırında); ileride hero'yu fon getirisine geçirmek istersek küçük bir adım kalır.
- **Durum:** tamamlandı (kod). VS Api kilidi bırakılınca Integration testleri koşulacak.
- **Sıradaki:** sıradaki — T-BES.5 (BES fon dağılımı eğitici projeksiyon).

## 2026-06-01 · İşlem geçmişine düzenle/sil — backend uçları + servis recompute + web UX
- **Görev(ler):** ad-hoc (T-TX.1) — kullanıcı geri bildirimi: "İşlem Geçmişi'nde sil/güncelle yok, BES katkı
  geçmişiyle aynı olmalı."
- **Tanı:** Backend'de yalnız `POST /api/holdings/{id}/transactions` vardı; PUT/DELETE yoktu. UI tarafında
  `TransactionHistory` salt-listesi (BES `BesContributionHistory` ✎/🗑 ikonlarına eşdeğer yok).
- **Ne yapıldı (backend):**
  1. `IHoldingService.UpdateTransactionAsync` + `DeleteTransactionAsync` eklendi.
  2. `HoldingService` implementasyonu: BES'i reddet (`not_allowed_for_bes`), `LoadOwnedWithTransactionsAsync`
     ile **UserId-scoped** yükle (IDOR yok), tx'i bul/yoksa 404, `ApplyDerivedPosition` ile **Miktar/AvgCost
     işlemlerden yeniden türetilir**. Delete: son işlem için 400 (`cannot_delete_last`, "Pozisyonu sil"e yönlendir).
  3. `HoldingsController`: `PUT/DELETE /api/holdings/{id}/transactions/{transactionId}`.
- **Ne yapıldı (shared+web):**
  1. `@finans/shared` api: `updateTransaction`, `deleteTransaction`. Tip: mevcut `TransactionInput` reuse.
  2. `lib/hooks`: `useUpdateTransaction`, `useDeleteTransaction` — başarıda holding + portföy invalidate.
  3. `TransactionHistory`: opsiyonel `onEdit`/`onDelete` → BES'tekiyle aynı ✎/🗑 ikonları + "İşlem" sütunu
     (bağlanmamışsa sütun gizlenir; geriye dönük uyumlu).
  4. `HoldingDetailPage`: düzenleme **modalı** (tür/miktar/birim fiyat/tarih — nakit modu birim fiyat
     gizler) + silme **ConfirmDialog**. Hata kullanıcıya `notify` ile aktarılır (son işlem silme uyarısı).
- **Dokunulan dosyalar:** `backend/src/Finans.Application/Portfolio/IHoldingService.cs`,
  `backend/src/Finans.Infrastructure/Services/HoldingService.cs`,
  `backend/src/Finans.Api/Controllers/HoldingsController.cs`,
  `backend/tests/Finans.Integration.Tests/PortfolioApiTests.cs`,
  `packages/shared/src/api/index.ts`, `web/src/lib/hooks.ts`,
  `web/src/components/TransactionHistory.tsx`, `web/src/routes/HoldingDetailPage.tsx`.
- **Test:** 5 yeni integration (Update_recomputes, Delete_recomputes, Delete_last→400, IDOR_PUT/DELETE→404,
  Update_BES_400). Tüm backend integration + 52/52 web testi yeşil. Senaryolar: SC-06 (ort. maliyet türetimi)
  ve SC-13 (IDOR 404) kapsanır.
- **Karar/Not:** Son işlemi silmek yasak — pozisyonu sıfır miktarda bırakmak kullanıcı niyetiyle örtüşmez;
  bunun yerine danger-zone "Pozisyonu sil" var. BES'te düz tx düzenle/sil yok (zaten katkı geçmişinde
  ✎/🗑 var). Düzenleme tarih bırakılırsa mevcut tarih korunur (request.Date null path).
- **Durum:** tamamlandı.
- **Sıradaki:** sıradaki — T-BES.5 (BES fon dağılımı eğitici projeksiyon).

## 2026-06-01 · BES TR-saat dilimi + Plan-source dedup (manuel girişler planı engellemez)
- **Görev(ler):** ad-hoc — kullanıcı geri bildirimi: (a) "1 Haziran katkı payı ödemesi gerçekleşmedi, bugün
  01.06 oldu ama devlet bekliyor durumuna gelmedi", (b) "Düzenli plan dışında manuel giriş ayda birden çok
  defa olabilir; manuel girişler planı engellememeli, plan yalnız ay başına 1 kayıt."
- **Tanı (iki ayrı kök):**
  1. **Saat dilimi:** Tüm BES tarih karşılaştırmaları `DateTime.UtcNow` ile yapıyordu. TR UTC+3 (DST yok);
     kullanıcı yerel saatte 01.06 olsa bile UTC hâlâ 31.05 olabilir. Sonuç: catch-up gün geçişinde
     tetiklenmiyor, ayrıca `paidAt=2026-06-01T00:00:00Z` olan kayıt `paidAt.Date > UtcNow.Date` çıkıp
     **Future** statüsünde kalıyordu (devlet bekliyor görünmüyordu).
  2. **Dedup birleşik:** `CatchUpBesPlanAsync` ve `GenerateBesContributionsAsync` "bu ayın herhangi bir kaydı"
     varsa o ayı atlıyordu. Manuel giriş varsa Plan tetiklenmiyordu — kullanıcının zihin modeli ile uyumsuz.
- **Ne yapıldı (saf hesap + servis):**
  1. **`HoldingService.TrNow()`** helper (UTC+3 sabit, TR'de DST yok). Kullanım: `ApplyReadPosition` BES
     dalı (`paidAt.Date ≤ today`), `ToBesDto` (durum/vesting/contributionDue için `asOf`).
  2. **`ToBesDto(... DateTime asOf)`** — parametre adı `nowUtc`→`asOf` (yanıltıcı Utc suffix kaldırıldı;
     çağıran timezone seçer). `BuildHoldingDtosAsync` artık `TrNow()` geçirir.
  3. **Plan-source dedup (kullanıcı kararı):** `IsPlanSource(source)` helper. `CatchUpBesPlanAsync`:
     `lastPlanPaid` ve `coveredPlanMonths` artık yalnız `Source=="Plan"` satırları sayar — manuel/Opening
     girişler engellemez. `GenerateBesContributionsAsync`: aynı şekilde `coveredPlanMonths` Plan-only.
  4. **CatchUp `from` heuristiği:** lastPlanPaid yoksa "bu aydan" başla (geçmiş aylar için plan satırı
     **geriye dönük üretilmez** — kullanıcının manuel geçmişiyle çakışıp düzinelerce sahte plan kaydı
     oluşmasın). Backfill için "Düzenli katkı/geçmiş" formu hâlâ kullanılabilir (zaten Plan-source ile).
  5. **CatchUp `now`** artık `TrNow()` — TR'de 01.06 00:00 geçince catch-up devreye girer.
- **Davranış değişimi (kullanıcı senaryosu):**
  - Bugün TR 01.06, plan günü=1, plan aktif → catch-up otomatik Plan-source 01.06 satırı ekler (manuel
    girişler engel değil); statü doğrudan **StatePending** (devlet bekliyor). Sayfayı açtığında görür.
  - Aynı ay birden çok manuel katkı → serbest (tek-ekleme formunda zaten dedup yok), plan ayrı serisini
    yürütür. Manuel + Plan satırları yan yana sıralanır.
- **Dokunulan dosyalar:** backend `Finans.Infrastructure/Services/HoldingService.cs` (TrNow + IsPlanSource +
  ApplyReadPosition TR-date + ToBesDto rename + Generate Plan-only dedup + CatchUp Plan-only dedup + TR-now).
- **Test:** backend **Application.Tests 72/72** · Infrastructure derlendi (0 hata) · web **52/52** + build temiz.
  ⚠ Integration koşumu VS Api DLL kilidi (PID 20864) nedeniyle bekliyor. Mevcut integration testleri davranışla
  uyumlu (paidAt günü Generate testinde 2025/9-11 → durum Deposited; UTC↔TR farkı ~3 saat status sonucunu
  bozmaz).
- **Karar/Not:** TR sabit UTC+3 (Europe/Istanbul, DST 2016'dan beri kaldırıldı) → AddHours(3) güvenli;
  Multi-tz kullanıcı geldiğinde `IBesTimezone` servisine taşınabilir. Plan-source dedup, catch-up'ın
  "her ay TAM bir Plan satırı" prensibini korur — manuel girişler özgürce eklenebilir.
- **Durum:** kod tam · web tam yeşil · backend birim yeşil. Integration koşumu VS'ye bağlı.
- **Sıradaki:** Commit + push (kullanıcı isteği) → sonra T-BES.5.

## 2026-05-31 · Düzenli BES katkı planı: "0 kayıt eklendi" hayalet fix + önizleme
- **Görev(ler):** ad-hoc — kullanıcı geri bildirimi: "Düzenli katkı/geçmiş ile geçmiş tarihli katkı ekliyorum
  ama hiçbir şey kaydedilmiyor."
- **Tanı:** `BesContributionPlanner.MonthlyDates` filtresi çok katıydı: `payDate ∈ [from, to]`. Form metni
  "aralıktaki her ay" diyordu ama mantık sadece günü aralık içinde KALAN ayları üretiyordu. Örn. `from=01.05,
  to=01.06, day=15` → Mayıs(15.05) dahil ama Haziran(15.06>01.06) düşüyordu. `from=20.04, to=10.05, day=15`
  → her iki ay da düşüyor → **0 kayıt** üretiliyordu. Backend "başarılı" dönüyor, kullanıcıya hiçbir feedback
  yok → kullanıcı "kayıt etmiyor" diye yaşıyor.
- **Ne yapıldı:**
  1. **Planner fix (saf hesap):** `MonthlyDates` artık `[from.month, to.month]` aralığındaki **her aya** ödeme
     üretir; gün 1–28'e clamp. Eski "day-in-range" filtresi kaldırıldı. Form metniyle ("aralıktaki her ay")
     tutarlı. İlgili unit test güncellendi (`Excludes_endpoints_outside_range` → `Includes_every_month_in_range`).
  2. **Form önizlemesi (UX):** `BesContributionPlanForm` artık gönder ÖNCESİ "Aralıkta <b>N</b> ay · <b>K</b>
     yeni kayıt eklenecek · <b>M</b> ay zaten kayıtlı (atlanacak)" satırı gösterir (mint/yeşil; willAdd=0 ise
     coral/sarı uyarı + buton **disabled** + "Tarih aralığını değiştir" ipucu). Frontend hesabı = backend
     planner ile birebir (her ay, idempotent ay bazlı dedup).
  3. **Form düzeni:** Submit butonu `.tx-row` dışına çıktı → tam-genişlik gold buton; preview tam görünür.
     CSS: `.tx-form > button[type="submit"]` (yeni stil + disabled) + `.tx-form .preview` (mint kutu) +
     `.tx-form .preview.warn` (coral, willAdd=0).
  4. **Toast geri bildirimi:** Başarılı submit'te `onBesPlanDone(addedCount)` — count > 0 ise "<b>N</b> katkı
     kaydı oluşturuldu" (success); 0 ise "Seçtiğin ayların hepsi zaten kayıtlıydı — yeni kayıt eklenmedi"
     (info). Kullanıcı sessiz başarısızlık yaşamaz.
  5. **Prop sözleşmesi:** `BesContributionPlanForm` artık `existingContributions: BesContribution[]` alır
     (önizleme için); HoldingDetailPage `h.bes.contributions` geçer.
- **Dokunulan dosyalar:** backend `Finans.Application/Portfolio/BesContributionPlanner.cs`; test
  `BesContributionPlannerTests.cs` (Excludes → Includes); web `components/BesContributionPlanForm.tsx`
  (preview + prop + outside submit); `routes/HoldingDetailPage.tsx` (existingContributions geç + onBesPlanDone
  count parametresi); `App.css` (.tx-form > submit + .preview/.warn).
- **Test:** backend **Application.Tests 72/72** (planner unit testleri yeşil) · Infrastructure derlendi (0 hata)
  · web **52/52** + build temiz. ⚠ Integration koşumu VS Api DLL kilidi (yeni PID 20864) nedeniyle bekliyor;
  Generate integration testi (3 kayıt, 2025/09-11) yeni davranışla da geçer (aralık 3 ay → 3 kayıt; eski
  filtre ile de aynıydı çünkü gün ortada).
- **Karar/Not:** Davranış değişikliği geri uyumlu değil ama gerçek kullanım yalnız iyileşir (önceden silent
  drop olan uç-aylar artık üretilir, idempotent dedup zaten ay bazlı). Önizleme sayesinde kullanıcı tek
  bakışta kaç kaydın oluşturulacağını görür.
- **Durum:** kod tam · web tam yeşil · backend birim yeşil · Integration koşumu VS'ye bağlı.
- **Sıradaki:** VS Api kilidi bırakılınca `dotnet test backend/Finans.slnx`; sonra T-BES.5.

## 2026-05-31 · BES UX turu (kullanıcı geri bildirimi 4 madde)
- **Görev(ler):** ad-hoc — kullanıcı feedback'i: (1) listede sağdaki "…" truncation kalksın, (2) "Durum" kolonu
  gereksiz (lejant açıklıyor) kaldır, (3) "Kendi katkın" → **"Katkı Payı"**, (4) "Bekleyen" devlet katkısı =
  geçmiş listesindeki "Gelecek Ödeme" satırının devlet değeri (eşleşsin), (5) modal form alanları pencere
  genişliğine yayılsın.
- **Ne yapıldı:**
  1. **`BesContributionHistory.tsx`** — Durum kolonu kaldırıldı (Tarih/Katkı Payı/Devlet/İşlem 4 sütun).
     Renk durumu zaten sol şerit (`.hist-deposited/.hist-pending/.hist-future`) + lejant ile gösteriliyor.
  2. **`App.css `.holdings-table.fit`** — `text-overflow: ellipsis` kaldırıldı (üç-nokta YOK; değerler tam
     görünür). `white-space: nowrap` korundu (tek satır).
  3. **`App.css `.tx-row input/select` + `.add-form input/select`** — `width: 100%; box-sizing: border-box`
     eklendi. Etiket flex-column içinde input artık hücreyi tam doldurur → modal genişliğine yayılır.
  4. **`HoldingService.ToBesDto`** — `statePending` artık **yalnız Future** satırların state'ini toplar
     (önceki: Future + StatePending). Geçmiş listesindeki "Gelecek Ödeme" satırının devlet değeri ile birebir
     eşleşir. StatePending satır (own ödendi, devlet henüz yatmadı) **"yolda"**: tabloda görünür, hiçbir toplama
     girmez. Test güncellendi (`Bes_contribution_increases_own_state_and_cost`: bugün eklenen katkı StatePending
     → statePending=0).
  5. **"Kendi katkın" → "Katkı Payı"** (tutarlı, BES standart terminolojisi): `BesContributionForm` ("Katkı Payı
     (TRY)"), `AddHoldingDialog` ("Birikmiş katkı payın"), `HoldingDetailPage` ("Yatırılan Katkı Payı" + alt
     açıklama metinleri). İlgili testler güncel.
- **Dokunulan dosyalar:** web `components/BesContributionHistory(.test).tsx`, `BesContributionForm(.test).tsx`,
  `AddHoldingDialog(.test).tsx`, `routes/HoldingDetailPage.tsx`, `App.css`. backend `Finans.Infrastructure/Services/
  HoldingService.cs` (ToBesDto switch); test `BesAndHistoryApiTests.cs` (StatePending=0 + comment).
- **Test:** web **52/52** + build temiz · backend **Application.Tests 72/72** · Infrastructure derlendi (0 hata).
  ⚠ Integration test koşumu VS Api DLL kilidi nedeniyle bekliyor (kod hatası yok — Infrastructure compile
  yeşil ve mantık değişikliği basit toplam değişimi).
- **Karar/Not:** StatePending satırının state'i hiçbir toplama girmez ("yolda" — Yatırılan ve Bekleyen arasında
  geçiş halinde). Bu, ownPending ile statePending'i simetrik (her ikisi de Future-only) tutar; kullanıcının
  beklediği "geçmiş listesindeki Gelecek Ödeme satırının devlet miktarı ile Bekleyen toplamının eşit olması"
  ilkesini sağlar. Toplam sum (Yatırılan + Bekleyen) tablo toplamından StatePending state kadar az olabilir;
  kabul edilebilir — kullanıcı satırı tabloda görür.
- **Durum:** kod tam · web tam yeşil · backend birim yeşil · Integration koşumu VS'ye bağlı.
- **Sıradaki:** VS Api kilidi bırakılınca `dotnet test backend/Finans.slnx` + migration uygulaması; sonra T-BES.5.

## 2026-05-31 · T-BES.9 kapanış — tüm testler yeşil (134 backend + 52 web)
- **Görev(ler):** T-BES.9 finalize (önceki turdan kalan): migration uygulama + integration test koşumu + 4 küçük fix.
- **Ne yapıldı (kapanış):**
  1. **Migration generated:** `dotnet ef migrations add AddBesBirthYear` — `BesDetails.BirthYear int?` AddColumn
     üretildi (önceki turdaki dosya boş stub'tı; sildim, yeniden oluşturdum, snapshot güncel). EF
     `PendingModelChangesWarning` artık atılmıyor — health/correlation testleri startup'ta çökmüyor.
  2. **`createBes` shared client eklendi** (önceki turda import eklenmişti ama fonksiyon body unutulmuş).
  3. **ApplyReadPosition fix:** İşlem yoksa saklanan değerleri SİLMEZ (Nakit gibi alış/satış işlemi olmayan
     pozisyonlar 0'a düşmesin). Önceki tur regression'ı (gold weight 0.310→0.312); doğru fix uygulandı.
  4. **Test fixture izolasyonu:** `BesContributionPlanApiTests.Generate_creates_monthly_records` — sınıf
     fixture'ı paylaşımlı (`IClassFixture<Sqlite…>`); önceki `Generate_allows_future_range` testi 2099 Plan
     kayıtları bırakıyordu → planRows count testin tarih aralığına filtrelendi (2025/9-11).
  5. **Web test fixture düzeltmeleri:** `BesContributionHistory.test.tsx` — tarih biçimi (gg.aa.yyyy) +
     lejant-tbody ayrımı (`within(tbody)` kapsama).
- **Test (final, hepsi YEŞİL):**
  - backend **Application.Tests 72** (+12 yeni: StateDepositDateFor / ContributionStatusFor / VestedRateFor / AgeFor)
  - backend **Integration.Tests 62** (BES create/edit/plan + ileri-tarih + statü; SqliteWebApplicationFactory)
  - web **52** (vitest); `npm run build` (tsc+vite) temiz; eslint 0
  - backend `Api.csproj` derleme: 0 hata, 0 uyarı
- **Dokunulan dosyalar (kapanış):** `packages/shared/src/api/index.ts` (+createBes); `web/src/components/BesContributionHistory.test.tsx`
  (fixture + within); `backend/src/Finans.Infrastructure/Services/HoldingService.cs` (ApplyReadPosition no-tx korur);
  `backend/src/Finans.Infrastructure/Persistence/Migrations/20260531213800_AddBesBirthYear.{cs,Designer.cs}` (yeni);
  `backend/src/Finans.Infrastructure/Persistence/Migrations/FinansDbContextModelSnapshot.cs` (BirthYear); `backend/tests/Finans.Integration.Tests/BesContributionPlanApiTests.cs` (filtre).
- **Karar/Not:** Migration **henüz canlı Postgres'e uygulanmadı** — VS'de `Update-Database` veya CLI'da User
  Secrets parolasıyla `dotnet ef database update` gerek (additive, nullable; güvenli). T-BES.9 kod açısından
  TAMAM, yalnız migration uygulaması bekliyor.
- **Durum:** kod tam · tüm test paketleri yeşil · migration canlı Postgres'e uygulama bekliyor (VS).
- **Sıradaki:** Migration uygula; sonra T-BES.5 (fon dağılımı eğitici projeksiyonu).

## 2026-05-31 · Ort. maliyet: okuma anında kaynaktan türet (anti-stale) + geçmiş listesi sol yüksekliğe uyar
- **Görev(ler):** ad-hoc (kullanıcı geri bildirimi: altın+BES ort. maliyet yanlış; geçmiş listeleri sol içerik
  yüksekliğine uysun)
- **Tanı:** Ort. maliyet zaten **dinamik** türetiliyordu AMA okuma yolu saklanan `Holding.AvgCost` cache
  alanını gösteriyordu (`BuildHoldingDtos` satır 436 `h.AvgCost`). Bu alan yalnız **yazma** işlemlerinde
  güncelleniyor (`ApplyDerivedPosition`/`ApplyBesTotals`); commit 2c4f3d7 (BES own-only) öncesi yazılmış
  kayıtlarda eski own+state kalmış → ekranda BES 350.573 (−%20,3) görünüyordu, doğrusu own-only 277.060 (+%0,8).
  Altın zaten doğruydu (6.068,69) çünkü işlemleri sonradan değişmemiş.
- **Ne yapıldı:**
  1. **Anti-stale FIX (backend):** `BuildHoldingDtosAsync` artık `Transactions`'ı da Include ediyor ve her
     holding için yeni **`ApplyReadPosition`** çağırıyor — okuma anında pozisyonu KAYNAKTAN yeniden türetir:
     BES → `AvgCost = OwnContribution` (cepten ödenen kendi katkı), diğer → `DerivePosition` ile işlemlerden
     ağırlıklı ort. + miktar. **Salt okunur** (SaveChanges yok); saklanan cache sürüklenmiş olsa bile gösterim
     daima doğru ve kendi içinde tutarlı (totalCost = avgCost×qty). Böylece backend restart'a gerek kalmadan
     mevcut DB kayıtları da düzelir.
  2. **Geçmiş listesi yüksekliği (CSS):** `.detail-grid` `align-items: start`→**`stretch`** (sağ sütun sol
     yüksekliğe uzar); `.detail-col` +`min-height:0`; **`.detail-col > .card`** `flex:1; column; min-height:0`
     (kart sütunu doldurur); `.history-scroll` `max-height:320px`→**`flex:1; min-height:0`** (sol yüksekliği
     doldurur; içerik fazlaysa İÇERİDE dikey kayar, yatay yok); mobil (≤860px) `.history-scroll max-height:60vh`.
     İşlem + Katkı geçmişine ortak uygulanır.
- **Dokunulan dosyalar:** backend `Finans.Infrastructure/Services/HoldingService.cs` (Include Transactions +
  `ApplyReadPosition` + döngü). web `src/App.css` (`.detail-grid`, `.detail-col`, `.detail-col>.card`,
  `.history-scroll`, mobil media).
- **Test:** web **49** geçti · `npm run build` temiz · backend Application.Tests **59** + Infrastructure derlendi
  (0 hata; 2 pre-existing IPAddress warn, alakasız). ⚠ Integration testleri VS Api DLL kilidi yüzünden bu turda
  koşulamadı (kod sorunu değil).
- **Karar/Not:** Okuma yolu artık cache'e güvenmiyor — kaynak (işlem/katkı) tek doğruluk kaynağı (§6, CLAUDE.md
  §3.1 deterministik hesap). Bu, yazıdaki cache drift sınıfını kökten çözer (yalnız bu vaka değil). Yazma yolu
  hâlâ cache'i günceller (dashboard/summary hızlı okuma) — tutarlı. **TODO (öneri):** eski `Holding.AvgCost`
  cache alanı tamamen kaldırılabilir mi diye değerlendir (ayrı görev; PortfolioCalculationService.CalculateHoldings
  imzası buna bağlı).
- **Durum:** tamam (web + Application/Infrastructure derleme/test yeşil); integration testleri VS kilidi bırakılınca
- **Sıradaki:** VS Api durdurulunca `dotnet test backend/Finans.slnx`; sonra T-BES.5

## 2026-05-31 · Tarih girişi — native date input + ileri tarih serbest + klavye/Tab/takvim UX
- **Görev(ler):** ad-hoc (kullanıcı geri bildirimi: ileri tarihli katkı engellenmesin; tarih autocomplete/takvim +
  ↑↓ artır-azalt + ←→ segment geçişi; Tab ile alanlar arası geçiş çalışsın)
- **Ne yapıldı:**
  1. **`DateField` → native `<input type="date">`** (özel maskeli metin alanı kaldırıldı). Tarayıcı tek başına:
     takvim açılır (autocomplete), ↑/↓ gün-ay-yıl artır-azalt, ←/→ segment geçişi, **Tab** ile gezinme. Değer
     sözleşmesi aynı (ISO `YYYY-MM-DD`). `min` prop'u eklendi. `src/lib/dateMask.ts` silindi (artık gereksiz).
  2. **İleri tarih serbest:** Tüm katkı/işlem DateField'larında `max={today}` kaldırıldı (yalnız BES **başlangıç
     tarihi**/joined date'te kaldı — sözleşme geçmişte başlar). Backend'de `must_not_be_future` kontrolleri
     kaldırıldı: `AddBesContribution`, `UpdateBesContribution`, `GenerateBesContributions` (artık istenen aralık
     aynen üretilir, gelecek aylar dahil). Plan formu metni güncellendi (ileriye dönük plan).
  3. **Tema:** native date input için `input.date-input { color-scheme: dark }` + takvim ikonu altın tonu (filter);
     koyu temaya ve mevcut input stiline (`.tx-row input`) tam uyumlu. Tab geçişi native input'larla otomatik düzeldi.
- **Dokunulan dosyalar:** web `components/DateField.tsx` (yeniden yazıldı), `DateField.test.tsx` (native testler),
  `AddTransactionForm.tsx`, `BesContributionForm(.test).tsx`, `BesContributionPlanForm.tsx`, `routes/HoldingDetailPage.tsx`,
  `App.css` (date-input teması); `lib/dateMask.ts` (silindi). backend `Finans.Infrastructure/Services/HoldingService.cs`
  (3 future-check kaldırıldı), `Finans.Application/Portfolio/PortfolioDtos.cs` (doc). testler `BesContributionPlanApiTests.cs`
  (+Generate_allows_future_range), `BesContributionEditApiTests.cs` (+Add_and_edit_allow_future_paid_date)
- **Test:** web **49** geçti · `npm run build` temiz · backend Domain/Application/Infrastructure + Application.Tests
  **derlendi (0 hata)**. ⚠ Integration testleri VS Api'yi (PID kilidi) bıraktıktan sonra çalıştırılacak (DLL kopyalama
  kilidi; kodum değil). Yeni 2 integration testi yazıldı (ileri-tarih serbest doğrular)
- **Karar/Not:** Native `<input type=date>` seçildi (kullanıcı onayı) — takvim/ok-tuş/segment/Tab davranışı native,
  bug riski en düşük; "önceki gibi autocomplete" buydu. Gösterim biçimi tarayıcı diline bağlı (TR→gg.aa.yyyy);
  salt-gösterim tarihleri yine `formatDate` ile garanti gg.aa.yyyy (NFR-7). BES joined-date'te ileri tarih hâlâ
  yasak (sözleşme geçmişte başlar). İleri tarihli katkıda devlet katkısı oranı ödeme tarihine göre (≥2026 → %20).
- **Durum:** kod tamam; backend integration test çalıştırma VS'ye bağlı (bekliyor)
- **Sıradaki:** VS Api durdurulunca `dotnet test backend/Finans.slnx`; sonra T-BES.5

## 2026-05-31 · UX fix — İşlem ekle modalı: scroll/kırpma yerine alana sığan ızgara
- **Görev(ler):** ad-hoc (kullanıcı geri bildirimi: modal scroll/kırpma kabul edilemez, temaya uygun olmalı)
- **Ne yapıldı:** "İşlem ekle" modalında 3 alan (Miktar/Birim fiyat/İşlem tarihi) + buton dar modala yatay
  zorlanıp tarih alanı kırpılıyor ve scroll çıkıyordu. `.tx-row` grid `1fr 1fr auto` → **`repeat(auto-fit,
  minmax(150px, 1fr))`**; gönder butonu `grid-column: 1 / -1` ile kendi satırında tam genişlik. Modal
  `max-width` 480→**540px**. Geniş alanda 3 alan yan yana sığar, dar/mobilde alt alta iner — hiçbir
  koşulda yatay scroll/kırpma yok. Tarih alanı zaten `.tx-row input` temasını alıyor (tutarlı).
- **Dokunulan dosyalar:** web `src/App.css` (`.modal` max-width, `.tx-row` grid + submit full-width)
- **Test:** web **51** geçti · `npm run build` (tsc/vite) temiz
- **Karar/Not:** İlke — geliştirmelerde scroll'a kaçmadan alana sığan/uyarlanan, temaya uygun layout
  varsayılan olsun (kullanıcı geri bildirimi, kalıcı kural; bellekte de saklandı)
- **Durum:** tamam
- **Sıradaki:** T-BES.5 (fon dağılımı eğitici projeksiyonu) — değişmedi

## 2026-05-31 · BES — maliyet=kendi katkı + katkı düzenle/sil + düzenli plan (checkbox+otomatik devam) + geçmiş UX (T-BES.6b/7)
- **Görev(ler):** T-BES.6b (tamam), T-BES.7 (tamam) — kullanıcı geri bildirimi (6 madde).
- **Ne yapıldı:**
  1. **Maliyet = kişinin CEPTEN ödediği = yalnız kendi katkı** (own-only). `Holding.AvgCost = OwnContribution`
     (önceki own+state bırakıldı). Devlet katkısı fon değerinde → getiriye yansır. Seed BES AvgCost 148.554→120.000;
     etkilenen seed-toplam testleri güncellendi (totalCost 603.770→575.216, getiri %39→%45,9, reel %0,72→%5,72).
  2. **Katkı düzenle/sil:** `PUT/DELETE /holdings/{id}/bes/contributions/{cid}` — kümülatif (own/state) delta ile
     güncellenir, devlet katkısı yeni tarihin oranıyla, maliyet=own. `BesContributionDto.Id` eklendi.
  3. **Düzenli plan + otomatik devam:** `BesDetails.MonthlyAmount/ContributionDay/PlanActive` (+migration);
     katkı eklerken **"bundan sonraki katkılar için kullan"** checkbox → plan kurar; `CatchUpBesPlanAsync`
     görüntülemede (GetById) eksik ayları **otomatik üretir** (lazy; gerçek arka plan job T-BES.6b notu).
  4. **Geçmiş UX:** `.history-scroll` (dikey scroll, **yatay scroll yok**) + `.holdings-table.fit` (table-layout
     fixed → sütunlar genişliğe sığar, sticky başlık); BES geçmişinde **"Kaynak" sütunu kaldırıldı**, yerine
     **düzenle/sil ikon butonları** (`.icon-btn`). İşlem geçmişine de scroll/fit uygulandı.
  5. **Web:** shared `BesContribution.id`, `Bes.planActive/monthlyAmount`, `AddBesContributionInput.recurring`,
     `UpdateBesContributionInput`; `updateBesContribution`/`deleteBesContribution` + hook'lar; `BesContributionForm`
     recurring checkbox; `BesContributionHistory` ikon butonları (+test güncel); detayda düzenle modalı + sil onayı
     + "düzenli plan aktif" notu.
- **Dokunulan dosyalar:** backend `HoldingService.cs` (cost=own, Update/Delete contribution, CatchUp, ApplyBesTotals),
  `BesDetails.cs` (+plan alanları), `PortfolioDtos.cs`, `IHoldingService.cs`, `HoldingsController.cs`, `SeedData.cs`
  (AvgCost), migration `*_BesContributionPlan`; testler `BesContributionEditApiTests.cs`(yeni) + güncellenen seed-total
  testleri (Portfolio/Inflation/SeedConsistency/Sqlite/BesAndHistory). web `shared/{types,api}`, `lib/hooks.ts`,
  `BesContributionForm/History(.test).tsx`, `TransactionHistory.tsx`, `routes/HoldingDetailPage.tsx`, `App.css`.
  doküman `03 §A`(maliyet kararı + plan/contribution tabloları), `08`(T-BES.6b/7), `09`(SC-23).
- **Test:** backend **App 59 + Integration 60 = 119** · web **45** · shared 13 · lint/build temiz.
- **Karar/Not:** Otomatik devam = **lazy catch-up** (BES detayını açınca eksik aylar üretilir); uygulama kapalıyken
  üretim için gerçek arka plan job gerek (T-BES.6b notu, scheduler altyapısı). Catch-up `DateTime.UtcNow` kullanır →
  saat-bağımlı; deterministik birim testi yerine planner (saf) + düzenle/sil e2e ile kapsandı. **Migration canlı
  Postgres'e uygulandı** (BesContributionPlan; additive). Mevcut seed verisi idempotent (eski AvgCost değişmez).
- **Durum:** T-BES.6b, T-BES.7 tamam.
- **Sıradaki:** **T-BES.5 — fon dağılımı eğitici kâr/zarar projeksiyonu** (kullanıcının sırası).

## 2026-05-31 · BES düzenli katkı — tarih aralığından aylık kayıt üretimi + katkı geçmişi + hatırlatma (T-BES.6) + "Düzenle" buton teması
- **Görev(ler):** T-BES.6 (tamam); T-BES.6b (otomatik zamanlayıcı/plan kalıcılığı) planlandı.
- **Ne yapıldı:**
  1. **`BesContribution` tablosu** (HoldingId, OwnAmount, StateAmount, PaidAtUtc, Source) + EF config +
     **migration** (`BesContributions`). `Holding.BesContributions` nav.
  2. **Üretim:** `BesContributionPlanner.MonthlyDates` (saf; aralık→aylık ödeme tarihleri, gün 1–28 kıskaç) +
     `GenerateBesContributionsAsync` (`POST /holdings/{id}/bes/contributions`): kapsanan aylar için kayıt,
     **idempotent** (kayıtlı ay atlanır), **gelecek ay üretilmez** (to≤bugün), devlet katkısı her ayın
     **tarihindeki orana** göre. Tek katkı (`AddBesContribution`) da artık kayıt oluşturur.
  3. **Katkı geçmişi:** `BesDto.Contributions` + `ContributionDue`; detay sağ sütununda BES için
     **"Katkı Geçmişi"** (tarih/kendi/devlet/kaynak) — `Transaction` yok, bu kayıtlar gösterilir.
  4. **Hatırlatma:** son katkı bu aydan eskiyse `ContributionDue=true` → detayda **"bu ayın katkısını gir"** notu.
  5. **Web:** shared `Bes.contributions/contributionDue` + `GenerateBesContributionsInput` + `generateBesContributions`;
     `useGenerateBesContributions`; `BesContributionPlanForm` (tutar/gün/aralık) + `BesContributionHistory` (+test);
     detayda "Düzenli katkı / geçmiş" butonu + modal + due notu.
  6. **"Düzenle" buton teması:** tanımsız `.link` → varsayılan buton chrome (kutu) görünüyordu; temalı
     `.edit-link` (gold inline-link, hover/focus) eklendi, buton ona geçti.
- **Dokunulan dosyalar:** backend yeni `Domain/Portfolio/BesContribution.cs`, `Application/Portfolio/
  BesContributionPlanner.cs`, migration `*_BesContributions`; düzenlenen `Holding.cs`, `FinansDbContext.cs`,
  `PortfolioConfigurations.cs`, `HoldingService.cs` (Generate + kayıt + ToBesDto), `IHoldingService.cs`,
  `PortfolioDtos.cs`, `HoldingsController.cs`; testler `BesContributionPlannerTests.cs`,
  `BesContributionPlanApiTests.cs`. web yeni `BesContributionPlanForm/History(.test).tsx`; `shared/{types,api}`,
  `lib/hooks.ts`, `routes/HoldingDetailPage.tsx`, `App.css` (.edit-link). doküman `08`(T-BES.6/6b), `09`(SC-22).
- **Test:** backend **App 59 + Integration 58 = 117** · web **45** · shared 13 · lint/build temiz.
- **Karar/Not:** Tam **otomatik zamanlayıcı** (job ile gün gelince otomatik kayıt) ve **plan kalıcılığı**
  T-BES.6b'ye bırakıldı (scheduler altyapısı yok); şimdilik "tarih aralığından bugüne dek üret" = backfill +
  "devam". **Migration canlı Postgres'e uygulanmalı** (`dotnet ef database update` — additive: yeni tablo).
- **Durum:** T-BES.6 tamam.
- **Sıradaki:** **T-BES.5 — fon dağılımı eğitici kâr/zarar projeksiyonu** (kullanıcının sırası).

## 2026-05-31 · BES devlet katkısı oranı TARİHE BAĞLI yapıldı — geriye dönük değil (kullanıcı geri bildirimi)
- **Görev(ler):** T-BES.1/2 düzeltmesi (kullanıcı haklı uyarısı: %30→%20 değişimi geçmiş katkıları etkilememeli).
- **Sorun:** `BesRules.StateContributionRate` tek sabit %20 idi → "oran her zaman %20" anlamına geliyordu;
  geçmiş/geri-tarihli katkı yanlış oran alırdı. (Birikmiş `StateContribution` zaten yeniden hesaplanmıyordu,
  o yön doğruydu.)
- **Ne yapıldı:** Oran **tarih çizelgesine** çevrildi — `BesRules.StateContributionRateOn(date)`: 2026-01-01
  öncesi **%30**, sonrası **%20** (geriye dönük DEĞİL). `BesCalculator.StateContributionFor(own, paidAtUtc)`
  ödeme tarihine göre uygular. `AddBesContributionRequest.PaidAtUtc` (ops., gelecek→400). Web: katkı formuna
  **ödeme tarihi** alanı + tarihe göre dinamik oran önizlemesi (%20/%30); detay açıklaması "2026-01-01'den
  %20, öncesi %30 — geçmiş katkılar etkilenmez".
- **Dokunulan dosyalar:** `BesRules.cs`, `BesCalculator.cs`, `PortfolioDtos.cs` (PaidAtUtc), `HoldingService.cs`;
  `BesCalculatorTests.cs` (2025→%30, 2026→%20); web `shared/types`, `BesContributionForm.tsx`(+test),
  `HoldingDetailPage.tsx` (açıklama).
- **Test:** backend **App 55 + Integration 56 = 111** · web **43** · shared 13 · lint/build temiz.
- **Durum:** tamamlandı — oran artık tarihe bağlı (doğru).
- **Sıradaki:** T-BES.5 (fon dağılımı eğitici projeksiyon) / T2.8.

## 2026-05-31 · BES detaylı analiz — araştırma + devlet katkısı %20 + hak ediş türetme + başlangıç tarihi (T-BES.1-3)
- **Görev(ler):** T-BES.1, T-BES.2, T-BES.3 (tamam); T-BES.4-6 planlandı (08 epik).
- **Araştırma (web, kaynak: egm.org.tr / allianz SSS):** Devlet katkısı **%20** (2026-01-01'den; önceki %30,
  RG 2026-01-07). Üst sınır = yıllık brüt asgari ücretin %20'si (2026 ≈ 79.272 ₺). Hak ediş 3/6/10 yıl
  kademeli; kesin yüzdeler kaynaklarda **çelişkili** → uygulamada kaba durum (NotVested/Partially/Vested),
  emeklilik 10 yıl+56 yaş. **Oran/eşik mevzuata tabi → lansman öncesi EGM/SPK doğrulaması ŞART** (CLAUDE.md §2).
- **Ne yapıldı:**
  1. **`BesRules`** (tek kaynak; oran/cap/eşik + "doğrulanmalı" notları) + **`BesCalculator`** (saf):
     `StateContributionFor` (%20), `YearsInSystem`, `VestingStateFor` (yıl→kaba durum). 9 birim testi (SC-20).
  2. Katkı endpoint'i sabit %30 → **%20** (BesCalculator); hak ediş artık **okuma anında türetilir**
     (`BuildHoldingDtosAsync` + katkıda) → saklanan VestingState'e güvenilmez.
  3. **`PUT /holdings/{id}/bes`** (`UpdateBesAsync`): başlangıç tarihi güncelle → hak ediş yeniden türer;
     gelecek tarih→400, BES değil→400, IDOR→404. Integration 3 senaryo (SC-21).
  4. **Web:** shared `UpdateBesInput`+`updateBes`; `useUpdateBes`; detayda başlangıç "· Düzenle" (date modal,
     "Kaydet") + **devlet katkısı açıklaması** (%20 + üst sınır) + **hak ediş kademe notu** + disclaimer.
- **Dokunulan dosyalar:** backend yeni `Application/Portfolio/{BesRules,BesCalculator}.cs`; düzenlenen
  `HoldingService.cs`(+UpdateBesAsync, %20, vesting türetme), `IHoldingService.cs`, `PortfolioDtos.cs`
  (UpdateBesRequest), `HoldingsController.cs` (PUT /bes); testler `BesCalculatorTests.cs`(yeni),
  `BesAndHistoryApiTests.cs` (%20 + 3 PUT test), `PriceFetchServiceTests.cs` (flaky saat→2030 fix).
  web `shared/{types,api}`, `lib/hooks.ts`, `routes/HoldingDetailPage.tsx`. doküman `08`(T-BES epik),
  `09`(SC-20/21), `03`(BES kural notu).
- **Test:** backend **App 54 + Integration 56 = 110** · web **43** · shared **13** · lint/build temiz.
  Not: zaman-bağımlı flaky `PriceFetchServiceTests` (sabit saat seed gerçek-now'dan eski kalınca FX sırası
  bozuluyordu) → stub saati **2030**'a alınıp deterministik yapıldı.
- **Karar/Not:** **Fon dağılımı kâr/zarar senaryosu (T-BES.5)** kasıtlı planlandı, bu turda yapılmadı:
  projeksiyon olduğu için **eğitici/varsayımsal çerçeve + disclaimer** ile tasarlanmalı (CLAUDE.md §2 —
  senaryo serbest, gelecek tahmini/tavsiye yasak); hesap KODDA (bileşik getiri), girdiler kullanıcıdan.
  Seed'in birikmiş state katkısı (%30 dönemi) değişmedi (geçmiş; %20 yeni katkılara uygulanır).
- **Durum:** T-BES.1-3 tamam; T-BES.4 (yıllık cap), **T-BES.5 (senaryo)**, T-BES.6 (katkı planı) bekliyor.
- **Sıradaki:** kullanıcı onayıyla **T-BES.5 fon-dağılımı eğitici projeksiyon** ya da T2.8 gözlemlenebilirlik.

## 2026-05-31 · UX tutarlılığı — canlı fiyat ticker'ı + nakit/canlı varlıkta elle fiyat kaldırma (ad-hoc, kullanıcı geri bildirimi)
- **Görev(ler):** ad-hoc (T2.6 üstüne kullanıcı geri bildirimi). İki soru: (a) nakit için elle fiyat/işlem
  gerekli mi? (b) canlı veriyi kayan-yazı olarak görmek.
- **Ne yapıldı:**
  1. **`PriceTicker` (kayan yazı):** statik `LivePrices` çiplerinin yerine geçti. Altın/döviz değerleri +
     **kaynak etiketi** ("Kaynak: Frankfurter (döviz) · Truncgil (altın)") kesintisiz akar (içerik 2×, CSS
     marquee); hover'da durur; `prefers-reduced-motion`'da akış kapanır + yatay kaydırma (a11y). `stale`→"~yaklaşık".
  2. **Elle "Fiyatı güncelle" görünürlük kuralı (detay sayfası):** yalnız fiyatı **sabit/canlı OLMAYAN**
     varlıklarda (Hisse/Fon/BES). **Nakit** (sabit ₺1) ve **Altın/Döviz** (canlı, otomatik — elle giriş bir
     sonraki tazelemede ezilirdi) için buton gizlendi + bağlam notu ("Nakit fiyatı sabittir"/"canlı kaynaktan
     otomatik"). Çakışma/tuzak ("girdim ama döndü") giderildi → her varlıkta tek doğruluk kaynağı.
  3. `LivePrices.tsx` + testi kaldırıldı (PriceTicker ikamesi); App.css çip stilleri ticker stilleriyle değişti.
  4. **Otomatik tazeleme (seçenek b):** `usePrices`/`useNudges` → `refetchInterval` 5 dk (sekme önplanda) +
     `refetchOnWindowFocus`. Backend 10 dk cache → poll'lar çoğunlukla cache-hit (dış API'ye gitmez); arka
     planda durur. Fiyat `refreshedAtUtc` değişince mevcut effect summary/holdings'i invalidate eder.
     "↻ Yenile" butonu açık kontrol olarak kalır.
  5. **Nakit "Para ekle / çıkar" relabel (kullanıcı onayı):** `AddTransactionForm` `cash` modu → "Para ekle"/
     "Para çıkar", **birim fiyat alanı yok**, tutar = miktar, `unitPrice=1` gönderilir (backend değişmedi).
     `TransactionHistory` `cash` → "Para eklendi/çıkarıldı". Detay: buton "＋ Para ekle / çıkar", modal başlığı
     + toast nakit'e göre. Hisse/Fon/BES'te eski Alış/Satış aynı. Nakit modunda gönder butonu **"Kaydet"**
     ("Para çıkar" seçiliyken "Ekle" kafa karıştırıcıydı; Alış/Satış formunda "Ekle" kaldı).
- **Dokunulan dosyalar:** yeni `web/src/components/PriceTicker.tsx`(+test), `routes/HoldingDetailPage.test.tsx`;
  düzenlenen `routes/PortfolioPage.tsx`, `routes/HoldingDetailPage.tsx`, `App.css`; silinen
  `components/LivePrices.tsx`(+test); doküman `09` (SC-W4 güncel + SC-W5).
- **Test:** web **37→41** (PriceTicker 2 + HoldingDetailPage 3 + AddTransactionForm cash 1; LivePrices 2 çıktı).
  `tsc -b`+`vite build`+**eslint 0** temiz. (SC-W4 güncel, SC-W5 + SC-W6 eklendi.)
- **Karar/Not (kullanıcıya cevap):** "Canlı" = çekme + cache (push değil) → tetikleyici gerek; **otomatik
  tazeleme eklendi** (5 dk + odak) + **Yenile butonu** açık kontrol olarak kaldı. **Nakit:** fiyat sabit (₺1)
  → elle güncelleme YOK; işlem modeli **"Para ekle/çıkar"** olarak yeniden adlandırıldı (alış/satış değil).
- **Durum:** tamamlandı (ticker + elle-fiyat kuralı + otomatik tazeleme + nakit relabel).
- **Sıradaki:** (kullanıcı bakacak) — sonra T2.8 gözlemlenebilirlik.

## 2026-05-31 · Dağıtık cache katmanı — IAppCache (Redis-opsiyonel) + single-flight + metrik (T2.7)
- **Görev(ler):** T2.7 (tamam).
- **Ne yapıldı:**
  1. **`IAppCache` portu (Application/Common):** `GetOrCreateAsync`/`GetAsync`/`SetAsync`/`SingleFlightAsync`.
  2. **`DistributedAppCache` (Infrastructure/Caching):** `IDistributedCache` üstünde JSON serileştirme +
     per-anahtar **single-flight** (in-process `SemaphoreSlim`, stampede koruması) + hit/miss metriği.
  3. **Redis OPSİYONEL:** `ConnectionStrings:Redis` varsa `AddStackExchangeRedisCache`, yoksa
     `AddDistributedMemoryCache`. **Yerel dev Redis'siz çalışır** ([[local-dev-database]] — Docker'sız);
     compose'a `redis:7-alpine` + `ConnectionStrings__Redis=redis:6379` eklendi (yalnız compose yığını).
  4. **`CacheMetrics`:** `System.Diagnostics.Metrics.Meter "Finans.Cache"` → `finans.cache.requests`
     sayacı (etiket: result=hit/miss, cache=anahtar öneki). OTel-uyumlu → T2.8 Prometheus exporter bağlar.
  5. **Taşıma:** `CachedFxRateProvider` (artık serileştirilebilir `List<FxQuote>` cache'ler → converter'ı
     her çağrı kurar; `EfFxRateProvider.GetQuotesAsync` eklendi), `CachedInflationRateProvider`
     (`InflationHolder`), `PriceFetchService` (GetAsync/SetAsync + **SingleFlightAsync** → eşzamanlı
     /prices dış API'yi bir kez tetikler). `IMemoryCache` kullanımı kaldırıldı.
- **Dokunulan dosyalar:** yeni `Application/Common/IAppCache.cs`, `Infrastructure/Caching/{CacheMetrics,
  DistributedAppCache}.cs`; düzenlenen `Infrastructure/{DependencyInjection.cs, Finans.Infrastructure.csproj
  (+StackExchangeRedis), Persistence/EfFxRateProvider.cs, Services/Cached{Fx,Inflation}RateProvider.cs,
  Pricing/PriceFetchService.cs}`, `Api/{Program.cs, appsettings.json}`, `docker-compose.yml`; testler
  yeni `Integration.Tests/Caching/DistributedAppCacheTests.cs`, düzenlenen `Pricing/PriceFetchServiceTests.cs`.
- **Test:** **SC-19** (3: GetOrCreate ilk-sonra-cache, single-flight 8 eşzamanlı→factory 1, miss→null/set).
  Mevcut FX-cache (ProviderCacheTests) + PriceFetchService cache testleri davranış korunarak yeşil.
  `dotnet test` **yeşil: Application 45 + Integration 53 = 98**, 0 hata.
- **Karar/Not:** Cache değerleri JSON serileştirilir → cache'lenen tip serileştirilebilir olmalı
  (FxQuote/InflationHolder/PriceRefreshResult öyle). Single-flight şimdilik süreç-içi (en sık stampede);
  çoklu-replika dağıtık kilidi ileride — kısa TTL + idempotent yazımlar (snapshot/fxrate dedupe) köprüler.
  Yerel dev hiç etkilenmedi (in-memory fallback). Bkz. [[local-dev-database]].
- **Durum:** tamamlandı
- **Sıradaki:** T2.8 — gözlemlenebilirlik yığını (Compose'a Seq + Prometheus + Grafana; OTel metrik
  exporter → `Finans.Cache` + RED + bağımlılık; ilk dashboard/alarm). Ardından T2.9 reverse proxy + rate limit.

## 2026-05-31 · Web — canlı fiyat + nudge görünürlüğü (T2.6)
- **Görev(ler):** T2.6 (tamam). Faz 2 fiyat zinciri **kullanıcıya görünür** hale geldi.
- **Ne yapıldı:**
  1. **shared sözleşme:** `PriceDto`/`PricesResponse`/`Nudge`/`NudgeSeverity`/`NudgesResponse` tipleri +
     `getPrices()` (`/api/prices`) ve `getNudges()` (`/api/portfolio/nudges`) istemci metotları.
  2. **web hook'ları:** `usePrices` (60 sn staleTime) + `useNudges` (120 sn) + `queryKeys.prices/nudges`.
  3. **`LivePrices` bileşeni:** altın/döviz çipleri (`quoteCurrency` ile TR biçim); `stale` → "~yaklaşık".
  4. **`NudgesCard` bileşeni:** API kural-notları (`.nudge` + şiddet rengi: Warning altın / Info nane) +
     **"yatırım tavsiyesi değildir" disclaimer'ı** (NFR-2); not yoksa çizmez.
  5. **PortfolioPage bağlama:** topbar'a **"↻ Yenile"** butonu (`prices.refetch`+`nudges.refetch`+toast) +
     fiyat refreshedAt/`stale`'e bağlı **"son güncelleme · yaklaşık"** etiketi; canlı fiyat strip; Nudge
     kartı. **Kritik:** `prices.refreshedAtUtc` değişince (backend `CurrentPrice`'ı yazdı) `useEffect`
     summary+holdings'i invalidate eder → pano canlı değeri yansıtır (TanStack v5'te query onSuccess yok).
  6. **Çift nudge temizliği:** `PortfolioInsights`'taki client-side hesaplanan "yoğunlaşma" nudge'ı
     kaldırıldı (artık yetkili API `NudgesCard` var; çakışma yok).
  7. **CSS:** `.price-chips`/`.price-chip(.stale)`, `.nudge-list`, `.nudge.nudge-info` (nane), `.freshness.stale`
     — `.mobile-topbar` DOSYA-SONU kuralı korunarak öncesine eklendi (CSS sıra).
- **Dokunulan dosyalar:** `packages/shared/src/{types,api}/index.ts`; web yeni `components/{LivePrices,NudgesCard}.tsx`
  (+ `.test.tsx`); düzenlenen `web/src/lib/hooks.ts`, `routes/PortfolioPage.tsx` (+ test mock), `components/PortfolioInsights.tsx`,
  `App.css`; doküman `04`(zaten T2.4'te), `08`, `09` (SC-W4).
- **Test:** web **37 yeşil** (33→37: LivePrices 2 + NudgesCard 2; PortfolioPage test mock'una prices/nudges
  eklendi). shared 13. `tsc -b` + `vite build` + **eslint 0 hata** temiz.
- **CANLI DOĞRULANDI** (backend http:5298 PostgreSQL'e karşı + web 5174, gerçek dış API): `GET /api/prices`
  canlı döndü — EUR 53,43 · USD 45,886 · gram altın **6.687,67 ₺** (truncgil), `hasStale:false`. Pano
  (tarayıcı): canlı fiyat çipleri + "↻ Yenile" + freshness etiketi; **3 nudge** kartı (Yoğunlaşma %85,
  Tek varlık Altın %71, Nakit %0,3) + "tavsiye değildir" disclaimer; holdings canlı fiyatı yansıttı
  (Altın 209 gr → ₺1.397.723; USD 2000×45,886=₺91.772; EUR 800×53,43=₺42.744); toplam ₺1.973.851,79.
- **Karar/Not:** Fiyat tazeleme tetikleyici `GET /api/prices` (query); özet/holdings saf okuma kalıp
  invalidation ile canlanır (read purity korunur). Stale → "yaklaşık" (UI), kullanıcı bilgilendirilir.
- **Durum:** tamamlandı — canlı doğrulandı.
- **Sıradaki:** Faz 2 altyapı T2.7 (Redis) / T2.8 (gözlem) / T2.9 (proxy) — işlevsel Faz 2 bitti.

## 2026-05-31 · `NudgeRuleEngine` — kural tabanlı eğitici notlar + `GET /nudges` (T2.5)
- **Görev(ler):** T2.5 (tamam).
- **Ne yapıldı:**
  1. **Saf motor (Application/Portfolio `NudgeRuleEngine`):** portföy özetinden (hazır oranlar)
     deterministik notlar üretir. Kurallar: **yoğunlaşma** (en büyük 2 varlık ≥%60), **tek varlık
     ağırlığı** (en büyük ≥%40), **düşük nakit** (nakit <%5). Boş portföyde (değer ≤0) not yok.
     Her not durumu betimler + çerçeve sunar — **somut al/sat yönlendirmesi YOK** (CLAUDE.md §2).
  2. **Model:** `Nudge`(Id, Icon, Title, Body, `NudgeSeverity` Info/Warning) + `NudgesResponse`.
     TR yüzde formatı yardımcı (≥%10 tam, altı 1 ondalık virgüllü: "%64" / "%0,7").
  3. **Servis + uç nokta:** `INudgeService`→`NudgeService` (summary'i per-user alıp motordan geçirir;
     yeni sayı üretmez) · `GET /api/portfolio/nudges` (PortfolioController, baseCurrency opsiyonel).
  4. **DI:** `NudgeRuleEngine` singleton (saf) + `INudgeService` scoped.
- **Dokunulan dosyalar:** yeni `src/Finans.Application/Portfolio/{NudgeRuleEngine,INudgeService}.cs`,
  `src/Finans.Infrastructure/Services/NudgeService.cs`; düzenlenen `Infrastructure/DependencyInjection.cs`,
  `Api/Controllers/PortfolioController.cs`; yeni testler
  `tests/Finans.Application.Tests/Portfolio/NudgeRuleEngineTests.cs` (xUnit Assert — Application.Tests'te
  FluentAssertions yok), `tests/Finans.Integration.Tests/NudgesApiTests.cs`; doküman `09` (SC-09), `08`.
- **Test:** **SC-09** — 6 unit (yoğunlaşma fires/diversified-değil, tek-varlık eşik+ad, düşük-nakit
  alt/üst, boş→0, **tavsiye-kelimesi yok**) + 2 e2e (seed→`concentration`+`low-cash`; admin boş→0,
  per-user). `dotnet test` **yeşil: Application 45 + Integration 50 = 95**, 0 hata.
- **Karar/Not:** Sayısal yargı koddadır (deterministik kural), LLM yorumu Faz 3. Eşikler `internal const`
  (ileride seçenek). Notlar UI'da disclaimer altında gösterilecek (NFR-2, T2.6). 04 §5 nudge sözleşmesi
  mevcut taslakla uyumlu (id/icon/title/body/severity).
- **Durum:** tamamlandı
- **Sıradaki:** **T2.6 Web** — `usePrices`/`useNudges` hook'ları; "Yenile" butonu + "son güncelleme/
  yaklaşık (stale)" etiketi + Nudge kartı. **Web görünürlüğü burada kullanıcıya çıkar** (canlı doğrulama).

## 2026-05-31 · `GET /api/prices` + summary'i canlı fiyatla besle (T2.4)
- **Görev(ler):** T2.4 (tamam).
- **Ne yapıldı:**
  1. **DTO (Application/Pricing `PricesDtos`):** `PricesResponse` (RefreshedAtUtc, FromCache, HasStale,
     FailedSources, Prices) + `PriceDto` (Kind, Currency, Price, QuoteCurrency, AsOfUtc, Source, **Stale**).
  2. **`PricesController` (`GET /api/prices`):** ince controller → `IPriceFetchService.RefreshAsync` →
     tırnakları DTO'ya eşler. Parametre yok (tüm fiyatlanabilir varlıkları tazeler); fiyatlar global.
  3. **"summary'i besle" — tasarım kararı:** summary'yi network-refresh'e BAĞLAMADIM (mevcut
     `Summary_matches_seed_totals` testini kırardı + read-on-write anti-pattern + testte ağ). Yerine:
     `GET /api/prices` `Holding.CurrentPrice`'ı yazar (T2.2'den) → summary/holdings **saf okuma** olarak
     canlı fiyatı yansıtır. Web (T2.6) sırayı kurar: /prices → summary+holdings tazele.
  4. **04 §5 sözleşmesi** gerçek uygulamayla güncellendi (taslak `symbols`/`symbol` → `kind`+`currency`
     + tur meta'sı `refreshedAt/fromCache/hasStale/failedSources`).
- **Dokunulan dosyalar:** yeni `src/Finans.Application/Pricing/PricesDtos.cs`,
  `src/Finans.Api/Controllers/PricesController.cs`; yeni test
  `tests/Finans.Integration.Tests/Pricing/PricesApiTests.cs`; doküman `04` §5, `09` (SC-18 notu),
  `08-BACKLOG.md`.
- **Test:** **2 e2e** (her test KENDİ factory'si → izole Sqlite+seed; sağlayıcılar stub, ağsız):
  (a) canlı tırnak döner + `Holding.CurrentPrice` yazıldı → `GET /holdings` altın 7000 / USD 50 gösterir
  ("besle" uçtan uca kanıtı); (b) döviz kaynağı çöker → `GET /api/prices` 200 + `hasStale`/`stale:true`
  + son-bilinen 48 (source "Manual"), altın taze sürer. `dotnet test` **yeşil: App 39 + Integration 48 = 87**.
- **Karar/Not:** `GET /api/prices` = açık refresh tetikleyici; summary/holdings saf okuma kalır
  (deterministik + ağsız test + read purity). Stale UI'da "yaklaşık" olur (T2.6). Hisse fiyatı Faz 4.
- **Durum:** tamamlandı
- **Sıradaki:** T2.5 `NudgeRuleEngine` + `GET /api/portfolio/nudges` (kural tabanlı eğitici notlar,
  örn. nakit/yoğunlaşma eşiği — tavsiye değil). Ardından **T2.6 Web** → görünürlük kullanıcıya çıkar.

## 2026-05-31 · Fallback — dış API çökünce son bilinen fiyat + `stale` (T2.3)
- **Görev(ler):** T2.3 (tamam). T2.2'nin `FailedSources` izolasyon kancası üstüne kuruldu.
- **Ne yapıldı:**
  1. **`PriceQuote.IsStale`** (varsayılan `false`) — sağlayıcılar canlı tırnağı `false` üretir
     (mevcut çağrılar değişmedi); fallback `true` işaretler. **`PriceRefreshResult.HasStale`** türetilmiş.
  2. **Fallback (`PriceFetchService`):** bir sağlayıcı çökünce (catch) o sağlayıcının enstrümanları
     için **DB'den son bilinen** değer okunur (`LoadLastKnownAsync`): döviz → en güncel `FxRate`,
     altın → en güncel `PriceSnapshot` → `IsStale=true` tırnak. Hiç geçmiş yoksa enstrüman atlanır (log).
  3. **Bayat yazılmaz:** `PersistAsync` yalnız taze tırnağı yazar (savunmacı `!IsStale` filtre) →
     bayat değer geçmişi/CurrentPrice'ı kirletmez; sonuçta yalnız **gösterim** için döner.
  4. **Kısa retry-TTL:** bir kaynak çöktüyse sonuç **1 dk** cache'lenir (10 dk yerine) → çöken
     kaynak yakında yeniden denenir; tüm kaynaklar sağlıklıysa tam 10 dk.
  5. **Çökme yok:** sağlam kaynak (altın) taze yazmayı sürdürür; uygulama akışı kesilmez (NFR-5).
- **Dokunulan dosyalar:** `src/Finans.Application/Pricing/{PriceQuote,IPriceFetchService}.cs`,
  `src/Finans.Infrastructure/Pricing/PriceFetchService.cs`; test
  `tests/Finans.Integration.Tests/Pricing/PriceFetchServiceTests.cs` (izolasyon testi fallback'e
  güncellendi); doküman `09` (SC-08 [x]), `08-BACKLOG.md`.
- **Test:** **SC-08** — döviz sağlayıcı çöker → altın taze (7000), USD/EUR son-bilinen bayat (48/52),
  `HasStale`/`FailedSources` doğru, yeni FxRate yazılmadı, CurrentPrice son-bilinende. `dotnet test`
  **yeşil: Application 39 + Integration 46 = 85**, 0 hata.
- **Karar/Not:** Stale = "son bilinen, canlı kaynak ulaşılamadı"; geçmişe yazılmaz (yanlış gözlem
  olur). Çöken kaynakta kısa TTL bilinçli (uzun TTL bayatı kilitlerdi). TTL süresinin kendisi
  zaman-bağımlı → birim testte doğrulanmadı (MemoryCache saati enjekte gerektirir); davranış belgelendi.
- **Durum:** tamamlandı
- **Sıradaki:** T2.4 — `GET /api/prices` (RefreshAsync'i tetikler; `stale`/`asOf`/`source` yüzeye çıkar)
  + summary'i canlı fiyatla besle. **Web görünürlüğü burada başlar** (kullanıcıya gösterilecek).

## 2026-05-31 · `PriceFetchService` — canlı fiyat orkestrasyonu + cache + yazım (T2.2)
- **Görev(ler):** T2.2 (tamam).
- **Ne yapıldı:**
  1. **Sözleşme (Application/Pricing):** `IPriceFetchService.RefreshAsync` + `PriceRefreshResult`
     (Quotes, RefreshedAtUtc, FromCache, FailedSources).
  2. **Orkestrasyon (Infrastructure/Pricing `PriceFetchService`):** aktif fiyatlanabilir varlıklar
     (Gold + Fx, `PricingCurrency==TRY`) → `PriceInstrument` eşlemesi (`TryMapInstrument`; Fx para
     birimi `Symbol??Unit`'ten parse). `IEnumerable<IPriceProvider>` → `CanQuote` yönlendirmesiyle
     çekim; **her sağlayıcı izole** (try/catch → `FailedSources`'a düşer, log, diğerleri sürer).
  3. **Yazım (kritik bulgu):** Okuma yolu bugün `Holding.CurrentPrice` (denormalize); `PriceSnapshots`
     yalnız seed'liydi, `FxRates`'i `CurrencyConverter` okuyor. Dolayısıyla her tırnak için:
     **PriceSnapshot** (geçmiş; aynı `(asset,AsOf)` varsa atla → gün-içi FX yinelemesi önlenir) +
     döviz ise **FxRate** (`currency→TRY`, converter için) + ilgili **tüm** `Holding.CurrentPrice`
     (global, kullanıcıdan bağımsız) → özet/holdings anında canlı fiyatı yansıtır.
  4. **Cache:** 10 dk in-memory (`prices:refresh`); TTL içinde `RefreshAsync` dış çağrı/yazma
     yapmadan cache'ten döner (`FromCache=true`) — `CachedFxRateProvider` deseninin eşi (10 §3-4).
  5. **DI:** `IPriceFetchService → PriceFetchService` scoped (DbContext'e bağlı).
- **Dokunulan dosyalar:** yeni `src/Finans.Application/Pricing/IPriceFetchService.cs`,
  `src/Finans.Infrastructure/Pricing/PriceFetchService.cs`; düzenlenen
  `Infrastructure/DependencyInjection.cs`; yeni testler
  `tests/Finans.Integration.Tests/Pricing/{StubPriceProvider,PriceFetchServiceTests}.cs`;
  doküman `09` (SC-18), `08-BACKLOG.md`.
- **Test:** **SC-18** (3 senaryo: yazım→snapshot/fxrate/CurrentPrice; TTL cache→dış çağrı tekrar yok
  (call-count); sağlayıcı izolasyonu→biri çökse altın yazılır, USD seed'de kalır). İzole Sqlite+seed,
  stub sağlayıcı (ağsız). `dotnet test` **yeşil: Application 39 + Integration 46 = 85**, 0 hata.
- **Karar/Not:** Canlı fiyatın okuma yolu = `Holding.CurrentPrice` güncellemesi (mevcut denormalize
  deseni; read-path değiştirmeden minimal). Fiyat/FxRate **global** (per-user değil) — IDOR kapsamı
  dışı. Snapshot yazımı geçmiş için bilinçli (Performans grafiği T2.4+); retention ileride (T2.7+).
- **Durum:** tamamlandı
- **Sıradaki:** T2.3 — fallback: bir sağlayıcı çökünce son bilinen fiyat + `stale:true` (NFR-5, SC-08);
  `FailedSources` zaten bunun kancası. Ardından T2.4 `GET /api/prices` + summary'i besle.

## 2026-05-31 · Faz 2 başladı — fiyat sağlayıcı seçimi + `IPriceProvider` (T2.1)
- **Görev(ler):** T2.1 (tamam) — Faz 2 ilk adım.
- **Ne yapıldı:**
  1. **Sağlayıcı kararı (kullanıcı onaylı):** Döviz → **Frankfurter** (ECB, anahtarsız, kotasız,
     TRY dahil; her döviz için *doğrudan* TRY kuru → ters çevirme yok, tam isabet). Altın →
     **Truncgil** (TR piyasası gram altın, anahtarsız; yerel primli gerçek fiyat — saf XAU-spot
     türetmesinden daha doğru). İki canlı uç nokta WebFetch ile bugün doğrulandı (şekiller parser'a
     birebir yansıdı). İkisi de anahtarsız → repoda sır yok (CLAUDE.md §13).
  2. **Sözleşme (Application/Pricing):** kaynaktan bağımsız `IPriceProvider` (`Source`/`CanQuote`/
     `GetQuotesAsync`) + `PriceInstrument` (Kind: Currency/Gold) + `PriceQuote` (decimal fiyat,
     QuoteCurrency, AsOfUtc, Source). Orkestrasyon/cache/fallback bilerek üst katmana bırakıldı
     (T2.2/T2.3).
  3. **Sağlayıcılar (Infrastructure/Pricing):** `FrankfurterPriceProvider` (typed HttpClient,
     `GET /v1/latest?base={ccy}&symbols=TRY`, para birimleri paralel; TRY/döviz-olmayan atlanır) +
     `TruncgilGoldPriceProvider` (`GET /v4/today.json` → "GRA" **satış**; sayı/string alanları
     invariant decimal; zaman damgası yoksa `TimeProvider`). `PricingOptions` (yalnız kök adres).
  4. **DI:** `AddInfrastructure`'a opsiyonel `Action<PricingOptions>`; `AddHttpClient<>` typed
     client'lar + `TimeProvider.System`; her ikisi `IEnumerable<IPriceProvider>` olarak kaydedildi
     (T2.2 CanQuote'a göre yönlendirir). `Program.cs` "Pricing" bölümünü bind eder; `appsettings`'e
     anahtarsız uç noktalar (yorumlu).
- **Dokunulan dosyalar:** yeni `src/Finans.Application/Pricing/{IPriceProvider,PriceInstrument,PriceQuote}.cs`;
  yeni `src/Finans.Infrastructure/Pricing/{PricingOptions,FrankfurterPriceProvider,TruncgilGoldPriceProvider}.cs`;
  düzenlenen `Infrastructure/DependencyInjection.cs`, `Infrastructure/Finans.Infrastructure.csproj`
  (+`Microsoft.Extensions.Http`), `Api/Program.cs`, `Api/appsettings.json`; yeni testler
  `tests/Finans.Integration.Tests/Pricing/{StubHttpMessageHandler,FrankfurterPriceProviderTests,TruncgilGoldPriceProviderTests}.cs`;
  doküman `02-ARCHITECTURE.md` §2.3 (karar), `09-TESTING-STRATEGY.md` (SC-17), `08-BACKLOG.md`.
- **Test:** **SC-17** (8 senaryo: doğrudan-kur ayrıştırma, döviz/TRY atlama, CanQuote yönlendirme,
  HTTP hata→istisna; gram-altın satış, string-sayı, altın-yok→atla, GRA-eksik→istisna) — HTTP stub
  handler ile, ağsız. `dotnet test` **yeşil: Application 39 + Integration 43 = 82**, 0 hata.
- **Karar/Not:** Sağlayıcı seçimi kalıcı karar → `02` §2.3'e işlendi. Ters-çevirme yerine *doğrudan*
  kur (finansal hassasiyet, NFR-1). Gram altın **satış** fiyatı (manşet, kullanıcı beklentisi);
  alış muhafazakâr realize-değer için ileride seçenek. Gayriresmi kaynak riski soyutlama + T2.3
  fallback ile karşılanacak. Not: build sırasında geçen oturumun kilitli `Finans.Api.exe` dev
  sunucusu durduruldu (gerekirse `dotnet run` ile tekrar başlatılır).
- **Durum:** tamamlandı
- **Sıradaki:** T2.2 — `PriceFetchService` (IEnumerable<IPriceProvider> → CanQuote yönlendirme,
  5-15 dk cache, `PriceSnapshots`/`FxRates`'e yaz); ardından T2.3 fallback (SC-08), T2.4 `GET /prices`.

## 2026-05-30 · Web UX/UI 2. tur — kullanıcı geri bildirimi (6 madde) + canlı doğrulama (T1.21)
- **Görev(ler):** T1.21 (tamam) — kullanıcı canlı turda 6 madde işaret etti.
- **Ne yapıldı:**
  1. **Sola ağırlık / sağ boşluk:** dashboard'da donut artık `grid-2` içinde "Değer Seyri" önizleme
     kartının yanında; `.app-content` `margin:0 auto` (geniş ekranda ortalanır). Detay sayfası
     2 sütun + `max-width` 720→1100.
  2. **Detay yoğunluğu:** İşlem/fiyat/BES-katkı formları **modale** taşındı (yeni genel `Modal` +
     `AddTransactionForm`/`BesContributionForm` `onDone` prop'u). Sayfada artık buton tetikliyor;
     işlem geçmişi sağ sütunda kart içinde (çift başlık kaldırıldı, dar sütuna sığacak CSS).
  3. **Tüm taslak menüleri:** yeni rotalar + nav grupları (Portföy / Akıl & Öğren) — İşlemler,
     Performans, Senaryo, Hisse Analizi, Eğitim (ortak `ComingSoonPage`, faz rozetli, gerekli
     yerlerde disclaimer). Analiz da bu bileşene taşındı.
  4. **Performans bölümü:** `PerformancePage` — dönem sekmeleri (1A/3A/1Y/Tümü) + zaman-serisi
     yer tutucu (canlı fiyat geçmişi Faz 2'de) + **gerçek veriden** kalem bazında getiri çubukları.
  5. **🔴 Mobil menü erişilemezliği (KÖK NEDEN):** CSS kaynak sırası hatası — base `.mobile-topbar
     {display:none}` kuralı, onu gösteren `@media`'dan SONRA geliyordu (medya sorgusu specificity
     eklemez → none kazanıyordu). Düzeltme: gösterim kuralı dosya **sonuna** (base'den sonra) alındı.
     **CSSOM sırası + drawer state makinesi canlı doğrulandı.**
  6. **Sticky topbar boşluğu + arkaplan uyumu:** negatif margin kaldırıldı, `top:0`'da sıfır boşlukla
     sabit; `.app-content` üst dolgusu topbar'a devredildi. İki geri bildirim turu sonrası: düz `--bg`
     dolgu/sert çizgi → kısa süre yarı saydam gradient → **nihai: `background:transparent` + sadece
     `backdrop-filter: blur(14px)`** (renk tint'i YOK). Kaydırılmadan atmosfer gradyanı olduğu gibi
     görünür (bant yok); kaydırınca içerik buzlu-cam gibi yumuşar, başlık okunaklı. **Canlı doğrulandı.**
  7. **Marka adı → "Nirengi":** sidebar + mobil bar (`Ni`+altın `rengi`) ve sayfa başlığı (`index.html`).
     Kod/paket adları (`@finans/*`) İngilizce kod konvansiyonu gereği değişmedi; yalnızca kullanıcıya
     görünen marka. Bkz. [[brand-name-nirengi]].
  8. **Header full-bleed:** 1920px'te içerik 1320'de ortalanınca topbar yalnızca ortadaki sütunu
     kaplıyordu → kaydırınca "kesik dikdörtgen". `--gutter` değişkeniyle (app-content padding =
     topbar negatif margin) topbar **ana alanın tüm genişliğine** yayıldı, iç içeriği 1320 sütununa
     hizalı kaldı. Ölçümle doğrulandı (topbar 250→1910, kartlar 425→1735).
  9. **Confirm "Vazgeç" butonu teması:** `.btn-ghost`'a `border` tanımlı olmadığından tarayıcının
     varsayılan açık-gri kenarlığı sızıyordu → temalı (`panel-2` zemin + `line` kenarlık + hover gold).
  10. **Proje ikonu (favicon + marka):** Vite varsayılan mor logosu yerine **anlam taşıyan ikon**.
     6 konsept + "N" yükseliş varyantları kullanıcıya canlı önizlemeyle (geçici `web/public/*.html`)
     sunuldu; seçim: **yükselen nirengi/grafik çizgisi** (4 düğüm: çık → tepe → geri çekilme → daha
     yüksek zirve — "N"in grafik yorumu, nirengi + portföy yükselişi). Uygulandı: `web/public/favicon.svg`
     (altın gradient kutu + koyu glyph), `BrandMark` bileşeni (sidebar + mobil bar `◆` yerine),
     `index.html` (apple-touch + manifest + theme-color), yeni `manifest.webmanifest`. Önizleme dosyaları
     silindi. Geri bildirim: glyph kutuda boş kalıyordu → şekil büyütüldü (bbox ~11–53, stroke 5,
     düğüm r5.5) + `BrandMark` svg 24→32px. **Canlı doğrulandı** (sidebar markası + favicon).
     Bkz. [[brand-name-nirengi]].
  11. **Tutarlı dikey ritim:** boşluklar dağınıktı (kpis gap 15 vs grid 16; nudge→tablo 0px bitişik;
     inline `marginBottom`'lar). `.page` yardımcı sınıfı (`display:flex; gap:16px`) tüm içerik
     sayfalarının (Portföy/Performans/Ayarlar/ComingSoon + Skeleton) kök section'ına eklendi; blok
     margin'leri (kpis/grid-2/grid-3/nudge/topbar/setgrp/disclaimer/inline) kaldırıldı → **tüm bloklar
     arası birebir 16px** (ölçümle doğrulandı: topbar→kpis→grid-2→grid-3→nudge→tablo = 16/16/16/16/16).
- **Dokunulan dosyalar:** yeni `components/{Modal,ComingSoonPage}.tsx`,
  `routes/{PerformancePage,TransactionsPage,ScenarioPage,StocksPage,EducationPage}.tsx`;
  düzenlenen `App.tsx` (nav grupları+rotalar), `main.tsx`, `App.css` (sticky/centre/drawer-sıra/
  periods/chart-frame/perf-bars/modal-form/detay-grid), `routes/{PortfolioPage,HoldingDetailPage,
  AnalysisPage}.tsx`, `components/{AddTransactionForm,BesContributionForm,TransactionHistory}.tsx`;
  yeni testler `{Modal,ComingSoonPage,PerformancePage}`.
- **Test:** web **33 yeşil** (28→33), shared 13, `tsc -b` + `vite build` + **eslint 0 hata** temiz.
  **Canlı doğrulama (5173 + backend 5298 + PostgreSQL):** dashboard (donut+Değer Seyri), Performans
  (gerçek getiri çubukları), detay (modal açılışı), 8 menü grubu, mobil drawer state makinesi, sticky.
- **Karar/Not:** Görsel doğrulamada `resize_window` viewport'u küçültmedi → mobil görünüm CSSOM
  kuralı sırası + drawer state makinesi (hamburger.click → sidebar.open+scrim+body-lock) ile kanıtlandı.
  Gelecek-faz menüleri kurgusal veri içermez (yer tutucu + faz rozeti).
- **Durum:** tamamlandı
- **Sıradaki:** Faz 2 — T2.1 fiyat sağlayıcı (zaman serisi gelince Performans grafiği canlanır).

## 2026-05-30 · Web UX/UI yükseltme — mobil nav, durumlar, geri bildirim, a11y (T1.20)
- **Görev(ler):** T1.20 (tamam) — kullanıcı: "UX/UI uzmanı gözüyle neler yanlış/eksik analiz et, daha
  kullanışlı ve kontrol edilebilir bir görünüm/site oluştur"
- **Ne yapıldı:** Görsel dil korunarak **işlevsel/etkileşimsel** boşluklar kapatıldı:
  - 🔴 **Mobil navigasyon kırıktı** (`<1040px` sidebar `display:none`, yerine hiçbir şey yok → telefonda
    gezinme + "Varlık Ekle" erişilemiyordu). Düzeltme: **off-canvas drawer** + sabit **mobil üst bar**
    (hamburger + marka + ＋). Escape/scrim/nav-tıklama ile kapanır; arka plan kaydırma kilidi.
  - 🔴 Boş durum mobilde **yanlış** yönlendiriyordu ("soldaki menü"). Yeni `EmptyState` (ikon + metin +
    cihazdan bağımsız **çalışan CTA** → `AppShellContext` ile modalı açar).
  - 🔴 `window.confirm` → stilize `ConfirmDialog` (alertdialog, odak yönetimi, Escape). "Sil" başlıktan
    alındı, alta **danger-zone**'a taşındı.
  - 🟠 Düz "Yükleniyor…" → `Skeleton`/`PortfolioSkeleton` (shimmer). Hata → **"Tekrar dene"** (retry).
  - 🟠 Aksiyon geri bildirimi yoktu → `ToastProvider`/`useToast` (ekle/fiyat/sil/para birimi).
  - 🟠 `AddHoldingDialog`: `<select>` → **type-chips** (ikonlu, radiogroup), autofocus, **Tab odak tuzağı**,
    satır-içi doğrulama ipucu, dolu formda yanlış kapanma koruması, koşullu mount (taze state).
  - 🟠 Pozisyon tablosu dar ekranda yatay kaydırma yerine **`data-label`'lı kart düzeni**; tüm satır tıklanır.
  - 🟠 KPI'lara eğitici **info-tooltip** (net kâr/zarar, reel getiri).
  - 🟡 Saate duyarlı selamlama, **veri tazeliği** etiketi, **sticky topbar**, skip-link + focus-visible,
    `prefers-reduced-motion`, HealthBadge stili, özel scrollbar. Ölü `HeroCard.tsx` silindi. Analiz sayfası
    "Yakında" görseliyle zenginleşti (disclaimer korunur).
- **Dokunulan dosyalar:** yeni `web/src/lib/{appShell.tsx,greeting.ts}`,
  `web/src/components/{Toast,ConfirmDialog,Skeleton,EmptyState,InfoTip}.tsx`; düzenlenen `web/src/App.tsx`,
  `web/src/App.css`, `web/src/components/{AddHoldingDialog,KpiGrid,HoldingsTable,AllocationDonut}.tsx`,
  `web/src/routes/{PortfolioPage,HoldingDetailPage,SettingsPage,AnalysisPage}.tsx`; silinen `HeroCard.tsx`;
  yeni testler `{greeting,Toast,ConfirmDialog,EmptyState}` + güncellenen `{PortfolioPage,AddHoldingDialog}` testleri.
- **Test:** web **28 yeşil** (19→28, +9 yeni), shared **13**, `tsc -b` + `vite build` temiz, **eslint 0 hata**
  (önceden var olan 2 lint hatası — AllocationDonut mutasyon + AddHoldingDialog set-state-in-effect — da giderildi).
  ⚠️ Canlı görsel doğrulama yapılmadı (backend 5310 ayakta değildi) — istenirse backend+Vite ile turlanabilir.
- **Karar/Not:** "Varlık Ekle" modalı kabuk durumundan `AppShellContext` ile açılır (boş durum + mobil için).
  Tablo responsive'i salt-CSS (`data-label`) → DOM/semantik korunur, testler stabil.
- **Durum:** tamamlandı
- **Sıradaki:** Faz 2 — T2.1 fiyat sağlayıcı + `IPriceProvider`.

## 2026-05-30 · Web görsel yükseltme — taslak referanslı zengin pano (T1.19)
- **Görev(ler):** T1.19 (tamam) — kullanıcı: "taslağı referans alıp daha güzel/görsel-yüksek frontend"
- **Ne yapıldı:** `portfoy-web-panosu-taslak.html` tasarım dili Faz 1'in **gerçek-veri** ekranlarına uygulandı
  (gelecek faz ekranları kurgusal koyulmadı). Yeni:
  - **assetMeta** (varlık türü → ikon + renk, ortak). **App.css** baştan tasarım sistemi olarak yazıldı.
  - **Sidebar:** marka mark'ı (◆), ikonlu gezinme + bölüm etiketleri, gradient "Varlık Ekle" (kabuk-seviyesi
    modal), kullanıcı + sağlık. **Topbar:** greeting + serif başlık + badge.
  - **Pano:** `KpiGrid` (glow'lu hero + 4 KPI), zengin `AllocationDonut` (merkez etiket), `PortfolioInsights`
    (En İyi/Zayıf + Hızlı Bilgi + Yoğunlaşma + **computed nudge** — top-2 yoğunlaşma > %50). `HoldingsTable`
    ikon + alt-bilgi + **renkli ağırlık çubuğu**. Tümü gerçek veriden.
  - **Detay:** ikon başlık + `detail-hero` + drow'lar + **BES split** (kendi/devlet katkı açıklamalı) +
    başlangıç tarihi + işlem geçmişi + formlar. **Ayarlar** sayfası (yeni route, para birimi chip'leri + disclaimer).
  - **Modal** (Varlık Ekle) yeni başlık/kapat yapısı. `vite.config` proxy hedefi `VITE_API_TARGET` ile ayarlanabilir.
- **Dokunulan dosyalar:** `web/src/App.css` (rewrite), `web/src/App.tsx`, `web/src/main.tsx`,
  `web/src/lib/assetMeta.ts`, `web/src/components/{KpiGrid,PortfolioInsights,AllocationDonut,HoldingsTable,
  AddHoldingDialog}.tsx`, `web/src/routes/{PortfolioPage,HoldingDetailPage,SettingsPage}.tsx`,
  ilgili testler, `web/vite.config.ts`.
- **Test:** web **19 yeşil** (testler yeni yapıya güncellendi), shared 13, `tsc` + `vite build` temiz.
  **Canlı görsel doğrulama:** kendi Release backend'im (5310, VS Debug kilidine takılmadan) + Vite (5180)
  ile pano/holdings/BES-detay ekran görüntüleriyle onaylandı — tüm tasarım taslakla birebir, gerçek veriyle.
- **Karar/Not:** Gelecek faz ekranları (Senaryo/Hisse/Eğitim/performans grafiği) backend gerektirdiği için
  yapılmadı. Para-birimi maruziyeti kartı atlandı (Fx pozisyonları TRY-fiyatlı → model temiz exposure vermiyor).
- **Durum:** tamamlandı
- **Sıradaki:** Faz 2 — T2.1 fiyat sağlayıcı + `IPriceProvider`.

## 2026-05-30 · BES özel modeli (T1.17) + pozisyon işlem geçmişi (T1.18) + BES veri onarımı
- **Görev(ler):** T1.17, T1.18 (tamam) — kullanıcı bildirimi: "BES'e işlem ekledim tüm değerler bozuldu"
- **Kök neden:** BES nominal hesap (kendi+devlet katkısı + fon getirisi), "alış/satış → ağırlıklı ort.
  maliyet" modeline uymuyor. Genel `AddTransaction` BES'e işlem ekleyince maliyeti işlemlerden yeniden
  türetip 148.554→4.250'ye düşürdü (getiri +%6.473!). Seed BES'i de transaction'sız nominal'di.
- **Ne yapıldı:**
  - **Veri onarımı (SQL):** bozuk BES işlemi silindi, `AvgCost` 148.554'e geri (kendi 120k + devlet 28.554).
  - **T1.17 BES modeli (backend):** `AddTransaction` BES'i reddediyor (400, "aylık katkı kullanın").
    Yeni `POST /holdings/{id}/bes-contribution` (kendi + devlet; verilmezse %30 TR kuralı) → `BesDetails`
    büyür, `AvgCost = own + state`. `BesDto`'ya `JoinedAtUtc` (başlangıç, nullable). Web: BES'te
    "Aylık katkı ekle" formu (alış/satış yerine) + %30 önizleme + başlangıç tarihi gösterimi.
  - **T1.18 işlem geçmişi:** `GET /holdings/{id}` → `transactions` listesi (en yeni üstte); web
    `TransactionHistory` tablosu her pozisyonun detayında. BES'te boş (işlem yok → katkı özeti gösterilir).
- **Dokunulan dosyalar:** `Finans.Application/Portfolio/{PortfolioDtos,IHoldingService}.cs`,
  `Finans.Infrastructure/Services/HoldingService.cs`, `Finans.Api/Controllers/HoldingsController.cs`,
  `packages/shared/src/{types,api}/index.ts`, `web/src/lib/hooks.ts`,
  `web/src/components/{BesContributionForm,TransactionHistory}.tsx` (+test),
  `web/src/routes/HoldingDetailPage.tsx`, `web/src/App.css`,
  `tests/Finans.Integration.Tests/BesAndHistoryApiTests.cs`, `.claude/docs/03-DATA-MODEL.md` §11.
- **Test:** backend **Application 39 + Integration 35 = 74 yeşil** (+4: BES reddi/katkı/geçmiş/başlangıç),
  web **19** (+4 BES form/geçmiş), shared 13, tsc temiz. **Release konfigürasyonunda** build/test edildi —
  kullanıcı app'i VS Debug'da çalıştırdığı için `bin/Debug` kilitli; `bin/Release` ayrı → kilide takılmadan.
- **Karar/Not:** BES ort.-maliyet kuralının istisnası (03 §11'e işlendi). Kullanıcının VS Debug instance'ı
  ESKİ backend → yeni BES uçlarını görmek için **VS debug oturumunu yeniden başlatması** gerekir (frontend HMR).
  BES katkı geçmişi (her katkı kaydı) ileride; şimdilik kümülatif kendi/devlet gösteriliyor.
- **Durum:** tamamlandı
- **Sıradaki:** Faz 2 — T2.1 fiyat sağlayıcı + `IPriceProvider`.

## 2026-05-30 · Mevcut pozisyona alış/satış işlemi ekleme UI'ı (T1.16)
- **Görev(ler):** T1.16 (tamam) — kullanıcı bildirimi: "pozisyonlara ekleme/çıkarma yapamıyorum"
- **Ne yapıldı:** **Gerçek eksik** — backend `POST /holdings/{id}/transactions` + shared `addTransaction`
  + `useAddTransaction` hook'u T1.6'dan beri vardı ama **çağıran UI yoktu** (detayda yalnız fiyat-güncelle/sil).
  - **`AddTransactionForm`:** Alış (ekleme) / Satış (çıkarma) segment + miktar + birim fiyat →
    `useAddTransaction`. Backend ort. maliyet/miktarı işlemlerden yeniden türetir (T1.5); fazla satış→400
    (hata zarfı gösterilir). Başarıda holding + summary + holdings invalidate → sayfa tazelenir.
  - `HoldingDetailPage`'e bağlandı (fiyat-güncelle formunun üstünde). CSS: alış/satış segment (yeşil/kırmızı).
- **Dokunulan dosyalar:** `web/src/components/AddTransactionForm.tsx` (+test),
  `web/src/routes/HoldingDetailPage.tsx`, `web/src/App.css`, `.claude/docs/08-BACKLOG.md`.
- **Test:** web **15 yeşil** (+3: alış POST gövdesi, satış type=Sell, eksikte pasif), `tsc` temiz.
- **Karar/Not:** Görsel doğrulama atlandı — kullanıcı app'i Visual Studio'da debug'da çalıştırıyor
  (Finans.Api PID 14732, parent VsDebugConsole, 5298'i tutuyor) → kendi backend'imi başlatamadım,
  onunkine de dokunmadım. Endpoint zaten o instance'ta mevcut; frontend HMR/rebuild ile görünür.
- **Durum:** tamamlandı
- **Sıradaki:** Faz 2 — T2.1 fiyat sağlayıcı + `IPriceProvider`.

## 2026-05-30 · Faz 1 loose-end taraması — Disclaimer (NFR-2, SC-W2) + bayat işaretler
- **Görev(ler):** Faz 1 kapanış denetimi (kullanıcı "atlanan adım var mı?")
- **Ne yapıldı:**
  - **Denetim:** Faz 0 + Faz 1 tüm T-görevleri `[x]`. Açık SC'lerin çoğu sonraki fazlar
    (SC-08 fiyat/Faz 2, SC-10/11 LLM, SC-12 hisse, SC-14/16 kimlik). Faz 1'e ait 3 loose-end bulundu.
  - **SC-W2 (NFR-2 — #1 kural):** gerçek boşluktu → `Disclaimer` bileşeni ("yatırım tavsiyesi
    değildir") + `AnalysisPage`'de her zaman görünür + RTL testi. (Daha önce Faz 3'e ertelenmişti.)
  - **Bayat işaretler düzeltildi:** SC-W1 (format testi T1.10'da vardı), SC-15 (hata-maskeleme testi
    T0.13'te vardı), T1.9 (`[~]`→`[x]`; web seçici T1.11'de yapılmıştı) → katalog/backlog gerçeğe çekildi.
  - **SC-W3 (Web E2E)** bilinçli ertelendi → Faz 2 (Playwright gerçek akış, iki-sunucu orkestrasyonu).
- **Dokunulan dosyalar:** `web/src/components/Disclaimer.tsx`, `web/src/routes/AnalysisPage.tsx` (+test),
  `web/src/App.css`, `.claude/docs/{08-BACKLOG,09-TESTING-STRATEGY}.md`.
- **Test:** web **12 yeşil** (+1 disclaimer SC-W2), `tsc` temiz.
- **Karar/Not:** Faz 1'de kalan tek bilinçli erteleme **SC-W3 web E2E** (Faz 2'de iki sunucu ayakta
  iken). Geri kalan her şey kapalı.
- **Durum:** tamamlandı
- **Sıradaki:** Faz 2 — T2.1 fiyat sağlayıcı + `IPriceProvider`.

## 2026-05-30 · Seed verisini çeşitlendir (4→7 pozisyon, zarar + USD-fiyatlı hisse)
- **Görev(ler):** ad-hoc (kullanıcı isteği) — seed zenginleştirme
- **Ne yapıldı:**
  - **Seed 4→7 pozisyon:** mevcut (altın/dolar/BES/nakit) + **Euro** (800 € @47,50) + **Apple** (12 @175 $,
    **USD-fiyatlı** → summary'de gerçek USD→TRY ×48 çevrimi) + **Teknoloji Fonu** (1.500 @28,00, güncel
    23,50 → **−%16,1 zarar**, eğitici negatif örnek). Yeni `Asset`/`Holding`/`Transaction`/`PriceSnapshot`.
  - **Yeni baz TRY toplamları:** maliyet 603.770 · değer 839.213 · kâr +235.443 · **+%39,0** · reel **+%0,7**.
  - **Testler güncellendi:** DB-seed okuyan 4 integration testi yeni sayılara çekildi (USD-fiyatlı kalem
    ×48 çevrilerek baz-TRY toplamı). PortfolioApiTests'e AAPL kur-çevrimi + fon negatif-getiri kontrolü
    eklendi. **Application.Tests'e dokunulmadı** (kendi hardcoded "altın formül" setleri, DB'den bağımsız).
  - Yerel PostgreSQL TRUNCATE + `dotnet run -- seed` ile yeniden tohumlandı; **canlı UI görsel doğrulandı**
    (7-dilim donut, Apple $→₺ çevrimi, fon kırmızı zarar).
- **Dokunulan dosyalar:** `Finans.Infrastructure/Seed/SeedData.cs`, `tests/Finans.Integration.Tests/
  {SeedConsistencyTests,SqliteIntegrationTests,PortfolioApiTests,InflationRealReturnTests}.cs`,
  `.claude/docs/03-DATA-MODEL.md` §12.
- **Test:** `dotnet test` **Application 39 + Integration 31 = 70 yeşil**. Görsel doğrulandı.
- **Karar/Not:** Apple USD-fiyatlı tutuldu → summary'de kur çevrimi artık gerçek veriyle de test ediliyor.
  Build sırasında görsel-doğrulama koşumundan kalan orphan `Finans.Api` süreci DLL kilitlemişti → sonlandırıldı
  (ileride TaskStop sonrası child süreç kontrolü).
- **Durum:** tamamlandı
- **Sıradaki:** Faz 2 — T2.1 fiyat sağlayıcı + `IPriceProvider`.

## 2026-05-30 · Canlı görsel doğrulama + FX/enflasyon in-memory cache (T1.15) → FAZ 1 MVP TAMAM
- **Görev(ler):** T1.15 (cache, tamam), T1.8 (BES detay ekranı, tamam) · Faz 1 kapanışı
- **Ne yapıldı:**
  - **Görsel doğrulama:** backend (5298) + Vite (5173) canlı; PostgreSQL'e karşı portföy sayfası,
    HeroCard, donut+lejant, Pozisyonlar tablosu ve "Varlık Ekle" modalı ekran görüntüsüyle doğrulandı
    (tr-TR biçim, renkler, BES devlet katkısı ayrı). Uçtan uca zincir çalışıyor.
  - **Cache (T1.15):** `CachedFxRateProvider` + `CachedInflationRateProvider` decorator'ları
    (`IMemoryCache`, 60 sn TTL, global anahtar — kullanıcı-bağımsız). `AddMemoryCache` + DI'da
    Ef sağlayıcı → cache decorator sarması. §13 "dış çağrı/DB cache'lenir" kapısı.
  - **T1.8:** BES detay ekranında "Kendi katkın / Devlet katkısı (ayrı) / Hak ediş" gösterimi.
- **Dokunulan dosyalar:** `Finans.Infrastructure/Services/{CachedFxRateProvider,CachedInflationRateProvider}.cs`,
  `Finans.Infrastructure/DependencyInjection.cs`, `tests/Finans.Integration.Tests/ProviderCacheTests.cs`.
- **Test:** cache testi (TTL içinde DB'ye güncel kur eklendi → sağlayıcı hâlâ eski/cached değer döndü =
  cache hit kanıtı). `dotnet test` **Application 39 + Integration 31 = 70 yeşil**. Web 11 + shared 13.
  Görsel: canlı PostgreSQL'e karşı tüm ekranlar doğrulandı.
- **Karar/Not:** Per-user summary'nin **server** cache'i ertelendi — tek kullanıcılı dev'de React
  Query istemcide tazeliyor; gerekince UserId anahtarlı server cache + mutation invalidation eklenir.
  Playwright e2e gerçek akış güncellemesi Faz 2'ye (iki sunucu orkestrasyonu).
- **Durum:** tamamlandı → **FAZ 1 — Portföy Takip MVP TAMAM**
- **Sıradaki:** Faz 2 — canlı fiyat API'si + bağlama-duyarlı eğitici notlar (nudge).

## 2026-05-30 · Web: "Varlık Ekle" modal formu → POST /holdings (T1.14) — Faz 1 web seti bitti
- **Görev(ler):** T1.14 (tamam)
- **Ne yapıldı:** `AddHoldingDialog` — özel overlay modal (jsdom-dostu, role=dialog/aria-modal,
  Escape/dış tık kapatma). Form: tür (preset birim), ad, sembol, para birimi, birim, miktar,
  alış birim fiyatı → `useCreateHolding` POST /holdings. İstemci doğrulama (ad/birim/miktar>0/
  fiyat≥0) + backend hata zarfı (çakışma/validasyon) gösterimi. PortfolioPage'e "+ Varlık Ekle"
  butonu. Üretim `vite build` temiz (343 kB / 108 kB gzip).
- **Dokunulan dosyalar:** `web/src/components/AddHoldingDialog.tsx` (+test),
  `web/src/routes/PortfolioPage.tsx`, `web/src/App.css`.
- **Test:** web **11 yeşil** (+3: kapalı render yok, doldur→POST gövdesi doğru→kapan, eksikte pasif),
  `tsc` + `vite build` temiz.
- **Karar/Not:** Faz 1'in tüm web ekranları (T1.11-14) tamam; başarı zinciri kullanıcı→form→
  POST→invalidate→özet/tablo tazelenir. Sıradaki: canlı görsel doğrulama + T1.15 cache.
- **Durum:** tamamlandı
- **Sıradaki:** Canlı görsel doğrulama (web+backend), sonra T1.15 in-memory cache.

## 2026-05-30 · Web: Holdings tablosu + varlık detay (T1.13)
- **Görev(ler):** T1.13 (tamam)
- **Ne yapıldı:** `HoldingsTable` (gerçek tablo; dar ekran yatay kaydırma; ad→detay link, tr-TR
  biçim, pozitif/negatif renk). `HoldingDetailPage` (route `/holdings/:id`): metrikler + BES
  (devlet katkısı ayrı) + **güncel fiyat güncelle** (FR-1.8, `useUpdateHolding`) + **sil**
  (`useDeleteHolding` → portföye dön). `formatNumber` (tr-TR miktar) shared'a eklendi.
  PortfolioPage'e "Pozisyonlar" bölümü.
- **Dokunulan dosyalar:** `web/src/components/HoldingsTable.tsx` (+test),
  `web/src/routes/{HoldingDetailPage,PortfolioPage}.tsx`, `web/src/main.tsx` (route),
  `web/src/App.css`, `packages/shared/src/format/index.ts` (formatNumber).
- **Test:** web **8 yeşil** (+2 tablo: link/biçim, boş), shared 13, `tsc` temiz.
- **Karar/Not:** Detay route olarak yapıldı (modal değil) — derin link + geri tuşu doğal.
  Holdings'in dar-ekran kart varyantı yatay kaydırmaya bırakıldı (MVP).
- **Durum:** tamamlandı
- **Sıradaki:** T1.14 "Varlık Ekle" formu (modal) → POST /holdings.

## 2026-05-30 · Web: AllocationDonut (SVG) + legend (T1.12)
- **Görev(ler):** T1.12 (tamam)
- **Ne yapıldı:** `AllocationDonut` — SVG donut (her dilim ağırlığı kadar yay, varlık-türü
  renkleri DESIGN.md token'ından) + lejant (ad / işaretsiz % / değer). `role="img"` + aria-label
  (erişilebilir özet). PortfolioPage'de hero ile yan yana (geniş ekran grid). `formatPercent`'e
  `signed` parametresi eklendi → ağırlıkta "+" yok (getiride var).
- **Dokunulan dosyalar:** `web/src/components/AllocationDonut.tsx` (+test), `web/src/routes/PortfolioPage.tsx`,
  `web/src/App.css`, `packages/shared/src/format/index.ts` (+test).
- **Test:** shared **13 yeşil** (signed=false), web **6 yeşil** (+3 donut: ad/%, aria, boş), `tsc` temiz.
- **Durum:** tamamlandı
- **Sıradaki:** T1.13 Holdings tablo/kart + varlık detay.

## 2026-05-30 · Web: AppShell (sidebar) + HeroCard + summary bağlama + para birimi seçici (T1.11)
- **Görev(ler):** T1.11 (tamam); T1.9'un web ayağı (para birimi seçici) da burada
- **Ne yapıldı:**
  - **AppShell** sidebar düzenine geçti (geniş ekran sol sidebar; dar ekran üst bar — responsive grid).
  - **HeroCard:** toplam değer (büyük) + net kâr / getiri / reel getiri / maliyet; pozitif/negatif renk.
  - **CurrencySelector:** TRY/USD/EUR segment; seçim `useUpdateSettings` ile tercihi günceller →
    summary/holdings invalidate → seçilen baz pb'ye göre yeniden hesaplanır.
  - **PortfolioPage** veri bağlandı (`usePortfolioSummary`/`useSettings`); loading/error/boş durum.
  - CSS: sidebar, hero, seçici, sayfa başlığı (DESIGN.md token'ları). Test yardımcısı
    `renderWithProviders` (QueryClient sarmalı).
- **Dokunulan dosyalar:** `web/src/App.tsx`, `web/src/App.css`,
  `web/src/components/{HeroCard,CurrencySelector}.tsx`, `web/src/routes/PortfolioPage.tsx`,
  `web/src/routes/PortfolioPage.test.tsx`, `web/src/test/renderWithProviders.tsx`.
- **Test:** web **3 yeşil** (HeroCard tr-TR biçim + seçici aria-pressed; fetch mock), `tsc` temiz.
- **Karar/Not:** Görsel doğrulama web seti (T1.12-14) bitince topluca yapılacak. Donut (T1.12) ve
  holdings tablosu (T1.13) sayfaya eklenecek.
- **Durum:** tamamlandı
- **Sıradaki:** T1.12 `AllocationDonut` (SVG/conic) + legend.

## 2026-05-30 · `@finans/shared` API tipleri + istemci + web React Query hook'ları (T1.10)
- **Görev(ler):** T1.10 (tamam)
- **Ne yapıldı:**
  - **shared/types:** 04 sözleşmesi tipleri — `AssetType`/`TransactionType`/`VestingState`,
    `Holding`/`Bes`/`PortfolioSummary`/`AllocationSlice`, `CreateHoldingInput`/`TransactionInput`/
    `UpdateHoldingInput`, `Settings`, hata zarfı (`ApiErrorEnvelope`).
  - **shared/api:** `createApiClient` genişletildi (summary/holdings CRUD/transactions/settings);
    `request` artık hata zarfını çözüp `ApiError{status,code,message}` fırlatıyor, 204'ü gövdesiz çözüyor.
  - **web/lib/hooks.ts:** TanStack Query hook'ları (usePortfolioSummary/useHoldings/useHolding/
    useSettings + create/addTx/update/delete/updateSettings mutation'ları; query key'ler + invalidation).
  - **vitest:** `include` yalnızca `src` → Playwright `e2e/*.spec.ts` artık vitest'e sızmıyor.
- **Dokunulan dosyalar:** `packages/shared/src/{types,api}/index.ts`, `packages/shared/src/api/api.test.ts`,
  `web/src/lib/hooks.ts`, `web/vite.config.ts`.
- **Test:** shared **12 yeşil** (4 yeni api: baseCurrency URL, hata zarfı parse, 204, gövdesiz hata),
  web **2 yeşil**, shared+web `tsc --noEmit` temiz.
- **Karar/Not:** Hook'lar şimdilik web'de (shared'ı React'a bağımlı kılmamak için); mobil (Faz M)
  gelince shared'a taşınır. Vite proxy backend'i `http://localhost:5298` bekliyor (web `pnpm dev`).
- **Durum:** tamamlandı
- **Sıradaki:** T1.11 Web AppShell + HeroCard + summary bağlama + para birimi seçici.

## 2026-05-30 · Settings endpoint — baz para birimi (T1.9 backend)
- **Görev(ler):** T1.9 (backend kısmı tamam; web seçimi T1.11'de)
- **Ne yapıldı:** `GET/PUT /api/settings` (04 §4) — `ISettingsService`/`SettingsService` (kullanıcıya
  kapsanır), `SettingsDto`/`UpdateSettingsRequest`, `SettingsController`. PUT `User.BaseCurrency`'i günceller.
- **Dokunulan dosyalar:** `Finans.Application/Portfolio/ISettingsService.cs`,
  `Finans.Infrastructure/Services/SettingsService.cs`, `Finans.Infrastructure/DependencyInjection.cs`,
  `Finans.Api/Controllers/SettingsController.cs`, `tests/Finans.Integration.Tests/SettingsApiTests.cs`.
- **Test:** 3 integration (GET seed TRY; PUT→USD kalıcı; ayar kullanıcılar arası sızmıyor).
  `dotnet test` **Application 39 + Integration 30 = 69 yeşil**.
- **Karar/Not:** Web tarafı (para birimi seçici) T1.11 AppShell ile gelecek.
- **Durum:** tamamlandı (backend)
- **Sıradaki:** T1.10 `@finans/shared` (API tipleri + TanStack Query hook + tr-TR format).

## 2026-05-30 · Portföy API: Holdings CRUD + summary (T1.6 + T1.7) — ilk gerçek endpoint'ler
- **Görev(ler):** T1.6 (tamam), T1.7 (tamam) · ayrıca T1.8 kısmi (bes alanı), T1.15 kısmi (per-user)
- **Ne yapıldı:**
  - **Endpoint'ler (04 §4):** `GET/POST/PUT/DELETE /api/holdings`, `POST /holdings/{id}/transactions`,
    `GET /api/portfolio/summary`. DTO'lar Application'da; servisler (`IHoldingService`/`IPortfolioService`)
    arayüzü Application, EF impl Infrastructure (mevcut sağlayıcı desenine uygun).
  - **Per-user izolasyon (11 §3):** `ICurrentUser` (Faz 1: `X-User-Id` başlığı / `Auth:DevUserId`
    dev varsayılanı; Faz 5 JWT'ye hazır). Her sorgu `WHERE UserId`; başkasının id'si → 404 (IDOR yok).
  - **Validasyon + hata:** DataAnnotations → `InvalidModelStateResponseFactory` ApiError zarfı;
    `AppExceptionHandler` (NotFound→404, Validation→400, Conflict→409) GlobalExceptionHandler'dan önce.
  - **Akış:** create varlığı katalogdan bul/oluştur + ilk işlemle pozisyon; ort. maliyet/miktar
    `DerivePosition` ile (T1.5); summary fx (T1.3) + enflasyon (T1.4) ile baz pb'ye çevirip özetler.
  - **JSON:** enum'lar string (`JsonStringEnumConverter`).
- **Dokunulan dosyalar:** `Finans.Application/Common/{ICurrentUser,ApplicationExceptions}.cs`,
  `Finans.Application/Portfolio/{PortfolioDtos,IHoldingService}.cs`,
  `Finans.Infrastructure/Services/{HoldingMapping,HoldingService,PortfolioService}.cs`,
  `Finans.Infrastructure/DependencyInjection.cs`, `Finans.Api/Auth/HttpCurrentUser.cs`,
  `Finans.Api/ErrorHandling/AppExceptionHandler.cs`, `Finans.Api/Controllers/{Holdings,Portfolio}Controller.cs`,
  `Finans.Api/Program.cs`, `appsettings.Development.json` (Auth:DevUserId),
  `tests/Finans.Integration.Tests/PortfolioApiTests.cs`.
- **Test:** SC-01 (summary/holdings) + SC-04 (BES ayrı) + SC-07 (geçersiz→400) + SC-13 (IDOR→404) —
  9 yeni integration testi. `dotnet test` **Application 39 + Integration 27 = 66 yeşil**. Ayrıca
  **canlı PostgreSQL'e karşı smoke**: summary/holdings/create/IDOR/validasyon/delete doğrulandı,
  test verisi temizlendi (DB seed-tutarlı).
- **Karar/Not:** **EF tuzağı** — `Entity.Id = Guid.CreateVersion7()` initializer'ı yüzünden, izlenen
  bir parent'ın koleksiyonuna eklenen yeni child'ı EF "mevcut" sanıp UPDATE'e çevirir (0 row → sahte
  `DbUpdateConcurrencyException`). `AddTransaction`'da yeni işlem **açıkça `EntityState.Added`** ile
  işaretlendi (`db.Add` create'te grafiği zaten Added yapıyor). Allocation şimdilik kalem-başına dilim
  (seed'de tür=kalem). Cache (T1.15) ve web ekranları (T1.8) sonraki.
- **Durum:** tamamlandı
- **Sıradaki:** T1.9 settings (baz pb) endpoint, sonra T1.10 `@finans/shared` + web (T1.11-14).

## 2026-05-30 · Reel getiri verisi bağlama (T1.4) + ort. maliyet türetimi (T1.5)
- **Görev(ler):** T1.4 (tamam), T1.5 (tamam) · Faz 1 · sıralı
- **Ne yapıldı:**
  - **T1.4 — enflasyon bağlama:** `IInflationRateProvider` (Application) + `EfInflationRateProvider`
    (en güncel dönemin yıllık oranı, PeriodEndUtc max). DI scoped. `RealReturn` saf formülü
    zaten vardı (T1.2); artık seed enflasyonu (0,38) yüklenip summary'ye besleniyor →
    reel getiri ≈ %9,89 (nominal %51,6).
  - **T1.5 — ort. maliyet türetimi:** `PortfolioCalculationService.DerivePosition` (saf, statik):
    `AvgCost = Σ(Buy.Qty×UnitPrice + Buy.Fee)/Σ Buy.Qty`, `Quantity = Σ Buy.Qty − Σ Sell.Qty`.
    **Satış ortalamayı bozmaz**, sadece miktarı düşürür (ort. maliyet yöntemi; FIFO/LIFO Faz 5).
    Yeni saf record'lar: `TransactionInput`, `PositionBasis` (EF entity'sine bağımsız).
- **Dokunulan dosyalar:** `Finans.Application/Portfolio/IInflationRateProvider.cs`,
  `.../PortfolioModels.cs` (+TransactionInput/PositionBasis), `.../PortfolioCalculationService.cs`
  (+DerivePosition), `Finans.Infrastructure/Persistence/EfInflationRateProvider.cs`,
  `.../DependencyInjection.cs`, `tests/.../Portfolio/PositionDerivationTests.cs`,
  `tests/Finans.Integration.Tests/{InflationRealReturnTests,PositionDerivationConsistencyTests}.cs`.
- **Test:** SC-05 (enflasyon bağlama integration: provider 0,38 yükler, summary reel %9,89) +
  SC-06 (DerivePosition unit 8 test: tek/çok alış, komisyon, satış ort. bozmaz, alış-satış-alış,
  boş, yalnız satış + seed tx→holding tutarlılık integration). `dotnet test`
  **Application 39 + Integration 18 = 57 yeşil**.
- **Karar/Not:** İki saf çekirdek (RealReturn, DerivePosition) artık veri katmanına bağlandı.
  Enflasyon dönem-duyarlı seçimi (pozisyon ufkuna göre) ileri fazda; cache Faz 2. `DerivePosition`
  T1.6 HoldingService'te işlem değişiminde Holding.Quantity/AvgCost'u yeniden hesaplamak için kullanılacak.
- **Durum:** tamamlandı
- **Sıradaki:** T1.6 Holdings CRUD endpoint + DTO + validasyon (+IDOR/per-user kapsam) · T1.7 `GET /portfolio/summary`.

## 2026-05-30 · `CurrencyConverter` + `IFxRateProvider` (EF) + test (T1.3)
- **Görev(ler):** T1.3 (tamam) · Faz 1
- **Ne yapıldı:**
  - **Saf `CurrencyConverter`** (`Finans.Application/Portfolio/`): değişmez kur anlık
    görüntüsü alan, I/O'suz, deterministik dönüşüm. Aynı pb → birim; doğrudan kur (×);
    ters kur (÷); çapraz kur (pivot pb üzerinden iki adım). `Convert`/`TryConvert`/`RateFor`.
  - **Hassasiyet kararı (NFR-1):** ters yön `1/Rate` saklanıp çarpılmaz — **bölme** ile
    uygulanır. Aksi halde 96.000 ₺ → 1.999,99… $ çıkıyordu; finans uygulamasında kabul
    edilemez. Çapraz kur de adım adım çarp/böl. Sıfır/negatif tırnak güvenlik için atlanır.
  - **`IFxRateProvider`** (Application arayüzü) + **`EfFxRateProvider`** (Infrastructure):
    her pb çifti için EN GÜNCEL tırnağı (AsOfUtc) yükler. EF `GroupBy().First()` SQL'e
    çevrilemediğinden sıralı çekip bellekte gruplanır (FxRates küçük). DI: provider scoped,
    `PortfolioCalculationService` singleton.
- **Dokunulan dosyalar:** `Finans.Application/Portfolio/CurrencyConverter.cs`,
  `Finans.Infrastructure/Persistence/EfFxRateProvider.cs`,
  `Finans.Infrastructure/DependencyInjection.cs`,
  `tests/Finans.Application.Tests/Portfolio/CurrencyConverterTests.cs`,
  `tests/Finans.Integration.Tests/FxRateProviderTests.cs`.
- **Test:** SC-03 unit (11 test: doğrudan/ters/çapraz, tam hassasiyet, bilinmeyen kur→hata/false,
  güvensiz tırnak atlama) + integration (2 test: seed'den en güncel kur 48 seçilir, çapraz
  EUR→USD=104). `dotnet test` **Application 31 + Integration 15 = 46 yeşil**.
- **Karar/Not:** Converter saf; gerçek bağlama (USD varlığın summary'de TRY'ye çevrilmesi)
  T1.7 endpoint'inde. Cache (anahtar pb çifti) T1.15/Faz 2'de.
- **Durum:** tamamlandı
- **Sıradaki:** T1.4 reel getiri (enflasyon verisi bağlama) + T1.5 ort. maliyet türetimi (tx→AvgCost).

## 2026-05-30 · `PortfolioCalculationService` + birim testleri (T1.1 + T1.2) → FAZ 1 başladı
- **Görev(ler):** T1.1 (tamam), T1.2 (tamam) · Faz 1 ilk görevi
- **Ne yapıldı:**
  - **Saf hesaplama servisi** (`Finans.Application/Portfolio/`): CLAUDE.md §6 formülleri
    tek yerde, yan etkisiz, I/O yok. Statik saf yardımcılar: `TotalCost`, `CurrentValue`,
    `Profit`, `ReturnRatio`, `WeightedAverageCost`, `RealReturn` (Fisher). Toplayıcı
    `CalculateSummary` (özet + dağılım, ops. enflasyonla reel getiri) ve `CalculateHoldings`
    (kalem metrikleri + ağırlık). Tüm hesap `decimal`; yuvarlama yok (NFR-1).
  - **Modeller** (`PortfolioModels.cs`): `HoldingInput`/`HoldingResult`/`PortfolioSummary`/
    `AllocationSlice` — EF entity'sine bağımsız saf record'lar (04 §4 şekliyle uyumlu).
  - **Null politikası:** fiyatsız/sıfır-maliyetli kalemde değer/kâr/getiri null; reel
    getiri enflasyon yoksa null (04 §4 sözleşmesi).
  - **20 birim testi:** seed seti BİREBİR (maliyet 422.970, değer 641.403, kâr +218.433,
    +%51,6; altın +%43), dağılım ağırlıkları (04 §4: 0,405/0,436/0,150/0,009, toplam 1),
    reel getiri formülü, kenar durumlar (boş portföy, fiyatsız kalem, sıfır maliyet).
- **Dokunulan dosyalar:** `backend/src/Finans.Application/Portfolio/PortfolioModels.cs`,
  `.../Portfolio/PortfolioCalculationService.cs`,
  `backend/tests/Finans.Application.Tests/Portfolio/PortfolioCalculationServiceTests.cs`.
- **Test:** SC-01 (unit ✓), SC-02 (✓), SC-05 (✓), SC-06 (saf çekirdek ✓) — `dotnet test`
  **Application 20/20 + Integration 13/13 = 33 yeşil**. (SC-01 integration, SC-06 tx türetimi
  sonraki görevlerde.)
- **Karar/Not:** Servis tamamen saf (entity'siz record girdi) → repository/EF bağlama
  T1.6/T1.7'ye bırakıldı. Faz 1 varsayımı: kalemler baz pb cinsinden fiyatlı; para birimi
  dönüşümü (T1.3) girdiler servise verilmeden uygulanacak. `RealReturn`/`WeightedAverageCost`
  saf çekirdekleri burada test edildi; veri bağlama T1.4/T1.5.
- **Durum:** tamamlandı
- **Sıradaki:** T1.3 `CurrencyConversionService` + `FxRates` (elle kur) + test.

## 2026-05-30 · Test altyapısı tamam — Sqlite fixture + Playwright (T0.11) → FAZ 0 BİTTİ
- **Görev(ler):** T0.11 (tamam) · dal `feat/test-infra` · **Faz 0 kapanışı**
- **Ne yapıldı:**
  - **Model sağlayıcı-duyarlı:** Npgsql'e özgü `citext`/`xmin`(xid)/`HasPostgresExtension`
    `FinansDbContext.OnModelCreating`'de `Database.IsNpgsql()` koşuluna alındı; aksi
    sağlayıcıda (Sqlite/InMemory) `IPAddress`→string converter. Npgsql model/migration
    değişmedi (design-time Npgsql). Konfig sınıflarından provider-özgü parçalar çıkarıldı.
  - **Sqlite integration fixture:** `SqliteWebApplicationFactory` (in-memory bağlantı
    açık tutulur; `ConfigureTestServices` ile Npgsql DbContext kaydı ada-göre temizlenip
    Sqlite eklenir) + `SqliteIntegrationTests` (EnsureCreated+seed; /health/ready Healthy,
    seed tutarlılığı relational'da, CK_Holdings_Quantity negatifi reddediyor).
  - **Playwright iskeleti (web):** `@playwright/test` + `playwright.config.ts`
    (channel "chrome" → indirme yok; `webServer` vite'ı otomatik başlatır) +
    `e2e/smoke.spec.ts` (yüklenme + tr-TR format + route geçişi) + `e2e` script'i.
- **Dokunulan dosyalar:** `Finans.Infrastructure/Persistence/FinansDbContext.cs`,
  `Configurations/{Portfolio,Identity}Configurations.cs`, `tests/.../Sqlite*.cs`,
  `tests/.../Finans.Integration.Tests.csproj` (Sqlite paketi), `web/playwright.config.ts`,
  `web/e2e/smoke.spec.ts`, `web/package.json`.
- **Test:** backend `dotnet test` **13/13**, web vitest 2, shared 8, **Playwright e2e 1** —
  hepsi yeşil. (e2e sistem Chrome ile; backend kapalıyken proxy hatası beklenen, test bağımsız.)
- **Karar/Not:** Model artık sağlayıcı-portatif (prod Npgsql, test Sqlite); IsNpgsql
  koşulu tek nokta. Playwright iskelet — gerçek akışlar Faz 1'de. **Faz 0 DoD'un tamamı
  karşılandı.**
- **Durum:** tamamlandı → **FAZ 0 BİTTİ**
- **Sıradaki:** Faz 1 — T1.1 `PortfolioCalculationService` (saf hesap + birim test, NFR-1).

## 2026-05-30 · Docker temeli — compose ile migrate+seed'li API (T0.14)
- **Görev(ler):** T0.14 (tamam) · dal `feat/docker`
- **Ne yapıldı:**
  - `backend/Dockerfile`: çok aşamalı (sdk:10.0 build → aspnet:10.0-alpine runtime),
    csproj-önce restore (cache), **non-root** (`USER $APP_UID` → uid 1654), düz HTTP
    8080, busybox wget HEALTHCHECK (`/health`). `backend/.dockerignore`.
  - `docker-compose.yml` (kök): postgres (healthcheck + volume + dev 5433) + api
    (depends_on healthy, env ile connstr/CORS/migrate+seed). Parola env/`.env`
    (dev varsayılan finans_dev; repoda gerçek sır yok).
  - Program.cs: bayrakla **başlangıçta migration (+ops. seed)** (`Database__Apply
    MigrationsOnStartup`/`Database__Seed`; testlerde varsayılan kapalı → DB'siz) +
    **koşullu HttpsRedirection** (`Security__UseHttpsRedirection`, container'da false).
- **Dokunulan dosyalar:** `backend/Dockerfile`, `backend/.dockerignore`,
  `docker-compose.yml`, `Finans.Api/Program.cs`, `appsettings.json`.
- **Test:** `dotnet test` 10/10 yeşil (değişmedi; startup-migration varsayılan kapalı).
  **Canlı `docker compose up --build`:** /health & /health/ready (DB) Healthy,
  /api/health 200; başlangıç migrate+seed → cost=422.970/value=641.403/holdings=4;
  `docker exec api id` → uid=1654(app) (non-root doğrulandı).
- **Karar/Not:** Container düz HTTP servis eder, TLS reverse proxy'de sonlanır
  (T2.9); postgres host portu (5433) dev için, prod'da kaldırılır (iç ağ — 11 §5).
  Startup-migration yalnız dev/compose kolaylığı; prod'da kapalı.
- **Durum:** tamamlandı
- **Sıradaki:** T0.11 kalanı (Sqlite integration fixture + Playwright iskeleti) →
  Faz 0 TAM kapanış.

## 2026-05-30 · Güvenlik + gözlemlenebilirlik temeli (T0.12 + T0.13)
- **Görev(ler):** T0.12, T0.13 (tamam) · dal `feat/security-observability`
- **Ne yapıldı:**
  - **T0.12** Serilog (`Serilog.AspNetCore`, Console sink) + `CorrelationIdMiddleware`
    (X-Correlation-ID üret/echo + LogContext) + `UseSerilogRequestLogging` +
    **redaksiyon iskeleti** (`SensitiveDataDestructuringPolicy`: password/token/
    secret/email içeren nesnelerde `***`, dar etki). Health: `/health` (liveness,
    predicate false) + `/health/ready` (`AddDbContextCheck`, tag "ready").
  - **T0.13** Global hata maskeleme (`GlobalExceptionHandler : IExceptionHandler` +
    `AddExceptionHandler`/`UseExceptionHandler`): istemciye sözleşmeli
    `{error:{code:"INTERNAL_ERROR",message}}`, stack/iç detay yalnız log'da
    (04 §2, 11 §4). CORS allow-list (`Cors:AllowedOrigins`, `*` yok). Secret:
    `dotnet user-secrets init` (UserSecretsId), parola repoda değil (env/secrets).
- **Dokunulan dosyalar:** `Finans.Api/Program.cs` (Serilog+health+CORS+exception+
  correlation boru hattı), `Finans.Api/ErrorHandling/{ApiError,GlobalExceptionHandler}.cs`,
  `Finans.Api/Observability/{CorrelationIdMiddleware,SensitiveDataDestructuringPolicy}.cs`,
  `appsettings.json`+`.Development.json` (Cors), `Finans.Api.csproj` (paketler+UserSecretsId),
  `tests/.../ObservabilitySecurityTests.cs`, `tests/.../AssemblyInfo.cs`.
- **Test:** `dotnet test` **10/10 yeşil** (3 ardışık koşu — flaky değil). Yeni: liveness
  health, correlation üret/echo, hata maskeleme (stack sızmaz), redaksiyon (hassas
  maskelenir/diğerleri korunur). **Canlı doğrulama:** /health & /health/ready (DB)
  Healthy, correlation header, CORS 5174 kabul / evil.com red, Serilog request log.
- **Karar/Not:** Integration testleri **sıralı** koşar (`DisableTestParallelization`)
  — statik `Log.Logger`+`CloseAndFlush` paralel host'larda çakışıyordu. Güvenlik
  başlıkları/rate-limit/TLS reverse proxy'de (T2.9). Redaksiyon alan listesi Faz 1'de genişler.
- **Durum:** tamamlandı (T0.12, T0.13)
- **Sıradaki:** T0.14 (Docker: API Dockerfile non-root + compose api+postgres) +
  T0.11 kalanı → Faz 0 kapanışı.

## 2026-05-30 · Tasarım token'ları + fontlar (DESIGN.md → web)
- **Görev(ler):** T0.9 (tamam) · dal `feat/design-tokens`
- **Ne yapıldı:**
  - `@finans/shared/theme`: DESIGN.md §2-4 token'ları **tek kaynak** TS objesi
    (`tokens`: color/font/radius/space/shadow) + `cssVariables()` üretici
    (camelCase→kebab, grup önekleri: `--font-*`/`--radius-*`/`--space-*`/`--shadow-*`).
  - Web: `@fontsource-variable/fraunces` + `hanken-grotesk` (self-hosted, CDN yok);
    `applyTheme()` token'ları paint öncesi `:root`'a enjekte ediyor; `index.css`
    atmosfer (iki radial-gradient) + Fraunces başlık + Hanken gövde + tabular-nums;
    `App.css` token'lara taşındı (gold/mint/coral, panel, hero gölge).
  - Font ailesi `'Fraunces Variable'` (web) + `'Fraunces'` fallback (mobil expo-font).
- **Dokunulan dosyalar:** `packages/shared/src/theme/index.ts` (+`theme.test.ts`),
  `web/src/lib/applyTheme.ts` (+test), `web/src/main.tsx`, `web/src/index.css`,
  `web/src/App.css`, `web/src/vite-env.d.ts`, `web/package.json`.
- **Test:** `pnpm test` yeşil — shared 8 (format 4 + theme 4), web 2 (render + applyTheme).
  `pnpm --filter @finans/web build` yeşil. **Görsel doğrulama:** Vite dev (5180)
  Chrome screenshot — Fraunces/Hanken, altın aksan, mint pozitif, coral health
  hatası, atmosfer halesi DESIGN.md ile uyumlu.
- **Karar/Not:** Token'lar runtime'da `:root`'a enjekte (FOUC yok, render öncesi);
  fontsource side-effect import'u için `declare module "@fontsource-variable/*"`.
- **Durum:** tamamlandı
- **Sıradaki:** T0.12 (Serilog + /health,/health/ready) / T0.13 (güvenlik+CORS) /
  T0.14 (Docker compose) + T0.11 kalanı → Faz 0 kapanışı.

## 2026-05-29 · Veri katmanı: EF Core + entity'ler + migration + tutarlı seed
- **Görev(ler):** T0.4, T0.5, T0.6, T0.6b (tamam) · dal `feat/data-layer`
- **Ne yapıldı:**
  - **T0.4** EF Core + Npgsql (`Npgsql.EntityFrameworkCore.PostgreSQL`) +
    `FinansDbContext` (Infrastructure). Global convention'lar: decimal→numeric(18,6),
    enum→varchar (HasConversion<string>). citext eklentisi. `AddInfrastructure` DI +
    `DesignTimeDbContextFactory` (env `ConnectionStrings__Postgres`).
  - **T0.5** Domain entity'leri: portföy (Asset, Holding, Transaction, BesDetails,
    PriceSnapshot, FxRate, InflationRate) + kimlik/audit (User, Role,
    UserRoleAssignment, RefreshToken, AuditLog). Base `Entity` (UUIDv7 default).
    Konfigürasyonlar: check constraint'ler (numeric>=0, enum allow-list),
    unique/index'ler, soft-delete query filter, xmin concurrency (xid shadow),
    citext Email, inet IP, FK delete davranışları (User→Holdings cascade,
    Asset→Holdings restrict, AuditLog→User SetNull).
  - **T0.6** `InitialCreate` migration üretildi ve **gerçek Postgres'te (Docker)
    `database update` ile uygulandı** → 12 tablo + 19 check constraint doğrulandı.
  - **T0.6b** `SeedData.cs` — idempotent (deterministik MD5-tabanlı GUID +
    Users.Any guard), `dotnet run -- seed` ile migrate+seed. Sayılar **birebir
    tutarlı**: TotalCost 422.970,00 / Value 641.403,00 / Profit +218.433,00 /
    Return %51,6 (SQL ile doğrulandı). İkinci çalıştırma çoğaltmadı.
- **Dokunulan dosyalar:** `backend/src/Finans.Domain/**` (Common, Enums, Portfolio,
  Identity), `backend/src/Finans.Infrastructure/**` (Persistence/FinansDbContext,
  Configurations, DesignTimeDbContextFactory, DependencyInjection, Seed/SeedData,
  Persistence/Migrations), `Finans.Api/Program.cs` (DI + `-- seed`), `appsettings.json`,
  `tests/Finans.Integration.Tests/SeedConsistencyTests.cs`.
- **Test:** `dotnet test` **4/4 yeşil** — HealthEndpoint (WebApplicationFactory) +
  SeedConsistency (EF InMemory): toplamlar, idempotency, BES devlet katkısı ayrı.
  Testler DB'siz koşar (CI-uyumlu). Canlı doğrulama: migration apply + seed totals
  gerçek Postgres'te SQL ile teyit.
- **Karar/Not (kalıcı):**
  - **xmin concurrency** `UseXminAsConcurrencyToken()` bu Npgsql sürümünde yok →
    `Property<uint>("Version").HasColumnName("xmin").HasColumnType("xid").IsConcurrencyToken()`.
  - **EF paket hizalama:** Npgsql provider Relational 10.0.4 çekiyordu, Design 10.0.8
    → açık `Microsoft.EntityFrameworkCore.Relational 10.0.8` referansıyla birleştirildi
    (MSB3277 giderildi). Test InMemory de 10.0.8.
  - **Seed yeri/şekli:** `dotnet run -- seed` (migrate+seed+çık). Eğitim (C) tabloları
    Faz 5'e ertelendi (§13.3) — T0.5 yalnızca A+B.
  - **Bağlantı dizesi:** parola repoda yok; appsettings parolasız, env/User Secrets
    ile verilir (CLAUDE.md §13). Doğrulama Docker Postgres (5433) ile yapıldı, container temizlendi.
- **Durum:** tamamlandı (T0.4/T0.5/T0.6/T0.6b)
- **Sıradaki:** T0.9 (DESIGN.md token'ları) + T0.12/T0.13/T0.14 (Serilog/güvenlik/Docker)
  → Faz 0 kapanışı.

## 2026-05-29 · Faz 0 iskelet: monorepo + .NET backend + web ayağa kalktı
- **Görev(ler):** T0.1, T0.2, T0.3, T0.7, T0.8, T0.10 (tamam); T0.11 (kısmen)
- **Ne yapıldı:**
  - **T0.1** `.gitignore` (bin/obj, node_modules, dist, .expo, sır kalıpları:
    `*.env`, `appsettings.*.local.json`, secrets).
  - **T0.2** pnpm workspaces (`pnpm-workspace.yaml` + kök `package.json`) +
    `@finans/shared` paketi: `types` (HealthResponse, CurrencyCode), `api`
    (createApiClient + ApiError), `theme` (iskelet), `format` (formatCurrency/
    formatPercent tr-TR, çekirdek). pnpm corepack ile değil, `npm i -g pnpm`
    (11.5.0) ile kuruldu (corepack Program Files'a yazamadı).
  - **T0.3** .NET çözümü `backend/Finans.slnx` + 4 katman (Api/Application/
    Domain/Infrastructure) + 2 test projesi (Application.Tests, Integration.Tests).
    Referans yönü: Api→App+Infra, App→Domain, Infra→App.
  - **T0.7** `GET /api/health` → `{status:"ok"}` (HealthController).
  - **T0.8** Web iskeleti (Vite React-TS `web/`) + React Router (createBrowserRouter)
    + TanStack Query + `@finans/shared` bağlı; AppShell + Portföy/Analiz route'ları.
  - **T0.10** HealthBadge web'de `/api/health`'ten canlı veri çekiyor; **proxy
    zinciri canlı doğrulandı** (vite 5174 → proxy → backend 5298 → `{status:ok}`).
  - **T0.11 (kısmen)** backend `Finans.Integration.Tests` (WebApplicationFactory)
    + FluentAssertions; web Vitest + RTL kuruldu. **Kalan:** Sqlite fixture
    (DB gelince), Playwright iskeleti.
- **Dokunulan dosyalar:** `.gitignore`, `package.json`, `pnpm-workspace.yaml`,
  `packages/shared/**`, `backend/**` (slnx + 6 proje), `web/**` (Vite app + testler).
- **Test:** backend `dotnet test` yeşil (HealthEndpointTests 1/1, WebApplicationFactory);
  `pnpm test` yeşil (shared format 4/4, web PortfolioPage render 1/1);
  `pnpm --filter @finans/web build` yeşil (tsc + vite). Canlı E2E: proxy→health 200 ✓.
- **Karar/Not (kalıcı):**
  - **.NET hedefi `net10.0`** — kurulu SDK 10.0.300; .NET 8 runtime yok. Dokümanlardaki
    "NET 8" yerine net10.0 (LTS). `02 §2.3` güncellendi.
  - **Çözüm formatı `.slnx`** (.NET 10 varsayılanı).
  - **FluentAssertions 7.2.0'a sabitlendi** — 8.x ticari lisansa geçti; 7.x Apache.
  - **Web yığını vite 5.4 + @vitejs/plugin-react 4.3 + vitest 3.2'ye sabitlendi**
    — Vite 8 (scaffold) vitest ile iki-vite çakışması yaratıyordu; tek sürüm vite 5.
  - `@finans/shared` build adımsız tüketiliyor (exports → `src/*.ts`); Bundler
    çözümleme, göreli import'larda uzantı yok; `erasableSyntaxOnly` uyumlu (parametre-
    özelliği yok).
- **Durum:** tamamlandı (T0.1/T0.2/T0.3/T0.7/T0.8/T0.10); T0.11 kısmen.
- **Sıradaki:** T0.4-T0.6 (EF Core + entity'ler + migration + seeder) — backend
  veri katmanı. Paralelde T0.9 (DESIGN.md token'ları) + T0.12/T0.13/T0.14 kapıları.

## 2026-05-29 · Veri modeli derinleştirildi + eğitim modeli + tutarlı seed
- **Görev(ler):** ad-hoc (veri modeli)
- **Ne yapıldı:** `03-DATA-MODEL` baştan yazıldı, kolon-düzeyinde derinlik:
  konvansiyonlar (UUIDv7, numeric(18,6), UTC, concurrency, soft-delete), enum
  allow-list'leri, **kimlik/güvenlik/audit** tabloları (Users genişletildi,
  Roles, RefreshTokens, AuditLogs), Asset'te Unit↔PricingCurrency ayrımı,
  InflationRates. **Eğitim modülü** tam modellendi (Tracks, Lessons, Sections,
  Prerequisites, ConceptTags, Quizzes/Questions/Options, UserLessonProgress,
  UserQuizAttempts). **Kapsamlı seed** taslağa birebir tutarlı (641.403/422.970/
  +218.433/+%51,6 — node ile doğrulandı) + eğitim içeriği seed'i.
- **Dokunulan dosyalar:** `docs/03-DATA-MODEL.md` (yeniden yazıldı),
  `docs/04-API-CONTRACT.md` (§7.5 eğitim uçları), `docs/08-BACKLOG.md`
  (T0.5 kimlik/audit + T0.6b kapsamlı seeder + Faz 5 T5E.1-4 eğitim)
- **Test:** seed sayıları `node` ile yeniden hesaplanıp taslak başlığıyla
  birebir doğrulandı; bu set aynı zamanda integration fixture'ı (`09` SC-01..06).
- **Karar/Not:** BES getiri tabanı = own+state katkı (taslaktaki +%88); devlet
  katkısı UI'da ayrı. Eğitim içeriği DB'de (Markdown gövde). Ders "Locked"
  durumu ön-koşuldan türetilir, saklanmaz. Seeder idempotent, ayrı `SeedData.cs`.
- **Durum:** tamamlandı
- **Sıradaki:** T0.1 — `git init` + `.gitignore`

## 2026-05-29 · Web frontend (ReactJS+Vite) + web-öncelikli yeniden plan
- **Görev(ler):** ad-hoc (mimari/plan yeniden düzenleme)
- **Ne yapıldı:** Web yüzeyi eklendi ve **birincil** yapıldı. Yeni
  `13-WEB-FRONTEND` (monorepo, Vite React, paylaşılan paket, web düzen
  uyarlaması, web'e özel güvenlik/perf/izleme). Tüm plan web-öncelikli olacak
  şekilde yeniden düzenlendi; mobil ayrı "FAZ M" koluna alındı.
- **Dokunulan dosyalar:** `docs/13-` (yeni); `CLAUDE.md` (§3 mimari diyagram +
  monorepo, §7 yapı); `ROADMAP.md` (web-öncelikli not); `docs/01-` (NFR-13),
  `02-` (§3 frontend/monorepo), `05-` (sıra notu), `06-` (monorepo+web kurulum),
  `08-` (Faz 0/1 web'e çevrildi + FAZ M eklendi), `09-` (§3W web test + SC-W1..3),
  `10/11/12-` (web pointer), `docs/README.md` (indeks + sıra notu)
- **Test:** yok (mimari/doküman); web test araçları (Vitest+RTL+Playwright)
  `09` §3W'ye, senaryolar SC-W1..W3'e eklendi.
- **Karar/Not:** Web = **ayrı React+Vite SPA**, monorepo + `@finans/shared`
  (tip/token/format paylaşımı; kod değil sözleşme paylaşımı). **Web öncelikli**;
  mobil sonra (FAZ M). Tek API ikisine hizmet eder; CORS web origin allow-list.
- **Durum:** tamamlandı
- **Sıradaki:** T0.1 — `git init` + `.gitignore`

## 2026-05-29 · Performans, güvenlik & gözlemlenebilirlik mimarisi
- **Görev(ler):** ad-hoc (mimari altyapı)
- **Ne yapıldı:** Çok kullanıcı + hız + maliyet + güvenlik + izleme için üç
  kalıcı doküman: `10-PERFORMANCE-SCALABILITY` (bütçeler, cache katmanları,
  stateless ölçeklenme, maliyet), `11-SECURITY` (STRIDE tehdit modeli, per-user
  izolasyon/IDOR, JWT/Argon2, sırlar, KVKK, güvenlik testleri, kontrol listesi),
  `12-OBSERVABILITY` (Serilog+Seq, OTel+Prometheus+Grafana, health check, audit
  log, alarm). Tümü iş akışına bağlandı.
- **Dokunulan dosyalar:** `docs/10-`, `docs/11-`, `docs/12-` (yeni),
  `CLAUDE.md` (§11 kapı + yeni §13), `docs/01-` (NFR-10/11/12), `docs/02-`
  (§5-6 dağıtım/güvenlik), `docs/06-` (DoD + yapma listesi), `docs/08-`
  (T0.11-13, T1.15, T2.7-9, T3.9, Faz5, çapraz-kesen), `docs/09-` (SC-13..16,
  SC-P1), `docs/README.md` (indeks)
- **Test:** yok (mimari/doküman) — güvenlik/perf testleri `09` §5'e senaryo
  olarak eklendi (SC-13 IDOR zorunlu); kod gelince yazılacak.
- **Karar/Not:** Barındırma = **self-hosted/VPS + Docker** (açık kaynak).
  İzleme = **Serilog+Seq / OTel+Prometheus+Grafana** (maliyetsiz, self-host).
  En kritik kural: **per-user veri izolasyonu** (IDOR/BOLA), kimlik açılmadan
  testi yeşil olmalı.
- **Durum:** tamamlandı
- **Sıradaki:** T0.1 — `git init` + `.gitignore`

## 2026-05-29 · Test disiplini kuruldu (senaryo-önce / yeşil-kapı)
- **Görev(ler):** ad-hoc (süreç altyapısı)
- **Ne yapıldı:** Her geliştirmeyle birlikte birim + olaylara yönelik (senaryo)
  testlerin yazılıp yeşile getirilmesini zorunlu kılan disiplin kuruldu.
  `09-TESTING-STRATEGY.md` (piramit, backend xUnit/integration, mobil Jest+RTL,
  Given-When-Then senaryo formatı, 14 maddelik yaşayan senaryo kataloğu, görev
  başına akış, kapsam) yazıldı. İş akışına entegre edildi.
- **Dokunulan dosyalar:** `.claude/docs/09-TESTING-STRATEGY.md` (yeni),
  `CLAUDE.md` (§11 yeşil-kapı adımı + yeni §12), `06-DEV-PLAYBOOK.md` (§4-5),
  `08-BACKLOG.md` (T0.10 test altyapısı + çapraz-kesen kural),
  `.claude/tasks/TASKLOG.md` (şablona **Test** alanı), `docs/README.md` (indeks)
- **Test:** yok (süreç/doküman; çalışacak kod henüz yok) — kural bundan sonraki
  her kod görevinde geçerli.
- **Karar/Not:** Test disiplini = senaryo-önce, test-yanında, yeşil olmadan
  tamam yok. Mobil E2E (Maestro) Faz 2+'a ertelendi; Faz 1'de mobil Jest+RTL,
  olay testlerinin ağırlığı backend integration'da.
- **Durum:** tamamlandı
- **Sıradaki:** T0.1 — `git init` + `.gitignore`

## 2026-05-29 · Görev takip sistemi kuruldu
- **Görev(ler):** ad-hoc (süreç altyapısı)
- **Ne yapıldı:** `.claude/tasks/` altında otomatik görev takibi kuruldu —
  TASKLOG (bu dosya), ACTIVE.md, README.md (protokol), SessionStart hook.
  Otomasyon iki katmanlı: CLAUDE.md §11 protokolü + `.claude/settings.json`
  oturum-başı hook'u (her oturumda görev durumu otomatik bağlama yüklenir).
- **Dokunulan dosyalar:** `.claude/tasks/README.md`,
  `.claude/tasks/TASKLOG.md`, `.claude/tasks/ACTIVE.md`,
  `.claude/tasks/session-start.mjs`, `.claude/settings.json`, `CLAUDE.md` (§11)
- **Karar/Not:** Worklog backlog'a referans verir; `08-BACKLOG.md` görev
  durumlarının kaynağıdır. Görev içeriği yargı gerektirdiği için güncellemeyi
  Claude yapar; hook yalnızca durumu görünür kılar.
- **Durum:** tamamlandı
- **Sıradaki:** T0.1 — `git init` + `.gitignore`

## 2026-05-29 · Mimari doküman seti + taslak sürücüsü
- **Görev(ler):** ad-hoc (proje tasarımı / dokümantasyon)
- **Ne yapıldı:** Greenfield proje için kıdemli-mimar doküman seti üretildi
  (`.claude/docs/` 01–08 + README). HTML taslağı gerçekten render edilip her
  ekranı incelendi; ondan türetilmiş mobil şartname yazıldı. Taslağı süren
  `run-finans-prototype` skill'i + `driver.mjs` (playwright-core, sistem Chrome)
  kuruldu ve doğrulandı (6 ekran görüntüsü).
- **Dokunulan dosyalar:** `.claude/docs/*.md` (01–08, README),
  `.claude/skills/run-finans-prototype/{SKILL.md,driver.mjs,.gitignore,package.json}`
- **Karar/Not:** Veri modelinde açık kararlar çözüldü (ort. maliyet
  Transactions'tan türetilir; PostgreSQL). Taslaktaki sayılar elle yerleştirilmiş,
  veri kaynağı değil — gerçek uygulamada her rakam .NET'te deterministik.
- **Durum:** tamamlandı
- **Sıradaki:** Görev takip sistemini kur (↑ bir üstteki girdi)

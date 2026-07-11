# CLAUDE.md — Finans / Portföy Uygulaması

> Bu dosya Claude Code tarafından otomatik okunur. Projenin vizyonunu, mimari
> kararlarını ve kurallarını içerir. Her geliştirme oturumunda buradaki ilkelere
> uy. Tasarım detayları için `DESIGN.md` dosyasına bak.

---

## 1. Proje Özeti

Bireysel yatırımcının **birden fazla varlık sınıfını tek yerde takip edip
anlamasını** sağlayan, mobil öncelikli bir portföy + eğitim uygulaması.

**Vizyon:** Kullanıcı portföyünü görür, ne anlama geldiğini öğrenir ve
"bunu nasıl daha iyi yönetirim?" sorusuna eğitici, çerçeve sunan yorumlar alır.

**İlk hedef kullanıcı:** Yatırıma yeni başlamış, daha önce denemiş ama yol
tutturamamış, öğrenmek isteyen kişi. Sıfır bilgi varsayımıyla sade dil kullan.

**İş hedefi:** Önce kişisel kullanım, ardından gelir getiren bir ürüne dönüşüm
(abonelik modeli muhtemel — ileride).

---

## 2. EN ÖNEMLİ KURAL — Yatırım tavsiyesi DEĞİL

Bu uygulama **yatırım tavsiyesi vermez.** Türkiye'de yatırım danışmanlığı SPK
lisansına tabidir. Bu yüzden:

- "Şu hisseyi al / sat", "altından çık dövize gir", "bu yükselir" gibi
  **kişiye özel, somut alım-satım yönlendirmesi YAPILMAZ.**
- Bunun yerine: **çerçeve, açıklama, senaryo ve farkındalık** sunulur.
  - "Bu metrik şu durumda yüksek/riskli sayılır" ✅
  - "Portföyünün %84'ü iki varlıkta — bu yoğunlaşma şu riski taşır" ✅
  - "Bu hisse yükselecek / al" ❌
- Her analiz/yorum ekranında **"yatırım tavsiyesi değildir"** çerçevesi görünür.
- Geleceği tahmin etme; **mevcut durumu açıkla ve geçmişi göster.**

> Bu kural ürünü zayıflatmaz; daha eğitici ve yasal olarak savunulabilir yapar.
> Lansman öncesi bir SPK/fintech avukatıyla mutlaka doğrulanmalı.

---

## 3. Teknik Mimari

```
┌─────────────────┐   ┌─────────────────┐
│  Web (ReactJS   │   │  React Native   │     ← iki ön yüz, TEK API
│  + Vite) ★1.    │   │  (mobil) sonra  │       paylaşılan paket: @finans/shared
└────────┬────────┘   └────────┬────────┘
         │  HTTPS / REST / JSON │
         └──────────┬───────────┘
                    ▼
            ┌──────────────────┐
            │   .NET Web API    │
            │   (backend)       │
            └────────┬──────────┘
                     │
        ┌────────────┼─────────────────┐
        ▼            ▼                 ▼
  ┌──────────┐ ┌──────────────┐ ┌────────────┐
  │ Veritabanı│ │ Fiyat API'leri│ │  LLM API   │
  │(PostgreSQL)│ │(altın/döviz/ │ │(yorum/açıkl)│
  │           │ │  hisse)       │ │            │
  └──────────┘ └──────────────┘ └────────────┘
```

- **Backend:** .NET Web API (geliştiricinin güçlü olduğu alan — buradan başla).
  **Tek API** hem web hem mobile hizmet eder.
- **Web (BİRİNCİL):** ReactJS + Vite SPA. Projenin **öncelikli yüzeyi.** Detay:
  `docs/13-WEB-FRONTEND.md`.
- **Mobil (SONRA):** React Native (Expo). Web parası oturduktan sonra eklenir.
  ⚠️ Geliştirici için ilk kez. Detay: `docs/05-MOBILE-SPEC.md`.
- **Monorepo:** `web/`, `mobile/`, `backend/`, `packages/shared/` (ortak TS:
  API tipleri, tasarım token'ları, format util'leri). pnpm workspaces.
- **Veritabanı:** PostgreSQL (karar). Hassas finansal veri → KVKK uyumu ve
  güvenlik baştan tasarıma girer (`docs/11-SECURITY.md`).

### 3.1 LLM Mimarisi — KRİTİK KARAR

**Sayısal hesap KODDA, yorum LLM'de.**

- Tüm sayısal hesaplar (getiri, dağılım, reel getiri, oranlar) **.NET tarafında,
  deterministik formüllerle** yapılır. (Formüller § 6'da.)
- LLM'e **asla ham sayı verip "hesapla" denmez** — halüsinasyon riski yüksek ve
  yanlış rakam veren bir finans uygulaması en büyük güven kırıcıdır.
- LLM'in görevi: .NET'in **hesaplayıp hazır verdiği** sayıları alıp
  **eğitici, sade dille yorumlamak**, dikkat çekilecek noktaları ve senaryoları
  açıklamak.
- LLM çıktısı **yapılandırılmış (structured/JSON)** istenmeli ki mobil tarafta
  güvenle parse edilip kartlara yerleşsin.

### 3.2 Çoklu Para Birimi

- Kullanıcı bir **baz para birimi** seçer (TRY / USD / EUR).
- Her varlık kendi para biriminde tutulur; gösterimde **güncel kurdan baz
  para birimine** çevrilip toplanır.
- Kur dönüşümü de koddaki deterministik hesaba dahildir, LLM'e bırakılmaz.

### 3.3 Veri Kaynakları

- **Altın / Döviz:** Ücretsiz katmanlı API'ler test için yeterli.
- **ABD hisseleri:** Ücretsiz/uygun API'ler mevcut (örn. metrik verisi).
- **BIST hisseleri:** Güvenilir veri çoğunlukla **ücretli ve zor** → ileri faza
  bırakıldı. Hedef borsalar: **BIST + ABD** (karar verildi).

---

## 4. Faz Planı / Yol Haritası

> Tüm vizyon 1-2 ayda bitmez. Faz faz ilerle; her fazın sonunda çalışan bir
> ürün olsun (motivasyon için kritik).

- **Faz 0 — Hazırlık (~1 hafta):** React Native temelleri (ekran geçişi +
  API'den veri çekme denemesi). Paralelde .NET API iskeleti + veritabanı şeması.
- **Faz 1 — Portföy Takip MVP (~2-4 hafta):** Elle varlık girişi; .NET tüm
  hesapları yapar (maliyet, değer, getiri, dağılım, reel getiri); mobilde özet
  ekranı + dağılım grafiği. BES'in devlet katkısını **ayrı satır** olarak tut.
  *Tek başına kullanılabilir ürün.*
- **Faz 2 — Canlı fiyat + küçük bilgilendirmeler (~2-3 hafta):** Altın/döviz
  fiyat API'si; portföy ekranına bağlama duyarlı eğitici notlar (başta basit
  kurallarla tetiklenebilir).
- **Faz 3 — LLM yorum katmanı (~2-3 hafta):** .NET'in hesapladığı sayıları LLM'e
  gönderip portföyü eğitici dille yorumlatma. Prompt tasarımı + "tavsiye değil"
  çerçevesi burada oturur.
- **Faz 4 — Hisse temel analiz modülü (süre veri kaynağına bağlı):** Metrikleri
  çek (F/K, PD/DD, temettü verimi, kâr büyümesi), LLM **ne anlama geldiğini**
  açıklasın. Tahmin/öneri YOK.
- **Faz 5-8 — Strateji dalgaları (2026-07-11; detay: `.claude/docs/14-PRODUCT-STRATEGY.md`):**
  - **Faz 5:** Fiyat geçmişi → Değer Seyri grafiği + Senaryo v1 (geçmişe dönük).
  - **Faz 6:** Eğitim MVP ("portföyünle öğren" dersleri) + kavram sözlüğü — vizyonun kalbi.
  - **Faz 7:** Okuryazarlık onboarding'i, kimlik/çok kullanıcı, PWA, bildirim,
    TEFAS, altın kültürü modülü, demo mod (kapalı beta).
  - **Faz 8:** Davranış aynası, enflasyon paneli, mobil, gelir modeli +
    **SPK/KVKK hukuki onay (lansman kapısı)**.

---

## 5. Veri Modeli (taslak — Faz 0'da kesinleşecek)

İlk tahmini tablolar (henüz tasarlanmadı, sıradaki adım):

- `Users` — kullanıcı, baz para birimi tercihi
- `Assets` — varlık türü, ad, sembol/birim, para birimi
- `Holdings` — kullanıcının bir varlıktaki pozisyonu (miktar, ort. maliyet)
- `Transactions` — alış/satış işlemleri (ort. maliyet bunlardan hesaplanır)
- `PriceSnapshots` — fiyat geçmişi (reel getiri ve senaryo için)
- BES için özel alanlar: kendi katkı payı, devlet katkısı (ayrı), vesting durumu

> Not: Ortalama maliyet ya `Holdings`'te tutulur ya da `Transactions`'tan
> hesaplanır. Faz 0'da bu karar netleştirilecek.

---

## 6. Hesaplama Formülleri (deterministik — KODDA)

```
Kalem getirisi (oran)   = (güncel değer − toplam maliyet) / toplam maliyet
Net kâr                 = güncel değer − toplam maliyet
Ağırlıklı ort. maliyet  = Σ(miktarᵢ × fiyatᵢ) / Σ(miktarᵢ)
Varlık ağırlığı (%)     = varlık güncel değeri / portföy toplam değeri
Reel getiri             = (1 + nominal getiri) / (1 + enflasyon) − 1
Para birimi dönüşümü    = tutar × güncel_kur(varlık_pb → baz_pb)

Hisse metrikleri:
  F/K                   = hisse fiyatı / hisse başı kâr (EPS)
  PD/DD                 = piyasa değeri / defter değeri
  Temettü verimi (%)    = hisse başı temettü / hisse fiyatı
```

> Bu formüllerin tamamı .NET tarafında. LLM yalnızca **sonuçları yorumlar.**

---

## 7. Önerilen Proje Yapısı

```
finans/                       ← monorepo (pnpm workspaces)
├── CLAUDE.md                 ← bu dosya (proje kökü)
├── DESIGN.md                 ← tasarım rehberi
├── backend/                  ← .NET Web API (src/ + tests/)
├── packages/
│   └── shared/               ← @finans/shared (API tipleri, token, format)
├── web/                      ← ★ BİRİNCİL: ReactJS + Vite (docs/13)
├── mobile/                   ← SONRA: React Native / Expo (docs/05)
└── .claude/docs/             ← mühendislik dokümanları (01–13)
```
> Ayrıntılı mimari ve karar dokümanları `.claude/docs/` altında (indeks:
> `.claude/docs/README.md`).

---

## 8. Geliştirme Konvansiyonları

- **Dil:** Kod ve değişken adları İngilizce; kullanıcıya görünen tüm metin
  Türkçe. LLM yorumları Türkçe üretilir.
- **Para/sayı formatı:** Türkçe gösterim — binlik ayraç nokta, ondalık virgül
  (örn. `422.970,50 ₺`). Hesaplama her zaman tam hassasiyetle; yuvarlama sadece
  gösterimde.
- **Para birimleri:** Asla `float`/`double` ile parasal hesap yapma →
  `decimal` kullan.
- **Güvenlik:** API anahtarları koda gömülmez (kullanıcı ortam değişkeni /
  secret yönetimi). Kullanıcı verisi KVKK uyumlu saklanır.
- **LLM çıktısı:** Daima yapılandırılmış (JSON) iste ve güvenli parse et;
  parse hatasında uygulama çökmemeli (fallback metin).
- **Test:** Hesaplama fonksiyonları için birim testi şart (yanlış rakam = kabul
  edilemez).

---

## 9. Sıradaki Adım

**Faz 4'ü bitir (T4.2-T4.4: hisse metrikleri + LLM açıklama), ardından Dalga 1:**
Faz 5 (fiyat geçmişi → Değer Seyri + Senaryo v1) → Faz 6 (Eğitim MVP + sözlük).
Görev kırılımı: `.claude/docs/08-BACKLOG.md` · strateji: `.claude/docs/14-PRODUCT-STRATEGY.md`.

---

## 10. Açık Kararlar / Notlar

- LLM sağlayıcı seçimi netleşmedi. Türkçe kalitesi, talimat takibi ve
  yapılandırılmış çıktı önemli kriterler; maliyet karşılaştırması yapılmalı.
  (Geliştirme aşamasında LLM kullanımı düşük → aylık birkaç dolar seviyesi.)
- BIST veri kaynağı maliyeti Faz 4'te değerlendirilecek.
- Hukuki (SPK + KVKK) doğrulama lansman öncesi şart.

---

## 11. Görev Takibi Protokolü (OTOMATİK — her oturum)

> Bu projede yapılan her anlamlı iş otomatik izlenir. Bu protokole **kullanıcı
> hatırlatmadan** uy. Detay ve dosyalar: `.claude/tasks/README.md`.

**Oturum başında:** SessionStart hook'u (`.claude/settings.json`) aktif görevleri
ve son worklog girdisini bağlama getirir. Bunu oku, "nerede kaldık"ı oradan al.

**Anlamlı bir iş yaptıktan sonra** (kod değişikliği, yeni doküman, kalıcı karar,
şema/endpoint ekleme, düzeltme, bağımlılık ekleme), **yanıtı bitirmeden önce**:

1. **Testler yeşil mi?** Kod/davranış değiştiyse ilgili testler yazılmış ve
   geçiyor olmalı (yeşil-kapı, bkz. §12). Değilse görev "tamam" değildir.
   Ayrıca **güvenlik & gözlemlenebilirlik kapısı** (§13) karşılanmalı.
2. **`.claude/tasks/TASKLOG.md`** → en üste bir girdi ekle (dosyadaki şablonla:
   tarih, görev ID, ne yapıldı, dokunulan dosyalar, **test**, karar/not, durum,
   sıradaki).
3. **`.claude/docs/08-BACKLOG.md`** → ilgili görevin durumunu güncelle
   (`[ ]→[~]→[x]`); varsa `09` §5 senaryo durumunu da güncelle.
4. **`.claude/tasks/ACTIVE.md`** → sıradaki göreve göre tazele (kısa tut).
5. Kalıcı bir **karar** verildiyse onu ilgili kalıcı dokümana da işle
   (örn. veri modeli → `docs/03-DATA-MODEL.md`).

**Kayıt GEREKMEZ:** salt soru-cevap, keşif/okuma, hiçbir şeyi değiştirmeyen turlar.

> Bugünün tarihini sistemden al; göreceli tarih ("yarın") yazma, mutlak tarih yaz.

---

## 12. Test Disiplini (OTOMATİK — her geliştirme)

> Çekirdek kural: *Senaryo-önce, test-yanında, yeşil olmadan "tamam" yok.*
> Detay ve senaryo kataloğu: `.claude/docs/09-TESTING-STRATEGY.md`.

Her geliştirme görevinde, **kullanıcı istemese bile**:

1. **Senaryo:** İlgili FR/NFR'den senaryo(ları) `09` §5 kataloğuna ekle
   (Given-When-Then) — koddan önce.
2. **Test yaz:** Hem **birim** (özellikle parasal hesap — **zorunlu**, NFR-1)
   hem **olaylara yönelik** (dış API hatası, kullanıcı akışı, fallback) testler.
3. **Yeşile getir:** `dotnet test` ve/veya `npm test` → hepsi yeşil.
4. Görev **ancak testleri yeşilken** kapanır (§11 ile birlikte işle).

İki kategori her zaman düşünülür: **unit** (izole hesap/mantık) +
**senaryo/olay** (uçtan uca davranış). Mobil E2E aracı Faz 2+'a ertelendi;
o zamana dek olay testlerinin ağırlığı backend integration testlerindedir.

---

## 13. Güvenlik, Performans & Gözlemlenebilirlik Kapıları (OTOMATİK)

> Finansal veri uygulaması — bunlar sonradan eklenecek "ekstra" değil, baştan
> mimari kuraldır. Detay: `docs/11-SECURITY.md`, `10-PERFORMANCE-SCALABILITY.md`,
> `12-OBSERVABILITY.md`. Barındırma: **self-hosted/VPS + Docker**, izleme yığını
> **Serilog+Seq / OpenTelemetry+Prometheus+Grafana** (açık kaynak).

Her geliştirmede, **kullanıcı istemese bile** şunlar sağlanır:

- 🔒 **Per-user veri izolasyonu (en kritik):** Her veri erişimi `UserId` ile
  kapsanır; bir kullanıcı asla başkasının verisini göremez (başkasının kaydı →
  404). Yeni endpoint'te bu kontrol **zorunlu** + IDOR testi (`09` SC-13).
- 🔒 **Sır repoda yok:** anahtar/parola env/secret'ta (`11` §6).
- 🔒 **Girdi sunucuda doğrulanır, hata iç detay sızdırmaz, DTO kullanılır** (`11` §4).
- 🔒 **Log'a sır/PII/token yazılmaz** (redaksiyon, `12` §3).
- ⚡ **Dış çağrı (DB/fiyat/LLM) cache'lenir, async, projeksiyonlu**; cache anahtarı
  `UserId` içerir (`10` §3-4). Performans bütçeleri `10` §2.
- 👁 **Yapılandırılmış log + health check ilk günden**; yeni dış bağımlılıkta
  metrik + fallback (`12`, `10`).

> Hızlı kontrol: `11` §10 (güvenlik kontrol listesi) ve `10` §9 / `12` §10
> (yapma listeleri). Çok-kullanıcı (kimlik) açılmadan IDOR + AuthZ + rate-limit
> testleri yeşil olmalı.

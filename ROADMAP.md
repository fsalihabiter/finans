# ROADMAP.md — Detaylı Faz Planı

> Bu dosya `CLAUDE.md` § 4'teki özet yol haritasının ayrıntılı halidir. Her faz
> için: amaç, görevler (backend / mobil / diğer), kullanılacak araçlar,
> teslimat ve **tamamlanma kriteri** (Definition of Done) yer alır.
> İlke: **her fazın sonunda çalışan bir şey olmalı.**

> 🔄 **GÜNCELLEME — Frontend sırası: WEB ÖNCELİKLİ.** Proje artık monorepo'dur
> (`web/`, `mobile/`, `backend/`, `packages/shared/`). Birincil yüzey **ReactJS
> + Vite web uygulamasıdır** (`.claude/docs/13-WEB-FRONTEND.md`). Aşağıdaki her
> fazdaki **"Mobil görevleri"** bölümlerini artık **"Frontend görevleri — önce
> WEB"** olarak oku; aynı ekran/akış önce web'de yapılır. **Mobil (React Native)**
> aynı API ve `@finans/shared` paketi üzerine **ayrı bir kol** olarak, web
> parası oturduktan sonra (bkz. "FAZ M") eklenir. Güncel uygulanabilir görev
> listesi: `.claude/docs/08-BACKLOG.md`.

---

## Genel Bakış

| Faz | Ad | Tahmini Süre | Sonuç |
|-----|----|--------------|-------|
| 0 | Hazırlık & İskelet | ~1 hafta | Boş ama çalışan iki proje + şema |
| 1 | Portföy Takip MVP | ~2-4 hafta | Elle veri ile çalışan portföy (tek başına ürün) |
| 2 | Canlı Fiyat & Bilgilendirme | ~2-3 hafta | Otomatik güncel değer + eğitici notlar |
| 3 | LLM Yorum Katmanı | ~2-3 hafta | Portföyü eğitici dille yorumlayan analiz |
| 4 | Hisse Temel Analiz | Veri kaynağına bağlı | Metrik çek + LLM açıklasın |
| 5 | Ötesi & Ürünleşme | — | Yeni varlıklar, simülasyon, gelir modeli |

**Bağımlılık zinciri:** 0 → 1 → 2 → 3 → 4 → 5. Faz 3, Faz 1'in hesap çıktısına
muhtaç (LLM o sayıları yorumlar). Faz 4 bağımsız ilerleyebilir ama veri kaynağı
kararı beklemeli.

> ⚠️ **Tavsiye değil kuralı:** Faz 3 ve 4'e geçmeden `CLAUDE.md` § 2'yi tekrar
> oku. Bu fazların çıktısı asla "al/sat/yükselir" dememeli.

---

## FAZ 0 — Hazırlık & İskelet (~1 hafta)

**Amaç:** İki projeyi de ayağa kaldırmak, veritabanı şemasını kesinleştirmek ve
React Native'e ısınmak. Kod yazmaktan çok "boru hattını" kurmak.

### Backend görevleri
- .NET Web API projesi oluştur (boş iskelet).
- Katman yapısını kur: `API` (controller) / `Application` (servis-iş mantığı) /
  `Domain` (entity) / `Infrastructure` (veritabanı erişimi).
- ORM seç ve kur (Entity Framework Core önerilir).
- **Veritabanı şemasını tasarla ve migration oluştur** (aşağıdaki taslak).
- Tek bir test endpoint'i: `GET /api/health` → `{ "status": "ok" }`.

### Mobil görevleri (öğrenme ağırlıklı)
- React Native ortamını kur (Expo ile başlamak yeni başlayan için en kolayı).
- Mini deneme uygulaması yap:
  1. İki-üç ekran arası geçiş (navigation).
  2. `.NET`'teki `/api/health` endpoint'inden veri çekip ekranda gösterme.
  3. Bir liste render etme (FlatList).
- `DESIGN.md` § 6'daki tema token dosyasını (`theme/colors.ts`) oluştur.

### Diğer
- Git deposu + `.gitignore` (build çıktıları, secret'lar hariç).
- API anahtarları için secret yönetimi planı (henüz anahtar yok ama yeri hazır).

### Veritabanı şeması taslağı (kesinleştir)
```
Users(Id, Email, BaseCurrency, CreatedAt)
Assets(Id, Type, Name, Symbol, Currency)        -- Altın, USD, Hisse...
Holdings(Id, UserId, AssetId, Quantity, AvgCost) -- veya Transactions'tan türet
Transactions(Id, HoldingId, Type, Quantity, UnitPrice, Date) -- Alış/Satış
PriceSnapshots(Id, AssetId, Price, Date)         -- reel getiri & senaryo için
BesDetails(Id, HoldingId, OwnContribution, StateContribution, JoinedAt, VestingState)
```
> **Karar verilecek:** Ortalama maliyet `Holdings.AvgCost`'ta mı tutulacak,
> yoksa `Transactions`'tan mı hesaplanacak? (Transactions'tan türetmek daha
> doğru ama biraz daha iş.) Para alanları **`decimal`**.

### Teslimat
Çalışan (boş) .NET API + mobil deneme uygulaması + onaylı şema/migration.

### ✅ Tamamlanma Kriteri
- Mobil uygulama `/api/health`'ten veriyi çekip ekranda gösteriyor.
- `dotnet ef migrations` ile veritabanı oluşuyor.
- Tema token dosyası hazır, bir ekranda kullanılıyor.

---

## FAZ 1 — Portföy Takip MVP (~2-4 hafta)

**Amaç:** Kullanıcının elle girdiği varlıklarla, **tüm sayısal hesapların doğru
yapıldığı** çalışan bir portföy. Bu faz tek başına kullanılabilir bir üründür.

### Backend görevleri
- **Hesaplama servisi** (`PortfolioCalculationService`) — `CLAUDE.md` § 6'daki
  formüller: kalem getirisi, net kâr, ağırlıklı ort. maliyet, dağılım %,
  reel getiri, çoklu para birimi → baz para birimine çevirme.
- CRUD endpoint'leri:
  ```
  POST   /api/holdings           varlık ekle
  GET    /api/holdings           kullanıcının varlıkları
  PUT    /api/holdings/{id}      güncelle
  DELETE /api/holdings/{id}      sil
  GET    /api/portfolio/summary  toplam değer, maliyet, kâr, getiri, dağılım
  ```
- BES'i özel ele al: kendi katkısı ve devlet katkısı **ayrı satır/alan**.
- **Birim testleri** — hesaplama fonksiyonları için (yanlış rakam kabul edilemez).
  Senin tablondaki sayıları (422.970 maliyet, 641.403 değer) test verisi yap.

### Mobil görevleri
- Portföy özet ekranı: hero kart (değer/kâr/getiri) — taslaktaki gibi.
- Dağılım grafiği: `react-native-svg` veya `react-native-gifted-charts`
  (DESIGN.md § 6 — `conic-gradient` RN'de yok).
- Varlık listesi (FlatList) + dokununca **varlık detay** ekranı.
- **Varlık ekle** formu (tür, para birimi, miktar, maliyet, tarih) → POST.
- Baz para birimi seçimi (ayarlar veya ilk açılış).

### Diğer
- Bu fazda **fiyatlar elle girilir** (canlı fiyat Faz 2'de). Yani "güncel fiyat"
  alanını kullanıcı kendi günceller.

### Teslimat
Elle veri girilen, doğru hesaplayan, mobilde gösteren tam akışlı portföy.

### ✅ Tamamlanma Kriteri
- Kullanıcı varlık ekleyip/silip portföyünü görebiliyor.
- Toplam değer, kâr, getiri %, dağılım % **doğru hesaplanıyor** (testlerle
  kanıtlı).
- Çoklu para birimi baz para birimine doğru çevriliyor.
- BES'in devlet katkısı ayrı görünüyor.

---

## FAZ 2 — Canlı Fiyat & Bilgilendirme (~2-3 hafta)

**Amaç:** "Güncel fiyat"ı elle girmek yerine otomatik çekmek + portföy ekranına
bağlama duyarlı küçük eğitici notlar koymak.

### Backend görevleri
- Fiyat sağlayıcı entegrasyonu (altın, döviz için ücretsiz katmanlı API).
- `PriceFetchService` — dış API'den fiyat çek, `PriceSnapshots`'a yaz.
- Önbellekleme (cache) — her istekte dış API'yi yormamak için (örn. 5-15 dk).
- Endpoint: `GET /api/prices?symbols=XAU,USD` veya özet içine gömülü.
- **Bilgilendirme (nudge) motoru — basit kurallarla:** Örneğin
  `nakit oranı > %X` → "nakit enflasyonda erir" notu. Bu faz için **kural
  tabanlı yeterli**; LLM Faz 3'te devreye girer.

### Mobil görevleri
- Özet ekranındaki değerler artık canlı fiyattan geliyor (pull-to-refresh).
- Taslaktaki "nudge" kartını bağlama göre göster (gelen kurala göre).
- Son güncelleme zamanı / fiyat kaynağı bilgisi.

### Riskler
- Ücretsiz API'lerin istek limiti olabilir → cache şart.
- Fiyat verisi gecikmeli/eksik gelebilir → "yaklaşık" etiketi göster.

### ✅ Tamamlanma Kriteri
- Güncel değer dış kaynaktan otomatik geliyor, yenilenebiliyor.
- En az bir bağlama duyarlı eğitici not doğru tetikleniyor.
- Dış API çökerse uygulama çökmüyor (fallback: son bilinen fiyat).

---

## FAZ 3 — LLM Yorum Katmanı (~2-3 hafta)

**Amaç:** .NET'in hesapladığı sayıları LLM'e verip portföyü **eğitici dille
yorumlatmak.** Projenin "anlamlandırma" katmanı.

> ⚠️ Hesap KODDA, yorum LLM'de. LLM'e ham sayı verip "hesapla" denmez.

### Backend görevleri
- LLM sağlayıcı seç (Türkçe kalitesi + yapılandırılmış çıktı + maliyet kriteri).
- `LlmCommentaryService`:
  - Girdi: .NET'in hesapladığı **hazır sayılar** (dağılım, getiri, reel getiri,
    yoğunlaşma oranları).
  - Sistem promptu: "Sen bir finans **eğitmenisin**, danışman değil. Tavsiye
    verme, açıkla ve farkındalık yarat. Çıktıyı şu JSON şemasında ver: ..."
  - Çıktı: **yapılandırılmış JSON** (kart kart: başlık, açıklama, etiketler).
- Güvenli parse + fallback (JSON bozuksa düz metin/önceki yorum).
- Endpoint: `GET /api/portfolio/commentary`.
- Maliyet kontrolü: yorum sık sık değil, portföy değiştiğinde veya günde bir kez
  üretilip cache'lensin.

### Mobil görevleri
- Analiz sekmesi (taslaktaki gibi): genel sağlık, yoğunlaşma, reel getiri,
  senaryo kartları — artık LLM'den geliyor.
- Her ekranda **"yatırım tavsiyesi değildir"** çerçevesi (disc).
- Yükleniyor durumu (LLM yanıtı birkaç saniye sürebilir).

### Püf noktası
- Prompt mühendisliği işin kalbi. "Tavsiye değil, açıklama" sınırını promptta
  net çiz ve birkaç örnek (few-shot) ver.
- LLM'in uydurmaması için **sadece kendisine verilen sayıları** kullanmasını,
  yeni rakam üretmemesini iste.

### ✅ Tamamlanma Kriteri
- Analiz kartları gerçek portföy verisiyle LLM'den üretiliyor.
- Çıktı asla "al/sat/yükselir" demiyor; açıklayıcı/eğitici kalıyor.
- LLM hatası/JSON bozulması uygulamayı çökertmiyor.
- Yorum cache'leniyor (her açılışta yeni istek atılmıyor).

---

## FAZ 4 — Hisse Temel Analiz Modülü (süre veri kaynağına bağlı)

**Amaç:** Kullanıcının merak ettiği hissenin **mevcut metriklerini** çekip
LLM ile **ne anlama geldiğini** açıklamak. Eğitimi olmayan kişi için hisseyi
"okumak." **Tahmin/öneri yok.**

> ⚠️ "Geleceği tahmin" değil, "bugünkü tabloyu açıklama." Bkz. `CLAUDE.md` § 2.

### Ön karar
- **Veri kaynağı:** ABD hisseleri için ücretsiz/uygun API'ler var. BIST
  güvenilir veri çoğunlukla ücretli → maliyet/kapsam burada netleşmeli.

### Backend görevleri
- `StockDataService` — sembolle metrik çek: fiyat, F/K, PD/DD, temettü verimi,
  kâr büyümesi, sektör ortalaması (varsa).
- Endpoint: `GET /api/stocks/{symbol}/metrics`.
- `LlmStockExplainService` — metrikleri alıp "bu rakamlar ne anlatıyor" diye
  sade dille açıklatma (yine JSON, yine "tavsiye değil").

### Mobil görevleri
- Hisse arama + sembol seçme.
- Metrik kartları (taslaktaki 2x2 grid) + sektöre göre etiket.
- "Bu rakamlar ne anlatıyor?" LLM açıklama kartları + disclaimer.

### Riskler
- Metrik tanımları kaynaktan kaynağa değişebilir (F/K hesabı vs.) → kaynağı
  belge ve tutarlı kullan.
- BIST verisi pahalıysa: önce sadece ABD ile çıkıp BIST'i sonra ekle.

### ✅ Tamamlanma Kriteri
- Bir sembol için metrikler çekilip gösteriliyor.
- LLM metrikleri açıklıyor, "iyi/kötü/al" demeden çerçeve sunuyor.
- Veri bulunamayan sembolde anlamlı hata mesajı.

---

## FAZ 5 — Ötesi & Ürünleşme

**Amaç:** Ürünü genişletmek ve gelir modeline hazırlamak. Sırası ihtiyaca göre.

### Olası işler
- **Yeni varlık türleri:** Fon, gayrimenkul (değerleme yaklaşımı netleştirilecek),
  kripto (istenirse).
- **Senaryo simülasyonu (derin):** "Dağılımım şöyle olsaydı geçmiş 12 ayda ne
  olurdu?" — `PriceSnapshots` geçmişiyle, geriye dönük gösterim (tahmin değil).
- **Eğitim içeriği genişletme:** Seviyeli dersler, mini testler, ilerleme takibi.
- **Bildirimler:** Fiyat/oran eşiği uyarıları (yine tavsiye değil, bilgi).
- **Gelir modeli:** Abonelik (ücretsiz takip + ücretli analiz/eğitim?) — model
  belirle.
- **Hesap/kimlik:** Gerçek kullanıcı hesapları, güvenli giriş.

### Yapılması gerekenler (lansman öncesi)
- **Hukuki doğrulama:** SPK (yatırım tavsiyesi sınırı) + KVKK (veri) için
  uzman/avukat görüşü. **Şart.**
- Performans, güvenlik gözden geçirmesi.
- App Store / Play Store yayın süreci.

### ✅ Tamamlanma Kriteri
Bu faz açık uçlu; her özellik kendi "tamam" tanımıyla ilerler. Ürünleşme adımı
hukuki onay olmadan başlamaz.

---

## Genel Notlar

- **Süre tahminleri** tek kişilik, haftada ~48 saatlik çalışmaya ve React
  Native öğrenme eğrisine göre. Tam vizyon ~4-6 ay; ilk kullanılabilir ürün
  (Faz 1) 1-2 ay.
- Her fazı bitince küçük bir "ne öğrendim / ne değişti" notu `docs/` altına yaz —
  ileride çok işe yarar.
- Bir faz takılırsa: kapsamı küçült, fazı böl. "Çalışan küçük" > "yarım büyük".

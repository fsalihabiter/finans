# 01 — İhtiyaç Analizi (Needs Analysis)

> Amaç: "Bu uygulama tam olarak **kim için, neyi, neden** yapacak?" sorusunu
> mühendislik diline çevirmek. Kapsam, aktörler, gereksinimler, kısıtlar ve
> kabul kriterleri burada netleşir. Bu doküman `CLAUDE.md` vizyonunu ölçülebilir
> gereksinimlere indirger.

---

## 1. Problem Tanımı

Bireysel yatırımcı varlıklarını (altın, döviz, BES, hisse, nakit, fon)
**dağınık** takip ediyor: farklı uygulamalar, Excel, bankaların ekranları.
Sonuç: portföyün **toplam resmi** yok, **ne anlama geldiği** belirsiz, ve
"daha iyi nasıl yönetirim?" sorusuna eğitici bir cevap yok.

**Hedef kullanıcı (persona):** Yatırıma yeni başlamış / daha önce denemiş ama
yol tutturamamış, öğrenmek isteyen kişi. **Sıfır finansal bilgi varsayılır.**

**Çözüm:** Çok varlıklı portföyü tek yerde toplayan, her sayının ne anlama
geldiğini sade Türkçe ile öğreten, **tavsiye vermeden çerçeve sunan** mobil
uygulama.

---

## 2. Aktörler (Actors)

| Aktör | Açıklama | İhtiyacı |
|-------|----------|----------|
| **Bireysel Yatırımcı (birincil)** | Uygulamayı kullanan son kullanıcı | Portföyünü görmek, anlamak, öğrenmek |
| **Sistem (Backend)** | .NET API + hesaplama motoru | Doğru sayı üretmek, veri tutmak |
| **Fiyat Sağlayıcı (dış)** | Altın/döviz/hisse fiyat API'leri | Güncel fiyat beslemek (Faz 2+) |
| **LLM Sağlayıcı (dış)** | Yorum/açıklama üreten model | Sayıları eğitici dille yorumlamak (Faz 3+) |
| **Geliştirici (sen)** | Tek kişilik ekip | Faz faz inşa etmek |

> Faz 1'de "kullanıcı" tek ve yereldir (kimlik yok). Gerçek hesap/kimlik
> Faz 5'te gelir. Bu yüzden ilk fazlarda `UserId` sabit/tekil olabilir.

---

## 3. Fonksiyonel Gereksinimler (FR)

Her gereksinim bir faza bağlanmıştır (bkz. `ROADMAP.md`). `MUST` = o faz için
şart, `SHOULD` = arzu edilen, `COULD` = ilerideki faz.

### Faz 1 — Portföy Takip MVP
- **FR-1.1 (MUST)** Kullanıcı varlık ekleyebilir: tür, ad, sembol/birim, para
  birimi, miktar, ortalama maliyet, (ops.) alış tarihi.
- **FR-1.2 (MUST)** Kullanıcı varlığı düzenleyebilir / silebilir.
- **FR-1.3 (MUST)** Sistem portföy özetini hesaplar: toplam değer, toplam
  maliyet, net kâr, getiri %, varlık ağırlık dağılımı (%). Formüller `CLAUDE.md`
  § 6'dadır ve **kodda deterministik** yapılır.
- **FR-1.4 (MUST)** Çoklu para birimi: her varlık kendi para biriminde tutulur,
  gösterimde **baz para birimine** çevrilir. Baz para birimi kullanıcı tercihi.
- **FR-1.5 (MUST)** BES özel ele alınır: **kendi katkı payı ve devlet katkısı
  ayrı satır/alan** olarak görünür.
- **FR-1.6 (MUST)** Reel getiri (enflasyona göre) hesaplanır ve gösterilir.
- **FR-1.7 (MUST)** Varlık detay ekranı: miktar, ort. maliyet, güncel fiyat,
  toplam maliyet, portföy ağırlığı, kâr/zarar.
- **FR-1.8 (SHOULD)** "Güncel fiyat" bu fazda **elle** girilir/güncellenir
  (canlı fiyat Faz 2).

### Faz 2 — Canlı Fiyat & Bilgilendirme
- **FR-2.1 (MUST)** Altın/döviz fiyatları dış API'den otomatik çekilir.
- **FR-2.2 (MUST)** Fiyatlar cache'lenir (dış API limitini korumak için).
- **FR-2.3 (MUST)** Pull-to-refresh ile güncel değer yenilenir; son güncelleme
  zamanı gösterilir.
- **FR-2.4 (MUST)** **Kural tabanlı** bağlama duyarlı eğitici notlar (nudge):
  örn. "nakit oranı > %X" → uyarı notu.
- **FR-2.5 (MUST)** Dış API çökerse uygulama çökmez → son bilinen fiyat +
  "yaklaşık" etiketi.

### Faz 3 — LLM Yorum Katmanı
- **FR-3.1 (MUST)** Sistem, **kodun hesapladığı hazır sayıları** LLM'e gönderip
  portföyü eğitici dille yorumlatır. LLM'e ham veri verilip "hesapla" denmez.
- **FR-3.2 (MUST)** LLM çıktısı **yapılandırılmış JSON** (kart kart) olarak
  alınır ve güvenli parse edilir; bozuksa fallback (önceki yorum / düz metin).
- **FR-3.3 (MUST)** Her analiz ekranında **"yatırım tavsiyesi değildir"**
  çerçevesi görünür.
- **FR-3.4 (MUST)** Yorum cache'lenir (portföy değişince veya günde bir üretilir).

### Faz 4 — Hisse Temel Analiz
- **FR-4.1 (MUST)** Sembolle hisse metrikleri çekilir: fiyat, F/K, PD/DD,
  temettü verimi, kâr büyümesi (varsa sektör ortalaması).
- **FR-4.2 (MUST)** LLM metriklerin **ne anlama geldiğini** açıklar; "al/sat/
  yükselir" demez.
- **FR-4.3 (SHOULD)** Veri bulunamayan sembolde anlamlı hata mesajı.

### Faz 5 — Ötesi
- **FR-5.x (COULD)** Yeni varlık türleri (fon, gayrimenkul, kripto), geçmişe
  dönük senaryo simülasyonu, eğitim içeriği, bildirimler, gerçek kimlik/hesap,
  gelir modeli.

---

## 4. Fonksiyonel Olmayan Gereksinimler (NFR)

| Kod | Kategori | Gereksinim |
|-----|----------|------------|
| **NFR-1** | **Doğruluk** | Parasal hesaplar `decimal` ile; **asla `float`/`double`**. Hesaplama fonksiyonları birim testli. Yanlış rakam = kabul edilemez. |
| **NFR-2** | **Yasal (SPK)** | Çıktı asla kişiye özel alım-satım yönlendirmesi yapmaz. Her analiz/hisse ekranında "tavsiye değil" çerçevesi. Bkz. `CLAUDE.md` § 2. |
| **NFR-3** | **Yasal (KVKK)** | Finansal veri hassastır; veri minimizasyonu, şifreli saklama, kullanıcı verisini silme hakkı baştan tasarıma girer. |
| **NFR-4** | **Güvenlik** | API anahtarları koda gömülmez (env/secret). Backend ile mobil HTTPS. Girdi doğrulama. |
| **NFR-5** | **Dayanıklılık** | Dış API (fiyat/LLM) hatası uygulamayı çökertmez; her dış bağımlılıkta fallback. |
| **NFR-6** | **Performans** | Özet ekranı hızlı açılır; LLM yorumu/fiyat cache'lenir, her açılışta yeni istek atılmaz. |
| **NFR-7** | **Dil/Yerelleştirme** | Kullanıcıya görünen tüm metin Türkçe. Para/sayı formatı TR (binlik `.`, ondalık `,`, örn. `422.970,50 ₺`). Hesap tam hassasiyet, yuvarlama sadece gösterimde. |
| **NFR-8** | **Sürdürülebilirlik** | Katmanlı mimari, tema tek dosyadan, deterministik hesap/LLM yorum ayrımı net. |
| **NFR-9** | **Maliyet** | Geliştirmede LLM kullanımı düşük (aylık birkaç $). Cache + tetikleme disiplini ile kontrol edilir. Self-host açık kaynak yığın. Bkz. [`10`](10-PERFORMANCE-SCALABILITY.md) §7. |
| **NFR-10** | **Ölçeklenebilirlik** | Çok kullanıcıyı kaldıracak; **stateless API** + Redis cache + indeksli DB ile yatay ölçeklenebilir. Performans bütçeleri (p95) tanımlı. Bkz. [`10`](10-PERFORMANCE-SCALABILITY.md). |
| **NFR-11** | **Gözlemlenebilirlik** | Yapılandırılmış log + metrik + health check + audit log + alarm; admin sistem/güvenlik olaylarını izleyebilir. Bkz. [`12`](12-OBSERVABILITY.md). |
| **NFR-12** | **Veri izolasyonu** | Çok kullanıcıda her erişim `UserId` ile kapsanır; kullanıcılar birbirinin verisine erişemez (OWASP API1/BOLA). Bkz. [`11`](11-SECURITY.md) §3. |
| **NFR-13** | **Çok platform erişim** | **Birincil: web** (ReactJS, responsive — masaüstü + dar ekran), **sonra: mobil** (RN). İki yüzey **tek API** + `@finans/shared` paketini paylaşır. Modern tarayıcı desteği. Bkz. [`13`](13-WEB-FRONTEND.md). |

---

## 5. Kapsam Dışı (Out of Scope) — Şimdilik

- Gerçek para hareketi / **canlı banka API entegrasyonu** (açık bankacılık/
  ÖHVPS, otomatik hesap bağlama) / emir iletimi. **Yok.** Bu bir takip ve
  eğitim aracıdır, aracı kurum değil. *(Netleştirme 2026-07-19: kullanıcının
  kendi indirdiği banka ekstresini dosya olarak yüklemesi banka entegrasyonu
  DEĞİLDİR ve kapsam içine alındı → Faz 9, `08-BACKLOG.md`. Gerekçe ve ÖHVPS
  araştırması: `.claude/tasks/TASKLOG.md` 2026-07-19.)*
- Gelecek tahmini / fiyat öngörüsü. **Yok** (yasal + felsefi sınır).
- Çok kullanıcılı sosyal/topluluk özellikleri (Faz 5+).
- BIST canlı verisi (maliyet nedeniyle Faz 4'te değerlendirilir; önce ABD).

---

## 6. Kısıtlar & Varsayımlar

- **Tek kişilik ekip**, haftada ~48 saat. Süre tahminleri buna göre.
- Geliştirici **.NET'te güçlü, React Native'de yeni** → mobil öğrenme eğrisi
  Faz 0'a yedirilmiş.
- BIST güvenilir veri **ücretli ve zor**; ABD hisse verisi erişilebilir.
- LLM sağlayıcı henüz seçilmedi (kriter: Türkçe kalite, talimat takibi,
  yapılandırılmış çıktı, maliyet). Bkz. [`07-LLM-INTEGRATION.md`](07-LLM-INTEGRATION.md).
- **Lansman öncesi SPK + KVKK için hukuki doğrulama şart** (Faz 5).

---

## 7. Taslaktan Çıkan Gözlemler (mockup → gereksinim)

`portfoy-uygulamasi-taslak.html` incelendi (ekran görüntüleri:
`.claude/skills/run-finans-prototype/shots/`). Çıkan somut gereksinimler:

- **4 ana sekme:** Portföy, Analiz, Hisse, Eğitim + ortada "Varlık Ekle" FAB.
- **Hero kart:** toplam değer + net kâr pill'i + (maliyet / net kâr / getiri)
  üçlüsü → FR-1.3.
- **Donut dağılım + legend:** varlık ağırlıkları → FR-1.3. (RN'de `conic-gradient`
  yok; `react-native-svg`/grafik kütüphanesi gerekir — bkz. `05`/`DESIGN.md §6`.)
- **Holding satırı → detay overlay:** FR-1.7.
- **Nudge kartı:** Faz 2 kural tabanlı, Faz 3 LLM → FR-2.4 / FR-3.1.
- **Analiz kartları** (genel sağlık, yoğunlaşma, reel getiri, senaryo) +
  **disclaimer her zaman görünür** → FR-3.1, FR-3.3, NFR-2.
- **Hisse 2x2 metrik grid + "ne anlatıyor?" açıklama + disclaimer** → Faz 4.
- **Eğitim:** seviyeli dersler + ilerleme çubuğu → Faz 5 (içerik genişletme).

> ⚠️ **Veri tutarlılığı uyarısı:** Taslaktaki sayılar **elle yerleştirilmiş** ve
> hepsi birbirini doğrulamıyor (örn. donut ağırlıkları, hero toplamı, detaydaki
> maliyet kalemleri tam tutmuyor). Gerçek uygulamada **her sayı tek bir
> deterministik hesap motorundan** gelmeli (bkz. `02` Hesaplama Servisi). Taslak
> yalnızca **akış ve yerleşim** referansıdır, veri kaynağı değil.

---

## 8. Başarı Ölçütleri (proje düzeyi)

1. Faz 1 sonunda kullanıcı, elle girdiği varlıklarla **doğru hesaplanmış**
   (testle kanıtlı) bir portföy özetini mobilde görebiliyor.
2. Hiçbir ekran "al/sat/yükselir" demiyor; tüm çıktı açıklayıcı/eğitici.
3. Dış servis (fiyat/LLM) çökse bile uygulama çalışmaya devam ediyor.
4. Her faz sonunda **tek başına kullanılabilir** bir ürün var.

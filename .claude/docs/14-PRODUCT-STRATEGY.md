# 14 — Ürün Stratejisi: Finansal Okuryazarlık Platformuna Evrim

> **Tarih:** 2026-07-11 · **Durum:** ✅ Onaylandı ve plana işlendi (2026-07-11) —
> Dalga 1-3 → Faz 5-8 olarak `08-BACKLOG.md` (görev kırılımı), `ROADMAP.md`
> (faz detayları) ve `CLAUDE.md` §4/§9'a yansıtıldı.
> **Bağlam:** Faz 0-3 tamam, Faz 4 sürüyor. Ürün sahibinin netleşen vizyonu:
> *"Yatırım tavsiyesi değil; elindekini nasıl değerlendirir, portföyüne nasıl yön
> verirsin bilgisini kazandırmak — bilgisi olmayan insanları bilinçlendirmek,
> finansal okuryazarlık kazandırmak."* Bu doküman mevcut sistemi bu vizyonun
> merceğinden değerlendirir ve piyasada öne çıkaracak yol haritasını önerir.

---

## 1. Konumlandırma: "Nirengi" adı zaten stratejinin kendisi

Nirengi noktası, haritacılıkta **yön bulmak için kullanılan sabit referans**tır.
Ürünün vaadi de tam bu olmalı:

> **"Nirengi sana ne alacağını söylemez; haritayı okumayı öğretir."**

Tek cümlelik konumlandırma: *Türkiye'nin, kullanıcının **kendi gerçek portföyü
üzerinden** finansal okuryazarlık kazandıran, satış amacı gütmeyen tek uygulaması.*

Bu konumlandırmanın üç ayağı mevcut mimaride zaten var — bu tesadüf değil, avantaj:

1. **Deterministik hesap + LLM'in yalnız yorumlaması** → "rakamlar asla yanlış
   olmaz" güveni (CLAUDE.md §3.1).
2. **"Tavsiye değil" disiplini** ilk günden mimariye gömülü (CLAUDE.md §2) →
   SPK sınırına saygılı, eğitim istisnasında konumlanmış.
3. **BES/altın/döviz gibi Türkiye gerçeklerine özgü derinlik** → küresel
   uygulamaların (Delta, CoinStats vb.) hiç giremediği alan.

## 2. Mevcut Durum Değerlendirmesi (uzman bakışı)

### Güçlü yönler
| Alan | Neden güçlü |
|---|---|
| Hesap güvenilirliği | Parasal hesap kodda, testli (63 web + backend testleri); LLM sayı üretemez |
| BES modülü | Devlet katkısı ayrı satır, hak ediş kademeleri, katkı planı, projeksiyon — **TR pazarında bu derinlikte rakip yok** |
| Eğitici dil altyapısı | InfoTip'ler, nudge motoru, LLM yorum kartları, her ekranda disclaimer |
| Reel getiri | Enflasyon arındırılmış getiri ilk günden metrik — TR'de en kritik kavram |
| Mühendislik disiplini | Test/güvenlik/gözlemlenebilirlik kapıları, açık kaynak, monorepo + paylaşılan tipler |

### Kritik boşluklar (vizyona göre)
| Boşluk | Etki |
|---|---|
| **Eğitim sekmesi boş** (ComingSoon) | Vizyonun kalbi henüz ürüne girmedi — en büyük açık |
| **Senaryo sekmesi boş** + fiyat geçmişi birikmiyor | "Geçmişten öğren" vaadi çalışmıyor; Değer Seyri kartı da boş |
| Kimlik/çok kullanıcı yok | Bilinçlendirme misyonu tek kullanıcıyla ölçeklenemez |
| Onboarding yok | "Bilgisi olmayan insan" ilk açılışta ne yapacağını bilemez |
| Mobil yok (PWA da yok) | TR kullanıcısı mobil-first; erişim kısıtlı |
| Veri kırılganlığı | Ücretsiz LLM/fiyat katmanları 429/kesinti riski taşıyor (TASKLOG 2026-07-10) |

## 3. Rekabet Boşluğu: kimse bu koltukta oturmuyor

| Oyuncu | Odak | Neden bu vizyonu doldurmuyor |
|---|---|---|
| Midas | Aracı kurum + içerik | Amaç işlem yaptırmak; eğitim, satış hunisinin girişi |
| Fintables | BIST finansal tabloları | İleri seviye yatırımcıya; yeni başlayana duvar gibi |
| Banka uygulamaları | Ürün satışı | "Tavsiyeler" kendi fonlarına yönlendirir; tarafsız değil |
| Kolay BES / BEFAS | BES dar alanı | Portföy bütünü ve eğitim yok |
| İçerik siteleri/YouTube | Genel eğitim | **Senin portföyünle bağlantısız**; soyut kalır, davranış değiştirmez |

**Boşluk:** "Kendi gerçek rakamların üzerinden, sana bir şey satmaya çalışmayan,
seviyene göre konuşan finans eğitimi." Pedagojik olarak da en etkili yöntem budur:
soyut ders değil, **bağlamsal öğrenme** (kişinin kendi verisi üzerinden).

## 4. Öne Çıkaracak Özellikler — dört katman

### Katman A — Okuryazarlık çekirdeği (farkı yaratan)

- **A1. "Portföyünle Öğren" eğitim modülü (Eğitim sekmesini doldurur).**
  5-8 dakikalık mikro-dersler; her dersin sonunda **"Senin portföyünde"** bölümü
  dersin kavramını kullanıcının gerçek verisiyle gösterir (örn. çeşitlendirme
  dersi → kendi yoğunlaşma oranın), 2-3 soruluk pekiştirme quiz'i. İçerik statik
  (MDX/JSON, repo'da — topluluk katkısına açık), bağlam backend'den.
  İlk müfredat (8 ders): paranın zaman değeri ve enflasyon · reel getiri ·
  risk-getiri ilişkisi · çeşitlendirme ve yoğunlaşma · maliyet ortalaması ·
  BES'i doğru kullanmak · F/K, PD/DD okumak · davranışsal tuzaklar (FOMO, panik).
- **A2. Okuryazarlık profili + onboarding.** İlk açılışta 6-8 soruluk seviye
  ölçümü (utandırmayan, "haritada neredesin" tonunda) → UI açıklama derinliği ve
  LLM prompt tonu seviyeye göre (basit/orta/derin). İlerleme rozetleri, haftalık
  "portföy check-in" serisi (streak) — Duolingo mekaniği, finans içeriği.
- **A3. "Bunu neden görüyorum?" şeffaflığı.** Her nudge ve LLM kartında açılır
  detay: hangi veriden, hangi formülle. Ezber değil kavrayış; aynı zamanda güven.
- **A4. Kavram sözlüğü.** Tüm InfoTip içerikleri aranabilir tek indekste;
  derslere ve ekranlardaki kullanım yerlerine çapraz bağlantı.
- **A5. Davranış aynası (v2).** İşlem geçmişinden **geçmişe dönük** kalıp
  farkındalığı: "Son 3 alımın fiyat zirvelerine yakındı — bu kalıba FOMO denir"
  gibi yargısız, eğitici gözlem. Tavsiye değil; geçmişin aynası. (SPK açısından
  en hassas özellik — hukuk görüşüyle birlikte tasarlanmalı.)

### Katman B — Türkiye'ye özgü derinlik (ulusal ölçekte öne çıkaran)

- **B1. Enflasyon merkezli anlatı.** "Paranın erimesi" paneli: nominal vs reel
  değer grafiği, "bu para yastık altında dursaydı / mevduatta dursaydı"
  karşılaştırması (geçmiş veriyle, tahminsiz). Resmî TÜİK TÜFE bazlı; kaynak
  daima görünür.
- **B2. TEFAS/BEFAS fon verisi.** Fon fiyatları ve kategorileri (ücretsiz erişim);
  "fon gider oranı nedir, getirini nasıl yer" eğitimiyle birleşir. BES fon
  getirisi satırı zaten var — üstüne oturur.
- **B3. Altın kültürü modülü.** Çeyrek/yarım/tam/bilezik/22 ayar dönüşümleri,
  düğün altını takibi. Türkiye'de hane halkının en yaygın tasarruf aracı;
  hiçbir küresel uygulama bunu modellemiyor.
- **B4. BES tamamlayıcıları.** Yıl sonu devlet katkısı limiti farkındalığı
  ("bu yıl katkı payının %X'i devlet eşleşmesinden yararlandı"), fon dağılımı
  eğitimi.

### Katman C — Ürünleşme temeli (vizyonu taşıyacak iskelet)

- **C1. Fiyat geçmişi biriktirme → Değer Seyri + Senaryo.** `PriceSnapshots`
  zaten yazılıyor; günlük kapanış serisine dönüştür → pano grafiği (Sparkline
  bileşeni hazır) + "dağılımın X olsaydı son 12 ay nasıl geçerdi" geriye dönük
  simülatörü. **En yüksek kaldıraçlı teknik iş: iki boş sekmeyi birden açar.**
- **C2. Kimlik + çok kullanıcı.** Güvenlik dokümanındaki per-user izolasyon +
  IDOR testleri zaten hazır bekliyor; aile/arkadaş betasının önkoşulu.
- **C3. PWA.** React Native'den önce düşük maliyetli mobil erişim: manifest +
  service worker + yüklenebilirlik. RN (Faz M) sonra gelir.
- **C4. Bildirim (bilgi, tavsiye değil).** Haftalık portföy özeti + ders
  hatırlatması; fiyat eşiği bildirimi ("altın %X oynadı — reel getirin şöyle
  etkilendi" eğitici tonuyla).
- **C5. Veri dayanıklılığı.** Fiyat/LLM sağlayıcılarında sıralı fallback zinciri
  ve sağlayıcı sağlık panosu (Prometheus metrikleri mevcut, üstüne kural).

### Katman D — Ulusal etki ve topluluk

- **D1. Demo/misafir modu.** Kayıt olmadan örnek portföyle tüm eğitim akışı.
  Okul/üniversite atölyelerinde kullanılabilir; KVKK yükü sıfır; dönüşüm hunisinin
  de en üstü.
- **D2. Açık kaynak anlatısı.** Repo zaten public — README konumlandırmasını
  "Türkiye için açık kaynak finansal okuryazarlık altyapısı"na genişlet; ders
  içeriklerine katkı rehberi (PR ile ders eklenebilir).
- **D3. Kurumsal işbirlikleri.** FODER (Finansal Okuryazarlık ve Erişim Derneği),
  üniversite ekonomi/finans toplulukları, TÜBİTAK/BİGG destek programları;
  ileride bankalara **beyaz etiket eğitim modülü** (CSR bütçeleri) — gelir + etki.
- **D4. İçerik kanalı.** Haftalık anonim "portföy okuma" vakaları (blog/video):
  ürünün dilini ve tarafsızlığını kanıtlayan organik büyüme kanalı.

## 5. Gelir Modeli Seçenekleri (tarafsızlığı bozmayan)

| Model | Not |
|---|---|
| Freemium abonelik | Takip ücretsiz; LLM kişisel yorum + derin dersler + senaryo premium |
| B2B eğitim lisansı | Beyaz etiket okuryazarlık modülü (banka/işveren CSR) |
| Kurumsal atölye/içerik | D3 işbirliklerinin doğal uzantısı |
| **Reklam / fon yönlendirme komisyonu** | **ASLA** — tarafsızlık markanın kendisi; bir kez bozulursa geri gelmez |

## 6. Yasal Çerçeve (kısa hatırlatma)

- Mevcut "tavsiye değil" disiplini doğru zemin; ancak **davranış aynası (A5)** ve
  **bildirimler (C4)** kişiye özel yorum sınırına en yakın özellikler — SPK
  mevzuatı (yatırım danışmanlığı tebliği) merceğinde avukat görüşü **bu ikisi
  tasarlanırken** alınmalı, lansmana bırakılmamalı.
- KVKK: demo modu (D1) ve veri minimizasyonu yükü azaltır; çok kullanıcıya
  geçişte (C2) aydınlatma metni + açık rıza akışı gerekir. (`11-SECURITY.md` §KVKK)

## 7. Önerilen Öncelik Sırası

> İlke aynı: her dilim sonunda çalışan, gösterilebilir bir şey.

**Dalga 1 — "Vizyon ürüne girsin" (0-2 ay)**
1. Faz 4'ü bitir (T4.2-T4.4 — hisse metrikleri + LLM açıklama): başlanmış işi kapat.
2. **C1** fiyat geçmişi → Değer Seyri grafiği (+ Senaryo v1: tek varlık "dursaydı/almasaydım" karşılaştırması).
3. **A1** Eğitim MVP: 5 ders + "Senin portföyünde" bağlamı + quiz.
4. **A4** sözlük (InfoTip içeriklerinin indekslenmesi — düşük maliyet).

**Dalga 2 — "Kişiselleşme + erişim" (2-6 ay)**
5. **A2** onboarding + okuryazarlık profili (LLM tonu seviyeye bağlanır).
6. **C2** kimlik/çok kullanıcı → kapalı beta (aile/arkadaş).
7. **C3** PWA + **C4** haftalık özet bildirimi.
8. **B2** TEFAS + **B3** altın kültürü modülü.
9. **D1** demo modu + **D2** README/anlatı güncellemesi.

**Dalga 3 — "Ölçek + etki" (6-12 ay)**
10. **A5** davranış aynası (hukuk görüşüyle birlikte).
11. **B1** enflasyon paneli derinleştirme; senaryo simülatörü tam sürüm.
12. Mobil (React Native, Faz M) + gelir modeli kararı + SPK/KVKK hukuki doğrulama.
13. **D3** işbirlikleri (FODER/üniversiteler) + **D4** içerik kanalı.

## 8. Başarı Ölçütü: DAU değil, öğrenme

Kuzey yıldızı metriği klasik etkileşim değil, **okuryazarlık ilerlemesi** olmalı:

- Ders tamamlama ve quiz doğruluk oranı (seviye bazında),
- "Bunu neden görüyorum?" açılma oranı (merak sinyali),
- Nudge sonrası davranış farkındalığı (örn. yoğunlaşma nudge'ı sonrası
  çeşitlendirme dersine geçiş),
- Reel getiri kavrayışı: kullanıcı panelde nominal yerine reel sekmesine bakıyor mu.

Bu ölçüm çerçevesi hem ürünü hizalar hem de D3 işbirliklerinde "etki kanıtı"
olarak sunulabilir — ulusal düzeyde yön verici olma iddiasının somut dili budur.

---
*Bu doküman yaşayan bir stratejidir; her dalga sonunda gözden geçir. Görev
kırılımı `08-BACKLOG.md`'ye, kararlar ilgili kalıcı dokümanlara işlenir.*

# PROJECT_INTENT

> Kanonik kaynak. İlk sürüm `adm-analyze` ile mevcut kod tabanından ve
> `.claude/docs/` dokümanlarından **çıkarıldı** (2026-09-20, HEAD ac61b7e).
> Çıkarım olan yerler `[çıkarım]`, belgeye dayananlar `[kaynak: ...]` işaretli.
> Sondaki açık sorular hâlâ karar bekliyor.

## 1. Amaç (Purpose)

Nirengi, bireysel yatırımcının **birden fazla varlık sınıfını (hisse, altın,
döviz, BES) tek yerde takip etmesini** ve daha önemlisi **kendi gerçek portföyü
üzerinden finansal okuryazarlık kazanmasını** sağlayan, Türkiye'ye özgü bir web
uygulamasıdır. [kaynak: CLAUDE.md §1, 14-PRODUCT-STRATEGY §1]

Çözdüğü problem: yatırıma yeni başlayan kişi hem portföyünün ne durumda
olduğunu okuyamıyor hem de öğrenmek istediğinde karşısına ya satış hunisi
(aracı kurum/banka içeriği) ya da başlangıç seviyesini aşan teknik kaynaklar
çıkıyor. Nirengi ne alacağını söylemez; haritayı okumayı öğretir.
[kaynak: 14 §1, §3]

Ayırt edici ilke: **sayısal hesap kodda deterministik yapılır, LLM yalnızca
hazır sayıyı yorumlar.** [kaynak: CLAUDE.md §3.1 → D-001]

## 2. Hedefler (Goals)

- **G-1 — Doğru portföy takibi:** Kullanıcı çoklu varlık sınıfını ve çoklu para
  birimini tek panoda, tam hassasiyetli ve testli hesaplarla görür (maliyet,
  değer, getiri, dağılım, reel getiri, BES devlet katkısı ayrı satır).
  [kaynak: CLAUDE.md §4 Faz 1, §6]
- **G-2 — Bağlamsal finansal okuryazarlık:** Kullanıcı kendi verisi üzerinden
  ders alır; her dersin "Senin portföyünde" bölümü kavramı gerçek rakamıyla
  gösterir. Hedef müfredat 35 ders / 5+1 set. [kaynak: 14 §4-A1, 16-CURRICULUM]
- **G-3 — Tavsiye vermeden açıklama:** Ürün hiçbir noktada alım-satım
  yönlendirmesi, tahmin veya enstrüman sıralaması üretmez; çerçeve, açıklama ve
  geçmişe dönük senaryo sunar. [kaynak: CLAUDE.md §2, 15 §3.4]
- **G-4 — Türkiye gerçeklerine özgü derinlik:** BES (devlet katkısı, hak ediş,
  katkı planı), gram altın, TL enflasyonu ve reel getiri birinci sınıf
  kavramlardır. [kaynak: 14 §2, §4-B]
- **G-5 — Güvenilir ve sürdürülebilir mühendislik:** Parasal hesapta birim test
  zorunlu; per-user veri izolasyonu, sır yönetimi, gözlemlenebilirlik ve dış
  bağımlılıklarda fallback baştan mimari kuraldır.
  [kaynak: CLAUDE.md §12-§13, 09/10/11/12]

⚠ `G-2` ve `G-4` bugün ürünün ağırlık merkezi; `G-1` büyük ölçüde tamamlandı
(Faz 0-5). [çıkarım: 08-BACKLOG durum sayımı]

## 3. Kapsam dışı (Out of scope / will-not-do)

> Doctor'ın drift tespiti bu **somut** adları anahtar kelime olarak kullanır.

- Yatırım tavsiyesi / alım-satım sinyali: `al`, `sat`, `alın`, `satın`,
  `tavsiye`, `öneri (enstrüman için)`, `hedef fiyat`, `formasyon`, `sinyal`
  — yalnızca `Trap` bloklarında ve quiz çeldiricisinde geçebilir.
  [kaynak: CLAUDE.md §2, 16 §9.1 M7/M7a]
- Gelecek tahmini / fiyat projeksiyonu (`yükselecek`, `düşecek`, `olacak`).
  Kabul edilen kalıp: "şu oran **olursa**". [kaynak: 15 §6.2, T6.18]
- Enstrüman sıralaması: "X, Y'den iyi performans gösterdi" biçimi.
  [kaynak: 15 §3.4, SC-E5]
- Tek şirket / fon / aracı kurum adı ve sembolü ders içeriğinde.
  (Geniş endeks adları ölçüt olarak izinli — K3 kararı onay bekliyor.)
  [kaynak: 16 §12-K3]
- `float` / `double` ile parasal hesap. [kaynak: CLAUDE.md §8]
- LLM'e ham sayı verip hesap yaptırma. [kaynak: CLAUDE.md §3.1, 02 §7]
- İstemcide (web/mobil) parasal hesap. [kaynak: 02 §7]
- Reklam geliri ve fon yönlendirme komisyonu. [kaynak: 14 §5]
- Açık bankacılık / ÖHVPS entegrasyonu (Faz 9 kullanıcı ekstresi yükler).
  [kaynak: 08 Faz 9 başlığı]
- Erken mikroservis, aşırı soyutlama, RN-for-web. [kaynak: 02 §7, 02 §3]
- Vergi dersi (müfredat kararı). [kaynak: 15 §9]
- BIST veri kaynağı (maliyet nedeniyle ertelendi). [kaynak: CLAUDE.md §3.3, §10]

## 4. Kısıtlar (Constraints)

- **Hukuki:** SPK yatırım danışmanlığı sınırı; lansman öncesi SPK + KVKK avukat
  görüşü zorunlu kapıdır. Davranış aynası (A5) ve bildirimler (C4) tasarlanırken
  görüş alınır, lansmana bırakılmaz. [kaynak: CLAUDE.md §2, 14 §6]
- **Dil:** Kod/kimlik İngilizce, kullanıcıya görünen her metin Türkçe; para
  biçimi `tr-TR` (binlik nokta, ondalık virgül). [kaynak: CLAUDE.md §8]
- **Tek geliştirici + düşük bütçe:** ücretsiz/anahtarsız veri kaynakları
  tercih edilir; barındırma tek VPS + Docker Compose. [kaynak: 02 §2.3, §6]
- **Web birincil yüzey; mobil (React Native) sonra.** [kaynak: CLAUDE.md §3]
- **Kimlik henüz yok:** `X-User-Id` başlığı geçici; JWT Faz 7'de. Hassas veri
  fazları (Faz 9) bu kapıya bağlıdır. [kaynak: 02 §2.3, 08 Faz 9]

## 5. Başarı kriteri (Definition of success)

- Kuzey yıldızı **etkileşim değil öğrenme**: ders tamamlama + quiz doğruluk
  oranı, "bunu neden görüyorum?" açılma oranı, reel sekmesine bakma oranı.
  [kaynak: 14 §8]
- Portföy rakamları her zaman doğru: parasal hesapların birim testi yeşil,
  `decimal`, TR biçim. [kaynak: CLAUDE.md §12]
- Hiçbir üretilen metin tavsiye/tahmin/sıralama içermez — testle taranır (M7).
  [kaynak: 16 §9.1]
- Kullanıcı bir fiyat ve mum grafiğini okuyabilir, kalemini bir ölçütle
  karşılaştırabilir (Faz 6 DoD). [kaynak: 08 Faz 6 DoD]
- Bir kullanıcı asla başkasının verisini göremez (IDOR testleri yeşil).
  [kaynak: CLAUDE.md §13]

## Açık sorular (insan kararı gerekli)

1. `G-2`'nin ölçülebilir eşiği nedir? ("35 dersin tamamı yayında" mı, yoksa
   `14` §8 metriklerinden sayısal bir hedef mi?)
2. K3 (geniş endeks adlarının izinli olması) hâlâ onay bekliyor — kapsam dışı
   listesine bugünkü hali yazıldı. [kaynak: ACTIVE.md 2026-07-24]
3. Faz 9 (nakit akışı) hedefi `G-1`'in altına mı girer, yoksa yeni bir hedef
   (`G-6 — harcama bilinci`) mi açılmalı?

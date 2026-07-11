# Finans — Teknik Doküman Seti (`.claude/docs/`)

> Bu klasör, projeyi **her oturumda ne yapacağını bilerek, emin adımlarla**
> ilerletmek için hazırlanmış mühendislik dokümanlarıdır. Kök dizindeki
> `CLAUDE.md` (vizyon + kurallar), `DESIGN.md` (görsel sistem) ve `ROADMAP.md`
> (faz planı) **değişmez referanstır**; bu set onları somut mühendislik
> kararlarına, sözleşmelere ve adım adım uygulanabilir görevlere çevirir.

## Bu seti nasıl kullanırım? (Çalışma ritmi)

Her geliştirme oturumunun başında:

1. **Nerede olduğunu bul:** [`08-BACKLOG.md`](08-BACKLOG.md) → aktif fazın
   "Sıradaki" görevini al. Görevler bağımlılık sırasına dizili.
2. **Bağlamı tazele:** O görev hangi alana giriyorsa ilgili dokümanı aç
   (veritabanı → `03`, endpoint → `04`, ekran → `05`, LLM → `07`).
3. **Kuralları doğrula:** Kod yazmadan önce `CLAUDE.md` § 2 ("tavsiye değil") ve
   § 8 (konvansiyonlar) ile [`06-DEV-PLAYBOOK.md`](06-DEV-PLAYBOOK.md)'deki
   "Definition of Done" maddesine bak.
4. **Yap → doğrula → işaretle:** Görevi tamamla, DoD'yi karşıla, backlog'da
   işaretle. Hesaplama kodu yazdıysan **birim testi olmadan tamam sayma.**

> İlke: *"Çalışan küçük > yarım büyük."* Her görev kendi içinde
> tamamlanabilir ve doğrulanabilir olmalı.

## Doküman haritası

| # | Doküman | Ne zaman aç? |
|---|---------|--------------|
| 01 | [Needs Analysis / İhtiyaç Analizi](01-NEEDS-ANALYSIS.md) | Kapsam, aktörler, fonksiyonel + fonksiyonel olmayan gereksinimler, yasal kısıtlar. Yeni özellik tartışırken. |
| 02 | [Architecture / Mimari](02-ARCHITECTURE.md) | Sistem bileşenleri, teknoloji seçimleri, katmanlar, güvenlik, dağıtım. Yeni servis/katman eklerken. |
| 03 | [Data Model / Veri Modeli](03-DATA-MODEL.md) | Tablolar, ilişkiler, kesinleşmiş kararlar (ort. maliyet türetimi), migration stratejisi. Şema değiştirirken. |
| 04 | [API Contract / API Sözleşmesi](04-API-CONTRACT.md) | Endpoint'ler, istek/yanıt DTO'ları, hata formatı, sürümleme. Yeni endpoint açarken. |
| 05 | [Mobile Spec / Mobil Şartname](05-MOBILE-SPEC.md) | Taslaktan türetilmiş ekran ekran şartname, bileşen envanteri, navigasyon, durum yönetimi, tema. Ekran yazarken. |
| 06 | [Dev Playbook / Geliştirme El Kitabı](06-DEV-PLAYBOOK.md) | Ortam kurulumu, konvansiyonlar, test stratejisi, git akışı, faz başına DoD. Oturum başında. |
| 07 | [LLM Integration / LLM Entegrasyonu](07-LLM-INTEGRATION.md) | Prompt tasarımı, JSON şeması, "tavsiye değil" korkulukları, cache, sağlayıcı seçimi. Faz 3-4'te. |
| 08 | [Backlog / Görev Listesi](08-BACKLOG.md) | Faz faz, bağımlılık sıralı, uygulanabilir görev kırılımı + "sıradaki adım". Her oturumda. |
| 09 | [Testing Strategy / Test Stratejisi](09-TESTING-STRATEGY.md) | Senaryo-önce/yeşil-kapı disiplini, test piramidi, senaryo kataloğu, görev başına test akışı. Kod/test yazarken. |
| 10 | [Performance & Scalability / Performans](10-PERFORMANCE-SCALABILITY.md) | Performans bütçeleri, önbellekleme katmanları, stateless ölçeklenme, maliyet kontrolü. Hız/ölçek/maliyet konusunda. |
| 11 | [Security / Güvenlik](11-SECURITY.md) | Tehdit modeli, kimlik & yetki (per-user izolasyon), veri koruması, sırlar, KVKK, güvenlik testleri. Her endpoint/PR'da. |
| 12 | [Observability / Gözlemlenebilirlik](12-OBSERVABILITY.md) | Loglama (Serilog+Seq), metrik/trace (OTel+Prometheus+Grafana), health check, audit log, alarm. İzleme kurarken. |
| 13 | [Web Frontend / Web Arayüz](13-WEB-FRONTEND.md) | **Birincil yüzey:** ReactJS+Vite SPA, monorepo + paylaşılan paket, web düzen uyarlaması. Web yazarken. |
| 14 | [Product Strategy / Ürün Stratejisi](14-PRODUCT-STRATEGY.md) | Finansal okuryazarlık vizyonu, rekabet boşluğu, öne çıkaracak özellik katmanları (A-D), dalga planı, gelir modeli. Yol haritası/öncelik tartışırken. |

> **Frontend sırası:** **Web öncelikli** (ReactJS+Vite, `13`). Mobil (React
> Native, `05`) aynı API ve `@finans/shared` paketi üzerine **sonradan** eklenen
> koldur.

## Taslak referansı

Tek çalışan artefakt: kök dizindeki `portfoy-uygulamasi-taslak.html`.
Onu render edip ekran görüntüsü almak için:

```bash
node .claude/skills/run-finans-prototype/driver.mjs
```

Ekran görüntüleri `.claude/skills/run-finans-prototype/shots/` altına düşer.
Detay için [`run-finans-prototype/SKILL.md`](../skills/run-finans-prototype/SKILL.md).

---
*Bu set yaşayan bir dokümandır. Bir karar değişirse ilgili dosyayı güncelle ve
kökteki `CLAUDE.md`/`ROADMAP.md` ile çelişki bırakma.*

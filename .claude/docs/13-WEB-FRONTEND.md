# 13 — Web Frontend (ReactJS / Vite) & Monorepo

> **Karar:** Web, projenin **birincil (öncelikli) yüzeyidir**; ayrı bir ReactJS
> (DOM) uygulaması olarak **Vite** ile geliştirilir. Mobil (Expo/React Native)
> aynı backend ve **paylaşılan paket** üzerine **sonradan** eklenen bir koldur.
> Mobil ekran şartnamesi `05`, tasarım sistemi `DESIGN.md`'dir; bu doküman web'e
> özel mimari, düzen uyarlaması ve monorepo yapısını tanımlar.

İlgili: `02` (mimari), `04` (API sözleşmesi — web aynı API'yi tüketir),
`10`/`11`/`12` (web istemci perf/güvenlik/izleme notları), `08` (web-öncelikli
backlog).

---

## 1. Neden ayrı React + Vite (RN-for-web değil)

- Web'e özel **gerçek DOM** UX: büyük ekran, fare/klavye, çok sütunlu pano,
  gerçek tablo, klavye kısayolları.
- "ReactJS frontend" isteğine birebir uyum; web'in kendi bileşen kütüphanesi.
- **Paylaşım koddan değil, sözleşmeden:** API tipleri, tasarım token'ları ve
  saf yardımcılar paylaşılır; sunum katmanı her platforma özel (en sağlam denge).

---

## 2. Monorepo Yapısı

```
finans/
├── backend/                  ← .NET Web API (değişmez; tek API herkese hizmet)
├── packages/
│   └── shared/               ← @finans/shared (platform-bağımsız TS)
│       ├── types/            ← API DTO tipleri (04 ile birebir)
│       ├── api/              ← fetch wrapper + TanStack Query hook'ları
│       ├── theme/            ← tasarım token'ları (TS obje + CSS değişken üretimi)
│       └── format/           ← formatCurrency/formatPercent (tr-TR, NFR-7)
├── web/                      ← ★ BİRİNCİL: Vite + React + TS
│   └── src/
│       ├── routes/           ← Portföy, Analiz, Hisse, Eğitim sayfaları
│       ├── components/       ← HeroCard, AllocationDonut, HoldingsTable, Disclaimer...
│       ├── layout/           ← AppShell (sidebar/topbar), modal'lar
│       └── lib/              ← query client, router
├── mobile/                   ← SONRA: Expo + React Native (05'i uygular)
└── .claude/, docs/, ...
```

- **Paket yöneticisi:** **pnpm workspaces** (verimli, monorepo-dostu). Kök
  `pnpm-workspace.yaml`: `packages/*`, `web`, `mobile`.
- **`@finans/shared`** hem web hem mobil tarafından tüketilir → tek doğruluk
  kaynağı (tip/sözleşme/token/format). Hesap **yok** (o backend'de).

---

## 3. Web Teknoloji Yığını

| Konu | Karar | Gerekçe |
|------|-------|---------|
| Build | **Vite + React 18 + TypeScript** | Hızlı, sade SPA; modern DX. |
| Yönlendirme | **React Router** | Sayfa başına URL (mobilde sekme, webde gerçek route → derin bağlantı, geri/ileri). |
| Sunucu durumu | **TanStack Query** | Mobil ile **aynı** (`@finans/shared`'da hook'lar paylaşılır). Cache/yenileme/loading hazır. |
| İstemci durumu | Context veya **Zustand** | Baz para birimi, tema gibi küçük global durum. |
| Stil | **CSS değişkenleri (token'lardan) + CSS Modules** | `DESIGN.md`/taslak zaten CSS değişkeni tabanlı → neredeyse birebir taşınır. (Tailwind tercih edilirse token'lardan beslenir.) |
| Grafik (donut) | **SVG** (veya `recharts`) | Taslaktaki conic donut webde `conic-gradient` ile **doğrudan** çalışır; istenirse SVG/recharts. |
| Format | `@finans/shared/format` | tr-TR; backend ham sayı verir, web formatlar (NFR-7). |
| Test | **Vitest + React Testing Library**; E2E **Playwright** | `09` ile uyumlu. |

---

## 4. Düzen Uyarlaması (mobil → web)

Tasarım **dili aynı** (koyu tema, altın aksan, Fraunces/Hanken — `DESIGN.md`),
**düzen** web'e uyarlanır:

| Öğe | Mobil (05) | Web (13) |
|-----|------------|----------|
| Navigasyon | Alt sekme çubuğu + FAB | **Sol sidebar** (geniş ekran) / üst bar (dar); "Varlık Ekle" belirgin buton |
| Ekranlar | Tek sütun, kaydırmalı | **Responsive grid**: geniş ekranda çok sütun (hero + donut yan yana, holdings tablo) |
| Holding listesi | `HoldingRow` kartları | Geniş ekranda **gerçek tablo** (sıralanabilir sütunlar); dar ekranda kart |
| Detay / Ekle | Alttan kayan overlay | **Modal/dialog** veya kendi route'u (`/holdings/:id`) |
| Hisse metrikleri | 2x2 grid | Geniş ekranda 4'lü satır + grafik alanı |
| Etkileşim | Dokunma | Fare + **klavye erişilebilirliği** (focus, tab sırası) |

- **Responsive kırılım:** mobil-genişlik → tek sütun (taslakla aynı his);
  tablet/masaüstü → çok sütunlu pano. `max-width` ile ortalı uygulama kabuğu.
- **Disclaimer ("yatırım tavsiyesi değildir")** webde de Analiz/Hisse'de
  **her zaman** görünür (NFR-2).

---

## 5. API Tüketimi (web = aynı sözleşme)

- Web, `04`'teki **aynı** REST endpoint'lerini tüketir; yeni backend gerekmez.
- `@finans/shared/api` içindeki fetch wrapper + TanStack Query hook'ları hem
  web hem mobil tarafından kullanılır (`useportfolioSummary`, `useHoldings`...).
- **CORS:** Backend allow-list'ine web origin'i eklenir (`11` §5). `*` yok.
- Ham sayı gelir, **web formatlar** (hesap webde yok — `02` §1, `10` §6).

---

## 6. Web'e Özel Güvenlik (`11` ile birlikte)

- **XSS:** React varsayılan kaçışı; `dangerouslySetInnerHTML` **kullanma**.
  LLM/metin içeriği düz metin olarak render edilir.
- **Token saklama:** access token **bellekte** (JS değişkeni/Context), refresh
  token **httpOnly + Secure + SameSite cookie** (Faz 5). **`localStorage`'a
  token koyma** (XSS'te çalınır).
- **CSP + güvenlik başlıkları:** reverse proxy'de (`11` §5; webe `Content-
  Security-Policy`, `X-Content-Type-Options`).
- **Per-user izolasyon** backend'de zaten zorunlu (`11` §3); web sadece
  gösterir, yetkiyi backend dayatır.

---

## 7. Web'e Özel Performans (`10` ile birlikte)

- **Kod bölme (code-splitting):** route bazında lazy import (Vite/React.lazy).
- **Bundle bütçesi:** ilk yük küçük; ağır grafik/sayfa tembel yüklenir.
- **TanStack Query cache** → tekrar veri çekme azalır.
- **Web Vitals** (LCP/CLS/INP) izlenir (`12` web notu).
- Statik varlıklar reverse proxy'den gzip/br + cache başlığıyla servis edilir.

---

## 8. Web'e Özel İzleme (`12` ile birlikte)

- **İstemci hata yakalama:** global error boundary; (opsiyonel) hafif hata
  raporlama. Backend log/metrik zaten `12`'de.
- **Web Vitals** metrikleri (performans bütçesi).
- Backend tarafı CorrelationId ile web isteği uçtan uca izlenebilir.

---

## 9. Erişilebilirlik (web)

- Semantik HTML, klavye ile tam gezinme, focus görünürlüğü, ARIA gerektiğinde.
- Kontrast `DESIGN.md` §8 ile korunur; kâr/zarar renk **+ işaret/ok**.
- Dokunma yerine fare; ama dokunmatik laptop'lar için hedefler yine rahat.

---

## 10. Yapma Listesi (web)

- ❌ Webde parasal hesap (backend'in verdiğini formatla).
- ❌ Token'ı `localStorage`'da tutmak (XSS riski).
- ❌ `dangerouslySetInnerHTML` ile LLM/kullanıcı metni basmak.
- ❌ Tip/sözleşme/token'ı web'de kopyalamak (→ `@finans/shared`).
- ❌ Disclaimer'sız analiz/hisse ekranı.
- ❌ Tek dev bundle (route bazlı kod bölme yap).

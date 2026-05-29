# 06 — Geliştirme El Kitabı (Dev Playbook)

> "Her seferinde ne yapacağını bilerek, emin adımlarla." Bu doküman **çalışma
> ritmini, konvansiyonları, test ve git akışını, ve faz başına Definition of
> Done**'ı toplar. Oturum başında buraya bak.

---

## 1. Oturum Ritmi (her seferinde)

1. **`08-BACKLOG.md`** → aktif fazın "Sıradaki" görevini al.
2. Görev alanına göre ilgili dokümanı aç (`03` DB, `04` API, `05` mobil, `07` LLM).
3. **Kural kontrolü:** `CLAUDE.md` § 2 (tavsiye değil) + § 8 (konvansiyon) +
   bu dosyanın § 5 (DoD).
4. Küçük, tamamlanabilir parça yap → **doğrula** → backlog'da işaretle.
5. Hesaplama kodu yazdıysan **birim testi yazmadan "tamam" deme** (NFR-1).

> İlke: *Çalışan küçük > yarım büyük.* Takılırsan kapsamı böl.

---

## 2. Ortam Kurulumu (Faz 0, bir kez)

### Monorepo (pnpm workspaces) — önce bu
```bash
# kökte:
pnpm init                      # veya elle package.json
# pnpm-workspace.yaml:
#   packages:
#     - "packages/*"
#     - "web"
#     - "mobile"
# paylaşılan paket:
mkdir -p packages/shared/src && pnpm --filter ./packages/shared init   # @finans/shared
```
> `@finans/shared`: API tipleri (`04`), tasarım token'ları (`DESIGN.md`),
> format util'leri (`13` §2). Web ve mobil bunu tüketir.

### Backend (.NET)
```bash
# .NET 8 SDK kurulu olmalı:  dotnet --version
dotnet new webapi -n Finans.Api -o backend/src/Finans.Api
# katman projeleri:
dotnet new classlib -n Finans.Domain        -o backend/src/Finans.Domain
dotnet new classlib -n Finans.Application    -o backend/src/Finans.Application
dotnet new classlib -n Finans.Infrastructure -o backend/src/Finans.Infrastructure
dotnet new xunit    -n Finans.Application.Tests -o backend/tests/Finans.Application.Tests
# EF Core (PostgreSQL):
dotnet add backend/src/Finans.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add backend/src/Finans.Infrastructure package Microsoft.EntityFrameworkCore.Design
```
> Bağımlılık yönü: `Api→Application→Domain`, `Infrastructure→Application`
> arayüzlerini implemente eder (`02` § 2.1).

### Web (ReactJS + Vite) — ★ BİRİNCİL yüzey
```bash
pnpm create vite web --template react-ts
pnpm --filter ./web add @tanstack/react-query react-router-dom @finans/shared
# stil: CSS değişkenleri (DESIGN.md token'larından) + CSS Modules (veya Tailwind)
pnpm --filter ./web dev          # geliştirme sunucusu
```
> Faz 0 hedefi: web iskeleti + `@finans/shared` bağlı + `/api/health`'ten veri
> çekip gösterme + tema token'ları CSS değişkeni olarak. Detay `13`.

### Mobil (React Native / Expo) — SONRAKİ kol (FAZ M)
```bash
pnpm create expo-app mobile
pnpm --filter ./mobile add react-native-svg @react-navigation/native @react-navigation/bottom-tabs @tanstack/react-query @finans/shared
# expo-font ile Fraunces/Hanken
```
> Web parası oturduktan sonra; aynı API + `@finans/shared` paketini paylaşır
> (`05`, `13` §2). Önce **öğrenme**, sonra ürün.

### Secret / config
- Backend: `dotnet user-secrets init` (dev). API anahtarları buraya, **repoya değil**.
- `.gitignore`: `bin/ obj/ node_modules/ dist/ .expo/ *.env appsettings.*.local.json`

---

## 3. Kod Konvansiyonları (`CLAUDE.md` § 8 + ek)

| Konu | Kural |
|------|-------|
| Dil | Kod/identifier **İngilizce**; kullanıcı metni **Türkçe**; LLM çıktısı Türkçe. |
| Para | **`decimal`** (backend) — asla float/double. Web/mobilde `number`, hesap yok (yalnızca formatla). |
| Format | TR (binlik `.`, ondalık `,`). Yuvarlama **sadece gösterimde** (`05` § 10). |
| Hesap yeri | Tüm sayısal hesap **backend**. LLM ve mobil hesap yapmaz. |
| LLM çıktısı | Daima JSON iste, **güvenli parse**, parse hatasında fallback. |
| Hata | İstemciye sözleşmeli hata (`04` § 2), stack trace sızdırma. |
| Tavsiye | Hiçbir çıktı "al/sat/yükselir" demez (`CLAUDE.md` § 2, NFR-2). |
| İsimlendirme | C#: PascalCase tip/metot, camelCase yerel. TS: camelCase, tip PascalCase. |
| Nullability | C# nullable reference types açık; mobil TS `strict`. |

---

## 4. Test Stratejisi

> **Tam strateji, senaryo formatı ve yaşayan senaryo kataloğu:**
> [`09-TESTING-STRATEGY.md`](09-TESTING-STRATEGY.md). Kural: *senaryo-önce,
> test-yanında, yeşil olmadan "tamam" yok* (`CLAUDE.md` §12). Her görevde hem
> **birim** hem **olaylara yönelik (senaryo)** test düşünülür.

| Katman | Ne test edilir | Araç |
|--------|----------------|------|
| **Hesaplama (ZORUNLU)** | `PortfolioCalculationService` formülleri, kur dönüşümü, ort. maliyet türetimi, reel getiri | xUnit |
| Servis | Holding CRUD, nudge kuralları | xUnit (+ in-memory DB) |
| LLM parse | Bozuk/eksik JSON'da fallback çalışıyor mu | xUnit |
| Mobil | Format util, kritik bileşen render | Jest + RTL (hafif) |

**Altın test verisi (her zaman aynı sayılar):** taslaktaki altın kalemi —
40 gr, ort. ~4.546 ₺/gr → toplam maliyet ≈ 181.851 ₺, güncel 6.500 ₺/gr →
değer 260.000 ₺, kâr ≈ +%43. Bu sayıları regresyon testi yap.

> **Kural (NFR-1):** Yeni bir formül/hesap eklediysen, PR'ı kapatmadan önce o
> hesabın birim testi **yeşil** olmalı. Yanlış rakam = kabul edilemez.

---

## 5. Definition of Done (DoD) — faz başına

`ROADMAP.md`'deki ✅ kriterlerinin mühendislik özeti:

- **Faz 0:** Mobil `/api/health`'ten veriyi gösteriyor; `dotnet ef migrations`
  ile DB oluşuyor; tema token dosyası bir ekranda kullanılıyor.
- **Faz 1:** Varlık ekle/sil/listele çalışıyor; toplam değer/kâr/getiri/dağılım
  **testlerle kanıtlı** doğru; çoklu para birimi baz pb'ye doğru çevriliyor;
  BES devlet katkısı ayrı görünüyor.
- **Faz 2:** Güncel değer dış kaynaktan otomatik + yenilenebilir; en az bir
  bağlama duyarlı not doğru tetikleniyor; dış API çökünce uygulama çökmüyor.
- **Faz 3:** Analiz kartları gerçek veriyle LLM'den; çıktı asla "al/sat/yükselir"
  demiyor; LLM/JSON hatası çökertmiyor; yorum cache'leniyor.
- **Faz 4:** Sembol metrikleri çekiliyor + LLM çerçeve sunarak açıklıyor;
  veri yoksa anlamlı hata.

> Her görev için **mini DoD:** senaryo (09 §5) + kod + **testler yeşil**
> (birim + olaylara yönelik) + **güvenlik/gözlemlenebilirlik kapısı** (`CLAUDE.md`
> §13: per-user izolasyon, sır repoda değil, log redaksiyonu, cache+async) +
> ilgili doküman güncel + worklog girdisi. Kapılar geçilmeden görev **kapanmaz**.

---

## 6. Git Akışı

> Proje şu an git deposu **değil**. İlk iş: `git init` + `.gitignore`.

- Ana dal `main` korunur; iş **kısa ömürlü dallarda** (`feat/holdings-crud`).
- Commit mesajı: konvansiyonel (`feat:`, `fix:`, `test:`, `docs:`).
- Her faz sonunda kısa "ne öğrendim / ne değişti" notu `docs/` altına
  (`ROADMAP.md` Genel Notlar).
- Build çıktıları ve secret'lar **asla** commit edilmez.

---

## 7. Taslağı Doğrulama (görsel referans)

Bir ekran yazarken taslakla karşılaştır:
```bash
node .claude/skills/run-finans-prototype/driver.mjs --screen portfoy
# → .claude/skills/run-finans-prototype/shots/portfoy.png
```
Detay: [`run-finans-prototype/SKILL.md`](../skills/run-finans-prototype/SKILL.md).

---

## 8. Sık Yapılan Hatalar (kaçın)

- ⛔ LLM'e ham sayı verip "hesapla". → Sayı kodda hazır, LLM yorumlar.
- ⛔ Mobilde getiri/dağılım hesaplama. → Backend'in verdiğini formatla.
- ⛔ `float`/`double` ile para. → `decimal`.
- ⛔ Disclaimer'sız analiz/hisse ekranı. → Her zaman görünür.
- ⛔ Dış API'ye fallback'siz güvenme. → Çökerse uygulama çökmesin.
- ⛔ Faz atlamak (örn. canlı fiyat olmadan LLM yorumuna koşmak). → Zincir: 0→1→2→3→4.
- ⛔ `UserId` ile kapsanmamış veri sorgusu (IDOR riski). → Her erişim kullanıcıya kapsanır (`11` §3).
- ⛔ Sırrı repoya/koda koymak. → env/secret (`11` §6).
- ⛔ Log'a sır/PII/token yazmak. → redaksiyon (`12` §3).
- ⛔ Cache'siz/bloklayan dış çağrı. → cache + async (`10` §3-4).

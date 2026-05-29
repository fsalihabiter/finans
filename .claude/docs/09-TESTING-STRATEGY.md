# 09 — Test Stratejisi (Testing Strategy)

> **Çekirdek kural:** *Senaryo-önce, test-yanında, yeşil olmadan "tamam" yok.*
> Her geliştirme görevi, kendi testleriyle **birlikte** hazırlanır; testler
> geçmeden görev tamamlanmış sayılmaz. Hesaplama (parasal) testleri **zorunlu**
> (NFR-1). Bu doküman testlerin **ne, nasıl, ne zaman** yazılacağını tanımlar.

İlgili: `06-DEV-PLAYBOOK.md` §4-5 (DoD), `CLAUDE.md` §11-12 (protokol),
`08-BACKLOG.md` (görev başına test eşlemesi), `01-NEEDS-ANALYSIS.md` (FR/NFR).

---

## 1. Felsefe & Test Türleri

İki kategori istiyoruz (kullanıcı talebi):

1. **Unit (birim) testler** — saf fonksiyon/servis davranışı, izole. Özellikle
   **hesaplama** (getiri, dağılım, kur, reel getiri, ort. maliyet).
2. **Olaylara yönelik (senaryo/davranış) testler** — bir **olay** veya
   **kullanıcı akışı** uçtan uca doğru sonuç veriyor mu? Örn:
   - Kullanıcı varlık ekler → portföy özeti doğru güncellenir.
   - Dış fiyat API'si çöker → uygulama çökmez, son bilinen fiyat + "yaklaşık".
   - LLM bozuk JSON döner → fallback devreye girer, kart şeması korunur.

### Test piramidi (bu yığın için)

```
        ▲  Senaryo / E2E akış  (az sayıda, kritik kullanıcı yolları)
       ▲▲  Integration         (endpoint + DB + dış servis mock/fallback)
      ▲▲▲  Unit                (çok sayıda, hızlı; hesap = zorunlu)
```

> Tabana yatır: çok sayıda hızlı birim testi + orta katman integration. E2E
> **az ama kritik** (Faz 2+'da gerçek araçla; Faz 1'de bileşen testi yeterli).

---

## 2. Backend Test Mimarisi (.NET / xUnit)

| Proje | Tür | Kapsam | Araç |
|-------|-----|--------|------|
| `Finans.Application.Tests` | Unit | `PortfolioCalculationService`, `CurrencyConversionService`, ort. maliyet türetimi, reel getiri, nudge kuralları, LLM parse/fallback | xUnit, FluentAssertions |
| `Finans.Integration.Tests` | Integration / Senaryo | HTTP endpoint + gerçek pipeline + DB; dış servis (fiyat/LLM) mock'lu olay testleri | xUnit + `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) |

**DB stratejisi (integration):**
- Faz 1: **SQLite in-memory** veya EF in-memory — hızlı, kurulumsuz.
- Faz 2+: **Testcontainers (PostgreSQL)** — prod ile birebir sadakat (kararı
  ihtiyaç doğunca al; erken optimizasyon yok).

**Dış servis (olay) izolasyonu:** Fiyat ve LLM istemcileri `IPriceProvider` /
`ILlmClient` arayüzleri arkasında (bkz. `02` §2.2, `07` §2). Testte bunların
mock'u verilir → "API çöktü", "JSON bozuldu" gibi **olaylar tetiklenebilir**.

### Kurulum (Faz 0 — test altyapısı)
```bash
# unit test projesi (zaten T0.2'de)
dotnet add backend/tests/Finans.Application.Tests package FluentAssertions
# integration/senaryo test projesi
dotnet new xunit -n Finans.Integration.Tests -o backend/tests/Finans.Integration.Tests
dotnet add backend/tests/Finans.Integration.Tests package Microsoft.AspNetCore.Mvc.Testing
dotnet add backend/tests/Finans.Integration.Tests package Microsoft.EntityFrameworkCore.Sqlite
# (Faz 2+) dotnet add ... package Testcontainers.PostgreSql
```

### Çalıştırma
```bash
dotnet test                              # tüm testler
dotnet test --filter Category=Calc       # sadece hesaplama (hızlı geri besleme)
```

---

## 3W. Web Test Mimarisi (ReactJS + Vite) — BİRİNCİL

| Tür | Kapsam | Araç |
|-----|--------|------|
| Unit | `@finans/shared` format/util (tr-TR), saf yardımcılar | **Vitest** |
| Bileşen | `HeroCard`/`AllocationDonut`/`HoldingsTable` render; `Disclaimer` Analiz'de **her zaman** var; kâr/zarar renk+işaret | **Vitest + React Testing Library** |
| E2E / akış | Tarayıcıda uçtan uca kullanıcı yolu (varlık ekle → özet güncellenir) | **Playwright** |

```bash
pnpm --filter ./web test          # Vitest (unit + bileşen)
pnpm --filter ./web test:e2e      # Playwright
```
> Web birincil yüzey olduğu için **olaylara yönelik testlerin UI ucu burada**
> (Playwright); iş mantığı/olayın backend ucu integration testlerinde.

## 3. Mobil Test Mimarisi (React Native) — FAZ M

| Tür | Kapsam | Araç | Faz |
|-----|--------|------|-----|
| Unit | `formatCurrency`/`formatPercent` ve saf yardımcılar | **Jest** | 0-1 |
| Bileşen | `HeroCard` özeti gösteriyor mu, `HoldingRow` trend rengi/oku, `Disclaimer` analiz ekranında **her zaman** var mı | **Jest + React Native Testing Library (RTL)** | 1 |
| E2E / akış | Gerçek cihaz/emülatör üzerinde uçtan uca kullanıcı yolu | **(ertelendi)** — Faz 2+'da Maestro önerilir | 2+ |

> **Karar:** Faz 1'de mobil tarafta **Jest + RTL** ile birim + bileşen testi.
> Gerçek E2E aracı (Maestro), ekran akışları oturduğunda (Faz 2+) eklenir.
> O zamana kadar "olaylara yönelik" doğrulamanın ağırlığı **backend integration
> senaryo testlerindedir** (asıl iş mantığı orada).

### Kurulum & çalıştırma
```bash
# Expo + jest-expo genelde hazır gelir; değilse:
npx expo install jest-expo jest @testing-library/react-native
npm test
```

---

## 4. Senaryo Formatı (Given-When-Then)

Her olay/akış testi bir senaryodan türer. Senaryolar **kod yazılmadan önce**
yazılır ve ilgili FR/NFR'ye bağlanır.

```
Senaryo SC-01 (FR-1.3): Varlık eklenince portföy özeti güncellenir
  Given: Boş portföy, baz para birimi TRY
  When : 40 gr altın, ort. 4.546 ₺/gr eklenir; güncel fiyat 6.500 ₺/gr
  Then : totalCost ≈ 181.851 ₺, currentValue = 260.000 ₺,
         netProfit ≈ +78.149 ₺, returnRatio ≈ 0,43
```

Bu senaryo **iki katmanda** test edilebilir: hesaplama kısmı **unit** (sadece
`PortfolioCalculationService`), uçtan uca kısım **integration** (`POST /holdings`
→ `GET /portfolio/summary`).

---

## 5. Senaryo Kataloğu (yaşayan liste)

> Geliştirme ilerledikçe büyür. Her satır: ID, ilgili gereksinim, kapsayan test
> seviyesi, durum. Yeni özellik = önce buraya senaryo ekle.

| SC | Gereksinim | Senaryo (özet) | Seviye | Durum |
|----|-----------|----------------|--------|-------|
| SC-01 | FR-1.3 | Altın kalemi eklenince değer/kâr/getiri doğru (40gr→260.000, +%43) | Unit + Integration | [ ] |
| SC-02 | FR-1.3 | Çok varlıklı portföyde dağılım % toplamı 1,0 (±yuvarlama) | Unit | [ ] |
| SC-03 | FR-1.4 | USD varlık TRY baz pb'ye güncel kurdan çevrilir | Unit + Integration | [ ] |
| SC-04 | FR-1.5 | BES'te devlet katkısı kendi katkısından **ayrı** dönüyor | Integration | [ ] |
| SC-05 | FR-1.6 | Reel getiri = (1+nominal)/(1+enflasyon)−1 doğru | Unit | [ ] |
| SC-06 | §03 §5 | Birden çok alış → ağırlıklı ort. maliyet doğru türeniyor | Unit | [ ] |
| SC-07 | FR-1.1/NFR-4 | Geçersiz girdi (miktar ≤ 0) → 400 + TR hata mesajı | Integration | [ ] |
| SC-08 | FR-2.5/NFR-5 | **Olay:** fiyat API'si çöker → son bilinen fiyat + `stale:true`, çökme yok | Integration | [ ] |
| SC-09 | FR-2.4 | Nakit oranı eşik üstü → ilgili nudge tetiklenir | Unit + Integration | [ ] |
| SC-10 | FR-3.2 | **Olay:** LLM bozuk JSON döner → fallback, 200, şema korunur | Unit + Integration | [ ] |
| SC-11 | FR-3.3/NFR-2 | Commentary çıktısı yasaklı yönlendirme ("al/sat/yükselir") içermez | Unit (filtre) | [ ] |
| SC-12 | FR-4.3 | Bilinmeyen sembol → anlamlı 404 hata | Integration | [ ] |
| SC-W1 | NFR-7 | `formatCurrency(641403)`="641.403 ₺", `formatPercent(0.516)`="%51,6" (`@finans/shared`) | Unit (Vitest) | [ ] |
| SC-W2 | NFR-2 | **Web** Analiz sayfası render'ında `Disclaimer` her zaman mevcut | Bileşen (RTL) | [ ] |
| SC-W3 | FR-1.1/1.3 | **Web E2E:** varlık ekle → portföy özeti güncellenir | E2E (Playwright) | [ ] |
| SC-M1 | NFR-2/7 | (Faz M) Mobil format + Analiz disclaimer pariteleri | Unit/Bileşen (mobil) | [ ] |
| **SC-13** | **NFR-12, `11`§3** | **Güvenlik:** Kullanıcı A, B'nin holding id'siyle istek atar → **404** (IDOR/BOLA yok). **Kimlik açılmadan zorunlu.** | Integration | [ ] |
| SC-14 | NFR-4, `11`§5 | **Güvenlik:** Eşik üstü istek → **429** (rate limit) | Integration | [ ] |
| SC-15 | NFR-4, `11`§4 | **Güvenlik:** Hata yanıtında stack trace / iç detay sızmıyor | Integration | [ ] |
| SC-16 | NFR-4 | **Güvenlik:** Token'sız/expired istek → 401 (Faz 5+) | Integration | [ ] |
| SC-P1 | NFR-6/10, `10`§2 | **Performans:** N eşzamanlı kullanıcı `summary` → p95 bütçe içinde (cache isabet/ıska) | Yük (k6/NBomber) | [ ] |

> Durum: `[ ]` yazılmadı · `[~]` test var, kod devam · `[x]` yeşil.

---

## 6. Görev Başına İş Akışı (her geliştirme adımı)

Bir backlog görevini (`T-x.y`) alırken:

```
1. SENARYO: İlgili FR/NFR'den senaryo(ları) §5 kataloğuna ekle (Given-When-Then).
2. TEST:    Senaryoyu test koduna dök (unit ve/veya integration).
            - Hesaplama içeren her görevde unit test ZORUNLU.
            - Bir "olay" (dış API/hata/akış) varsa integration/senaryo testi.
3. UYGULA:  Kodu yaz; testleri yeşile getir.
4. ÇALIŞTIR: dotnet test  /  npm test  → hepsi yeşil.
5. KAPAT:   Görev "tamam"; TASKLOG'a **Test** alanıyla işle (CLAUDE.md §11),
            08-BACKLOG'da [x], §5 kataloğunda senaryo [x].
```

> "Senaryo-önce" = adım 1-2 koddan önce başlar. Katı TDD (önce kırmızı test)
> şart değil; ama **görev, testleri yeşil olmadan kapanmaz** (yeşil-kapı).

---

## 7. Kapsam (Coverage) Beklentileri

| Alan | Hedef |
|------|-------|
| Hesaplama servisleri (`PortfolioCalculation`, `CurrencyConversion`, reel getiri, ort. maliyet) | **~%100 satır + kritik sınır durumları** (sıfır miktar, tek varlık, negatif kâr, kur eksik) |
| Endpoint'ler (CRUD + summary + commentary) | En az "mutlu yol" + 1 hata yolu integration |
| Dış servis olayları (fiyat/LLM hata) | Her fallback yolunun en az 1 testi (NFR-5, FR-3.2) |
| Mobil util/kritik bileşen | Format util %100; disclaimer & trend rengi gibi NFR'ye bağlı davranışlar |

> Sayısal kapsam yüzdesini fetiş yapma; **kritik para hesabı ve hata yolları**
> kapalı olsun. Yanlış rakam veya çöken dış servis = kabul edilemez.

---

## 8. Sınır Durumları Kontrol Listesi (hesaplama)

Her hesaplama testinde bunları düşün:
- Sıfır miktar / boş portföy (bölme-sıfır koruması).
- Tek varlık (%100 ağırlık).
- Negatif kâr (zarar) — renk/işaret doğru.
- Eksik kur / eksik fiyat → anlamlı davranış (hata değil çökme değil).
- `decimal` hassasiyeti — yuvarlama yalnızca gösterimde, hesap tam (NFR-1, NFR-7).
- Büyük sayılar (binlik ayraç, tabular hizalama gösterimde).

---

## 9. CI'ya Doğru (ileri not)

Git kurulunca (T0.1) basit bir doğrulama: commit/PR öncesi `dotnet test` +
`npm test` yeşil. İleride GitHub Actions ile otomatikleştirilebilir (Faz 2+).
Erken aşamada **lokal yeşil-kapı** disiplini yeterli.

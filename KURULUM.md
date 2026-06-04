# KURULUM — Finans / Nirengi'yi sıfırdan ayağa kaldırma

> Bu doküman repoyu **ilk kez klonlayan** birinin makinesini, projeyi çalıştırabilir
> hâle getirmesi için gereken her şeyi adım adım anlatır. Hedef: kopyala-yapıştırla
> 30-45 dk içinde hem **backend API**'yi hem **web ön yüzü**nü açabilmek.
>
> Mimariye ve kararlara değil **kuruluma** odaklıdır. Mimari için
> [`CLAUDE.md`](CLAUDE.md), kararlar için [`.claude/docs/`](.claude/docs/) klasörü.

---

## 0. İki kurulum yolu var — önce hangisini seçeceğine karar ver

Projeyi iki şekilde koşturabilirsin. **B'yi öneririz** (geliştirme için daha hızlı,
hot-reload var). A, ürünün "gerçek dağıtım kıyafetiyle" (TLS + reverse proxy +
rate limit) nasıl göründüğünü test etmek istediğinde lazım olur.

| | A) **Tam Docker yığını** | B) **Yerel dev (önerilen)** |
|---|---|---|
| Komut | `docker compose up --build` | `dotnet run` + `pnpm dev` |
| URL | `https://localhost` (Caddy/TLS) | `http://localhost:5173` (web) + arka uç ayrı |
| Postgres | Compose içindeki Postgres (yalıtık) | Makinene **kendin** kurarsın (5432) |
| Redis | Compose içinde (dağıtık cache) | Yok — in-memory cache'e düşer |
| TLS | Var (Caddy internal CA) | Yok — düz HTTP |
| Hot reload | Yok (web compose'da değil) | Var (Vite HMR + dotnet watch) |
| Ne zaman | "Üretim gibi" doğrulama, T2.9 kontrolü | Günlük geliştirme |

Her iki yol için de **0-3 arası bölümler ortak**. Ardından §4 (yol A) **veya**
§5-§7 (yol B) okunur.

---

## 1. Sistem gereksinimleri (programlar)

Aşağıdakiler **mutlaka** kurulu olmalı. Sürümler test edilmiş minimumlar.

| Yazılım | Sürüm | Niye? | Doğrulama |
|---------|-------|-------|-----------|
| **Git** | ≥ 2.40 | Repoyu klonlamak | `git --version` |
| **.NET SDK** | **10.0** | Backend (`Finans.Api`) | `dotnet --version` → `10.x.x` |
| **Node.js** | **≥ 20** (LTS) | Web ön yüz + monorepo | `node --version` |
| **pnpm** | **11.5.x** | Monorepo paket yöneticisi | `pnpm --version` |
| **PostgreSQL** | **17** | Veritabanı *(sadece yol B)* | `psql --version` |
| **Docker Desktop** | son sürüm | *(sadece yol A)* | `docker --version` + `docker compose version` |

Opsiyonel (önerilir):
- **VS Code** + uzantıları: `C#` (Microsoft), `ESLint`, `Prettier`, `Docker`.
- **Rider** veya **Visual Studio 2022/2025** (.NET için tercih edersen).

### 1.1 Windows için kurulum komutları (winget)

```powershell
winget install --id Git.Git -e
winget install --id Microsoft.DotNet.SDK.10 -e
winget install --id OpenJS.NodeJS.LTS -e          # Node 20+
winget install --id PostgreSQL.PostgreSQL.17 -e   # yalnız yol B
winget install --id Docker.DockerDesktop -e       # yalnız yol A

# pnpm — Node geldikten sonra (corepack ile):
corepack enable
corepack prepare pnpm@11.5.0 --activate
```

> PostgreSQL kurulumunda yükleyici bir **superuser parolası** isteyecek. Onu bir
> yere yaz (sonradan kendi `finans` kullanıcımızı oluştururken lazım olmaz ama
> `psql -U postgres` ile bağlanmak istersen gerekir).

### 1.2 macOS için (Homebrew)

```bash
brew install git node@20 pnpm
brew install --cask dotnet-sdk           # 10.x stream
brew install postgresql@17               # yalnız yol B  →  brew services start postgresql@17
brew install --cask docker               # yalnız yol A
```

### 1.3 Linux için

`.NET 10`: Microsoft'un resmi yönergesine bak
(`apt`/`dnf` üzerinden `dotnet-sdk-10.0`). Node 20: NodeSource veya `nvm`.
`pnpm`: `corepack enable && corepack prepare pnpm@11.5.0 --activate`.
Postgres 17: dağıtımın paket yöneticisinden. Docker: docker.io + docker-compose-plugin.

---

## 2. Repoyu klonla

```bash
git clone <repo-url> finans
cd finans
```

> Bu repo bir **monorepo**'dur (pnpm workspaces). Üst klasörde `package.json` +
> `pnpm-workspace.yaml`, alt çocukları `web/`, `packages/shared/`, `backend/`
> (backend ayrı — .NET tarafı). Bağımlılıkları **her klasörde ayrı ayrı değil,
> kökten bir kez** yüklersin.

---

## 3. Node bağımlılıkları (her iki yol için de gerekli)

Kökte:

```bash
pnpm install
```

Bu komut:
- Web (`web/`) ve `@finans/shared` paketinin bağımlılıklarını kurar
- `node_modules/` ve `pnpm-lock.yaml` ile reprodüktif kurulum sağlar

### 3.1 Playwright (yalnız e2e testleri için)

E2E testleri çalıştırmayacaksan bu adımı atla. Çalıştıracaksan:

```bash
pnpm --filter @finans/web exec playwright install --with-deps chromium
```

---

# Yol A — Tam Docker yığını

> "Hiç PostgreSQL kurmak istemiyorum, sadece tek komutla çalışsın." dersen bu yol.
> [Yol B](#yol-b--yerel-geliştirme-önerilen)'yi tercih edersen bu bölümü atla.

## 4. Docker compose ile her şeyi tek komutta kaldır

### 4.1 Parolayı ayarla (opsiyonel ama tavsiye edilir)

Repoda parola **yok** (güvenlik kuralı — `CLAUDE.md` §13). Compose dev varsayılanı
olarak `finans_dev` kullanır. Değiştirmek istersen kökte bir `.env` aç:

```bash
# .env  (repoya GİRMEZ — .gitignore'da)
POSTGRES_PASSWORD=benim_guclu_parolam
```

### 4.2 Yığını ayağa kaldır

```bash
docker compose up --build
```

İlk seferde 2-5 dk sürer (imajlar indirilir + API imajı derlenir). Şunlar olur:

1. `postgres:17-alpine` ayağa kalkar (iç ağda, dışarı kapalı)
2. `redis:7-alpine` ayağa kalkar (iç ağda, dağıtık cache)
3. `api` imajı derlenir (.NET 10 SDK → publish → alpine runtime, non-root)
4. API açılışta **migrate + seed** yapar (`Database__ApplyMigrationsOnStartup=true`)
5. `caddy:2-alpine` 80/443'ü dinler — TLS terminasyonu burada

### 4.3 Tarayıcıdan aç

- **https://localhost/health** → `{"status":"Healthy"}` döner
- **https://localhost/api/...** → API uçları (örn. `/api/holdings`)

> Tarayıcı **ilk açılışta "Bu siteye git" / "advanced → proceed"** uyarısı
> verecektir. Sebep: Caddy `tls internal` — kendi imzaladığı bir sertifika
> üretiyor. Localhost geliştirme için normal.

### 4.4 Web ön yüzünü Vite'tan koştur (compose'da yok)

Compose'da yalnız API + altyapı var. Web hâlâ `pnpm dev` ile koşar (hot reload için):

```bash
pnpm dev:web
```

`http://localhost:5173` açılır. Web, API'ye Caddy üzerinden (`https://localhost/api/...`)
gider — CORS izinleri compose env'inde önceden açık.

### 4.5 Yığını durdur / temizle

```bash
docker compose down              # konteynerleri kaldır (veriler durur)
docker compose down -v           # + volume'leri sil (Postgres verisi gider!)
```

---

# Yol B — Yerel geliştirme (önerilen)

> Günlük geliştirme akışı. PostgreSQL'i makinene tek sefer kuruyorsun, sonra
> `dotnet run` + `pnpm dev` ikilisiyle hot-reload alıyorsun.

## 5. PostgreSQL'i yerelde kur

### 5.1 PostgreSQL 17'yi başlat (eğer kurulumda otomatik açılmadıysa)

- **Windows:** `services.msc` → `postgresql-x64-17` → Başlat (kurulum otomatik aktif eder)
- **macOS:** `brew services start postgresql@17`
- **Linux:** `sudo systemctl start postgresql`

### 5.2 `finans` veritabanı + kullanıcı oluştur

`psql` ile süper kullanıcı olarak bağlan (Windows'ta "SQL Shell (psql)" başlat menüsünde):

```sql
-- bağlantı: psql -U postgres
CREATE USER finans WITH PASSWORD 'finans_dev';
CREATE DATABASE finans OWNER finans;
GRANT ALL PRIVILEGES ON DATABASE finans TO finans;
```

Doğrula:

```bash
psql -U finans -d finans -h localhost -c "SELECT version();"
```

> Parolayı değiştirebilirsin — sadece bir sonraki adımda User Secrets'a koyduğun
> parolayla aynı olmalı.

### 5.3 Parolayı **User Secrets**'a koy (REPOYA YAZMA)

`backend/src/Finans.Api/appsettings.json` içindeki bağlantı dizesi parolasız:
`"Postgres": "Host=localhost;Port=5432;Database=finans;Username=finans"`.
Parolayı kullanıcı sırlarıyla ekle:

```bash
cd backend/src/Finans.Api
dotnet user-secrets set "ConnectionStrings:Postgres" \
  "Host=localhost;Port=5432;Database=finans;Username=finans;Password=finans_dev"
cd ../../..
```

> User Secrets dosyası **ev dizininde** kalır (Windows:
> `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`) — repoda olmaz,
> commit edilmez. `UserSecretsId` projenin csproj'unda tanımlı.

## 6. Backend API'yi başlat

```bash
cd backend
dotnet restore
dotnet run --project src/Finans.Api
```

Açılışta (Development ortamında) şunlar olur:

1. EF Core **migration'ları uygular** (`Database__ApplyMigrationsOnStartup=true`)
2. Seed verisini ekler (Yatırımcı kullanıcısı + örnek holding'ler — idempotent)
3. Kestrel ayağa kalkar — varsayılan `http://localhost:5xxx` (Properties/launchSettings'e bak)

Doğrulama:

```bash
curl http://localhost:5xxx/health
# → {"status":"Healthy"}
```

> Yerel dev'de **Redis yok** — uygulama otomatik in-memory cache'e düşer. Bu
> beklenen davranış.

## 7. Web ön yüzünü başlat

Yeni bir terminalde:

```bash
pnpm dev:web
```

→ http://localhost:5173 açılır. Vite hot reload aktif; `web/src/...` değişikliklerini
anında görürsün. API'ye CORS izni `appsettings.Development.json`'da
`http://localhost:5173` için açık.

> 5173 doluysa Vite kendiliğinden 5174'e kayar — o da CORS allow-list'te.

---

## 8. Doğrulama — her şey ayakta mı?

| Kontrol | Komut / URL | Beklenen |
|---|---|---|
| Backend health | `curl http://localhost:5xxx/health` (B) veya `https://localhost/health` (A) | `Healthy` |
| Web açılıyor mu | Tarayıcı: `http://localhost:5173` | Nirengi giriş ekranı |
| API'den veri | Web'de "Portföy" sekmesi | Seed holding'leri listede |
| Test koşusu (backend) | `dotnet test` (`backend/` içinde) | 99/99 yeşil |
| Test koşusu (web) | `pnpm --filter @finans/web test` | tümü yeşil |

---

## 9. Sık karşılaşılan sorunlar

### "Port 5432 zaten kullanımda"
Yol A'yı koşturuyorsan ve aynı zamanda yerel Postgres'in de açıksa **çakışma yok**
— compose Postgres'i `expose` ediyor (dışarı port açmıyor). Eğer eski bir compose
dosyasından `ports: 5432:5432` kalmışsa kapat veya kaldır.

### "User Secrets is configured for missing project"
`dotnet user-secrets set ...` komutunu mutlaka `backend/src/Finans.Api/` içinden
veya `--project backend/src/Finans.Api` parametresiyle çalıştır.

### "EF migration error: relation already exists"
Veritabanı eski şema bırakmış olabilir. Geliştirme verisiyse sıfırla:
```sql
DROP DATABASE finans; CREATE DATABASE finans OWNER finans;
```
Sonra `dotnet run` tekrar migrate eder.

### Caddy/TLS uyarısı tarayıcıda inatla kalıyor
Bir kez "advanced → proceed" dedikten sonra tarayıcı hatırlar. HSTS cache'i
sıkıntı çıkarırsa `chrome://net-internals/#hsts` → `Delete domain security
policies` → `localhost`.

### Docker Desktop "WSL 2 backend" hatası (Windows)
`wsl --update && wsl --set-default-version 2`, sonra Docker Desktop'ı yeniden
başlat.

### pnpm "ERR_PNPM_UNSUPPORTED_ENGINE"
Node sürümün < 20'dir. `node --version` ile doğrula, yükselt.

### Web `pnpm dev` çalışıyor ama API çağrıları 404 / CORS
Backend ayakta mı? Yol B'deysen `dotnet run` koşuyor olmalı. Yol A'daysan
compose ayakta mı (`docker compose ps`)?

---

## 10. Ek: hangi dokümanı ne zaman okuyayım?

- [`CLAUDE.md`](CLAUDE.md) — proje vizyonu, en önemli kurallar (tavsiye değil!),
  hesap formülleri, mimari kararlar
- [`DESIGN.md`](DESIGN.md) — UI tasarım rehberi, renk / tipografi / token'lar
- [`ROADMAP.md`](ROADMAP.md) — faz planı, "şu an neredeyiz"
- [`.claude/docs/06-DEV-PLAYBOOK.md`](.claude/docs/06-DEV-PLAYBOOK.md) — günlük
  geliştirme ritmi, konvansiyonlar, DoD
- [`.claude/docs/03-DATA-MODEL.md`](.claude/docs/03-DATA-MODEL.md) — veri şeması
- [`.claude/docs/04-API-CONTRACT.md`](.claude/docs/04-API-CONTRACT.md) — API uçları
- [`.claude/docs/11-SECURITY.md`](.claude/docs/11-SECURITY.md) — güvenlik kuralları
  (per-user izolasyon, sırlar, rate limit)
- [`.claude/docs/13-WEB-FRONTEND.md`](.claude/docs/13-WEB-FRONTEND.md) — web özel

---

## 11. Hızlı başlangıç — TL;DR

**Hiç düşünmeden, yol B (yerel dev):**

```bash
# 1) prereq: .NET 10 SDK + Node 20+ + pnpm 11.5 + PostgreSQL 17
git clone <repo-url> finans && cd finans
pnpm install

# 2) DB
psql -U postgres -c "CREATE USER finans WITH PASSWORD 'finans_dev';"
psql -U postgres -c "CREATE DATABASE finans OWNER finans;"

# 3) parolayı User Secrets'a
dotnet user-secrets set "ConnectionStrings:Postgres" \
  "Host=localhost;Port=5432;Database=finans;Username=finans;Password=finans_dev" \
  --project backend/src/Finans.Api

# 4) iki terminal
dotnet run --project backend/src/Finans.Api      # terminal 1
pnpm dev:web                                       # terminal 2

# 5) tarayıcı: http://localhost:5173
```

**Tek komut (yol A, Docker):**

```bash
git clone <repo-url> finans && cd finans
pnpm install
docker compose up --build
pnpm dev:web      # web ayrı; https://localhost/api/* ile konuşur
# → https://localhost (Caddy TLS — bir kez "proceed" de)
```

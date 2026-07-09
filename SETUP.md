# SETUP — Getting Nirengi running from scratch

> This guide walks a **first-time cloner** through everything needed to run the
> project: both the **backend API** and the **web frontend**, in 30–45 minutes of
> copy-paste. It focuses on *setup*, not architecture — for architecture see
> [`CLAUDE.md`](CLAUDE.md) and the engineering docs in [`.claude/docs/`](.claude/docs/).

---

## 0. Two ways to run — pick one first

You can run the project two ways. **We recommend B** for development (faster,
hot reload). A is for when you want to see the product in its "real deployment
outfit" (TLS + reverse proxy + rate limiting + observability stack).

| | A) **Full Docker stack** | B) **Local dev (recommended)** |
|---|---|---|
| Command | `docker compose up --build` | `dotnet run` + `pnpm dev:web` |
| URL | `https://localhost` (Caddy/TLS) | `http://localhost:5173` (web) + API separate |
| PostgreSQL | Inside compose (isolated) | You install it **yourself** (port 5432) |
| Redis | Inside compose (distributed cache) | None — falls back to in-memory cache |
| Observability | Seq + Prometheus + Grafana included | Not started (optional) |
| TLS | Yes (Caddy internal CA) | No — plain HTTP |
| Hot reload | No (web isn't in compose) | Yes (Vite HMR + `dotnet watch`) |
| When | "Production-like" verification | Day-to-day development |

Sections **1–3 are common to both paths.** Then read §4 (path A) **or** §5–§7
(path B).

---

## 1. System requirements

The following must be installed. Versions are tested minimums.

| Software | Version | Why | Verify |
|----------|---------|-----|--------|
| **Git** | ≥ 2.40 | Cloning the repo | `git --version` |
| **.NET SDK** | **10.0** | Backend (`Finans.Api`) | `dotnet --version` → `10.x.x` |
| **Node.js** | **≥ 20** (LTS) | Web frontend + monorepo | `node --version` |
| **pnpm** | **11.5.x** | Monorepo package manager | `pnpm --version` |
| **PostgreSQL** | **17+** | Database *(path B only)* | `psql --version` |
| **Docker Desktop** | latest | *(path A only)* | `docker --version` + `docker compose version` |

Optional (recommended): **VS Code** with `C#`, `ESLint`, `Prettier`, `Docker`
extensions — or **Rider** / **Visual Studio 2022+** for the .NET side.

### 1.1 Windows (winget)

```powershell
winget install --id Git.Git -e
winget install --id Microsoft.DotNet.SDK.10 -e
winget install --id OpenJS.NodeJS.LTS -e          # Node 20+
winget install --id PostgreSQL.PostgreSQL.17 -e   # path B only
winget install --id Docker.DockerDesktop -e       # path A only

# pnpm — after Node is installed (via corepack):
corepack enable
corepack prepare pnpm@11.5.0 --activate
```

> The PostgreSQL installer will ask for a **superuser password**. Write it down —
> you'll need it to connect as `psql -U postgres` when creating our `finans` user.

### 1.2 macOS (Homebrew)

```bash
brew install git node@20 pnpm
brew install --cask dotnet-sdk           # 10.x stream
brew install postgresql@17               # path B only  →  brew services start postgresql@17
brew install --cask docker               # path A only
```

### 1.3 Linux

**.NET 10:** follow Microsoft's official instructions (`dotnet-sdk-10.0` via
`apt`/`dnf`). **Node 20:** NodeSource or `nvm`. **pnpm:**
`corepack enable && corepack prepare pnpm@11.5.0 --activate`.
**PostgreSQL 17:** your distro's package manager. **Docker:** docker.io +
docker-compose-plugin.

---

## 2. Clone the repo

```bash
git clone https://github.com/fsalihabiter/finans.git
cd finans
```

> This is a **monorepo** (pnpm workspaces): `package.json` + `pnpm-workspace.yaml`
> at the root, with `web/` and `packages/shared/` as workspace children.
> `backend/` is the separate .NET side. You install JS dependencies **once from
> the root**, not per folder.

---

## 3. Node dependencies (needed for both paths)

From the root:

```bash
pnpm install
```

This installs dependencies for the web app (`web/`) and the `@finans/shared`
package, reproducibly via `pnpm-lock.yaml`.

### 3.1 Playwright (e2e tests only)

Skip this unless you plan to run e2e tests:

```bash
pnpm --filter @finans/web exec playwright install --with-deps chromium
```

---

# Path A — Full Docker stack

> "I don't want to install PostgreSQL, just make it work with one command."
> If you prefer [path B](#path-b--local-development-recommended), skip this section.

## 4. Bring everything up with Docker compose

### 4.1 Set the password (optional but recommended)

There are **no passwords in the repo** (security rule — `CLAUDE.md` §13). Compose
uses `finans_dev` as the dev default. To change it, create a `.env` at the root:

```bash
# .env  (NOT committed — it's in .gitignore)
POSTGRES_PASSWORD=my_strong_password
```

### 4.2 Start the stack

```bash
docker compose up --build
```

The first run takes 2–5 minutes (images download + the API image builds). What happens:

1. `postgres:17-alpine` starts (internal network, not exposed)
2. `redis:7-alpine` starts (internal network, distributed cache)
3. The `api` image builds (.NET 10 SDK → publish → alpine runtime, non-root)
4. The API **migrates + seeds** on startup (`Database__ApplyMigrationsOnStartup=true`)
5. `caddy:2-alpine` listens on 80/443 — TLS terminates here
6. Observability comes up: **Seq** (logs) on `127.0.0.1:8081`, **Prometheus** on
   `127.0.0.1:9090`, **Grafana** on `127.0.0.1:3001` (provisioned dashboard;
   default login `admin`/`admin`) — all bound to localhost only

### 4.3 Open in the browser

- **https://localhost/health** → returns `Healthy`
- **https://localhost/api/...** → API endpoints (e.g. `/api/holdings`)

> Your browser will warn **"proceed to site" / "advanced → proceed"** on first
> visit. Reason: Caddy `tls internal` self-signs a certificate. Normal for
> localhost development.

### 4.4 Run the web frontend via Vite (not in compose)

Compose contains only the API + infrastructure. The web app still runs through
`pnpm` (for hot reload):

```bash
pnpm dev:web
```

Opens `http://localhost:5173`. The web app talks to the API through Caddy
(`https://localhost/api/...`) — CORS is pre-configured in the compose env.

### 4.5 Stop / clean up

```bash
docker compose down              # remove containers (data survives)
docker compose down -v           # + delete volumes (PostgreSQL data is gone!)
```

---

# Path B — Local development (recommended)

> The day-to-day flow. Install PostgreSQL once, then work with the
> `dotnet run` + `pnpm dev:web` pair and enjoy hot reload.

## 5. Install PostgreSQL locally

### 5.1 Start PostgreSQL 17+ (if the installer didn't auto-start it)

- **Windows:** `services.msc` → `postgresql-x64-17` → Start (installer usually enables it)
- **macOS:** `brew services start postgresql@17`
- **Linux:** `sudo systemctl start postgresql`

### 5.2 Create the `finans` database + user

Connect with `psql` as the superuser (on Windows: "SQL Shell (psql)" in the
Start menu):

```sql
-- connect: psql -U postgres
CREATE USER finans WITH PASSWORD 'finans_dev';
CREATE DATABASE finans OWNER finans;
GRANT ALL PRIVILEGES ON DATABASE finans TO finans;
```

Verify:

```bash
psql -U finans -d finans -h localhost -c "SELECT version();"
```

> You can pick any password — it just has to match what you put into User
> Secrets in the next step.

### 5.3 Put the password into **User Secrets** (NEVER into the repo)

The connection string in `backend/src/Finans.Api/appsettings.json` has no
password: `"Postgres": "Host=localhost;Port=5432;Database=finans;Username=finans"`.
Add the password via user secrets:

```bash
cd backend/src/Finans.Api
dotnet user-secrets set "ConnectionStrings:Postgres" \
  "Host=localhost;Port=5432;Database=finans;Username=finans;Password=finans_dev"
cd ../../..
```

> The User Secrets file lives in your **home directory** (Windows:
> `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`) — never in the
> repo, never committed. `UserSecretsId` is defined in the project's csproj.

## 6. Start the backend API

```bash
cd backend
dotnet restore
dotnet run --project src/Finans.Api
```

On startup (in the Development environment):

1. EF Core **applies migrations** (`Database__ApplyMigrationsOnStartup=true`)
2. Seed data is inserted (a demo user + sample holdings — idempotent)
3. Kestrel starts on **`http://localhost:5298`**

Verify:

```bash
curl http://localhost:5298/api/health
# → {"status":"ok"}
```

> There is **no Redis** in local dev — the app automatically falls back to an
> in-memory cache. This is expected behaviour.

## 7. Start the web frontend

In a new terminal:

```bash
pnpm dev:web
```

→ opens http://localhost:5173. Vite hot reload is active; changes under
`web/src/...` appear instantly. CORS for `http://localhost:5173` is allowed in
`appsettings.Development.json`.

> If 5173 is busy, Vite slides to 5174 automatically — that port is also on the
> CORS allow-list.

---

## 8. Optional: LLM commentary (Phase 3 feature)

The **Analysis** page asks an LLM to explain your portfolio's already-computed
numbers in plain educational language. This is **optional** — without an API key
the backend uses a no-op client and the page shows a graceful fallback card;
nothing crashes.

To enable it, pick a provider and set the config **outside the repo**:

**Path B (User Secrets):**

```bash
cd backend/src/Finans.Api

# Option 1 — Anthropic (paid, best quality):
dotnet user-secrets set "Llm:Provider" "Anthropic"
dotnet user-secrets set "Llm:ApiKey"   "sk-ant-..."

# Option 2 — OpenRouter (free-tier models available):
dotnet user-secrets set "Llm:Provider" "OpenRouter"
dotnet user-secrets set "Llm:BaseUrl"  "https://openrouter.ai/api/"
dotnet user-secrets set "Llm:Model"    "qwen/qwen3-next-80b-a3b-instruct:free"
dotnet user-secrets set "Llm:ApiKey"   "sk-or-v1-..."
```

**Path A (compose):** set `LLM_PROVIDER`, `LLM_API_KEY`, `LLM_MODEL`,
`LLM_BASE_URL` in your root `.env` (see [`.env.example`](.env.example)).

> Free OpenRouter models are rate-limited and can be congested (HTTP 429) — the
> app then serves its last successful commentary or a fallback card by design.

---

## 9. Verification — is everything up?

| Check | Command / URL | Expected |
|---|---|---|
| Backend health | `curl http://localhost:5298/api/health` (B) or `https://localhost/health` (A) | healthy |
| Web loads | Browser: `http://localhost:5173` | Nirengi dashboard |
| Data from API | "Genel Bakış" (Overview) page | Seeded holdings in the table |
| Backend tests | `dotnet test` (inside `backend/`) | 246 green (156 unit + 90 integration) |
| Web tests | `pnpm --filter @finans/web test` | all green |

---

## 10. Troubleshooting

### "Port 5432 already in use"
If you run path A while a local PostgreSQL is also running, there is **no
conflict** — compose only `expose`s PostgreSQL internally (no host port). If an
old compose file left a `ports: 5432:5432` mapping, remove it.

### "User Secrets is configured for missing project"
Run `dotnet user-secrets set ...` from inside `backend/src/Finans.Api/`, or pass
`--project backend/src/Finans.Api`.

### "EF migration error: relation already exists"
The database may contain an old schema. If it's disposable dev data, reset it:
```sql
DROP DATABASE finans; CREATE DATABASE finans OWNER finans;
```
Then `dotnet run` migrates again.

### The Caddy/TLS warning won't go away
After one "advanced → proceed" the browser remembers. If the HSTS cache
misbehaves: `chrome://net-internals/#hsts` → `Delete domain security policies`
→ `localhost`.

### Docker Desktop "WSL 2 backend" error (Windows)
`wsl --update && wsl --set-default-version 2`, then restart Docker Desktop.

### pnpm "ERR_PNPM_UNSUPPORTED_ENGINE"
Your Node version is < 20. Check with `node --version` and upgrade.

### Web runs but API calls return 404 / CORS errors
Is the backend up? On path B, `dotnet run` must be running. On path A, is
compose up (`docker compose ps`)?

### Analysis page only shows a fallback card
That's the LLM layer degrading gracefully — either no API key is configured
(see §8) or the free-tier model is congested/rate-limited (HTTP 429). The
portfolio numbers are unaffected; they never come from the LLM.

---

## 11. Appendix: which doc do I read when?

- [`README.md`](README.md) — what the project is, features, architecture overview
- [`CLAUDE.md`](CLAUDE.md) — project vision, the cardinal rules (not advice!),
  calculation formulas, architectural decisions
- [`DESIGN.md`](DESIGN.md) — UI design guide: colors / typography / tokens
- [`ROADMAP.md`](ROADMAP.md) — phase plan, "where are we now"
- [`.claude/docs/06-DEV-PLAYBOOK.md`](.claude/docs/06-DEV-PLAYBOOK.md) — daily
  development rhythm, conventions, Definition of Done
- [`.claude/docs/03-DATA-MODEL.md`](.claude/docs/03-DATA-MODEL.md) — data schema
- [`.claude/docs/04-API-CONTRACT.md`](.claude/docs/04-API-CONTRACT.md) — API endpoints
- [`.claude/docs/11-SECURITY.md`](.claude/docs/11-SECURITY.md) — security rules
  (per-user isolation, secrets, rate limiting)
- [`.claude/docs/13-WEB-FRONTEND.md`](.claude/docs/13-WEB-FRONTEND.md) — web specifics

> Engineering docs under `.claude/docs/` are currently in Turkish; an English
> migration is planned.

---

## 12. Quick start — TL;DR

**No thinking, path B (local dev):**

```bash
# 1) prereqs: .NET 10 SDK + Node 20+ + pnpm 11.5 + PostgreSQL 17+
git clone https://github.com/fsalihabiter/finans.git && cd finans
pnpm install

# 2) DB
psql -U postgres -c "CREATE USER finans WITH PASSWORD 'finans_dev';"
psql -U postgres -c "CREATE DATABASE finans OWNER finans;"

# 3) password into User Secrets
dotnet user-secrets set "ConnectionStrings:Postgres" \
  "Host=localhost;Port=5432;Database=finans;Username=finans;Password=finans_dev" \
  --project backend/src/Finans.Api

# 4) two terminals
dotnet run --project backend/src/Finans.Api      # terminal 1  → :5298
pnpm dev:web                                     # terminal 2  → :5173

# 5) browser: http://localhost:5173
```

**One command (path A, Docker):**

```bash
git clone https://github.com/fsalihabiter/finans.git && cd finans
pnpm install
docker compose up --build
pnpm dev:web      # web is separate; talks to https://localhost/api/*
# → https://localhost (Caddy TLS — click "proceed" once)
```

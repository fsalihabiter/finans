# SETUP — Run Nirengi with Docker

> **The whole stack runs from a single Docker command.** The API, the web UI,
> the PostgreSQL database, the Redis cache, the TLS reverse proxy and the
> monitoring tools are all built and started inside Docker containers.
>
> **You do NOT need to install any of these on your machine:**
> .NET, Node.js, pnpm, **PostgreSQL**, Redis. Do not try to install PostgreSQL
> separately — the database ships inside the Compose stack, runs on an internal
> network, and is created + migrated + seeded automatically on first start.
> Installing your own PostgreSQL is only relevant for the *hot-reload code
> workflow* described in
> [`.claude/docs/06-DEV-PLAYBOOK.md`](.claude/docs/06-DEV-PLAYBOOK.md), not for
> simply running the app.

---

## 1. Requirements

The only two things you need on your machine:

| Software | Minimum version | Verify with |
|----------|-----------------|-------------|
| **Git** | 2.40 | `git --version` |
| **Docker Desktop** | 4.30 (includes Compose v2) | `docker --version` and `docker compose version` |

### Installing (Windows, PowerShell)

```powershell
winget install --id Git.Git -e
winget install --id Docker.DockerDesktop -e
```

- After installing Docker Desktop, **start it once from the Start menu** and let
  it finish its WSL2 setup. The whale icon in the system tray must be running
  (green) before you continue.
- macOS: `brew install git; brew install --cask docker` · Linux: your distro's
  `git` + `docker` + `docker-compose-plugin` packages.

> **Why no PostgreSQL install?** The `postgres` service in
> [`docker-compose.yml`](docker-compose.yml) uses the official
> `postgres:18-alpine` image. On the first `docker compose up` the API runs its
> migrations (`Database__ApplyMigrationsOnStartup: true`) and seeds demo data
> (`Database__Seed: true`) into that container's database. The data lives in a
> Docker volume (`postgres-data`) and survives restarts. Nothing touches a
> PostgreSQL on your host.

---

## 2. Get the code and configure

```powershell
git clone https://github.com/fsalihabiter/finans.git
cd finans
Copy-Item .env.example .env
```

`.env` holds all local secrets and is **git-ignored** — no secret ever goes into
the repo. It is **optional**: the app runs with the built-in dev defaults even if
you leave `.env` untouched. See **Section 3** for exactly what each value does
and whether you need it.

---

## 3. Secrets & configuration — the `.env` file

**Every password and API key lives in one place: the `.env` file at the project
root.** [`docker-compose.yml`](docker-compose.yml) reads these variables and
injects them into the containers. Edit `.env` in any text editor.

| Variable | Required? | Default | What it controls / what to put |
|----------|-----------|---------|--------------------------------|
| `POSTGRES_PASSWORD` | **Yes** | `finans_dev` | Password for the in-Docker PostgreSQL. The default works out of the box; change it if you like — it is only used internally between containers. |
| `LLM_API_KEY` | No (optional) | placeholder | API key for the **Analysis** page (AI-written commentary). While it is a placeholder or empty, the app still works — Analysis falls back to rule-based cards. Put a real key to enable AI commentary. See below. |
| `LLM_PROVIDER` / `LLM_MODEL` / `LLM_BASE_URL` | No | Anthropic / Haiku | Which AI provider and model the Analysis page uses. Change these together with `LLM_API_KEY`. See below. |
| `FINNHUB_API_KEY` | No (optional) | empty | Data source for the **Stock Analysis** page (US stocks). Empty → that page says "source not configured"; the rest of the app is unaffected. Free key: <https://finnhub.io/register>. |
| `GRAFANA_ADMIN_PASSWORD` | No | `admin` | Login password for the Grafana monitoring dashboard. Commented out by default. |

### 3.1 Enabling the AI Analysis page (optional)

The Analysis page writes educational commentary with an LLM. **You can skip this
entirely** — without a key the page shows deterministic, rule-based cards and the
rest of the app is fully functional. To turn on AI commentary, pick one option
and edit `.env`:

**Option A — Free (OpenRouter).** Get a free key at
<https://openrouter.ai/keys>, then set:

```dotenv
LLM_PROVIDER=OpenRouter
LLM_API_KEY=sk-or-v1-YOUR_OWN_KEY
LLM_MODEL=nvidia/nemotron-3-super-120b-a12b:free
LLM_BASE_URL=https://openrouter.ai/api/
```

> Quality on the free model is variable (occasional language leakage or a missing
> card); deterministic guards filter the worst, but full coverage isn't
> guaranteed.

**Option B — Anthropic (recommended, paid but ~1–2 cents per generation).**
Replace the placeholder key with your real Anthropic key and keep the defaults:

```dotenv
LLM_PROVIDER=Anthropic
LLM_API_KEY=sk-ant-YOUR_OWN_KEY
LLM_MODEL=claude-haiku-4-5-20251001
LLM_BASE_URL=https://api.anthropic.com/
```

> After changing `.env`, you only need to restart the API to pick up the new
> values: `docker compose up -d api`.

---

## 4. Bring it up

From the project root (`finans/`):

```powershell
docker compose up -d --build
```

The first run takes **2–5 minutes** (images download, then the API and the web
UI are compiled *inside* Docker). Subsequent starts take seconds.

Then open **https://localhost**

> ⚠️ On first visit the browser shows a certificate warning
> (*"Your connection is not private"*). This is expected: the local reverse
> proxy (Caddy) signs its own certificate with an internal CA. Click
> **Advanced → Proceed to localhost** once; it won't ask again.

Quick health checks:

```powershell
curl.exe -k https://localhost/health        # → Healthy
docker compose ps                           # all services Up / healthy
```

---

## 5. Everyday commands

| I want to… | PowerShell |
|---|---|
| Stop everything (data survives) | `docker compose down` |
| Start again | `docker compose up -d` |
| Apply an `.env` change (e.g. new API key) | `docker compose up -d api` |
| Update to the latest code | `git pull; docker compose up -d --build` |
| Rebuild after changing web/backend code | `docker compose up -d --build` |
| Watch API logs | `docker compose logs -f api` |
| **Wipe all data** and start fresh | `docker compose down -v` ⚠️ deletes the database |

---

## 6. What's running

| Service | Where | Notes |
|---|---|---|
| Web app + API | **https://localhost** | Caddy serves the UI and proxies `/api` |
| PostgreSQL 18 | internal only | not reachable from the host; data in the `postgres-data` volume |
| Redis | internal only | cache |
| Seq (logs) | http://localhost:8081 | admin panels bind to localhost only |
| Prometheus (metrics) | http://localhost:9090 | |
| Grafana (dashboard) | http://localhost:3001 | login `admin` / `admin` (or `GRAFANA_ADMIN_PASSWORD`) |

**Ports used by this project:** `80` and `443` (Caddy), `8081` (Seq), `9090`
(Prometheus), `3001` (Grafana). If you run other Docker projects, make sure these
five ports are free — see the troubleshooting row below for how to fix a clash.

---

## 7. Troubleshooting

| Symptom | Fix |
|---|---|
| `docker: command not found` / pipe errors | Docker Desktop isn't running — start it and wait for the green whale |
| `ports are not available: 80/443` (or 8081/9090/3001) | Another program or container uses that port (IIS, Skype, another proxy/project). Stop it, or change the host port on the left side of the mapping in [`docker-compose.yml`](docker-compose.yml) (e.g. `"8080:80"` for Caddy) |
| Analysis page says commentary is rule-based | `LLM_API_KEY` is missing/placeholder/invalid in `.env`, or the free model is rate-limited — set a working key/model (Section 3.1), then `docker compose up -d api` |
| Analysis still rule-based **with** a valid key; API logs show `PartialChain` / `certificate verify failed` / `SSL connection could not be established` | You are behind a corporate proxy that inspects TLS — the container doesn't trust the proxy's root CA. See **Section 8**. |
| `.env` has two `LLM_*` blocks / key doesn't match provider | An `sk-ant-...` key needs `LLM_PROVIDER=Anthropic`; an `sk-or-v1-...` key needs `LLM_PROVIDER=OpenRouter`. Keep exactly one block (Section 3.1). |
| Stock Analysis says "source not configured" | `FINNHUB_API_KEY` is empty in `.env` — add a free key, then `docker compose up -d api` |
| I changed web/backend code but see no difference | Images are built once — rebuild: `docker compose up -d --build` |
| Browser can't reach https://localhost | `docker compose ps` — is `caddy` Up? If not: `docker compose logs caddy` |
| Want to start completely clean | `docker compose down -v` then `docker compose up -d --build` (⚠️ deletes seeded/entered data) |

---

## 8. Behind a corporate proxy (TLS interception)

If your machine is on a corporate/enterprise network that **inspects HTTPS
traffic** (a "TLS-intercepting" or "SSL inspection" proxy — common in banks,
government, large companies), the API container cannot reach external services
(the LLM provider, Finnhub). The proxy re-signs every HTTPS connection with the
company's own root CA, and the container doesn't trust that CA. Symptoms:

- The **Analysis** page stays on rule-based cards even with a valid key.
- `docker compose logs api` shows
  `The remote certificate is invalid because of errors in the certificate chain: PartialChain`
  or `certificate verify failed`.

The fix is to (a) export the corporate root **and intermediate** CAs from your
machine's trust store into a PEM bundle, and (b) tell the container to trust it.
Both artifacts are **git-ignored** (machine/network-specific — they never go into
the repo).

### 8.1 Export your corporate CA bundle (Windows, PowerShell)

Dump the whole local trust store to `compose/certs/gumruk-ca.crt`. Exporting the
*entire* store guarantees the full interception chain (root + any intermediates)
is covered — you don't have to guess which CA the proxy uses:

```powershell
mkdir compose\certs -Force
$stores = 'Cert:\LocalMachine\Root','Cert:\LocalMachine\CA','Cert:\CurrentUser\Root','Cert:\CurrentUser\CA'
$seen = @{}; $lines = New-Object System.Collections.Generic.List[string]
foreach ($s in $stores) {
  Get-ChildItem $s -ErrorAction SilentlyContinue | ForEach-Object {
    if (-not $seen.ContainsKey($_.Thumbprint)) {
      $seen[$_.Thumbprint] = $true
      $b = [Convert]::ToBase64String($_.RawData,'InsertLineBreaks')
      $lines.Add('-----BEGIN CERTIFICATE-----'); $lines.Add($b); $lines.Add('-----END CERTIFICATE-----')
    }
  }
}
Set-Content compose\certs\gumruk-ca.crt $lines -Encoding ascii
```

> A ready-to-run copy of this script lives at `compose/certs/export-ca.ps1`
> (also git-ignored). Re-run it whenever the corporate CA rotates.

### 8.2 Tell the container to trust it

Create a **`docker-compose.override.yml`** at the repo root (Compose loads it
automatically on top of `docker-compose.yml`; it is git-ignored). It mounts the
bundle and merges it with the container's system CAs via `SSL_CERT_FILE` — no
image change, no root needed:

```yaml
services:
  api:
    volumes:
      - ./compose/certs:/certs:ro
    entrypoint:
      - /bin/sh
      - -c
      - >-
        cat /etc/ssl/certs/ca-certificates.crt /certs/gumruk-ca.crt > /tmp/ca-bundle.crt &&
        export SSL_CERT_FILE=/tmp/ca-bundle.crt &&
        exec dotnet Finans.Api.dll
```

Then force-recreate the API so it picks up the mount:

```powershell
docker compose up -d --force-recreate api
```

Verify (the count should be *system certs + your corporate certs*, and a real AI
call should no longer log `PartialChain`):

```powershell
docker compose exec api sh -c "grep -c 'BEGIN CERTIFICATE' /tmp/ca-bundle.crt"
curl.exe -sk https://localhost/api/portfolio/commentary   # real cards, not #fallback
```

> **Note on the proxy itself:** on this setup Docker Desktop already routes the
> container's traffic through the network's transparent proxy, so no
> `HTTP_PROXY`/`HTTPS_PROXY` variables are needed — only CA trust. If your
> container cannot make *any* outbound connection, add your proxy to the `api`
> service's environment (put the proxy URL with its credentials in `.env`, never
> in the committed compose file), e.g.
> `HTTPS_PROXY: ${CORP_PROXY}` / `NO_PROXY: "postgres,redis,seq,localhost"`.

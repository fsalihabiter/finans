# SETUP — Run Nirengi with Docker

> Everything — API, web UI, database, cache, reverse proxy, monitoring — runs
> from a **single Docker command**. You do not need .NET, Node.js, pnpm or
> PostgreSQL on your machine.
>
> *(Contributing code? The hot-reload development workflow lives in
> [`.claude/docs/06-DEV-PLAYBOOK.md`](.claude/docs/06-DEV-PLAYBOOK.md).)*

---

## 1. Requirements

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

---

## 2. Get the code and configure

```powershell
git clone https://github.com/fsalihabiter/finans.git
cd finans
Copy-Item .env.example .env
```

`.env` is optional but recommended (it is git-ignored; no secrets ever go into
the repo). Open it in any editor:

- `POSTGRES_PASSWORD` — database password. The dev default works out of the box.
- `LLM_API_KEY` — only needed for the **Analysis** page (AI commentary). Get a
  free key at [openrouter.ai](https://openrouter.ai/keys). Without a key the app
  still works; Analysis falls back to rule-based cards. A model known to work
  on the free tier: `LLM_MODEL=nvidia/nemotron-3-super-120b-a12b:free`.

---

## 3. Bring it up

```powershell
docker compose up -d --build
```

The first run takes **2–5 minutes** (images download, then the API and the web
UI are compiled *inside* Docker). Subsequent starts take seconds.

Then open **https://localhost**

> ⚠️ On first visit the browser shows a certificate warning
> (*"Your connection is not private"*). This is expected: the local reverse
> proxy signs its own certificate. Click **Advanced → Proceed to localhost**
> once; it won't ask again.

Quick health checks:

```powershell
curl.exe -k https://localhost/health        # → Healthy
docker compose ps                           # all services Up / healthy
```

---

## 4. Everyday commands

| I want to… | PowerShell |
|---|---|
| Stop everything (data survives) | `docker compose down` |
| Start again | `docker compose up -d` |
| Update to the latest code | `git pull; docker compose up -d --build` |
| Watch API logs | `docker compose logs -f api` |
| **Wipe all data** and start fresh | `docker compose down -v` ⚠️ deletes the database |

---

## 5. What's running

| Service | Where | Notes |
|---|---|---|
| Web app + API | **https://localhost** | Caddy serves the UI and proxies `/api` |
| PostgreSQL 18 | internal only | not reachable from the host |
| Redis | internal only | cache |
| Seq (logs) | http://localhost:8081 | admin panels bind to localhost only |
| Prometheus (metrics) | http://localhost:9090 | |
| Grafana (dashboard) | http://localhost:3001 | login `admin` / `admin` |

---

## 6. Troubleshooting

| Symptom | Fix |
|---|---|
| `docker: command not found` / pipe errors | Docker Desktop isn't running — start it and wait for the green whale |
| `ports are not available: 80/443` | Another program uses port 80/443 (IIS, Skype, another proxy). Stop it, or change Caddy's ports in `docker-compose.yml` |
| Analysis page says commentary is rule-based | `LLM_API_KEY` missing/invalid in `.env`, or the free model is rate-limited — set a working key/model, then `docker compose up -d api` |
| I changed web/backend code but see no difference | Images are built once — rebuild: `docker compose up -d --build` |
| Browser can't reach https://localhost | `docker compose ps` — is `caddy` Up? If not: `docker compose logs caddy` |

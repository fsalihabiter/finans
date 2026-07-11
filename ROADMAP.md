# ROADMAP.md — Detailed Phase Plan

> This file is the detailed version of the summary roadmap in `CLAUDE.md` § 4.
> For each phase: goal, tasks (backend / mobile / other), tools to use,
> deliverable and **Definition of Done**.
> Principle: **every phase must end with something that works.**

> 🔄 **UPDATE — Frontend order: WEB FIRST.** The project is now a monorepo
> (`web/`, `mobile/`, `backend/`, `packages/shared/`). The primary surface is the
> **ReactJS + Vite web app** (`.claude/docs/13-WEB-FRONTEND.md`). Read every
> **"Mobile tasks"** section below as **"Frontend tasks — WEB first"**; each
> screen/flow is built on the web first. **Mobile (React Native)** is added later
> as a **separate branch of work** on top of the same API and the
> `@finans/shared` package (see "PHASE M"), once the web side has settled.
> The current actionable task list lives in `.claude/docs/08-BACKLOG.md`.

---

## Overview

| Phase | Name | Estimated Duration | Outcome | Status |
|-------|------|--------------------|---------|:------:|
| 0 | Preparation & Skeleton | ~1 week | Two empty-but-running projects + schema | ✅ done |
| 1 | Portfolio Tracking MVP | ~2–4 weeks | Working portfolio with manual data (a standalone product) | ✅ done |
| 2 | Live Prices & Notes | ~2–3 weeks | Auto-updated values + educational notes | ✅ done |
| 3 | LLM Commentary Layer | ~2–3 weeks | Analysis that explains the portfolio in educational language | ✅ done |
| 4 | Stock Fundamentals | Depends on data source | Fetch metrics + let the LLM explain them | 🚧 in progress |
| 5 | Value History & Scenario v1 | ~2–3 weeks | Real value-over-time chart + backward-looking scenario (strategy **Wave 1**, C1) | 🔜 |
| 6 | Education MVP + Glossary | ~3–4 weeks | "Learn with your portfolio" lessons + concept glossary (strategy **Wave 1**, A1/A4 — the heart of the vision) | 🔜 |
| 7 | Personalization & Reach | ~2–4 months | Onboarding/literacy level, accounts, PWA, notifications, TEFAS funds, gold module, guest demo (strategy **Wave 2**) | 🔜 |
| 8 | Scale & Impact | open-ended | Behavior mirror, inflation panel, mobile (Phase M), revenue model + **legal sign-off** (strategy **Wave 3**) | 🔜 |

**Dependency chain:** 0 → 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8. Phases 5–8 implement
the product strategy in `.claude/docs/14-PRODUCT-STRATEGY.md` (positioning:
*"Nirengi doesn't tell you what to buy; it teaches you to read the map"* —
financial literacy through the user's own real portfolio). Task breakdown:
`.claude/docs/08-BACKLOG.md`.

> ⚠️ **The "not advice" rule:** before starting Phases 3 and 4, re-read
> `CLAUDE.md` § 2. The output of these phases must never say "buy / sell /
> it will go up."

---

## PHASE 0 — Preparation & Skeleton (~1 week) ✅

**Goal:** Get both projects standing, finalize the database schema, and warm up
to the frontend stack. Less about writing code, more about laying the pipeline.

### Backend tasks
- Create the .NET Web API project (empty skeleton).
- Set up the layering: `API` (controllers) / `Application` (services/business
  logic) / `Domain` (entities) / `Infrastructure` (data access).
- Choose and wire an ORM (Entity Framework Core recommended).
- **Design the database schema and create the migration** (draft below).
- A single test endpoint: `GET /api/health` → `{ "status": "ok" }`.

### Mobile tasks (learning-heavy)
- Set up the React Native environment (Expo is the easiest start for a beginner).
- Build a mini playground app:
  1. Navigation between two-three screens.
  2. Fetching data from the .NET `/api/health` endpoint and showing it.
  3. Rendering a list (FlatList).
- Create the theme token file from `DESIGN.md` § 6 (`theme/colors.ts`).

### Other
- Git repository + `.gitignore` (build outputs, secrets excluded).
- A secret-management plan for API keys (no keys yet, but the slot is ready).

### Database schema draft (finalize)
```
Users(Id, Email, BaseCurrency, CreatedAt)
Assets(Id, Type, Name, Symbol, Currency)         -- Gold, USD, Stock...
Holdings(Id, UserId, AssetId, Quantity, AvgCost)  -- or derive from Transactions
Transactions(Id, HoldingId, Type, Quantity, UnitPrice, Date) -- Buy/Sell
PriceSnapshots(Id, AssetId, Price, Date)          -- for real returns & scenarios
BesDetails(Id, HoldingId, OwnContribution, StateContribution, JoinedAt, VestingState)
```
> **Decision to make:** should average cost live in `Holdings.AvgCost`, or be
> computed from `Transactions`? (Deriving from transactions is more correct but
> slightly more work.) Money fields are **`decimal`**.
> *(Resolved: positions are derived from transactions at read time.)*

### Deliverable
A running (empty) .NET API + a mobile playground app + an approved schema/migration.

### ✅ Definition of Done
- The frontend fetches `/api/health` and displays the result.
- The database is created via `dotnet ef migrations`.
- The theme token file exists and is used on at least one screen.

---

## PHASE 1 — Portfolio Tracking MVP (~2–4 weeks) ✅

**Goal:** A working portfolio from manually entered assets, with **every numeric
calculation done correctly**. This phase is a usable product on its own.

### Backend tasks
- **Calculation service** (`PortfolioCalculationService`) — the formulas from
  `CLAUDE.md` § 6: per-holding return, net profit, weighted average cost,
  allocation %, real return, multi-currency → base-currency conversion.
- CRUD endpoints:
  ```
  POST   /api/holdings           add an asset
  GET    /api/holdings           the user's assets
  PUT    /api/holdings/{id}      update
  DELETE /api/holdings/{id}      delete
  GET    /api/portfolio/summary  total value, cost, profit, return, allocation
  ```
- Treat BES (Turkish private pension) specially: own contribution and state
  contribution as **separate lines/fields**.
- **Unit tests** — for the calculation functions (a wrong number is
  unacceptable). Use the numbers from the original spreadsheet
  (cost 422,970; value 641,403) as test data.

### Mobile tasks
- Portfolio summary screen: hero card (value/profit/return) — as in the mockup.
- Allocation chart: `react-native-svg` or `react-native-gifted-charts`
  (`DESIGN.md` § 6 — there is no `conic-gradient` in RN).
- Asset list (FlatList) + tap-through to an **asset detail** screen.
- **Add asset** form (type, currency, quantity, cost, date) → POST.
- Base-currency selection (settings or first launch).

### Other
- In this phase **prices are entered manually** (live prices arrive in Phase 2),
  i.e. the user maintains the "current price" field themselves.

### Deliverable
A full-flow portfolio: manual data in, correct math, visible on screen.

### ✅ Definition of Done
- The user can add/delete assets and see their portfolio.
- Total value, profit, return %, allocation % are **calculated correctly**
  (proven by tests).
- Multiple currencies convert correctly into the base currency.
- The BES state contribution is displayed separately.

---

## PHASE 2 — Live Prices & Educational Notes (~2–3 weeks) ✅

**Goal:** Fetch the "current price" automatically instead of typing it in +
put small context-aware educational notes on the portfolio screen.

### Backend tasks
- Price provider integration (free-tier APIs for gold and FX).
- `PriceFetchService` — pull prices from external APIs, write to `PriceSnapshots`.
- Caching — don't hammer the external API on every request (e.g. 5–15 min).
- Endpoint: `GET /api/prices?symbols=XAU,USD` or embedded in the summary.
- **Nudge engine — simple rules:** e.g. `cash ratio > X%` → "cash erodes under
  inflation" note. **Rule-based is enough** for this phase; the LLM joins in
  Phase 3.

### Mobile tasks
- Summary-screen values now come from live prices (pull-to-refresh).
- Show the mockup's "nudge" card contextually (based on the triggered rule).
- Last-updated time / price source info.

### Risks
- Free APIs may have request limits → caching is mandatory.
- Price data may be delayed/missing → show an "approximate" label.

### ✅ Definition of Done
- Current values arrive automatically from an external source and can refresh.
- At least one context-aware educational note triggers correctly.
- If the external API goes down the app doesn't break (fallback: last known price).

---

## PHASE 3 — LLM Commentary Layer (~2–3 weeks) ✅

**Goal:** Hand the numbers computed by .NET to an LLM and have it **explain the
portfolio in educational language.** This is the project's "sense-making" layer.

> ⚠️ Math in CODE, commentary in the LLM. Never give the LLM raw numbers and
> say "calculate".

### Backend tasks
- Choose an LLM provider (criteria: Turkish quality + structured output + cost).
- `LlmCommentaryService`:
  - Input: the **ready-made numbers** computed by .NET (allocation, returns,
    real return, concentration ratios).
  - System prompt: "You are a finance **educator**, not an advisor. Do not give
    advice; explain and build awareness. Return output in this JSON schema: ..."
  - Output: **structured JSON** (card by card: title, body, tags).
- Safe parsing + fallback (broken JSON → plain text / previous commentary).
- Endpoint: `GET /api/portfolio/commentary`.
- Cost control: don't generate on every view — regenerate when the portfolio
  changes or once a day, and cache it.

### Mobile tasks
- The Analysis tab (as in the mockup): overall health, concentration, real
  return, scenario cards — now coming from the LLM.
- A **"not investment advice"** disclaimer on every screen.
- Loading state (the LLM response can take a few seconds).

### Key insight
- Prompt engineering is the heart of this. Draw the "explanation, not advice"
  boundary sharply in the prompt and provide a few examples (few-shot).
- To prevent hallucination, require the LLM to use **only the numbers it was
  given** and never invent new figures.

### ✅ Definition of Done
- Analysis cards are generated by the LLM from real portfolio data.
- The output never says "buy/sell/will rise"; it stays explanatory/educational.
- An LLM failure or broken JSON does not crash the app.
- Commentary is cached (no fresh request on every open).

---

## PHASE 4 — Stock Fundamentals Module (duration depends on data source) 🚧

**Goal:** Fetch the **current metrics** of a stock the user is curious about and
have the LLM explain **what they mean**. "Reading" a stock for someone without
a finance background. **No predictions, no recommendations.**

> ⚠️ Not "predicting the future" — "explaining today's picture." See `CLAUDE.md` § 2.

### Pre-decision
- **Data source:** free/affordable APIs exist for US stocks. Reliable BIST
  (Istanbul exchange) data is mostly paid → cost/scope must be settled here.
  *(Decided 2026-06-20: **Finnhub** free tier for US stocks; BIST deferred.)*

### Backend tasks
- `StockDataService` — fetch metrics by symbol: price, P/E, P/B, dividend
  yield, earnings growth, sector average (if available).
- Endpoint: `GET /api/stocks/{symbol}/metrics`.
- `LlmStockExplainService` — take the metrics and have them explained in plain
  language ("what do these numbers say") — again JSON, again "not advice".

### Mobile tasks
- Stock search + symbol selection.
- Metric cards (the mockup's 2×2 grid) + sector-relative labels.
- "What do these numbers say?" LLM explanation cards + disclaimer.

### Risks
- Metric definitions vary between sources (how P/E is computed etc.) →
  document the source and use it consistently.
- If BIST data is expensive: launch with US only, add BIST later.

### ✅ Definition of Done
- Metrics are fetched and displayed for a symbol.
- The LLM explains the metrics and offers a framework without saying
  "good/bad/buy".
- A meaningful error message for symbols with no data.

---

## PHASE 5 — Value History & Scenario v1 (Wave 1 · strategy C1) 🔜

**Goal:** Turn the price history that has been accumulating since Phase 2 into
a real **value-over-time chart** and a first **backward-looking scenario** —
opens the two empty surfaces ("Value History" card + Scenario tab) at once.

- Daily portfolio value series derived deterministically from
  `PriceSnapshots` + `Transactions` (unit-tested, NFR-1) → `GET /api/portfolio/history`.
- Dashboard chart (the `Sparkline` component already exists) + period selector
  on the Performance page.
- Scenario v1: single-variable comparisons like "what if I hadn't bought X /
  had stayed in TRY" — **history, never prediction** (CLAUDE.md § 2).

**✅ DoD:** chart renders from real series; at least one scenario comparison
works; series math unit-tested; no prediction anywhere. Tasks: `08-BACKLOG` T5.1–T5.4.

---

## PHASE 6 — Education MVP + Glossary (Wave 1 · strategy A1/A4) 🔜

**Goal:** The heart of the vision — **"learn with your portfolio."** Micro-lessons
whose closing section shows the concept **on the user's own real numbers**
(e.g. the diversification lesson ends with *your* concentration ratio).

- Education data model + endpoints + seed (backlog T5E.1–T5E.4; model `03` §C).
- First curriculum (5 lessons): inflation & real return · risk-return ·
  diversification/concentration · cost averaging · using BES well. Content lives
  in the repo, open to community contributions.
- "In your portfolio" context API — computed in code, deterministic, no LLM.
- Searchable **concept glossary** built from the existing InfoTip content.
- Progress: badges + weekly check-in streak. Success metric = **learning
  progress, not DAU** (`14-PRODUCT-STRATEGY` § 8).

**✅ DoD:** 5 lessons readable with real-portfolio context; quiz + progress saved;
glossary searchable; no ComingSoon left on the Education tab. Tasks: T5E.1–T6.4.

---

## PHASE 7 — Personalization & Reach (Wave 2) 🔜

**Goal:** Make the experience match the user's level and open the product to
more people (closed beta).

- Literacy onboarding (6-8 questions) → explanation depth + LLM tone per level.
- Real accounts (JWT + Argon2id) + KVKK "delete my data"; IDOR/AuthZ/rate-limit
  tests green before multi-user opens.
- PWA (installable, pre-React-Native mobile reach) + weekly summary notification
  (information, not advice — legal lens).
- Turkey-specific depth: TEFAS/BEFAS fund data, gold-culture module
  (çeyrek/bilezik conversions, wedding gold).
- Guest/demo mode (no sign-up, sample portfolio — usable in schools/workshops).
- "Why am I seeing this?" transparency on nudges and LLM cards.

**✅ DoD:** closed beta works end-to-end: sign-up → literacy level →
personalized content; PWA installable; demo mode browsable without an account.
Tasks: T7.1–T7.9.

---

## PHASE 8 — Scale & Impact (Wave 3; gated on legal) 🔜

**Goal:** The features closest to the advice boundary + productization.

- Behavior mirror (judgment-free pattern awareness from transaction history) —
  **designed together with an SPK-lens lawyer**, not after.
- Inflation panel (nominal vs real, "if it had stayed under the mattress").
- Full scenario simulator (multi-allocation comparison), new asset classes.
- Mobile arm (Phase M below), provider fallback chains, at-rest encryption,
  security completion.
- **Revenue model decision** (freemium / B2B education licence; never ads or
  fund-referral commissions — neutrality is the brand) + **SPK + KVKK legal
  sign-off (mandatory launch gate)**.
- Partnerships (FODER, universities) + content channel — impact measured with
  the learning metrics of `14-PRODUCT-STRATEGY` § 8.

**✅ DoD:** open-ended; every feature carries its own DoD. **Productization does
not start without legal sign-off (T8.5).** Tasks: T8.1–T8.8.

---

## General Notes

- **Duration estimates** assume a single developer at ~48 hours/week and a
  React Native learning curve. Full vision ~4–6 months; first usable product
  (Phase 1) 1–2 months.
- After finishing each phase, write a short "what I learned / what changed"
  note — it pays off later. *(In practice this lives in
  `.claude/tasks/TASKLOG.md`.)*
- If a phase stalls: shrink the scope, split the phase.
  **"Small and working" beats "big and half-done."**

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
| 5 | Beyond & Productization | — | New asset classes, simulation, revenue model | 🔜 |

**Dependency chain:** 0 → 1 → 2 → 3 → 4 → 5. Phase 3 depends on Phase 1's
calculation output (the LLM comments on those numbers). Phase 4 can proceed
independently but had to wait for the data-source decision (made: Finnhub, US
stocks; BIST deferred).

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

## PHASE 5 — Beyond & Productization 🔜

**Goal:** Broaden the product and prepare the revenue model. Order as needed.

### Possible work
- **New asset classes:** funds, real estate (valuation approach TBD),
  crypto (if wanted).
- **Scenario simulation (deep):** "If my allocation had been X, what would the
  last 12 months have looked like?" — backward-looking display using the
  `PriceSnapshots` history (not prediction).
- **Expanded education content:** leveled lessons, mini quizzes, progress tracking.
- **Notifications:** price/ratio threshold alerts (again: information, not advice).
- **Revenue model:** subscription (free tracking + paid analysis/education?) —
  decide the model.
- **Accounts/identity:** real user accounts, secure sign-in.

### Must-dos (before launch)
- **Legal validation:** expert/lawyer opinion on SPK (the investment-advice
  boundary) + KVKK (data protection). **Mandatory.**
- Performance and security review.
- App Store / Play Store publishing process.

### ✅ Definition of Done
This phase is open-ended; each feature carries its own "done" definition.
Productization does not start without legal sign-off.

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

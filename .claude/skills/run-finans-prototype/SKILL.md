---
name: run-finans-prototype
description: Render, drive, and screenshot the Finans portföy mobil uygulamasının tıklanabilir HTML taslağını (portfoy-uygulamasi-taslak.html). Use when asked to run / launch / preview / open / screenshot the prototype, mockup, taslak, or any of its screens (Portföy, Analiz, Hisse, Eğitim, varlık detay, varlık ekle).
---

# run-finans-prototype

The Finans project is **greenfield** — no backend or mobile code exists yet.
The single runnable artifact is `portfoy-uygulamasi-taslak.html` at the repo
root: a self-contained, clickable mobile mockup (no build, no server, no
dependencies). It has 4 tabs switched by a JS `go()` function and two
bottom-sheet overlays opened by `openDetail()` / `openAdd()`.

You can't click a tab from markdown, so the harness is **`driver.mjs`** — it
launches the **system Chrome** via `playwright-core`, walks every screen
(switching tabs and opening overlays in-page), and writes one PNG per screen to
`shots/`. That driver is the primary agent path below.

**Paths in this file are relative to the repo root (`<unit>/`).** The driver
lives at `.claude/skills/run-finans-prototype/driver.mjs`.

## Prerequisites

- **Node.js** (tested on v24.11.0) and **Chrome or Edge** installed.
  - Windows Chrome is auto-detected at `C:\Program Files\Google\Chrome\Application\chrome.exe`
    (Edge and Linux/macOS paths are also probed). Override with
    `CHROME_PATH=<path>` if yours is elsewhere.
- **One-time install** of `playwright-core` inside the skill dir (≈1 package,
  no bundled browser download — it drives your existing Chrome):

```bash
cd .claude/skills/run-finans-prototype && npm install
```

## Run (agent path) — the driver

From the repo root:

```bash
node .claude/skills/run-finans-prototype/driver.mjs
```

This writes 6 PNGs to `.claude/skills/run-finans-prototype/shots/`:
`portfoy.png`, `analiz.png`, `hisse.png`, `egitim.png`, `overlay-detail.png`,
`overlay-add.png`. Then **Read those PNGs** to see the screens.

Targeted captures:

```bash
# one tab only: portfoy | analiz | hisse | egitim
node .claude/skills/run-finans-prototype/driver.mjs --screen analiz

# one overlay: detail | add
node .claude/skills/run-finans-prototype/driver.mjs --overlay add

# leave a headed browser open to poke by hand (Ctrl-C to quit)
node .claude/skills/run-finans-prototype/driver.mjs --keep-open
```

The default walk plus `--screen analiz` and `--overlay add` were run in this
environment and produced valid screenshots. `--keep-open` uses the same launch
path but blocks until you Ctrl-C.

## Run (human path)

Just double-click `portfoy-uygulamasi-taslak.html`, or open it in any browser —
it's a static file. Tabs and overlays work via the bottom nav and the gold `+`
FAB. The driver only exists because an agent needs a *programmatic* way to
switch tabs and capture each screen.

## Gotchas

- **The phone frame, not the page.** The driver screenshots the `.phone`
  locator, not the full viewport — the page has a centered intro header and a
  footnote around the phone that you usually don't want in the shot.
- **Tabs are display-toggled, not routed.** Only `.view.active` is visible;
  `go()` flips the class. There is no URL/route per screen, so you must call
  `go(...)` in-page (the driver does this via `page.evaluate`) rather than
  navigating.
- **Overlays render full-screen over the phone** with a `translateY` slide.
  The driver waits 500 ms after `openDetail()/openAdd()` so the slide-in
  finishes before the screenshot; the detail/add PNGs therefore show the
  overlay, not the tab beneath.
- **Google Fonts load over the network.** The driver waits for `networkidle`
  plus 600 ms so Fraunces/Hanken Grotesk are applied; without the wait the first
  shot can show fallback fonts.
- **`headless: true` is the default.** Pass `--keep-open` for a headed window;
  that flag also blocks (infinite await) until you Ctrl-C.
- **Sample data is illustrative, not derived.** Numbers in the mockup are
  hand-placed (e.g. the donut weights and the hero total don't all reconcile);
  the real app must compute every figure deterministically in .NET. See
  `.claude/docs/01-NEEDS-ANALYSIS.md`.

## Troubleshooting

- **`No Chrome/Edge found. Set CHROME_PATH=...`** — the auto-probe missed your
  install. Find it and re-run: `CHROME_PATH="C:\path\to\chrome.exe" node .claude/skills/run-finans-prototype/driver.mjs`.
- **`Cannot find package 'playwright-core'`** — you skipped the install step:
  `cd .claude/skills/run-finans-prototype && npm install`.
- **`Prototype not found`** — run from the repo root, or confirm
  `portfoy-uygulamasi-taslak.html` still sits at the project root (the driver
  resolves it three levels up from itself).
- **Blank / fallback-font screenshots** — usually no internet for Google Fonts.
  The layout is still valid; only the typeface differs.

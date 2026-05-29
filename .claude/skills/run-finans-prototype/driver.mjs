#!/usr/bin/env node
// driver.mjs — render & drive the Finans clickable HTML prototype.
//
// The prototype (portfoy-uygulamasi-taslak.html) is a single self-contained
// file with 4 tabs (Portföy / Analiz / Hisse / Eğitim) switched by a JS go()
// function, plus two bottom-sheet overlays (asset detail, add-asset) opened by
// openDetail() / openAdd(). This driver launches the SYSTEM Chrome via
// playwright-core (no bundled browser download), walks every screen, and writes
// a PNG per screen to ./shots/.
//
// Usage:
//   node driver.mjs                 # screenshot all screens
//   node driver.mjs --screen analiz # one tab only (portfoy|analiz|hisse|egitim)
//   node driver.mjs --overlay detail|add
//   node driver.mjs --keep-open     # leave the browser open (headed) for poking
//
// Paths are resolved relative to this file, so it works from any CWD.

import { chromium } from 'playwright-core';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { dirname, resolve, join } from 'node:path';
import { existsSync, mkdirSync } from 'node:fs';

const __dirname = dirname(fileURLToPath(import.meta.url));
// skill dir is <unit>/.claude/skills/run-finans-prototype → unit root is 3 up
const UNIT_ROOT = resolve(__dirname, '..', '..', '..');
const PROTOTYPE = join(UNIT_ROOT, 'portfoy-uygulamasi-taslak.html');
const SHOTS = join(__dirname, 'shots');

const CHROME_CANDIDATES = [
  process.env.CHROME_PATH,
  // Windows
  'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
  'C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe',
  process.env.LOCALAPPDATA && join(process.env.LOCALAPPDATA, 'Google\\Chrome\\Application\\chrome.exe'),
  'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
  // Linux
  '/usr/bin/chromium', '/usr/bin/chromium-browser', '/usr/bin/google-chrome',
  // macOS
  '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',
].filter(Boolean);

function findChrome() {
  for (const p of CHROME_CANDIDATES) if (existsSync(p)) return p;
  throw new Error('No Chrome/Edge found. Set CHROME_PATH=<path-to-chrome.exe>.');
}

function arg(name) {
  const i = process.argv.indexOf(name);
  return i >= 0 ? (process.argv[i + 1] ?? true) : undefined;
}

const TABS = ['portfoy', 'analiz', 'hisse', 'egitim'];

async function main() {
  if (!existsSync(PROTOTYPE)) throw new Error(`Prototype not found: ${PROTOTYPE}`);
  mkdirSync(SHOTS, { recursive: true });

  const keepOpen = !!arg('--keep-open');
  const executablePath = findChrome();
  console.log(`[driver] Chrome: ${executablePath}`);
  console.log(`[driver] Prototype: ${PROTOTYPE}`);

  const browser = await chromium.launch({ executablePath, headless: !keepOpen });
  // The phone frame is 390x820 + page padding; 480x1000 captures it with margin.
  const page = await browser.newPage({ viewport: { width: 480, height: 1000 }, deviceScaleFactor: 2 });
  await page.goto(pathToFileURL(PROTOTYPE).href, { waitUntil: 'networkidle' });
  await page.waitForTimeout(600); // let Google Fonts settle

  const phone = page.locator('.phone');

  const oneScreen = arg('--screen');
  const oneOverlay = arg('--overlay');

  async function shotPhone(name) {
    const out = join(SHOTS, `${name}.png`);
    await phone.screenshot({ path: out });
    console.log(`[driver] wrote ${out}`);
  }

  if (oneOverlay) {
    if (oneOverlay === 'detail') await page.evaluate(() => openDetail());
    else if (oneOverlay === 'add') await page.evaluate(() => openAdd());
    await page.waitForTimeout(500);
    await shotPhone(`overlay-${oneOverlay}`);
  } else if (oneScreen) {
    await page.evaluate((v) => go(document.querySelector(`.nav button[data-v="${v}"]`)), oneScreen);
    await page.waitForTimeout(400);
    await shotPhone(oneScreen);
  } else {
    // full walk: every tab + both overlays
    for (const v of TABS) {
      await page.evaluate((sel) => go(document.querySelector(sel)), `.nav button[data-v="${v}"]`);
      await page.waitForTimeout(400);
      await shotPhone(v);
    }
    // back to portföy, then overlays
    await page.evaluate(() => go(document.querySelector('.nav button[data-v="portfoy"]')));
    await page.waitForTimeout(200);
    await page.evaluate(() => openDetail());
    await page.waitForTimeout(500);
    await shotPhone('overlay-detail');
    await page.evaluate(() => closeOv('ov-detail'));
    await page.waitForTimeout(300);
    await page.evaluate(() => openAdd());
    await page.waitForTimeout(500);
    await shotPhone('overlay-add');
  }

  if (keepOpen) {
    console.log('[driver] --keep-open set; leaving browser open. Ctrl-C to quit.');
    await new Promise(() => {});
  }
  await browser.close();
  console.log('[driver] done.');
}

main().catch((e) => { console.error('[driver] ERROR:', e.message); process.exit(1); });

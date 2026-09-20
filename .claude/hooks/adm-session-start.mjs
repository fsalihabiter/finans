#!/usr/bin/env node
// adm-session-start.mjs — SessionStart hook: prints ADM bearings into context.
//
// Claude Code runs this on every session start; stdout is injected as context so
// "where we are" is visible without anyone asking. Reads ONLY canonical ADM state
// (.agent-method/): state.yaml, the active phase, the active GD, the newest
// session evidence (by closed_at — never "top of a log") and open doctor findings.
//
// This hook is a CONVENIENCE, not a source of truth and not required for
// correctness (ADM P5-P8 / INV-15): adm-open reads the same canonical files.
// Self-locating, never throws, never blocks a session.

import { readFileSync, existsSync, readdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join, resolve } from 'node:path';

const out = [];

try {
  const here = dirname(fileURLToPath(import.meta.url));
  const root = findRoot(here);
  if (!root) {
    process.stdout.write('ADM: .agent-method/ bulunamadı — bu repo ADM kullanmıyor olabilir.\n');
    process.exit(0);
  }
  const A = join(root, '.agent-method');

  const state = read(join(A, 'state.yaml'));
  const project = scalar(block(state, 'project'), 'name') || scalar(block(state, 'project'), 'id') || '(adsız proje)';
  const active = block(state, 'active');
  const phaseId = scalar(active, 'phase_id');
  const gdId = scalar(active, 'gd_id');
  const session = block(state, 'session');
  const openSessionId = scalar(session, 'id');
  const reviewStatus = scalar(block(state, 'review'), 'status');
  const blockers = listItems(state, 'blockers');

  out.push(`🧭 ADM — ${project} (kanonik durum: .agent-method/state.yaml)`);
  out.push('Oturum başı: `adm-open` · oturum sonu: `adm-close` (CLAUDE.md §11).');

  // ── Aktif faz ────────────────────────────────────────────────────────────
  if (phaseId) {
    const phase = section(read(join(A, 'charter', 'PHASES.md')), phaseId);
    out.push(`\n— AKTİF FAZ: ${phaseId} —\n${phase || '(PHASES.md içinde bu faz bulunamadı ⚠ INV-01)'}`);
  } else {
    out.push('\n— AKTİF FAZ: yok (state.yaml → active.phase_id boş) —');
  }

  // ── Aktif döngü (GD) ─────────────────────────────────────────────────────
  if (gdId) {
    const gdFile = join(A, 'cycles', `${gdId}.md`);
    if (existsSync(gdFile)) {
      const gd = read(gdFile);
      const status = scalar(gd, 'status');
      out.push(`\n— AKTİF GD: ${gdId} — ${scalar(gd, 'title') || ''} (status: ${status || '?'})`);
      if (status !== 'in-progress') {
        out.push(`⚠ INV-04: aktif GD'nin status'ü 'in-progress' olmalı, '${status}' görünüyor.`);
      }
      const goal = sectionByHeading(gd, '## 1. Goal');
      if (goal) out.push(goal);
    } else {
      out.push(`\n⚠ INV-04: state.yaml ${gdId} diyor ama cycles/${gdId}.md yok.`);
    }
  } else {
    const planned = listCycles(join(A, 'cycles'));
    out.push('\n— AKTİF GD: yok —' + (planned.length ? `\nHazır döngüler: ${planned.join(', ')}` : ''));
  }

  // ── Son oturum kanıtı (en yeni closed_at) ────────────────────────────────
  const last = newestSession(join(A, 'evidence', 'sessions'));
  out.push('\n— SON OTURUM KANITI —\n' + (last
    ? `${last.name} (closed_at: ${last.closedAt || '?'})\n${last.excerpt}`
    : '(henüz oturum kanıtı yok — ilk adm-close ile oluşur)'));

  // ── Açık doctor bulguları ────────────────────────────────────────────────
  const doctor = newestDoctor(join(A, 'evidence', 'doctor'));
  if (doctor) {
    out.push(`\n— AÇIK DOCTOR BULGULARI (${doctor.name}) —` +
      (doctor.open.length ? '\n' + doctor.open.join('\n') : '\n(açık bulgu yok)'));
  }

  // ── Uyarılar ─────────────────────────────────────────────────────────────
  const warn = [];
  if (openSessionId) warn.push(`⚠ INV-13: state.yaml'da açık bir oturum var (${openSessionId}). Eskimişse adm-close ile kapat — sessizce ÜZERİNE YAZMA, bildir.`);
  if (blockers.length) warn.push('⛔ Engeller: ' + blockers.join(' · '));
  if (reviewStatus && reviewStatus !== 'not-run' && reviewStatus !== 'clean') warn.push(`⚠ Review durumu: ${reviewStatus}`);
  if (warn.length) out.push('\n' + warn.join('\n'));

  process.stdout.write(out.join('\n') + '\n');
} catch (err) {
  // Bir hook asla oturumu bloke etmez.
  process.stdout.write(`ADM hook okunamadı (yok sayıldı): ${err?.message ?? err}\n`);
}

// ── yardımcılar ────────────────────────────────────────────────────────────

function read(p) {
  try { return existsSync(p) ? readFileSync(p, 'utf8') : ''; } catch { return ''; }
}

function findRoot(start) {
  let dir = resolve(start);
  for (let i = 0; i < 8; i++) {
    if (existsSync(join(dir, '.agent-method'))) return dir;
    const up = dirname(dir);
    if (up === dir) break;
    dir = up;
  }
  return null;
}

// `key:` altındaki girintili bloğu döndürür (küçük, bağımlılıksız YAML dilimi).
function block(text, key) {
  const lines = text.split(/\r?\n/);
  const start = lines.findIndex((l) => new RegExp(`^${key}:\\s*$`).test(l));
  if (start < 0) return '';
  const body = [];
  for (let i = start + 1; i < lines.length; i++) {
    const l = lines[i];
    if (/^\s*$/.test(l) || /^\s*#/.test(l)) { body.push(l); continue; }
    if (!/^\s+/.test(l)) break;
    body.push(l);
  }
  return body.join('\n');
}

// `key: value` — yorumu, tırnağı ve null'ı temizler.
function scalar(text, key) {
  const m = text.match(new RegExp(`^\\s*${key}:\\s*(.*)$`, 'm'));
  if (!m) return '';
  let v = m[1].replace(/\s+#.*$/, '').trim().replace(/^["']|["']$/g, '');
  return v === 'null' || v === '~' ? '' : v;
}

function listItems(text, key) {
  const inline = text.match(new RegExp(`^${key}:\\s*\\[(.*)\\]\\s*$`, 'm'));
  if (inline) return inline[1].split(',').map((s) => s.trim()).filter(Boolean);
  return block(text, key).split(/\r?\n/)
    .map((l) => l.match(/^\s*-\s+(.*)$/)?.[1]?.trim())
    .filter(Boolean);
}

// PHASES.md içinde `## PHASE-006 ...` başlığından sonraki bölüm.
function section(text, id) {
  const lines = text.split(/\r?\n/);
  const start = lines.findIndex((l) => l.startsWith('## ') && l.includes(id));
  if (start < 0) return '';
  const body = [lines[start]];
  for (let i = start + 1; i < lines.length && !lines[i].startsWith('## '); i++) body.push(lines[i]);
  return body.join('\n').trim();
}

function sectionByHeading(text, heading) {
  const lines = text.split(/\r?\n/);
  const start = lines.indexOf(heading);
  if (start < 0) return '';
  const body = [];
  for (let i = start + 1; i < lines.length && !lines[i].startsWith('## '); i++) body.push(lines[i]);
  return body.join('\n').trim();
}

function listCycles(dir) {
  try {
    return readdirSync(dir).filter((f) => /^GD-\d+.*\.md$/.test(f)).map((f) => f.replace(/\.md$/, ''));
  } catch { return []; }
}

function newestSession(dir) {
  let files = [];
  try { files = readdirSync(dir).filter((f) => f.endsWith('.md') && !f.startsWith('_')); } catch { return null; }
  const parsed = files.map((name) => {
    const text = read(join(dir, name));
    return { name, closedAt: scalar(text, 'closed_at'), text };
  });
  if (!parsed.length) return null;
  parsed.sort((a, b) => String(b.closedAt).localeCompare(String(a.closedAt)) || b.name.localeCompare(a.name));
  const top = parsed[0];
  const excerpt = top.text.split(/\r?\n/).filter((l) => !/^(---|\s*$)/.test(l)).slice(0, 14).join('\n');
  return { name: top.name, closedAt: top.closedAt, excerpt };
}

function newestDoctor(dir) {
  let files = [];
  try { files = readdirSync(dir).filter((f) => /^DOCTOR-.*\.md$/.test(f)); } catch { return null; }
  if (!files.length) return null;
  files.sort((a, b) => b.localeCompare(a));
  const name = files[0];
  const text = read(join(dir, name));
  // findings: - id / severity / summary / status dörtlüsünü kabaca eşle.
  const open = [];
  const chunks = text.split(/\n\s{2}- id:\s*/).slice(1);
  for (const c of chunks) {
    const id = c.split(/\r?\n/)[0].trim();
    if (!/status:\s*open/.test(c)) continue;
    const sev = c.match(/severity:\s*(\w+)/)?.[1] ?? '?';
    const sum = c.match(/summary:\s*"?([^"\n]{0,140})/)?.[1]?.trim() ?? '';
    open.push(`  ${id} [${sev}] ${sum}…`);
  }
  return { name, open };
}

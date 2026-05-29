#!/usr/bin/env node
// session-start.mjs — SessionStart hook: prints current task state into context.
//
// Claude Code runs this on every session start; its stdout is injected as
// context so both you and Claude immediately see "where we are" without anyone
// asking. It reads ACTIVE.md (current tasks) and the latest TASKLOG.md entry.
//
// Self-locating (resolves files relative to this script), so it works no matter
// the working directory. Prints nothing fatal on missing files — never blocks a
// session.

import { readFileSync, existsSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const ACTIVE = join(here, 'ACTIVE.md');
const LOG = join(here, 'TASKLOG.md');

function read(p) {
  try { return existsSync(p) ? readFileSync(p, 'utf8') : ''; } catch { return ''; }
}

// Latest TASKLOG entry = text from the first real "## " heading to the next.
// Headings inside ``` fenced blocks (e.g. the entry template) are ignored.
function latestLogEntry(text) {
  const lines = text.split(/\r?\n/);
  const starts = [];
  let inFence = false;
  lines.forEach((l, i) => {
    if (/^\s*```/.test(l)) { inFence = !inFence; return; }
    if (!inFence && /^##\s/.test(l)) starts.push(i);
  });
  if (!starts.length) return '';
  const from = starts[0];
  const to = starts[1] ?? lines.length;
  return lines.slice(from, to).join('\n').trim();
}

const active = read(ACTIVE).trim();
const entry = latestLogEntry(read(LOG));

const out = [];
out.push('📋 GÖREV TAKİBİ (otomatik) — bu oturumda görev ilerledikçe TASKLOG.md ve ACTIVE.md güncellenmeli. Bkz. CLAUDE.md §11.');
if (active) { out.push('\n— AKTİF GÖREVLER (.claude/tasks/ACTIVE.md) —\n' + active); }
if (entry) { out.push('\n— SON WORKLOG GİRDİSİ (.claude/tasks/TASKLOG.md) —\n' + entry); }
if (!active && !entry) { out.push('\n(Henüz görev kaydı yok. İlk işte ACTIVE.md ve TASKLOG.md doldurulmalı.)'); }

process.stdout.write(out.join('\n') + '\n');

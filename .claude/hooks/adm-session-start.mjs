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

  // ── Sıradaki açık işler (BACKLOG projeksiyonundan — ağ gerekmez) ─────────
  const backlog = openWork(join(A, 'generated', 'BACKLOG.md'));
  if (backlog) {
    out.push(`\n— SIRADAKİ AÇIK İŞLER (prio:1 · kanonik: GitHub Issues, D-022) —`);
    out.push(backlog.rows.length ? backlog.rows.join('\n') : '(prio:1 etiketli açık issue yok)');
    out.push(`Tümü: \`gh issue list --state open\` · projeksiyon: ${backlog.total} açık issue, son değişiklik ${backlog.generatedAt || '?'}${backlog.stale ? ' ⚠ (30 günden beri değişmedi — CI senkronu kırılmış olabilir: `gh run list --workflow adm-backlog-sync.yml`)' : ''}`);
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

  // ── Uyarılar ve notlar ───────────────────────────────────────────────────
  // ⚠ RV-087 (REVIEW-006): ikisi AYRI tutulur. Önceki hâlde "oturum yok" notu da
  // `warn` dizisine giriyordu, dolayısıyla uyarı bloğu HER oturumda basılıyordu ve
  // normal durum uyarı biçiminde bir satır üretiyordu. S-009'un kök neden teşhisi
  // "brifing her şeyi söylediği için adm-open gereksiz görünüyor" idi; sürekli
  // görünen bir uyarı aynı alışkanlığı besler — uyarı sürekliyse sinyal değildir.
  // Uyarı = eylem gerektiren bir sapma. Not = bağlam.
  const warn = [];
  const note = [];
  if (openSessionId) {
    warn.push(`⚠ INV-13: state.yaml'da açık bir oturum var (${openSessionId}). Eskimişse adm-close ile kapat — sessizce ÜZERİNE YAZMA, bildir.`);
  } else {
    // S-009 süreç bulgusu: bu brifing "nerede kaldık" sorusunu cevapladığı için
    // adm-open gereksiz GÖRÜNÜYOR ve dört oturum üst üste atlandı. Oysa hook
    // yalnız OKUR — oturumu açmaz, started_at/start_sha yazmaz. Sonuç: her
    // kapanışta diff tabanı elle seçiliyor ve INV-13'ün "tek açık oturum"
    // sözleşmesi hiç işletilmiyor. Hook oturumu KENDİSİ açamaz: koşulsuz
    // çalıştığı için salt soru-cevap turlarında da açardı. O yüzden uyarır.
    note.push(
      '📌 Açık ADM oturumu YOK (state.yaml → session boş). Bu brifing yalnız OKUR: ' +
      'oturumu açmaz, started_at/start_sha yazmaz. İş yapacaksan `adm-open` çalıştır — ' +
      'yoksa kapanışta diff tabanı elle seçilmek zorunda kalır (S-006\'dan beri her ' +
      'oturumda böyle oldu). Salt soru-cevap / keşif turuysa gerekmez.',
    );
  }
  if (blockers.length) warn.push('⛔ Engeller: ' + blockers.join(' · '));
  if (reviewStatus && reviewStatus !== 'not-run' && reviewStatus !== 'clean') warn.push(`⚠ Review durumu: ${reviewStatus}`);
  if (warn.length) out.push('\n— UYARI —\n' + warn.join('\n'));
  if (note.length) out.push('\n' + note.join('\n'));

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
  // ⚠ Anahtar satırında SONDA AÇIKLAMA olabilir (`session:   # ...`). Önceki
  // kalıp `^key:\s*$` idi ve bu yüzden `session:` bloğu HİÇ bulunamıyordu:
  // openSessionId her zaman boş dönüyor, dolayısıyla INV-13'ün "açık oturum
  // var" uyarısı hiçbir zaman çalışamıyordu (ölü kod). 2026-11-22'de bulundu.
  const start = lines.findIndex((l) => new RegExp(`^${key}:\\s*(#.*)?$`).test(l));
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
  // ⚠ RV-071 (REVIEW-006): sondaki açıklama izni `block()`'a eklenmişti ama BURAYA
  // eklenmemişti. Sonuç: `blockers: [INC-001]  # not` satırı hiçbir şeyle eşleşmiyor,
  // satır-içi kol boş dönüyor, blok kolu da `[...]` satırının altında girintili
  // öğe bulamıyor → ENGEL LİSTESİ SESSİZCE BOŞALIYOR ve hook'un en önemli uyarısı
  // ("⛔ Engeller") hiç basılmıyor. `session:` satırında aylarca fark edilmeyen
  // hatanın birebir aynısı, aynı dosyada, 70 satır altında.
  const inline = text.match(new RegExp(`^${key}:\\s*\\[(.*?)\\]\\s*(?:#.*)?$`, 'm'));
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

// generated/BACKLOG.md — GitHub Issues projeksiyonu (D-022). Salt okunur,
// ağ çağrısı yok: kanonik kaynak Issues, bu yalnız hızlı anlık görüntü.
function openWork(file) {
  const text = read(file);
  if (!text) return null;
  const generatedAt = text.match(/·\s*(\d{4}-\d{2}-\d{2}T[\d:]+Z)\s*·/)?.[1] ?? '';
  const total = text.match(/\*\*(\d+) açık issue\*\*/)?.[1] ?? '?';
  const rows = [];
  for (const line of text.split(/\r?\n/)) {
    const c = line.split('|').map((s) => s.trim());
    // | #N | Başlık | Tür | Önc. | Alan | Üst | Engelleyen | Etiket |
    if (c.length < 9 || !/^#\d+$/.test(c[1]) || c[4] !== '1') continue;
    const blocked = c[7] !== '—' ? `  ⛔ ${c[7]}` : '';
    rows.push(`  ${c[1]} ${c[2]}${blocked}`);
    if (rows.length >= 6) break;
  }
  // Damga artik son ICERIK degisikligini gosterir (uretici idempotent), o yuzden
  // esik genis: aylardir hic degismemesi CI senkronunun kirildigina isarettir.
  let stale = false;
  if (generatedAt) {
    const age = Date.now() - Date.parse(generatedAt);
    stale = Number.isFinite(age) && age > 30 * 864e5;
  }
  return { rows, total, generatedAt, stale };
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

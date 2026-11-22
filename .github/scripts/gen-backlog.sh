#!/usr/bin/env bash
# Generates .agent-method/generated/BACKLOG.md from GitHub Issues.
#
# This is a PROJECTION (INV-12 / INV-17): the canonical backlog source is
# GitHub Issues (D-022). Never edit the output by hand.
#
# Requires: gh (authenticated). Uses gh's embedded jq — no external jq needed.
# Usage: .github/scripts/gen-backlog.sh [output-path]
set -euo pipefail

OUT="${1:-.agent-method/generated/BACKLOG.md}"
REPO="${GH_REPO:-fsalihabiter/finans}"
FIELDS='number,title,labels,milestone,issueType,parent,blockedBy,state'

JQ='
def names($p): [.labels[].name | select(startswith($p)) | sub($p; "")] | join(", ");
def dash: if . == "" then "—" else . end;
def row:
  "| #" + (.number|tostring)
  + " | " + .title
  + " | " + ((.issueType.name) // "—")
  + " | " + (names("prio:") | dash)
  + " | " + (names("area:") | dash)
  + " | " + (if .parent then "#" + (.parent.number|tostring) else "—" end)
  + " | " + (([(.blockedBy.nodes // [])[] | "#" + (.number|tostring)] | join(" ")) | dash)
  + " | " + ([.labels[].name | select(startswith("gate:") or startswith("kind:") or . == "adm:gd")] | join(" ") | dash)
  + " |";
sort_by(.number)
| group_by((.milestone.title) // "~ Milestone atanmamış")
| map(
    "## " + ((.[0].milestone.title) // "~ Milestone atanmamış")
    + "  ·  " + (length|tostring) + " açık\n\n"
    + "| # | Başlık | Tür | Önc. | Alan | Üst | Engelleyen | Etiket |\n"
    + "|---|---|---|---|---|---|---|---|\n"
    + ([.[] | row] | join("\n"))
  )
| join("\n\n")
'

CLOSED_JQ='
sort_by((.milestone.number) // 999)
| group_by((.milestone.title) // "~ Milestone atanmamış")
| map("| " + ((.[0].milestone.title) // "~ Milestone atanmamış") + " | " + (length|tostring) + " |")
| join("\n")
'

total=$(gh issue list --repo "$REPO" --state open --limit 1000 --json number --jq 'length')
body=$(gh issue list --repo "$REPO" --state open --limit 1000 --json "$FIELDS" --jq "$JQ")
done_total=$(gh issue list --repo "$REPO" --state closed --limit 1000 --json number --jq 'length')
done_body=$(gh issue list --repo "$REPO" --state closed --limit 1000 --json number,milestone --jq "$CLOSED_JQ")
now=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

mkdir -p "$(dirname "$OUT")"
{
  echo "<!-- GENERATED FILE — DO NOT EDIT BY HAND. -->"
  echo "# BACKLOG — GitHub Issues projeksiyonu"
  echo
  echo "> **Üretilmiş dosya (INV-12 · INV-17).** Elle düzenlenmez; her düzenleme"
  echo "> bir sonraki üretimde kaybolur."
  echo ">"
  echo "> **Kanonik backlog kaynağı: GitHub Issues** → https://github.com/$REPO/issues (D-022)."
  echo "> Bu dosya çevrimdışı/ajan okuması için anlık görüntüdür."
  echo ">"
  echo "> Üretim: \`.github/scripts/gen-backlog.sh\` · $now · **$total açık issue**"
  echo ">"
  echo "> Sütunlar: **Üst** = üst issue (alt-görev ilişkisi) · **Engelleyen** ="
  echo "> bu iş başlayamadan kapanması gereken issue’lar (\`blocked by\`)."
  echo
  echo "$body"
  echo
  echo "---"
  echo
  echo "## Tamamlananlar — $done_total kapalı issue"
  echo
  echo "> Faz 0-5 ve Faz 6'nın bitmiş kalemleri 2026-11-22'de **geriye dönük**"
  echo "> kapalı issue olarak girildi (D-022). Uygulama notları issue gövdelerinde;"
  echo "> kaynak \`git show ac61b7e:.claude/docs/08-BACKLOG.md\` ve \`.claude/tasks/TASKLOG.md\`."
  echo
  echo "| Faz | Kapalı |"
  echo "|---|---:|"
  echo "$done_body"
  echo
  echo "Listelemek için:"
  echo
  echo '```bash'
  echo "gh issue list --repo $REPO --state closed --limit 200"
  echo '```'
  echo
  echo "---"
  echo
  echo "## Kapsam dışı (burada görünmez)"
  echo
  echo "- **Kapanmış işlerin tam listesi** → \`gh issue list --state closed\` (yukarıda yalnız faz bazlı sayım var); tarihsel notlar \`.claude/tasks/TASKLOG.md\` ve git geçmişindedir."
  echo "- **Kararlar** → \`.agent-method/decisions/D-NNN-*.md\` (kanonik, taşınmadı)."
  echo "- **Döngü gerekçesi / kabul kriterleri** → \`.agent-method/cycles/GD-NNN.md\` (kanonik)."
  echo "- **Fikirler** → \`.agent-method/pipeline/IDEAS.md\` (POOL'a yükseltme insan kararıdır)."
  echo "- **Test borcu** → \`.agent-method/pipeline/TEST_DEBT.md\` (INV-08)."
} > "$OUT"

echo "yazıldı: $OUT ($total açık issue)"

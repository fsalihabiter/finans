#!/usr/bin/env bash
# One-time: creates the GitHub Project (v2) board and adds every open issue.
#
# Needs a token scope the default gh login does NOT have. Run once:
#     gh auth refresh -s project,read:project
# (opens a browser / shows a one-time code — only you can approve it)
#
# Usage: .github/scripts/setup-project-board.sh ["Board title"]
set -euo pipefail

OWNER="${GH_OWNER:-fsalihabiter}"
REPO="${GH_REPO:-fsalihabiter/finans}"
TITLE="${1:-Nirengi — Yol Haritası}"

if ! gh project list --owner "$OWNER" >/dev/null 2>&1; then
  cat >&2 <<'MSG'
✗ Token'da proje kapsamı yok. Önce şunu çalıştır:

    gh auth refresh -s project,read:project

Sonra bu script'i tekrar koş.
MSG
  exit 1
fi

existing=$(gh project list --owner "$OWNER" --format json \
  --jq ".projects[] | select(.title == \"$TITLE\") | .number" 2>/dev/null || true)

if [ -n "$existing" ]; then
  NUM="$existing"
  echo "pano zaten var: #$NUM — $TITLE"
else
  NUM=$(gh project create --owner "$OWNER" --title "$TITLE" --format json --jq '.number')
  echo "pano oluşturuldu: #$NUM — $TITLE"
fi

echo "açık issue'lar ekleniyor…"
added=0
while read -r url; do
  [ -z "$url" ] && continue
  if gh project item-add "$NUM" --owner "$OWNER" --url "$url" >/dev/null 2>&1; then
    added=$((added + 1))
  fi
done < <(gh issue list --repo "$REPO" --state open --limit 1000 --json url --jq '.[].url')

echo "eklendi: $added issue"
echo
cat <<MSG
Pano: https://github.com/users/$OWNER/projects/$NUM

Kalan üç adım yalnız arayüzden yapılabilir (CLI desteklemiyor):

  1. Status alanına kolonları ekle:
     Backlog · Blocked · In progress · Review · Done
  2. Görünümler:
     - "Pano"     → Board, Status'a göre grupla
     - "Faz"      → Table, Milestone'a göre grupla, Labels sütunu açık
     - "Sıradaki" → Table, filtre: label:prio:1 -label:blocked, Status'a göre sırala
  3. Workflows → "Auto-add to project":
     repo = $REPO, filtre = is:issue is:open
     (GitHub Free planında proje başına 1 otomatik-ekleme kuralı hakkın var —
      bu onu kullanır, yeni issue'lar panoya kendiliğinden düşer.)
MSG

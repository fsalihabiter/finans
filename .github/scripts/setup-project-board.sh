#!/usr/bin/env bash
# One-time: creates the GitHub Project (v2) board and adds the repo's issues.
#
# Needs a token scope the default gh login does NOT have. Run once:
#     gh auth refresh -s project,read:project
# (opens a browser / shows a one-time code — only you can approve it)
#
# Usage: .github/scripts/setup-project-board.sh ["Board title"] [--all]
#   (default)  only OPEN issues  — an uncluttered working board
#   --all      open + closed     — the full history (Faz 0-5 included) on the board
#
# Re-running is safe: an existing board with the same title is reused and
# already-present items are skipped by GitHub.
set -euo pipefail

OWNER="${GH_OWNER:-fsalihabiter}"
REPO="${GH_REPO:-fsalihabiter/finans}"
TITLE="Nirengi — Yol Haritası"
STATE="open"
for a in "$@"; do
  case "$a" in
    --all) STATE="all" ;;
    --open) STATE="open" ;;
    -*) echo "bilinmeyen seçenek: $a" >&2; exit 2 ;;
    *) TITLE="$a" ;;
  esac
done

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

if [ "$STATE" = "all" ]; then
  echo "issue'lar ekleniyor (açık + kapalı — tam geçmiş)…"
else
  echo "açık issue'lar ekleniyor (kapalı geçmiş için: --all)…"
fi

added=0; failed=0
while read -r url; do
  [ -z "$url" ] && continue
  if gh project item-add "$NUM" --owner "$OWNER" --url "$url" >/dev/null 2>&1; then
    added=$((added + 1))
  else
    failed=$((failed + 1))
    echo "  eklenemedi: $url" >&2
  fi
done < <(gh issue list --repo "$REPO" --state "$STATE" --limit 1000 --json url --jq '.[].url')

echo "eklendi: $added issue$([ "$failed" -gt 0 ] && echo " · eklenemedi: $failed" || true)"

# ── Status kolonlari ──────────────────────────────────────────────────────────
# `gh project` var olan bir single-select alaninin seceneklerini duzenleyemez,
# ama GraphQL `updateProjectV2Field` edebilir. Yalniz VARSAYILAN kume
# (Todo/In Progress/Done) duruyorsa dokunulur: yeniden yazmak secenek id'lerini
# degistirip kalemlerdeki atamalari dusurebilir.
PID=$(gh project view "$NUM" --owner "$OWNER" --format json --jq '.id')
read -r FID HAS_BACKLOG <<EOF
$(gh api graphql -f query='
  query($p:ID!){ node(id:$p){ ... on ProjectV2 {
    field(name:"Status"){ ... on ProjectV2SingleSelectField { id options { name } } } } } }' \
  -F p="$PID" --jq '.data.node.field | "\(.id) \((.options|map(.name)|index("Backlog")) != null)"')
EOF

if [ "$HAS_BACKLOG" = "true" ]; then
  echo "Status kolonları zaten kurulu — dokunulmadı"
else
  gh api graphql -f query='
    mutation($f:ID!){
      updateProjectV2Field(input:{ fieldId:$f singleSelectOptions:[
        {name:"Backlog",     color:GRAY,   description:"Sırada — henüz başlanmadı"}
        {name:"Blocked",     color:RED,    description:"Başka bir issue kapanmadan başlayamaz (blocked by)"}
        {name:"In progress", color:YELLOW, description:"Aktif GD / üzerinde çalışılıyor"}
        {name:"Review",      color:PURPLE, description:"Kanıt sunuldu, insan kabulü bekliyor (INV-06)"}
        {name:"Done",        color:GREEN,  description:"Kabul edildi / kapandı"}
      ]}){ projectV2Field { ... on ProjectV2SingleSelectField { id } } }
    }' -F f="$FID" >/dev/null
  echo "Status kolonları kuruldu: Backlog · Blocked · In progress · Review · Done"
fi

# ── Durumlari GERCEK veriden doldur ──────────────────────────────────────────
# Blocked  = issue'nun `blocked by` ilişkisi var (GitHub'ın kendi verisi)
# In progress = aktif GD'nin issue'su (`adm:gd` etiketli, ustu olmayan kalem)
# Backlog  = gerisi.  Kapali kalemlerin durumu GitHub'da zaten "closed", elleme.
opt(){ gh api graphql -f query='
  query($p:ID!){ node(id:$p){ ... on ProjectV2 { field(name:"Status"){
    ... on ProjectV2SingleSelectField { options { id name } } } } } }' \
  -F p="$PID" --jq ".data.node.field.options[] | select(.name==\"$1\") | .id"; }

O_BACKLOG=$(opt Backlog); O_BLOCKED=$(opt Blocked); O_PROG=$(opt "In progress")
FID_S="$FID"
ACTIVE=$(gh issue list --repo "$REPO" --state open --limit 300 --label "adm:gd" \
         --json number,parent --jq 'first(.[] | select(.parent == null) | .number) // empty')
[ -n "$ACTIVE" ] && echo "aktif GD issue'su: #$ACTIVE → In progress"

tmp_b=$(mktemp); tmp_i=$(mktemp)
trap 'rm -f "$tmp_b" "$tmp_i"' EXIT
gh issue list --repo "$REPO" --state open --limit 300 --json number,blockedBy \
  --jq '.[] | "\(.number)\t\((.blockedBy.nodes|length) > 0)"' | sort > "$tmp_b"
gh project item-list "$NUM" --owner "$OWNER" --format json --limit 1000 \
  --jq '.items[] | select(.content.number != null) | "\(.content.number)\t\(.id)"' | sort > "$tmp_i"

np=0; nb=0; nk=0
while IFS=$'\t' read -r num iid isblocked; do
  [ -z "${isblocked:-}" ] && continue          # kapali kalem: engel verisi yok, atla
  if [ "$num" = "$ACTIVE" ];      then o="$O_PROG";    np=$((np+1))
  elif [ "$isblocked" = "true" ]; then o="$O_BLOCKED"; nb=$((nb+1))
  else                                 o="$O_BACKLOG"; nk=$((nk+1)); fi
  gh project item-edit --id "$iid" --project-id "$PID" --field-id "$FID_S" \
    --single-select-option-id "$o" >/dev/null 2>&1 || echo "  durum yazılamadı: #$num" >&2
done < <(join -t"$(printf '\t')" "$tmp_i" "$tmp_b")

echo "durumlar: In progress $np · Blocked $nb · Backlog $nk"
echo
cat <<MSG
Pano: https://github.com/users/$OWNER/projects/$NUM

Kalan iki adım yalnız arayüzden yapılabilir (ne CLI ne API destekliyor):

  1. Görünümler:
     - "Pano"     → Board, Status'a göre grupla
     - "Faz"      → Table, Milestone'a göre grupla, Labels sütunu açık
     - "Sıradaki" → Table, filtre: label:prio:1 -label:blocked, Status'a göre sırala
  2. Workflows → "Auto-add to project":
     repo = $REPO, filtre = is:issue is:open
     (GitHub Free planında proje başına 1 otomatik-ekleme kuralı hakkın var —
      bu onu kullanır, yeni issue'lar panoya kendiliğinden düşer.)

Not: kapalı issue'ları da panoya almak istersen bu script'i \`--all\` ile
tekrar koş — var olan pano yeniden kullanılır, mevcut kalemler atlanır.
MSG

import type { CommentaryCard } from "@finans/shared";

/**
 * Tek yorum kartı görünümü (T4.5'te listeden ayrıştırıldı — sekmeli gezgin de kullanır).
 * Emoji + başlık + gövde + opsiyonel "Kavram" bloğu + opsiyonel meter (0..1) + etiketler.
 */
export function CommentaryCardItem({
  card,
  fallback = false,
}: {
  card: CommentaryCard;
  fallback?: boolean;
}) {
  return (
    <article className={`commentary-card${fallback ? " is-fallback" : ""}`}>
      <header className="commentary-card-head">
        <span className="commentary-emoji" aria-hidden="true">{card.emoji}</span>
        <h3>{card.title}</h3>
      </header>
      <p className="commentary-body">{card.body}</p>
      {card.detail ? (
        <p className="commentary-detail">
          <span className="commentary-detail-label">Kavram: </span>
          {card.detail}
        </p>
      ) : null}
      {card.meter ? (
        <div className="commentary-meter" aria-label={`${card.meter.lowLabel} ↔ ${card.meter.highLabel}`}>
          <div
            className="commentary-meter-fill"
            style={{ width: `${Math.round(Math.max(0, Math.min(1, card.meter.value)) * 100)}%` }}
          />
          <div className="commentary-meter-labels">
            <span>{card.meter.lowLabel}</span>
            <span>{card.meter.highLabel}</span>
          </div>
        </div>
      ) : null}
      {card.tags && card.tags.length > 0 ? (
        <ul className="commentary-tags" aria-label="Etiketler">
          {card.tags.map((t) => (
            <li key={t} className="commentary-tag">#{t}</li>
          ))}
        </ul>
      ) : null}
    </article>
  );
}

/**
 * LLM yorum kartları ızgarası (T3.8 — 07 §4). Disclaimer bu komponentin DIŞINDA gösterilir
 * (sayfa-seviyesi, her zaman görünür — CLAUDE.md §2). Sekmeli görünüm için `CommentaryTabs`.
 */
export function CommentaryCardList({
  cards,
  source,
}: {
  cards: CommentaryCard[];
  source: string;
}) {
  if (cards.length === 0) return null;
  const fallback = source === "fallback";

  return (
    <div className="commentary-list" data-source={source}>
      {cards.map((c, i) => (
        <CommentaryCardItem key={i} card={c} fallback={fallback} />
      ))}
    </div>
  );
}

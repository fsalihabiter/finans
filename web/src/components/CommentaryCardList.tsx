import type { CommentaryCard } from "@finans/shared";

/**
 * LLM yorum kartları (T3.8 — 07 §4). Disclaimer bu komponentin DIŞINDA gösterilir (sayfa-seviyesi,
 * her zaman görünür — CLAUDE.md §2). Burada salt gösterim: emoji + başlık + gövde + opsiyonel
 * meter (0..1) + opsiyonel etiketler.
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
        <article key={i} className={`commentary-card${fallback ? " is-fallback" : ""}`}>
          <header className="commentary-card-head">
            <span className="commentary-emoji" aria-hidden="true">{c.emoji}</span>
            <h3>{c.title}</h3>
          </header>
          <p className="commentary-body">{c.body}</p>
          {c.meter ? (
            <div className="commentary-meter" aria-label={`${c.meter.lowLabel} ↔ ${c.meter.highLabel}`}>
              <div
                className="commentary-meter-fill"
                style={{ width: `${Math.round(Math.max(0, Math.min(1, c.meter.value)) * 100)}%` }}
              />
              <div className="commentary-meter-labels">
                <span>{c.meter.lowLabel}</span>
                <span>{c.meter.highLabel}</span>
              </div>
            </div>
          ) : null}
          {c.tags && c.tags.length > 0 ? (
            <ul className="commentary-tags" aria-label="Etiketler">
              {c.tags.map((t) => (
                <li key={t} className="commentary-tag">#{t}</li>
              ))}
            </ul>
          ) : null}
        </article>
      ))}
    </div>
  );
}

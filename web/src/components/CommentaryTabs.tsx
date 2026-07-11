import { useEffect, useState } from "react";
import type { CommentaryCard } from "@finans/shared";
import { CommentaryCardItem } from "./CommentaryCardList";

/**
 * Sekmeli yorum gezgini (T4.5 — kullanıcı geri bildirimi: kart ızgarası sıkışık,
 * "slide/tab başlıklarıyla ilerleme daha iyi"). Üstte sekme şeridi (emoji + başlık),
 * altta TEK kart geniş okuma alanında; ok butonlarıyla önceki/sonraki. Erişilebilirlik:
 * tablist/tab/tabpanel rolleri + klavye ok tuşları.
 */
export function CommentaryTabs({
  cards,
  source,
}: {
  cards: CommentaryCard[];
  source: string;
}) {
  const [active, setActive] = useState(0);

  // Kartlar değişince (yeni sembol/yenileme) ilk sekmeye dön; taşmayı önle.
  useEffect(() => {
    setActive(0);
  }, [cards]);

  if (cards.length === 0) return null;
  const current = Math.min(active, cards.length - 1);

  const onKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "ArrowRight") setActive((a) => Math.min(a + 1, cards.length - 1));
    if (e.key === "ArrowLeft") setActive((a) => Math.max(a - 1, 0));
  };

  return (
    <div className="commentary-tabs" data-source={source}>
      <div className="ctab-strip" role="tablist" aria-label="Yorum başlıkları" onKeyDown={onKeyDown}>
        {cards.map((c, i) => (
          <button
            key={i}
            type="button"
            role="tab"
            id={`ctab-${i}`}
            aria-selected={i === current}
            aria-controls="ctab-panel"
            tabIndex={i === current ? 0 : -1}
            className={`ctab${i === current ? " on" : ""}`}
            onClick={() => setActive(i)}
          >
            <span aria-hidden="true">{c.emoji}</span>
            <span className="ctab-title">{c.title}</span>
          </button>
        ))}
      </div>

      <div id="ctab-panel" role="tabpanel" aria-labelledby={`ctab-${current}`} className="ctab-panel">
        <CommentaryCardItem card={cards[current]} fallback={source === "fallback"} />
      </div>

      <div className="ctab-nav">
        <button
          type="button"
          className="btn-ghost"
          onClick={() => setActive((a) => Math.max(a - 1, 0))}
          disabled={current === 0}
        >
          ← Önceki
        </button>
        <span className="ctab-count tnum">
          {current + 1} / {cards.length}
        </span>
        <button
          type="button"
          className="btn-ghost"
          onClick={() => setActive((a) => Math.min(a + 1, cards.length - 1))}
          disabled={current === cards.length - 1}
        >
          Sonraki →
        </button>
      </div>
    </div>
  );
}

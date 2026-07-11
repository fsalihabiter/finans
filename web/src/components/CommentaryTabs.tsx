import { useEffect, useRef, useState } from "react";
import type { CommentaryCard } from "@finans/shared";
import { CommentaryCardItem } from "./CommentaryCardList";

/**
 * Yorum gezgini (T4.5 + devamı — kullanıcı geri bildirimi: başlıklar üstte şerit yerine
 * SOLDA dikey ray olsun; darda accordion). İki görünüm, tek bileşen:
 *  - Geniş ekran: solda dikey başlık rayı (tablist, aria-orientation="vertical",
 *    klavye ↑/↓/Home/End), sağda TEK kart geniş okuma alanında + Önceki/Sonraki.
 *  - Dar ekran (≤720px): accordion — her başlık aria-expanded'lı buton, içerik altında açılır.
 * jsdom'da matchMedia yok → ray görünümü (testler tablist yolunu doğrular; accordion
 * testleri matchMedia'yı taklit eder).
 */
const NARROW_QUERY = "(max-width: 720px)";

function useIsNarrow(): boolean {
  const canMatch =
    typeof window !== "undefined" && typeof window.matchMedia === "function";
  const [narrow, setNarrow] = useState(() =>
    canMatch ? window.matchMedia(NARROW_QUERY).matches : false,
  );

  useEffect(() => {
    if (!canMatch) return;
    const mq = window.matchMedia(NARROW_QUERY);
    const onChange = (e: MediaQueryListEvent) => setNarrow(e.matches);
    mq.addEventListener("change", onChange);
    return () => mq.removeEventListener("change", onChange);
  }, [canMatch]);

  return narrow;
}

export function CommentaryTabs({
  cards,
  source,
}: {
  cards: CommentaryCard[];
  source: string;
}) {
  const [active, setActive] = useState(0);
  const tabRefs = useRef<(HTMLButtonElement | null)[]>([]);
  const isNarrow = useIsNarrow();
  const fallback = source === "fallback";

  // Kartlar değişince (yeni sembol/yenileme) ilk başlığa dön; taşmayı önle.
  useEffect(() => {
    setActive(0);
  }, [cards]);

  if (cards.length === 0) return null;

  // ───── Accordion (dar ekran): tek açık panel; açık başlığa tıklayınca kapanır ─────
  if (isNarrow) {
    const open = Math.min(active, cards.length - 1);
    return (
      <div className="commentary-tabs is-accordion" data-source={source}>
        {cards.map((c, i) => {
          const isOpen = i === open;
          return (
            <div
              key={i}
              className={`cacc-item${isOpen ? " open" : ""}${fallback ? " is-fallback" : ""}`}
            >
              <button
                type="button"
                className="cacc-head"
                id={`cacc-head-${i}`}
                aria-expanded={isOpen}
                aria-controls={`cacc-body-${i}`}
                onClick={() => setActive(isOpen ? -1 : i)}
              >
                <span className="ctab-emoji" aria-hidden="true">{c.emoji}</span>
                <span className="cacc-title">{c.title}</span>
                <span className="cacc-chevron" aria-hidden="true">▾</span>
              </button>
              {isOpen ? (
                <div
                  id={`cacc-body-${i}`}
                  role="region"
                  aria-labelledby={`cacc-head-${i}`}
                  className="cacc-body"
                >
                  <CommentaryCardItem card={c} fallback={fallback} />
                </div>
              ) : null}
            </div>
          );
        })}
      </div>
    );
  }

  // ───── Dikey ray (geniş ekran): solda başlıklar, sağda tek panel ─────
  const current = Math.min(Math.max(active, 0), cards.length - 1);

  const moveTo = (i: number) => {
    setActive(i);
    tabRefs.current[i]?.focus();
  };

  const onKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "ArrowDown" || e.key === "ArrowRight") {
      e.preventDefault();
      moveTo(Math.min(current + 1, cards.length - 1));
    }
    if (e.key === "ArrowUp" || e.key === "ArrowLeft") {
      e.preventDefault();
      moveTo(Math.max(current - 1, 0));
    }
    if (e.key === "Home") {
      e.preventDefault();
      moveTo(0);
    }
    if (e.key === "End") {
      e.preventDefault();
      moveTo(cards.length - 1);
    }
  };

  return (
    <div className="commentary-tabs is-rail" data-source={source}>
      <div
        className="ctab-rail"
        role="tablist"
        aria-label="Yorum başlıkları"
        aria-orientation="vertical"
        onKeyDown={onKeyDown}
      >
        {cards.map((c, i) => (
          <button
            key={i}
            type="button"
            role="tab"
            id={`ctab-${i}`}
            ref={(el) => {
              tabRefs.current[i] = el;
            }}
            aria-selected={i === current}
            aria-controls="ctab-panel"
            tabIndex={i === current ? 0 : -1}
            className={`ctab${i === current ? " on" : ""}`}
            onClick={() => setActive(i)}
          >
            <span className="ctab-emoji" aria-hidden="true">{c.emoji}</span>
            <span className="ctab-title">{c.title}</span>
          </button>
        ))}
      </div>

      <div className="ctab-main">
        {/* key: başlık değişince panel yeniden çizilir → giriş animasyonu her seçimde oynar. */}
        <div
          key={current}
          id="ctab-panel"
          role="tabpanel"
          aria-labelledby={`ctab-${current}`}
          className="ctab-panel"
        >
          <CommentaryCardItem card={cards[current]} fallback={fallback} />
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
    </div>
  );
}

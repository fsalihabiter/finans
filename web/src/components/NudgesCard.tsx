import type { Nudge } from "@finans/shared";

/**
 * Kural tabanlı eğitici notlar (GET /portfolio/nudges, T2.6). Durumu açıklar ve
 * çerçeve sunar — **yatırım tavsiyesi değildir** (CLAUDE.md §2, NFR-2: disclaimer).
 * Not yoksa hiçbir şey çizmez (sağlıklı/boş portföy). Salt gösterim.
 */
export function NudgesCard({ nudges }: { nudges: Nudge[] }) {
  if (nudges.length === 0) return null;

  return (
    <div className="card">
      <div className="card-head">
        <h3>Eğitici Notlar</h3>
        <span className="mini">Farkındalık — tavsiye değil</span>
      </div>
      <div className="nudge-list">
        {nudges.map((n) => (
          <div key={n.id} className={`nudge nudge-${n.severity.toLowerCase()}`}>
            <div className="nudge-ic" aria-hidden="true">{n.icon}</div>
            <div className="nudge-tx">
              <b>{n.title}.</b> {n.body}
            </div>
          </div>
        ))}
      </div>
      <p className="note-muted">Bu notlar eğitim amaçlıdır, yatırım tavsiyesi değildir.</p>
    </div>
  );
}

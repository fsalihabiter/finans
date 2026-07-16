import { formatPercent } from "@finans/shared";
import type { Holding, PortfolioSummary } from "@finans/shared";
import { ASSET_META } from "../lib/assetMeta";

/** Portföyden türetilen içgörüler: en iyi/zayıf kalem, hızlı bilgi, yoğunlaşma nudge'ı.
 *  Tümü gerçek veriden hesaplanır — yatırım tavsiyesi değil, farkındalık (CLAUDE.md §2). */
export function PortfolioInsights({
  summary,
  holdings,
}: {
  summary: PortfolioSummary;
  holdings: Holding[];
}) {
  const withReturn = holdings.filter((h) => h.returnRatio !== null);
  const best = withReturn.reduce<Holding | null>(
    (a, h) => (a === null || h.returnRatio! > a.returnRatio! ? h : a),
    null,
  );
  const worst = withReturn.reduce<Holding | null>(
    (a, h) => (a === null || h.returnRatio! < a.returnRatio! ? h : a),
    null,
  );

  const cashWeight = summary.allocation
    .filter((a) => a.assetType === "Cash")
    .reduce((s, a) => s + a.weight, 0);

  const sorted = [...summary.allocation].sort((a, b) => b.weight - a.weight);
  const top2 = sorted.slice(0, 2);
  const top2Weight = top2.reduce((s, a) => s + a.weight, 0);

  const asOf = new Date(summary.asOf);
  const asOfText = Number.isNaN(asOf.getTime())
    ? "—"
    : new Intl.DateTimeFormat("tr-TR", { hour: "2-digit", minute: "2-digit" }).format(asOf);

  return (
    <>
      <div className="grid-3">
        <div className="card">
          <div className="card-head"><h3>En İyi & En Zayıf</h3></div>
          <div className="bestworst">
            <div className="bw-item">
              <div className="bw-t">En iyi</div>
              <div className="bw-n">{best ? best.name : "—"}</div>
              <div className={`bw-g tnum ${best && best.returnRatio! > 0 ? "up" : "down"}`}>
                {best ? (
                  <>
                    <span className="delta-arrow" aria-hidden="true">{best.returnRatio! > 0 ? "▲" : "▼"}</span>
                    {formatPercent(best.returnRatio!)}
                  </>
                ) : (
                  "—"
                )}
              </div>
            </div>
            <div className="bw-item">
              <div className="bw-t">En zayıf</div>
              <div className="bw-n">{worst ? worst.name : "—"}</div>
              <div className={`bw-g tnum ${worst && worst.returnRatio! >= 0 ? "up" : "down"}`}>
                {worst ? (
                  <>
                    <span className="delta-arrow" aria-hidden="true">{worst.returnRatio! >= 0 ? "▲" : "▼"}</span>
                    {formatPercent(worst.returnRatio!)}
                  </>
                ) : (
                  "—"
                )}
              </div>
            </div>
          </div>
          <p className="note-muted">
            Getiri uçları normaldir; biri kazanırken diğeri kaybedebilir. Çeşitlendirmenin amacı bu
            dalgalanmayı yumuşatmaktır.
          </p>
        </div>

        <div className="card">
          <div className="card-head"><h3>Hızlı Bilgi</h3></div>
          <div className="quickinfo">
            <div className="qi"><span>Nakit oranı</span><b className="tnum">{formatPercent(cashWeight, 1, true, false)}</b></div>
            <div className="qi"><span>İlk 2 kalem ağırlığı</span><b className="tnum">{formatPercent(top2Weight, 1, true, false)}</b></div>
            <div className="qi"><span>Pozisyon sayısı</span><b className="tnum">{holdings.length}</b></div>
            <div className="qi"><span>Son güncelleme</span><b>{asOfText}</b></div>
          </div>
        </div>

        <div className="card">
          <div className="card-head"><h3>Yoğunlaşma</h3></div>
          <div className="bestworst">
            {top2.map((a) => (
              <div className="bw-item" key={a.assetType + a.name}>
                <div className="bw-t">{ASSET_META[a.assetType].icon} {a.name}</div>
                <div className="bw-n tnum">{formatPercent(a.weight, 1, true, false)}</div>
              </div>
            ))}
          </div>
          <div
            className="conc-bar"
            role="img"
            aria-label={`İlk iki kalem portföyün ${formatPercent(top2Weight, 1, true, false)}'ini oluşturur`}
          >
            {top2.map((a, i) => (
              <i
                key={a.assetType + a.name}
                style={{
                  width: `${a.weight * 100}%`,
                  background: i === 0 ? "var(--accent, #8a94dc)" : "var(--accent-soft, #a6aee8)",
                }}
              />
            ))}
          </div>
          <p className="note-muted">
            İlk iki kalem toplam <b>{formatPercent(top2Weight, 1, true, false)}</b>. Yüksek
            yoğunlaşma, bu kalemler birlikte düştüğünde etkiyi büyütür.
          </p>
        </div>
      </div>
    </>
  );
}

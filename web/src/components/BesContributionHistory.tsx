import { useMemo } from "react";
import { formatCurrency, formatDate, formatPercent } from "@finans/shared";
import type { BesContribution, BesContributionStatus } from "@finans/shared";

/** Devlet katkısının katkı payına etkin oranı ("%30" gibi); katkı payı 0 ise gösterilmez. */
function stateRate(own: number, state: number): string | null {
  if (own <= 0 || state <= 0) return null;
  return formatPercent(state / own, 0, true, false);
}

const STATUS_CLS: Record<BesContributionStatus, string> = {
  Deposited: "hist-deposited",
  StatePending: "hist-pending",
  Future: "hist-future",
};

/**
 * BES katkı ödeme geçmişi (en yeni üstte): tarih + katkı payı + devlet katkısı + satır içi düzenle/sil.
 * Durum **renkli sol şerit** + lejant ile gösterilir (ayrı sütun YOK — lejant zaten anlamı verir).
 * Bekleyenler (sarı/gri) tabloda görünür ama toplama girmez.
 *
 * <p>Alttaki <b>toplam satırı</b> (`tfoot`) <b>Ödenmiş</b> (Deposited+StatePending) ve gerekirse
 * <b>Bekleyen</b> (Future) toplamlarını gösterir; "Ödenmiş katkı payı toplamı" backend'in
 * `AvgCost` (Ortalama maliyet) hesabıyla <b>birebir eşittir</b> — sütun toplamı gözle doğrulanır.</p>
 */
export function BesContributionHistory({
  contributions,
  onEdit,
  onDelete,
}: {
  contributions: BesContribution[];
  onEdit?: (c: BesContribution) => void;
  onDelete?: (c: BesContribution) => void;
}) {
  // Tek geçişte ödenmiş + bekleyen toplamları (own + state).
  const totals = useMemo(() => {
    let paidOwn = 0, paidState = 0, futureOwn = 0, futureState = 0;
    for (const c of contributions) {
      if (c.status === "Future") {
        futureOwn += c.ownAmount;
        futureState += c.stateAmount;
      } else {
        // Deposited + StatePending: kendi katkı ödendi → maliyet tabanına dahil.
        paidOwn += c.ownAmount;
        paidState += c.stateAmount;
      }
    }
    return { paidOwn, paidState, futureOwn, futureState };
  }, [contributions]);

  if (contributions.length === 0)
    return <p className="muted">Henüz katkı kaydı yok.</p>;

  return (
    <>
      <div className="hist-legend" aria-hidden="true">
        <span className="lg dep">Yatırıldı</span>
        <span className="lg pen">Devlet bekliyor</span>
        <span className="lg fut">Gelecek ödeme</span>
        <span className="lg" style={{ background: "none" }}>· Bekleyenler toplamlara dahil değildir</span>
      </div>
      <div className="history-scroll">
        <table className="holdings-table fit">
          <thead>
            <tr>
              <th scope="col">Tarih</th>
              <th scope="col" className="num">Katkı Payı</th>
              <th scope="col" className="num">Devlet</th>
              <th scope="col" className="num">İşlem</th>
            </tr>
          </thead>
          <tbody>
            {contributions.map((c) => {
              const opening = c.source === "Opening";
              const rate = stateRate(c.ownAmount, c.stateAmount);
              return (
                <tr key={c.id} className={`hist-row ${STATUS_CLS[c.status]}${opening ? " hist-opening" : ""}`}>
                  <td>{opening ? "Açılış" : formatDate(c.paidAtUtc)}</td>
                  <td className="num">{formatCurrency(c.ownAmount, "TRY")}</td>
                  <td className="num up">
                    {formatCurrency(c.stateAmount, "TRY")}
                    {rate && <span className="hist-rate tnum">{rate}</span>}
                  </td>
                  <td className="num">
                    <span className="row-actions">
                      <button type="button" className="icon-btn" aria-label="Düzenle" title="Düzenle" onClick={() => onEdit?.(c)}>✎</button>
                      <button type="button" className="icon-btn danger" aria-label="Sil" title="Sil" onClick={() => onDelete?.(c)}>🗑</button>
                    </span>
                  </td>
                </tr>
              );
            })}
          </tbody>
          <tfoot>
            <tr className="hist-total hist-total--paid">
              <th scope="row">Ödenmiş toplam</th>
              <td className="num">{formatCurrency(totals.paidOwn, "TRY")}</td>
              <td className="num up">
                {formatCurrency(totals.paidState, "TRY")}
                {stateRate(totals.paidOwn, totals.paidState) && (
                  <span className="hist-rate tnum">{stateRate(totals.paidOwn, totals.paidState)}</span>
                )}
              </td>
              <td></td>
            </tr>
            {(totals.futureOwn > 0 || totals.futureState > 0) && (
              <tr className="hist-total hist-total--future">
                <th scope="row">Bekleyen (toplama dahil değil)</th>
                <td className="num">{formatCurrency(totals.futureOwn, "TRY")}</td>
                <td className="num">{formatCurrency(totals.futureState, "TRY")}</td>
                <td></td>
              </tr>
            )}
          </tfoot>
        </table>
      </div>
    </>
  );
}

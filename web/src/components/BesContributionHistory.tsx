import { formatCurrency, formatDate } from "@finans/shared";
import type { BesContribution, BesContributionStatus } from "@finans/shared";

const STATUS_CLS: Record<BesContributionStatus, string> = {
  Deposited: "hist-deposited",
  StatePending: "hist-pending",
  Future: "hist-future",
};

/**
 * BES katkı ödeme geçmişi (en yeni üstte): tarih + kendi/devlet katkısı + satır içi düzenle/sil.
 * Durum **renkli sol şerit** + lejant ile gösterilir (ayrı sütun YOK — lejant zaten anlamı verir).
 * Bekleyenler (sarı/gri) tabloda görünür ama toplama girmez. Dikey kaydırma, yatay yok; sütunlar
 * sığar (kesilmez/elips yok).
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
              return (
                <tr key={c.id} className={`hist-row ${STATUS_CLS[c.status]}${opening ? " hist-opening" : ""}`}>
                  <td>{opening ? "Açılış" : formatDate(c.paidAtUtc)}</td>
                  <td className="num">{formatCurrency(c.ownAmount, "TRY")}</td>
                  <td className="num up">{formatCurrency(c.stateAmount, "TRY")}</td>
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
        </table>
      </div>
    </>
  );
}

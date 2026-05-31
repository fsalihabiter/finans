import { formatCurrency, formatDate } from "@finans/shared";
import type { BesContribution } from "@finans/shared";

/**
 * BES katkı ödeme geçmişi (en yeni üstte): tarih + kendi/devlet katkısı + satır içi
 * **düzenle/sil** ikon butonları. Dikey kaydırma (`.history-scroll`), yatay kaydırma yok.
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
          {contributions.map((c) => (
            <tr key={c.id}>
              <td>{formatDate(c.paidAtUtc)}</td>
              <td className="num">{formatCurrency(c.ownAmount, "TRY")}</td>
              <td className="num up">{formatCurrency(c.stateAmount, "TRY")}</td>
              <td className="num">
                <span className="row-actions">
                  <button type="button" className="icon-btn" aria-label="Düzenle" title="Düzenle" onClick={() => onEdit?.(c)}>✎</button>
                  <button type="button" className="icon-btn danger" aria-label="Sil" title="Sil" onClick={() => onDelete?.(c)}>🗑</button>
                </span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

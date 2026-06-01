import { formatCurrency, formatDate, formatNumber } from "@finans/shared";
import type { CurrencyCode, Transaction } from "@finans/shared";

/**
 * Pozisyonun geçmiş işlemleri (alış/satış). En yeni üstte. Birim fiyat varlığın
 * kendi para biriminde. Boşsa "henüz işlem yok". Satır içi düzenle/sil ikonları
 * (BES katkı geçmişiyle aynı UX). Son işlemi silmek yasak → backend 400 döner.
 */
export function TransactionHistory({
  transactions,
  currency,
  unit,
  cash = false,
  onEdit,
  onDelete,
}: {
  transactions: Transaction[];
  currency: CurrencyCode;
  unit: string;
  cash?: boolean;
  onEdit?: (t: Transaction) => void;
  onDelete?: (t: Transaction) => void;
}) {
  const typeLabel = (type: Transaction["type"]) =>
    cash
      ? type === "Buy" ? "Para eklendi" : "Para çıkarıldı"
      : type === "Buy" ? "Alış" : "Satış";
  const canEdit = Boolean(onEdit || onDelete);
  return (
    <section className="tx-history">
      {transactions.length === 0 ? (
        <p className="muted">Henüz işlem yok.</p>
      ) : (
        <div className="history-scroll">
          <table className="holdings-table fit">
            <thead>
              <tr>
                <th scope="col">Tarih</th>
                <th scope="col">Tür</th>
                <th scope="col" className="num">Miktar</th>
                <th scope="col" className="num">Birim fiyat</th>
                <th scope="col" className="num">Tutar</th>
                {canEdit && <th scope="col" className="num">İşlem</th>}
              </tr>
            </thead>
            <tbody>
              {transactions.map((t) => (
                <tr key={t.id}>
                  <td>{formatDate(t.transactedAtUtc)}</td>
                  <td className={t.type === "Buy" ? "pos" : "neg"}>
                    {typeLabel(t.type)}
                  </td>
                  <td className="num">
                    {formatNumber(t.quantity)} <span className="muted">{unit}</span>
                  </td>
                  <td className="num">{formatCurrency(t.unitPrice, currency)}</td>
                  <td className="num">{formatCurrency(t.quantity * t.unitPrice + t.fee, currency)}</td>
                  {canEdit && (
                    <td className="num">
                      <span className="row-actions">
                        {onEdit && (
                          <button type="button" className="icon-btn" aria-label="Düzenle" title="Düzenle" onClick={() => onEdit(t)}>✎</button>
                        )}
                        {onDelete && (
                          <button type="button" className="icon-btn danger" aria-label="Sil" title="Sil" onClick={() => onDelete(t)}>🗑</button>
                        )}
                      </span>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

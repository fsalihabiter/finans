import { formatCurrency, formatNumber } from "@finans/shared";
import type { CurrencyCode, Transaction } from "@finans/shared";

/** ISO tarihi tr-TR kısa biçimde (gün ay yıl). */
function formatDate(iso: string): string {
  const d = new Date(iso);
  return Number.isNaN(d.getTime())
    ? "—"
    : new Intl.DateTimeFormat("tr-TR", { day: "2-digit", month: "short", year: "numeric" }).format(d);
}

/**
 * Pozisyonun geçmiş işlemleri (alış/satış). En yeni üstte. Birim fiyat varlığın
 * kendi para biriminde. Boşsa "henüz işlem yok".
 */
export function TransactionHistory({
  transactions,
  currency,
  unit,
}: {
  transactions: Transaction[];
  currency: CurrencyCode;
  unit: string;
}) {
  return (
    <section className="tx-history">
      <h2>İşlem geçmişi</h2>
      {transactions.length === 0 ? (
        <p className="muted">Henüz işlem yok.</p>
      ) : (
        <div className="holdings-wrap">
          <table className="holdings-table">
            <thead>
              <tr>
                <th scope="col">Tarih</th>
                <th scope="col">Tür</th>
                <th scope="col" className="num">Miktar</th>
                <th scope="col" className="num">Birim fiyat</th>
                <th scope="col" className="num">Tutar</th>
              </tr>
            </thead>
            <tbody>
              {transactions.map((t) => (
                <tr key={t.id}>
                  <td>{formatDate(t.transactedAtUtc)}</td>
                  <td className={t.type === "Buy" ? "pos" : "neg"}>
                    {t.type === "Buy" ? "Alış" : "Satış"}
                  </td>
                  <td className="num">
                    {formatNumber(t.quantity)} <span className="muted">{unit}</span>
                  </td>
                  <td className="num">{formatCurrency(t.unitPrice, currency)}</td>
                  <td className="num">{formatCurrency(t.quantity * t.unitPrice + t.fee, currency)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

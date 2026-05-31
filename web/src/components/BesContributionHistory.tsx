import { formatCurrency } from "@finans/shared";
import type { BesContribution } from "@finans/shared";

function formatDate(iso: string): string {
  const d = new Date(iso);
  return Number.isNaN(d.getTime())
    ? "—"
    : new Intl.DateTimeFormat("tr-TR", { day: "2-digit", month: "short", year: "numeric" }).format(d);
}

/** BES katkı ödeme geçmişi (en yeni üstte): tarih + kendi/devlet katkısı + kaynak (Düzenli/Tekil). */
export function BesContributionHistory({ contributions }: { contributions: BesContribution[] }) {
  if (contributions.length === 0)
    return <p className="muted">Henüz katkı kaydı yok.</p>;

  return (
    <div className="holdings-wrap">
      <table className="holdings-table">
        <thead>
          <tr>
            <th scope="col">Tarih</th>
            <th scope="col" className="num">Kendi</th>
            <th scope="col" className="num">Devlet</th>
            <th scope="col">Kaynak</th>
          </tr>
        </thead>
        <tbody>
          {contributions.map((c, i) => (
            <tr key={`${c.paidAtUtc}-${i}`}>
              <td>{formatDate(c.paidAtUtc)}</td>
              <td className="num">{formatCurrency(c.ownAmount, "TRY")}</td>
              <td className="num up">{formatCurrency(c.stateAmount, "TRY")}</td>
              <td>{c.source === "Plan" ? "Düzenli" : "Tekil"}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

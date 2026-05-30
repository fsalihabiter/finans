import { useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { formatCurrency, formatNumber, formatPercent } from "@finans/shared";
import { AddTransactionForm } from "../components/AddTransactionForm";
import { BesContributionForm } from "../components/BesContributionForm";
import { TransactionHistory } from "../components/TransactionHistory";
import { useDeleteHolding, useHolding, useUpdateHolding } from "../lib/hooks";

function tone(value: number | null): string {
  if (value === null || value === 0) return "";
  return value > 0 ? "pos" : "neg";
}

function formatDate(iso: string): string {
  const d = new Date(iso);
  return Number.isNaN(d.getTime())
    ? "—"
    : new Intl.DateTimeFormat("tr-TR", { day: "2-digit", month: "long", year: "numeric" }).format(d);
}

/**
 * Varlık detayı (13 §4, route `/holdings/:id`): metrikler + BES (devlet katkısı ayrı) +
 * güncel fiyatı elle güncelle (FR-1.8) + pozisyonu sil. Geçerli kullanıcıya kapsanır
 * (backend); başkasının id'si → 404 → "bulunamadı".
 */
export function HoldingDetailPage() {
  const { id = "" } = useParams();
  const navigate = useNavigate();
  const holding = useHolding(id);
  const updatePrice = useUpdateHolding(id);
  const remove = useDeleteHolding();

  const [price, setPrice] = useState("");

  if (holding.isLoading) return <p className="muted">Yükleniyor…</p>;
  if (holding.isError || !holding.data) {
    return (
      <section>
        <p className="neg" role="alert">Pozisyon bulunamadı.</p>
        <Link to="/">← Portföye dön</Link>
      </section>
    );
  }

  const h = holding.data;
  const isBes = h.bes !== null;

  const onUpdatePrice = (e: React.FormEvent) => {
    e.preventDefault();
    const value = Number(price.replace(",", "."));
    if (!Number.isFinite(value) || value < 0) return;
    updatePrice.mutate({ currentPrice: value }, { onSuccess: () => setPrice("") });
  };

  const onDelete = () => {
    if (!window.confirm(`"${h.name}" pozisyonunu silmek istiyor musun?`)) return;
    remove.mutate(h.id, { onSuccess: () => navigate("/") });
  };

  return (
    <section className="detail">
      <Link to="/" className="muted">← Portföy</Link>
      <header className="page-head">
        <h1>
          {h.name}
          {h.symbol && <span className="muted detail-symbol"> {h.symbol}</span>}
        </h1>
        <button type="button" className="btn-danger" onClick={onDelete} disabled={remove.isPending}>
          Sil
        </button>
      </header>

      <dl className="detail-stats">
        <div><dt>Miktar</dt><dd>{formatNumber(h.quantity)} {h.unit}</dd></div>
        <div><dt>Ort. maliyet</dt><dd>{formatCurrency(h.avgCost, h.currency)}</dd></div>
        <div><dt>Güncel fiyat</dt><dd>{h.currentPrice === null ? "—" : formatCurrency(h.currentPrice, h.currency)}</dd></div>
        <div><dt>Toplam maliyet</dt><dd>{formatCurrency(h.totalCost, h.currency)}</dd></div>
        <div><dt>Değer</dt><dd>{h.currentValue === null ? "—" : formatCurrency(h.currentValue, h.currency)}</dd></div>
        <div><dt>Kâr</dt><dd className={tone(h.profit)}>{h.profit === null ? "—" : formatCurrency(h.profit, h.currency)}</dd></div>
        <div><dt>Getiri</dt><dd className={tone(h.returnRatio)}>{h.returnRatio === null ? "—" : formatPercent(h.returnRatio)}</dd></div>
        <div><dt>Portföy ağırlığı</dt><dd>{formatPercent(h.weight, 1, true, false)}</dd></div>
      </dl>

      {h.bes && (
        <div className="detail-bes">
          <h2>Bireysel Emeklilik (BES)</h2>
          <dl className="detail-stats">
            <div><dt>Kendi katkın</dt><dd>{formatCurrency(h.bes.ownContribution, h.currency)}</dd></div>
            <div><dt>Devlet katkısı</dt><dd className="pos">{formatCurrency(h.bes.stateContribution, h.currency)}</dd></div>
            <div><dt>Hak ediş</dt><dd>{h.bes.vestingState}</dd></div>
            <div><dt>Başlangıç</dt><dd>{h.bes.joinedAtUtc ? formatDate(h.bes.joinedAtUtc) : "—"}</dd></div>
          </dl>
        </div>
      )}

      {/* BES nominal hesap → aylık katkı; diğer pozisyonlar → alış/satış işlemi. */}
      {isBes ? (
        <BesContributionForm holdingId={h.id} />
      ) : (
        <AddTransactionForm holdingId={h.id} currency={h.currency} unit={h.unit} />
      )}

      <TransactionHistory
        transactions={h.transactions ?? []}
        currency={h.currency}
        unit={h.unit}
      />

      <form className="price-form" onSubmit={onUpdatePrice}>
        <label htmlFor="price">
          {isBes ? "Güncel fon değerini güncelle" : "Güncel fiyatı güncelle"} ({h.currency})
        </label>
        <div className="price-row">
          <input
            id="price"
            inputMode="decimal"
            placeholder={h.currentPrice === null ? "örn. 6500" : formatNumber(h.currentPrice)}
            value={price}
            onChange={(e) => setPrice(e.target.value)}
          />
          <button type="submit" disabled={updatePrice.isPending || price.trim() === ""}>
            Güncelle
          </button>
        </div>
        {updatePrice.isError && <p className="neg">Güncelleme başarısız.</p>}
      </form>
    </section>
  );
}

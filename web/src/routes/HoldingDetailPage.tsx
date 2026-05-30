import { useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { formatCurrency, formatNumber, formatPercent } from "@finans/shared";
import { AddTransactionForm } from "../components/AddTransactionForm";
import { BesContributionForm } from "../components/BesContributionForm";
import { TransactionHistory } from "../components/TransactionHistory";
import { useDeleteHolding, useHolding, useUpdateHolding } from "../lib/hooks";
import { ASSET_META, softBg } from "../lib/assetMeta";

function tone(value: number | null): string {
  if (value === null || value === 0) return "";
  return value > 0 ? "up" : "down";
}

function formatDate(iso: string): string {
  const d = new Date(iso);
  return Number.isNaN(d.getTime())
    ? "—"
    : new Intl.DateTimeFormat("tr-TR", { day: "2-digit", month: "long", year: "numeric" }).format(d);
}

const VESTING_TR: Record<string, string> = {
  NotVested: "Hak edilmedi",
  PartiallyVested: "Kısmen hak edildi",
  Vested: "Tamamlandı",
};

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

  const meta = ASSET_META[h.assetType];
  const profitSign = h.profit !== null && h.profit > 0 ? "+" : "";

  return (
    <section className="detail">
      <Link to="/" className="detail-back">← Portföy</Link>

      <div className="detail-head">
        <div className="asset-ic" style={{ background: softBg(meta.color) }}>{meta.icon}</div>
        <div>
          <h1>
            {h.name}
            {h.symbol && <span className="muted"> {h.symbol}</span>}
          </h1>
          <div className="dh-sub">{formatNumber(h.quantity)} {h.unit} · {meta.label}</div>
        </div>
        <div className="dh-sp" />
        <button type="button" className="btn-danger" onClick={onDelete} disabled={remove.isPending}>
          Sil
        </button>
      </div>

      <div className="detail-hero">
        <div className="dh-v tnum">
          {h.currentValue === null ? "—" : formatCurrency(h.currentValue, h.currency)}
        </div>
        <div className={`dh-g tnum ${tone(h.profit)}`}>
          {h.profit === null ? "—" : `${profitSign}${formatCurrency(h.profit, h.currency)}`}
          {h.returnRatio !== null && ` · ${formatPercent(h.returnRatio)}`}
        </div>
      </div>

      <div className="drow"><span className="dk">Miktar</span><span className="dv tnum">{formatNumber(h.quantity)} {h.unit}</span></div>
      <div className="drow"><span className="dk">Ortalama maliyet</span><span className="dv tnum">{formatCurrency(h.avgCost, h.currency)}</span></div>
      <div className="drow"><span className="dk">Güncel fiyat</span><span className="dv tnum">{h.currentPrice === null ? "—" : formatCurrency(h.currentPrice, h.currency)}</span></div>
      <div className="drow"><span className="dk">Toplam maliyet</span><span className="dv tnum">{formatCurrency(h.totalCost, h.currency)}</span></div>
      <div className="drow"><span className="dk">Portföy ağırlığı</span><span className="dv tnum">{formatPercent(h.weight, 1, true, false)}</span></div>

      {h.bes && (
        <>
          <div className="detail-section-title">Bireysel Emeklilik (BES)</div>
          <div className="split">
            <div className="sh"><span className="sl">Kendi katkın</span><span className="sr tnum">{formatCurrency(h.bes.ownContribution, h.currency)}</span></div>
            <div className="sd">Senin yatırdığın katkı payları — gerçek <b>yatırım performansının</b> tabanı.</div>
          </div>
          <div className="split">
            <div className="sh"><span className="sl">Devlet katkısı</span><span className="sr tnum up">{formatCurrency(h.bes.stateContribution, h.currency)}</span></div>
            <div className="sd">Devletin eklediği <b>sübvansiyon</b> (≈%30). Yatırım başarın değildir; bu yüzden ayrı gösterilir.</div>
          </div>
          <div className="drow"><span className="dk">Hak ediş</span><span className="dv">{VESTING_TR[h.bes.vestingState] ?? h.bes.vestingState}</span></div>
          <div className="drow"><span className="dk">Başlangıç</span><span className="dv">{h.bes.joinedAtUtc ? formatDate(h.bes.joinedAtUtc) : "—"}</span></div>
        </>
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

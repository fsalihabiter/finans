import { useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { formatCurrency, formatDate, formatNumber, formatPercent } from "@finans/shared";
import type { BesContribution, TransactionType } from "@finans/shared";
import { AddTransactionForm } from "../components/AddTransactionForm";
import { BesContributionForm } from "../components/BesContributionForm";
import { BesContributionPlanForm } from "../components/BesContributionPlanForm";
import { BesContributionHistory } from "../components/BesContributionHistory";
import { TransactionHistory } from "../components/TransactionHistory";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { Modal } from "../components/Modal";
import { useToast } from "../components/Toast";
import {
  useDeleteBesContribution,
  useDeleteHolding,
  useHolding,
  useUpdateBes,
  useUpdateBesContribution,
  useUpdateHolding,
} from "../lib/hooks";
import { ASSET_META, softBg } from "../lib/assetMeta";

function tone(value: number | null): string {
  if (value === null || value === 0) return "";
  return value > 0 ? "up" : "down";
}

const VESTING_TR: Record<string, string> = {
  NotVested: "Hak edilmedi",
  PartiallyVested: "Kısmen hak edildi",
  Vested: "Tamamlandı",
};

type ActiveModal = null | "tx" | "bes" | "price" | "besdate" | "besplan";

/** ISO tarihten <input type="date"> değeri (YYYY-MM-DD). */
function toDateInput(iso: string | null): string {
  if (!iso) return "";
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? "" : d.toISOString().slice(0, 10);
}

/**
 * Varlık detayı (13 §4, route `/holdings/:id`): geniş ekranda 2 sütun (özet/metrik +
 * BES | işlem geçmişi). İşlem ekleme / fiyat güncelleme / BES katkısı artık **modalda**
 * (#2 — sayfada yoğunluk yok); buton tetikler. Sil → onay diyaloğu + danger-zone.
 * Geçerli kullanıcıya kapsanır (backend); başkasının id'si → 404.
 */
export function HoldingDetailPage() {
  const { id = "" } = useParams();
  const navigate = useNavigate();
  const holding = useHolding(id);
  const updatePrice = useUpdateHolding(id);
  const updateBes = useUpdateBes(id);
  const updateContribution = useUpdateBesContribution(id);
  const deleteContribution = useDeleteBesContribution(id);
  const remove = useDeleteHolding();
  const { notify } = useToast();

  const [price, setPrice] = useState("");
  const [besDate, setBesDate] = useState("");
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [modal, setModal] = useState<ActiveModal>(null);
  const [editingC, setEditingC] = useState<BesContribution | null>(null);
  const [editOwn, setEditOwn] = useState("");
  const [editDate, setEditDate] = useState("");
  const [deletingC, setDeletingC] = useState<BesContribution | null>(null);
  const today = new Date().toISOString().slice(0, 10);

  if (holding.isLoading) return <p className="muted">Yükleniyor…</p>;
  if (holding.isError || !holding.data) {
    return (
      <section className="detail">
        <p className="neg" role="alert">Pozisyon bulunamadı.</p>
        <Link to="/" className="detail-back">← Portföye dön</Link>
      </section>
    );
  }

  const h = holding.data;
  const isBes = h.bes !== null;

  // Elle fiyat girişi yalnız fiyatı sabit/canlı OLMAYAN varlıklarda anlamlı:
  // Nakit → sabit ₺1 + "para ekle/çıkar"; Altın/Döviz → canlı kaynaktan (otomatik, elle giriş ezilir).
  const isCash = h.assetType === "Cash";
  const isLivePriced = h.assetType === "Gold" || h.assetType === "Fx";
  const canEditPrice = !isCash && !isLivePriced;

  const closeModal = () => setModal(null);

  const onUpdatePrice = (e: React.FormEvent) => {
    e.preventDefault();
    const value = Number(price.replace(",", "."));
    if (!Number.isFinite(value) || value < 0) return;
    updatePrice.mutate(
      { currentPrice: value },
      {
        onSuccess: () => {
          setPrice("");
          closeModal();
          notify("Güncel fiyat güncellendi.", "success");
        },
      },
    );
  };

  const onTxDone = (type: TransactionType) => {
    closeModal();
    const msg = isCash
      ? type === "Buy" ? "Para eklendi." : "Para çıkarıldı."
      : type === "Buy" ? "Alış işlemi eklendi." : "Satış işlemi eklendi.";
    notify(msg, "success");
  };

  const onBesDone = () => {
    closeModal();
    notify("BES katkısı eklendi.", "success");
  };

  const onBesPlanDone = () => {
    closeModal();
    notify("Düzenli katkı kayıtları oluşturuldu.", "success");
  };

  const openEditContribution = (c: BesContribution) => {
    setEditOwn(String(c.ownAmount));
    setEditDate(toDateInput(c.paidAtUtc));
    setEditingC(c);
  };

  const onEditContributionSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingC) return;
    const own = Number(editOwn.replace(",", "."));
    if (!Number.isFinite(own) || own <= 0 || editDate === "") return;
    updateContribution.mutate(
      { contributionId: editingC.id, input: { ownAmount: own, paidAtUtc: `${editDate}T00:00:00Z` } },
      { onSuccess: () => { setEditingC(null); notify("Katkı güncellendi.", "success"); } },
    );
  };

  const onDeleteContribution = () => {
    if (!deletingC) return;
    deleteContribution.mutate(deletingC.id, {
      onSuccess: () => { setDeletingC(null); notify("Katkı silindi.", "info"); },
    });
  };

  const openBesDate = () => {
    setBesDate(toDateInput(h.bes?.joinedAtUtc ?? null));
    setModal("besdate");
  };

  const onUpdateBesDate = (e: React.FormEvent) => {
    e.preventDefault();
    if (!besDate) return;
    updateBes.mutate(
      { joinedAtUtc: `${besDate}T00:00:00Z` },
      {
        onSuccess: () => {
          closeModal();
          notify("Başlangıç tarihi güncellendi.", "success");
        },
      },
    );
  };

  const onDelete = () => {
    remove.mutate(h.id, {
      onSuccess: () => {
        notify(`"${h.name}" silindi.`, "info");
        navigate("/");
      },
    });
  };

  const meta = ASSET_META[h.assetType];
  const profitSign = h.profit !== null && h.profit > 0 ? "+" : "";
  const priceLabel = isBes ? "Fon değerini güncelle" : "Fiyatı güncelle";

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
      </div>

      <div className="detail-grid">
        {/* Sol sütun: özet + metrikler + BES */}
        <div className="detail-col">
          <div className="detail-hero">
            <div className="dh-v tnum">
              {h.currentValue === null ? "—" : formatCurrency(h.currentValue, h.currency)}
            </div>
            <div className={`dh-g tnum ${tone(h.profit)}`}>
              {h.profit === null ? "—" : `${profitSign}${formatCurrency(h.profit, h.currency)}`}
              {h.returnRatio !== null && ` · ${formatPercent(h.returnRatio)}`}
            </div>
          </div>

          <div className="detail-actions">
            {isBes ? (
              <>
                <button type="button" className="btn-primary" onClick={() => setModal("bes")}>
                  ＋ Aylık katkı ekle
                </button>
                <button type="button" className="btn-ghost" onClick={() => setModal("besplan")}>
                  Düzenli katkı / geçmiş
                </button>
              </>
            ) : (
              <button type="button" className="btn-primary" onClick={() => setModal("tx")}>
                ＋ {isCash ? "Para ekle / çıkar" : "İşlem ekle"}
              </button>
            )}
            {canEditPrice && (
              <button type="button" className="btn-ghost outlined" onClick={() => setModal("price")}>
                {priceLabel}
              </button>
            )}
          </div>
          {!canEditPrice && (
            <p className="note-muted">
              {isCash
                ? "Nakit fiyatı sabittir (₺1,00) — güncelleme gerekmez."
                : "Fiyat canlı kaynaktan otomatik güncellenir (Frankfurter/Truncgil) — elle giriş gerekmez."}
            </p>
          )}

          <div className="drow"><span className="dk">Miktar</span><span className="dv tnum">{formatNumber(h.quantity)} {h.unit}</span></div>
          <div className="drow"><span className="dk">Ortalama maliyet</span><span className="dv tnum">{formatCurrency(h.avgCost, h.currency)}</span></div>
          <div className="drow"><span className="dk">Güncel fiyat</span><span className="dv tnum">{h.currentPrice === null ? "—" : formatCurrency(h.currentPrice, h.currency)}</span></div>
          <div className="drow"><span className="dk">Toplam maliyet</span><span className="dv tnum">{formatCurrency(h.totalCost, h.currency)}</span></div>
          <div className="drow"><span className="dk">Portföy ağırlığı</span><span className="dv tnum">{formatPercent(h.weight, 1, true, false)}</span></div>

          {h.bes && (
            <>
              <div className="detail-section-title">Bireysel Emeklilik (BES)</div>
              {h.bes.contributionDue && (
                <div className="nudge nudge-info">
                  <div className="nudge-ic" aria-hidden="true">💡</div>
                  <div className="nudge-tx">
                    Bu ayın katkısını henüz girmedin. <b>"Aylık katkı ekle"</b> ile girebilir ya da
                    <b> "Düzenli katkı / geçmiş"</b> ile bugüne kadar tamamlayabilirsin.
                  </div>
                </div>
              )}
              <div className="split">
                <div className="sh"><span className="sl">Kendi katkın</span><span className="sr tnum">{formatCurrency(h.bes.ownContribution, h.currency)}</span></div>
                <div className="sd">Senin yatırdığın katkı payları — gerçek <b>yatırım performansının</b> tabanı.</div>
              </div>
              <div className="split">
                <div className="sh"><span className="sl">Devlet katkısı</span><span className="sr tnum up">{formatCurrency(h.bes.stateContribution, h.currency)}</span></div>
                <div className="sd">Devletin eklediği <b>sübvansiyon</b>: kendi katkının <b>%20'si</b> (<b>2026-01-01'den</b>; öncesi %30 — oran geriye dönük değil, geçmiş katkılar etkilenmez). Üst sınır yıllık brüt asgari ücretin %20'si. Yatırım başarın değildir, ayrı gösterilir.</div>
              </div>
              <div className="drow"><span className="dk">Hak ediş</span><span className="dv">{VESTING_TR[h.bes.vestingState] ?? h.bes.vestingState}</span></div>
              <div className="drow">
                <span className="dk">Başlangıç</span>
                <span className="dv">
                  {h.bes.joinedAtUtc ? formatDate(h.bes.joinedAtUtc) : "—"}
                  <span className="muted"> · </span>
                  <button type="button" className="edit-link" onClick={openBesDate}>Düzenle</button>
                </span>
              </div>
              <p className="note-muted">
                Hak ediş sistemde kalış süresine bağlıdır: <b>&lt;3 yıl</b> hak edilmez · <b>3–10 yıl</b> kısmi ·{" "}
                <b>10 yıl</b> (ve 56 yaş) tam. Oran ve eşikler mevzuata tabidir; bilgilendirme amaçlıdır,
                yatırım tavsiyesi değildir.
              </p>
              {h.bes.planActive && (
                <p className="note-muted">
                  🔁 Düzenli katkı planı aktif{h.bes.monthlyAmount ? `: ${formatCurrency(h.bes.monthlyAmount, h.currency)}/ay` : ""}.
                  Tutar değiştirilene kadar, ay geldikçe otomatik katkı kaydı eklenir.
                </p>
              )}
            </>
          )}
        </div>

        {/* Sağ sütun: işlem geçmişi */}
        <div className="detail-col">
          <div className="card">
            <div className="card-head"><h3>{isBes ? "Katkı Geçmişi" : "İşlem Geçmişi"}</h3></div>
            {isBes ? (
              <BesContributionHistory
                contributions={h.bes?.contributions ?? []}
                onEdit={openEditContribution}
                onDelete={setDeletingC}
              />
            ) : (
              <TransactionHistory
                transactions={h.transactions ?? []}
                currency={h.currency}
                unit={h.unit}
                cash={isCash}
              />
            )}
          </div>
        </div>
      </div>

      <div className="danger-zone">
        <div>
          <div className="dz-title">Pozisyonu sil</div>
          <div className="dz-desc">Bu pozisyon ve tüm işlem geçmişi kalıcı olarak silinir.</div>
        </div>
        <button
          type="button"
          className="btn-danger"
          onClick={() => setConfirmOpen(true)}
          disabled={remove.isPending}
        >
          Sil
        </button>
      </div>

      {/* ───── Modallar (#2) ───── */}
      {modal === "tx" && (
        <Modal title={isCash ? "Para ekle / çıkar" : "İşlem ekle"} onClose={closeModal}>
          <AddTransactionForm
            holdingId={h.id}
            currency={h.currency}
            unit={h.unit}
            cash={isCash}
            onDone={onTxDone}
          />
        </Modal>
      )}
      {modal === "bes" && (
        <Modal title="Aylık katkı ekle" onClose={closeModal}>
          <BesContributionForm holdingId={h.id} onDone={onBesDone} />
        </Modal>
      )}
      {modal === "besplan" && (
        <Modal title="Düzenli katkı / geçmişi doldur" onClose={closeModal}>
          <BesContributionPlanForm holdingId={h.id} onDone={onBesPlanDone} />
        </Modal>
      )}
      {editingC && (
        <Modal title="Katkıyı düzenle" onClose={() => setEditingC(null)}>
          <form className="tx-form" onSubmit={onEditContributionSubmit} aria-label="Katkıyı düzenle">
            <div className="tx-row">
              <label>
                Tutar (TRY)
                <input inputMode="decimal" autoFocus value={editOwn} onChange={(e) => setEditOwn(e.target.value)} required />
              </label>
              <label>
                Ödeme tarihi
                <input type="date" max={today} value={editDate} onChange={(e) => setEditDate(e.target.value)} required />
              </label>
              <button type="submit" disabled={updateContribution.isPending}>
                {updateContribution.isPending ? "Kaydediliyor…" : "Kaydet"}
              </button>
            </div>
            {updateContribution.isError && <p className="neg" role="alert">Güncelleme başarısız.</p>}
          </form>
        </Modal>
      )}
      {modal === "besdate" && (
        <Modal title="BES başlangıç tarihi" onClose={closeModal}>
          <form className="price-form bare" onSubmit={onUpdateBesDate}>
            <div className="price-row">
              <input
                id="besdate"
                type="date"
                autoFocus
                max={today}
                value={besDate}
                onChange={(e) => setBesDate(e.target.value)}
              />
              <button type="submit" disabled={updateBes.isPending || besDate === ""}>
                Kaydet
              </button>
            </div>
            {updateBes.isError && <p className="neg">Güncelleme başarısız.</p>}
          </form>
        </Modal>
      )}
      {modal === "price" && (
        <Modal title={`${priceLabel} (${h.currency})`} onClose={closeModal}>
          <form className="price-form bare" onSubmit={onUpdatePrice}>
            <div className="price-row">
              <input
                id="price"
                inputMode="decimal"
                autoFocus
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
        </Modal>
      )}

      <ConfirmDialog
        open={deletingC !== null}
        title="Katkıyı sil?"
        message="Bu katkı kaydı silinecek; toplam katkı ve maliyet buna göre güncellenecek."
        confirmLabel="Evet, sil"
        busy={deleteContribution.isPending}
        onConfirm={onDeleteContribution}
        onCancel={() => setDeletingC(null)}
      />

      <ConfirmDialog
        open={confirmOpen}
        title="Pozisyonu sil?"
        message={`"${h.name}" pozisyonu ve tüm işlem geçmişi kalıcı olarak silinecek. Bu işlem geri alınamaz.`}
        confirmLabel="Evet, sil"
        busy={remove.isPending}
        onConfirm={onDelete}
        onCancel={() => setConfirmOpen(false)}
      />
    </section>
  );
}

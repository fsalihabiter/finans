import { useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { formatCurrency, formatDate, formatNumber, formatPercent } from "@finans/shared";
import type { BesContribution, Transaction, TransactionType } from "@finans/shared";
import { AddTransactionForm } from "../components/AddTransactionForm";
import { BesContributionForm } from "../components/BesContributionForm";
import { BesContributionPlanForm } from "../components/BesContributionPlanForm";
import { BesContributionHistory } from "../components/BesContributionHistory";
import { DateField } from "../components/DateField";
import { TransactionHistory } from "../components/TransactionHistory";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { Modal } from "../components/Modal";
import { useToast } from "../components/Toast";
import {
  useDeleteBesContribution,
  useDeleteHolding,
  useDeleteTransaction,
  useHolding,
  useUpdateBes,
  useUpdateBesContribution,
  useUpdateHolding,
  useUpdateTransaction,
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

type ActiveModal = null | "tx" | "bes" | "price" | "bessettings" | "besplan";

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
  const updateTransaction = useUpdateTransaction(id);
  const deleteTransaction = useDeleteTransaction(id);
  const remove = useDeleteHolding();
  const { notify } = useToast();

  const [price, setPrice] = useState("");
  const [besDate, setBesDate] = useState("");
  const [besBirth, setBesBirth] = useState("");
  const [besDay, setBesDay] = useState("");
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [modal, setModal] = useState<ActiveModal>(null);
  const [editingC, setEditingC] = useState<BesContribution | null>(null);
  const [editOwn, setEditOwn] = useState("");
  const [editDate, setEditDate] = useState("");
  const [deletingC, setDeletingC] = useState<BesContribution | null>(null);
  const [editingTx, setEditingTx] = useState<Transaction | null>(null);
  const [editTxType, setEditTxType] = useState<TransactionType>("Buy");
  const [editTxQty, setEditTxQty] = useState("");
  const [editTxPrice, setEditTxPrice] = useState("");
  const [editTxDate, setEditTxDate] = useState("");
  const [deletingTx, setDeletingTx] = useState<Transaction | null>(null);
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

  const onBesPlanDone = (addedCount: number) => {
    closeModal();
    notify(
      addedCount > 0
        ? `${addedCount} katkı kaydı oluşturuldu.`
        : "Seçtiğin ayların hepsi zaten kayıtlıydı — yeni kayıt eklenmedi.",
      addedCount > 0 ? "success" : "info",
    );
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

  const openEditTransaction = (t: Transaction) => {
    setEditTxType(t.type);
    setEditTxQty(String(t.quantity));
    setEditTxPrice(String(t.unitPrice));
    setEditTxDate(toDateInput(t.transactedAtUtc));
    setEditingTx(t);
  };

  const onEditTransactionSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingTx) return;
    const qty = Number(editTxQty.replace(",", "."));
    const price = isCash ? 1 : Number(editTxPrice.replace(",", "."));
    if (!Number.isFinite(qty) || qty <= 0) return;
    if (!isCash && (!Number.isFinite(price) || price < 0)) return;
    if (editTxDate === "") return;
    updateTransaction.mutate(
      {
        transactionId: editingTx.id,
        input: {
          type: editTxType,
          quantity: qty,
          unitPrice: price,
          date: `${editTxDate}T00:00:00Z`,
        },
      },
      {
        onSuccess: () => {
          setEditingTx(null);
          notify("İşlem güncellendi.", "success");
        },
      },
    );
  };

  const onDeleteTransaction = () => {
    if (!deletingTx) return;
    deleteTransaction.mutate(deletingTx.id, {
      onSuccess: () => { setDeletingTx(null); notify("İşlem silindi.", "info"); },
      onError: (err) => {
        // Son işlemi silmeye çalışırsa backend 400 döner; mesajı kullanıcıya göster.
        notify(err instanceof Error ? err.message : "İşlem silinemedi.", "error");
      },
    });
  };

  const openBesSettings = () => {
    setBesDate(toDateInput(h.bes?.joinedAtUtc ?? null));
    setBesBirth(h.bes?.birthYear ? String(h.bes.birthYear) : "");
    setBesDay(h.bes?.contributionDay ? String(h.bes.contributionDay) : "");
    setModal("bessettings");
  };

  const onUpdateBesSettings = (e: React.FormEvent) => {
    e.preventDefault();
    if (!besDate) return;
    const birthYear = besBirth.trim() === "" ? null : Number(besBirth);
    const day = besDay.trim() === "" ? null : Number(besDay);
    if (birthYear !== null && (!Number.isInteger(birthYear) || birthYear < 1920 || birthYear > new Date().getFullYear())) return;
    if (day !== null && (!Number.isInteger(day) || day < 1 || day > 28)) return;
    updateBes.mutate(
      { joinedAtUtc: `${besDate}T00:00:00Z`, birthYear, contributionDay: day },
      {
        onSuccess: () => {
          closeModal();
          notify("BES ayarları güncellendi.", "success");
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
                <div className="sh"><span className="sl">Yatırılan Katkı Payı</span><span className="sr tnum">{formatCurrency(h.bes.ownContribution, h.currency)}</span></div>
                {h.bes.fundReturnRatio !== null && (
                  <div className="bes-fund-row">
                    <span className="muted">Güncel değer</span>
                    <span className="tnum">{formatCurrency(h.bes.ownValue, h.currency)}</span>
                    <span className={`tnum ${tone(h.bes.ownProfit)}`}>
                      {h.bes.ownProfit > 0 ? "+" : ""}{formatCurrency(h.bes.ownProfit, h.currency)}
                      {" · "}{formatPercent(h.bes.fundReturnRatio)}
                    </span>
                  </div>
                )}
                <div className="sd">Cebinden ödediğin, hesabına geçmiş katkı payları — gerçek <b>yatırım performansının</b> tabanı. Fon getirisi <b>kendi payına da</b> işler.</div>
              </div>
              <div className="split">
                <div className="sh"><span className="sl">Yatırılan devlet katkısı</span><span className="sr tnum up">{formatCurrency(h.bes.stateContribution, h.currency)}</span></div>
                {h.bes.fundReturnRatio !== null && (
                  <div className="bes-fund-row">
                    <span className="muted">Güncel değer</span>
                    <span className="tnum">{formatCurrency(h.bes.stateValue, h.currency)}</span>
                    <span className={`tnum ${tone(h.bes.stateProfit)}`}>
                      {h.bes.stateProfit > 0 ? "+" : ""}{formatCurrency(h.bes.stateProfit, h.currency)}
                      {" · "}{formatPercent(h.bes.fundReturnRatio)}
                    </span>
                  </div>
                )}
                <div className="sd">Devletin eklediği <b>sübvansiyon</b> (hesabına geçmiş kısım): katkı payının <b>%20'si</b> (<b>2026-01-01'den</b>; öncesi %30 — geriye dönük değil). Bu tutar <b>fonda işletilir</b>; getirisi ayrı kâr/zarar olarak yukarıda görünür.</div>
              </div>
              {(h.bes.ownPending > 0 || h.bes.statePending > 0) && (
                <div className="split">
                  <div className="sh">
                    <span className="sl">Bekleyen</span>
                    <span className="sr tnum">
                      {h.bes.ownPending > 0 && <span className="pend-own">{formatCurrency(h.bes.ownPending, h.currency)}</span>}
                      {h.bes.ownPending > 0 && h.bes.statePending > 0 && <span className="muted"> + </span>}
                      {h.bes.statePending > 0 && <span className="pend-state">{formatCurrency(h.bes.statePending, h.currency)} devlet</span>}
                    </span>
                  </div>
                  <div className="sd">
                    Henüz hesaba geçmemiş tutarlar — devlet katkısı, katkı payı ödemesini izleyen ayın sonunda yatar.
                    <b> Toplam birikime ve getiriye dahil edilmez.</b>
                  </div>
                </div>
              )}
              <div className="drow">
                <span className="dk">Hak ediş</span>
                <span className="dv">{VESTING_TR[h.bes.vestingState] ?? h.bes.vestingState} · <b>%{Math.round(h.bes.vestedRate * 100)}</b></span>
              </div>
              <div className="drow">
                <span className="dk">Hak kazanılan tutar</span>
                <span className="dv tnum up">{formatCurrency(h.bes.vestedAmount, h.currency)}</span>
              </div>
              <div className="drow">
                <span className="dk">Başlangıç</span>
                <span className="dv">{h.bes.joinedAtUtc ? formatDate(h.bes.joinedAtUtc) : "—"}</span>
              </div>
              <div className="drow">
                <span className="dk">Ödeme günü</span>
                <span className="dv">{h.bes.contributionDay ? `Her ayın ${h.bes.contributionDay}. günü` : "—"}</span>
              </div>
              <div className="drow">
                <span className="dk">Doğum yılı</span>
                <span className="dv">
                  {h.bes.birthYear ?? "—"}
                  <span className="muted"> · </span>
                  <button type="button" className="edit-link" onClick={openBesSettings}>Ayarları düzenle</button>
                </span>
              </div>
              <p className="note-muted">
                Hak ediş sistemde kalış süresine bağlıdır: <b>&lt;3 yıl %0</b> · <b>3–6 yıl %15</b> ·{" "}
                <b>6–10 yıl %35</b> · <b>10 yıl+ %60</b> · <b>10 yıl + 56 yaş %100</b>. Hak kazanılan tutar
                yalnız <b>devlet katkısına</b> uygulanır (katkı payın her zaman senindir); yaklaşık değerdir.
                Oran ve eşikler mevzuata tabidir; bilgilendirme amaçlıdır, yatırım tavsiyesi değildir.
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

        {/* Sağ sütun: işlem/katkı geçmişi — yükseklik sol içeriğe göre (detail-col--history) */}
        <div className="detail-col detail-col--history">
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
                onEdit={openEditTransaction}
                onDelete={setDeletingTx}
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
          <BesContributionPlanForm
            holdingId={h.id}
            existingContributions={h.bes?.contributions ?? []}
            onDone={onBesPlanDone}
          />
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
                <DateField value={editDate} onChange={setEditDate} required ariaLabel="Ödeme tarihi" />
              </label>
              <button type="submit" disabled={updateContribution.isPending}>
                {updateContribution.isPending ? "Kaydediliyor…" : "Kaydet"}
              </button>
            </div>
            {updateContribution.isError && <p className="neg" role="alert">Güncelleme başarısız.</p>}
          </form>
        </Modal>
      )}
      {editingTx && (
        <Modal title={isCash ? "Hareketi düzenle" : "İşlemi düzenle"} onClose={() => setEditingTx(null)}>
          <form className="tx-form" onSubmit={onEditTransactionSubmit} aria-label={isCash ? "Hareketi düzenle" : "İşlemi düzenle"}>
            <div className="tx-type" role="group" aria-label={isCash ? "Hareket türü" : "İşlem türü"}>
              <button
                type="button"
                className={editTxType === "Buy" ? "active" : ""}
                aria-pressed={editTxType === "Buy"}
                onClick={() => setEditTxType("Buy")}
              >
                {isCash ? "Para ekle" : "Alış"}
              </button>
              <button
                type="button"
                className={editTxType === "Sell" ? "active sell" : ""}
                aria-pressed={editTxType === "Sell"}
                onClick={() => setEditTxType("Sell")}
              >
                {isCash ? "Para çıkar" : "Satış"}
              </button>
            </div>
            <div className="tx-row">
              <label>
                {isCash ? `Tutar (${h.currency})` : `Miktar (${h.unit})`}
                <input
                  inputMode="decimal"
                  autoFocus
                  value={editTxQty}
                  onChange={(e) => setEditTxQty(e.target.value)}
                  required
                />
              </label>
              {!isCash && (
                <label>
                  Birim fiyat ({h.currency})
                  <input
                    inputMode="decimal"
                    value={editTxPrice}
                    onChange={(e) => setEditTxPrice(e.target.value)}
                    required
                  />
                </label>
              )}
              <label>
                {isCash ? "Tarih" : "İşlem tarihi"}
                <DateField value={editTxDate} onChange={setEditTxDate} required ariaLabel="İşlem tarihi" />
              </label>
              <button type="submit" disabled={updateTransaction.isPending}>
                {updateTransaction.isPending ? "Kaydediliyor…" : "Kaydet"}
              </button>
            </div>
            {updateTransaction.isError && (
              <p className="neg" role="alert">
                {updateTransaction.error instanceof Error ? updateTransaction.error.message : "Güncelleme başarısız."}
              </p>
            )}
          </form>
        </Modal>
      )}
      {modal === "bessettings" && (
        <Modal title="BES ayarları" onClose={closeModal}>
          <form className="tx-form" onSubmit={onUpdateBesSettings} aria-label="BES ayarları">
            <div className="tx-row">
              <label>
                Başlangıç tarihi
                <DateField value={besDate} onChange={setBesDate} max={today} required ariaLabel="Başlangıç tarihi" />
              </label>
              <label>
                Doğum yılı (ops.)
                <input
                  inputMode="numeric"
                  value={besBirth}
                  onChange={(e) => setBesBirth(e.target.value)}
                  placeholder="örn. 1985"
                />
              </label>
              <label>
                Ödeme günü (1–28)
                <input
                  type="number"
                  min={1}
                  max={28}
                  value={besDay}
                  onChange={(e) => setBesDay(e.target.value)}
                  placeholder="1"
                />
              </label>
              <button type="submit" disabled={updateBes.isPending || besDate === ""}>
                {updateBes.isPending ? "Kaydediliyor…" : "Kaydet"}
              </button>
            </div>
            <p className="note-muted">
              Doğum yılı yalnız tam hak ediş (%100) için 56 yaş kontrolünde kullanılır. Ödeme günü, düzenli
              katkı planının her ay hangi gün işleneceğini belirler.
            </p>
            {updateBes.isError && <p className="neg" role="alert">Güncelleme başarısız.</p>}
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
        open={deletingTx !== null}
        title={isCash ? "Hareketi sil?" : "İşlemi sil?"}
        message={isCash
          ? "Bu para hareketi silinecek; bakiye buna göre güncellenecek."
          : "Bu işlem silinecek; miktar ve ortalama maliyet işlemlerden yeniden hesaplanacak."}
        confirmLabel="Evet, sil"
        busy={deleteTransaction.isPending}
        onConfirm={onDeleteTransaction}
        onCancel={() => setDeletingTx(null)}
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

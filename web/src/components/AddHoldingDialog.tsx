import { useEffect, useRef, useState } from "react";
import type { AssetType, CreateHoldingInput, CurrencyCode } from "@finans/shared";
import { useCreateHolding } from "../lib/hooks";
import { useToast } from "./Toast";
import { ASSET_META } from "../lib/assetMeta";

const ASSET_TYPES: { value: AssetType; label: string; unit: string }[] = [
  { value: "Gold", label: "Altın", unit: "gram" },
  { value: "Fx", label: "Döviz", unit: "USD" },
  { value: "Stock", label: "Hisse", unit: "adet" },
  { value: "Fund", label: "Fon", unit: "adet" },
  { value: "Cash", label: "Nakit", unit: "TRY" },
  { value: "Bes", label: "BES", unit: "birim" },
];

const CURRENCIES: CurrencyCode[] = ["TRY", "USD", "EUR"];

interface FormState {
  assetType: AssetType;
  name: string;
  symbol: string;
  currency: CurrencyCode;
  unit: string;
  quantity: string;
  unitPrice: string;
}

const INITIAL: FormState = {
  assetType: "Gold",
  name: "",
  symbol: "",
  currency: "TRY",
  unit: "gram",
  quantity: "",
  unitPrice: "",
};

const toNumber = (s: string) => Number(s.replace(",", "."));

const FOCUSABLE =
  'a[href], button:not([disabled]), input, select, textarea, [tabindex]:not([tabindex="-1"])';

/**
 * "Varlık Ekle" modalı (13 §4, FR-1.1) → POST /api/holdings. İlk alış işlemiyle
 * pozisyon oluşturur; backend ort. maliyeti işlemden türetir. Sayısal hesap YOK —
 * sadece girdi toplar. Tür seçimi görsel chip'lerle; ilk alana autofocus, Tab
 * odak tuzağı (a11y); dolu formda yanlışlıkla dışına tıklama kapatmaz (veri korunur).
 */
export function AddHoldingDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const create = useCreateHolding();
  const { notify } = useToast();
  const [form, setForm] = useState<FormState>(INITIAL);
  const dialogRef = useRef<HTMLDivElement>(null);
  const firstFieldRef = useRef<HTMLInputElement>(null);

  // Açılışta ilk alana odaklan. (Form durumu sıfırlamaya gerek yok: bileşen
  // yalnızca açıkken mount edilir — her açılış taze state ile başlar.)
  useEffect(() => {
    if (open) requestAnimationFrame(() => firstFieldRef.current?.focus());
  }, [open]);

  // Escape ile kapat + Tab odak tuzağı (modal dışına çıkmasın).
  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        onClose();
        return;
      }
      if (e.key !== "Tab" || !dialogRef.current) return;
      const items = Array.from(
        dialogRef.current.querySelectorAll<HTMLElement>(FOCUSABLE),
      ).filter((el) => el.offsetParent !== null);
      if (items.length === 0) return;
      const first = items[0];
      const last = items[items.length - 1];
      if (e.shiftKey && document.activeElement === first) {
        e.preventDefault();
        last.focus();
      } else if (!e.shiftKey && document.activeElement === last) {
        e.preventDefault();
        first.focus();
      }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open, onClose]);

  if (!open) return null;

  const set = (patch: Partial<FormState>) => setForm((f) => ({ ...f, ...patch }));

  const onAssetTypeChange = (value: AssetType) => {
    const preset = ASSET_TYPES.find((a) => a.value === value);
    set({ assetType: value, unit: preset?.unit ?? form.unit });
  };

  const quantity = toNumber(form.quantity);
  const unitPrice = toNumber(form.unitPrice);
  const valid =
    form.name.trim() !== "" &&
    form.unit.trim() !== "" &&
    Number.isFinite(quantity) &&
    quantity > 0 &&
    Number.isFinite(unitPrice) &&
    unitPrice >= 0;

  const dirty = JSON.stringify(form) !== JSON.stringify(INITIAL);

  // Dolu formda yanlışlıkla overlay tıklamasıyla veri kaybını önle.
  const onOverlayClick = () => {
    if (!dirty) onClose();
  };

  const onSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!valid) return;
    const input: CreateHoldingInput = {
      assetType: form.assetType,
      name: form.name.trim(),
      symbol: form.symbol.trim() || null,
      currency: form.currency,
      unit: form.unit.trim(),
      transaction: { type: "Buy", quantity, unitPrice },
    };
    create.mutate(input, {
      onSuccess: () => {
        notify(`${input.name} portföyüne eklendi.`, "success");
        onClose();
      },
    });
  };

  return (
    <div className="modal-overlay" onClick={onOverlayClick}>
      <div
        ref={dialogRef}
        className="modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="add-holding-title"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="modal-top">
          <h2 id="add-holding-title">Varlık Ekle</h2>
          <button type="button" className="modal-close" aria-label="Kapat" onClick={onClose}>✕</button>
        </div>
        <form onSubmit={onSubmit} className="add-form">
          <div className="field-group">
            <span className="field-label">Tür</span>
            <div className="type-chips" role="radiogroup" aria-label="Varlık türü">
              {ASSET_TYPES.map((a) => (
                <button
                  key={a.value}
                  type="button"
                  role="radio"
                  aria-checked={form.assetType === a.value}
                  className={form.assetType === a.value ? "sel" : ""}
                  onClick={() => onAssetTypeChange(a.value)}
                >
                  <span aria-hidden="true">{ASSET_META[a.value].icon}</span> {a.label}
                </button>
              ))}
            </div>
          </div>

          <label>
            Ad
            <input
              ref={firstFieldRef}
              value={form.name}
              onChange={(e) => set({ name: e.target.value })}
              placeholder="örn. Altın (gram)"
              required
            />
          </label>

          <div className="add-row">
            <label>
              Sembol (ops.)
              <input value={form.symbol} onChange={(e) => set({ symbol: e.target.value })} placeholder="XAU" />
            </label>
            <label>
              Para birimi
              <select value={form.currency} onChange={(e) => set({ currency: e.target.value as CurrencyCode })}>
                {CURRENCIES.map((c) => (
                  <option key={c} value={c}>{c}</option>
                ))}
              </select>
            </label>
            <label>
              Birim
              <input value={form.unit} onChange={(e) => set({ unit: e.target.value })} placeholder="gram" required />
            </label>
          </div>

          <div className="add-row">
            <label>
              Miktar
              <input
                inputMode="decimal"
                value={form.quantity}
                onChange={(e) => set({ quantity: e.target.value })}
                placeholder="40"
                required
              />
            </label>
            <label>
              Alış birim fiyatı ({form.currency})
              <input
                inputMode="decimal"
                value={form.unitPrice}
                onChange={(e) => set({ unitPrice: e.target.value })}
                placeholder="4546,275"
                required
              />
            </label>
          </div>

          {create.isError && (
            <p className="neg" role="alert">
              {create.error instanceof Error ? create.error.message : "Eklenemedi."}
            </p>
          )}

          {!valid && !create.isError && (
            <p className="form-hint">Ad, miktar ve alış fiyatı zorunlu. Miktar 0'dan büyük olmalı.</p>
          )}

          <div className="add-actions">
            <button type="button" className="btn-ghost" onClick={onClose}>
              Vazgeç
            </button>
            <button type="submit" disabled={!valid || create.isPending}>
              {create.isPending ? "Ekleniyor…" : "Ekle"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

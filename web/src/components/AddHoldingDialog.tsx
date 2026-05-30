import { useEffect, useState } from "react";
import type { AssetType, CreateHoldingInput, CurrencyCode } from "@finans/shared";
import { useCreateHolding } from "../lib/hooks";

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

/**
 * "Varlık Ekle" modalı (13 §4, FR-1.1) → POST /api/holdings. İlk alış işlemiyle
 * pozisyon oluşturur; backend ort. maliyeti işlemden türetir. Sayısal hesap YOK —
 * sadece girdi toplar. Hata zarfı (validasyon/çakışma) kullanıcıya gösterilir.
 */
export function AddHoldingDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const create = useCreateHolding();
  const [form, setForm] = useState<FormState>(INITIAL);

  // Açılışta formu ve hatayı sıfırla; Escape ile kapat.
  useEffect(() => {
    if (open) {
      setForm(INITIAL);
      create.reset();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => e.key === "Escape" && onClose();
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
    create.mutate(input, { onSuccess: onClose });
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div
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
          <label>
            Tür
            <select
              value={form.assetType}
              onChange={(e) => onAssetTypeChange(e.target.value as AssetType)}
            >
              {ASSET_TYPES.map((a) => (
                <option key={a.value} value={a.value}>{a.label}</option>
              ))}
            </select>
          </label>

          <label>
            Ad
            <input
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

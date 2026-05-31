import { useEffect, useRef, useState } from "react";
import type { AssetType, CreateBesInput, CreateHoldingInput, CurrencyCode } from "@finans/shared";
import { useCreateBes, useCreateHolding } from "../lib/hooks";
import { useToast } from "./Toast";
import { ASSET_META } from "../lib/assetMeta";
import { DateField } from "./DateField";

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
  // ── BES'e özel açılış bakiyesi alanları (assetType === "Bes") ──
  providerName: string;
  joinedAt: string;
  birthYear: string;
  currentFundValue: string;
  openingOwn: string;
  openingState: string;
  monthlyAmount: string;
  contributionDay: string;
}

const INITIAL: FormState = {
  assetType: "Gold",
  name: "",
  symbol: "",
  currency: "TRY",
  unit: "gram",
  quantity: "",
  unitPrice: "",
  providerName: "",
  joinedAt: "",
  birthYear: "",
  currentFundValue: "",
  openingOwn: "",
  openingState: "",
  monthlyAmount: "",
  contributionDay: "",
};

const toNumber = (s: string) => Number(s.replace(",", "."));

const FOCUSABLE =
  'a[href], button:not([disabled]), input, select, textarea, [tabindex]:not([tabindex="-1"])';

/**
 * "Varlık Ekle" modalı (13 §4, FR-1.1). Standart varlıklar (altın/döviz/hisse/fon/nakit) ilk alış
 * işlemiyle `POST /api/holdings`'e gider. **BES** seçilince form BES'e özel **açılış bakiyesi**
 * alanlarına döner (T-BES.8): plan adı, başlangıç/doğum yılı, güncel fon değeri + birikmiş kendi/
 * devlet katkı toplamları + opsiyonel düzenli plan → `POST /api/holdings/bes`. Sayısal hesap YOK.
 * İlk alana autofocus, Tab odak tuzağı (a11y); dolu formda dışına tıklama kapatmaz (veri korunur).
 */
export function AddHoldingDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const create = useCreateHolding();
  const createBes = useCreateBes();
  const { notify } = useToast();
  const [form, setForm] = useState<FormState>(INITIAL);
  const dialogRef = useRef<HTMLDivElement>(null);
  const firstFieldRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (open) requestAnimationFrame(() => firstFieldRef.current?.focus());
  }, [open]);

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
  const isBes = form.assetType === "Bes";
  const today = new Date().toISOString().slice(0, 10);
  const thisYear = new Date().getFullYear();

  const onAssetTypeChange = (value: AssetType) => {
    const preset = ASSET_TYPES.find((a) => a.value === value);
    set({ assetType: value, unit: preset?.unit ?? form.unit });
  };

  // ── Doğrulama: standart vs BES ──
  const quantity = toNumber(form.quantity);
  const unitPrice = toNumber(form.unitPrice);
  const standardValid =
    form.name.trim() !== "" &&
    form.unit.trim() !== "" &&
    Number.isFinite(quantity) && quantity > 0 &&
    Number.isFinite(unitPrice) && unitPrice >= 0;

  const fundValue = toNumber(form.currentFundValue);
  const openingOwn = toNumber(form.openingOwn);
  const openingState = toNumber(form.openingState);
  const birthYear = form.birthYear.trim() === "" ? null : Number(form.birthYear);
  const contributionDay = form.contributionDay.trim() === "" ? null : Number(form.contributionDay);
  const besValid =
    form.name.trim() !== "" &&
    form.joinedAt !== "" &&
    Number.isFinite(fundValue) && fundValue >= 0 &&
    Number.isFinite(openingOwn) && openingOwn >= 0 &&
    Number.isFinite(openingState) && openingState >= 0 &&
    (birthYear === null || (Number.isInteger(birthYear) && birthYear >= 1920 && birthYear <= thisYear)) &&
    (contributionDay === null || (Number.isInteger(contributionDay) && contributionDay >= 1 && contributionDay <= 28));

  const valid = isBes ? besValid : standardValid;
  const pending = isBes ? createBes.isPending : create.isPending;
  const err = isBes ? createBes.error : create.error;
  const isError = isBes ? createBes.isError : create.isError;

  const dirty = JSON.stringify(form) !== JSON.stringify(INITIAL);
  const onOverlayClick = () => {
    if (!dirty) onClose();
  };

  const onSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!valid) return;
    if (isBes) {
      const monthly = toNumber(form.monthlyAmount);
      const input: CreateBesInput = {
        name: form.name.trim(),
        providerName: form.providerName.trim() || null,
        currency: form.currency,
        joinedAtUtc: `${form.joinedAt}T00:00:00Z`,
        birthYear,
        currentFundValue: fundValue,
        openingOwn,
        openingState,
        monthlyAmount: Number.isFinite(monthly) && monthly > 0 ? monthly : null,
        contributionDay,
      };
      createBes.mutate(input, {
        onSuccess: () => {
          notify(`${input.name} portföyüne eklendi.`, "success");
          onClose();
        },
      });
      return;
    }
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
            {isBes ? "Plan / şirket adı" : "Ad"}
            <input
              ref={firstFieldRef}
              value={form.name}
              onChange={(e) => set({ name: e.target.value })}
              placeholder={isBes ? "örn. Kadın Temel Emeklilik Planı" : "örn. Altın (gram)"}
              required
            />
          </label>

          {isBes ? (
            <>
              <p className="form-hint">
                BES'te bugünkü durumu <b>açılış bakiyesi</b> olarak gir — geçmiş katkıları tek tek
                girmene gerek yok. Güncel fon değeri ile bugüne dek birikmiş kendi ve devlet katkı
                toplamlarını yaz; sonrasını aylık katkı/düzenli plan ile sürdürürsün.
              </p>
              <div className="add-row">
                <label>
                  Başlangıç tarihi
                  <DateField value={form.joinedAt} onChange={(v) => set({ joinedAt: v })} max={today} required ariaLabel="BES başlangıç tarihi" />
                </label>
                <label>
                  Doğum yılı (ops.)
                  <input inputMode="numeric" value={form.birthYear} onChange={(e) => set({ birthYear: e.target.value })} placeholder="1985" />
                </label>
                <label>
                  Para birimi
                  <select value={form.currency} onChange={(e) => set({ currency: e.target.value as CurrencyCode })}>
                    {CURRENCIES.map((c) => (<option key={c} value={c}>{c}</option>))}
                  </select>
                </label>
              </div>
              <label>
                Güncel fon değeri ({form.currency})
                <input inputMode="decimal" value={form.currentFundValue} onChange={(e) => set({ currentFundValue: e.target.value })} placeholder="örn. 279.378" required />
              </label>
              <div className="add-row">
                <label>
                  Birikmiş katkı payın ({form.currency})
                  <input inputMode="decimal" value={form.openingOwn} onChange={(e) => set({ openingOwn: e.target.value })} placeholder="örn. 120.000" required />
                </label>
                <label>
                  Birikmiş devlet katkısı ({form.currency})
                  <input inputMode="decimal" value={form.openingState} onChange={(e) => set({ openingState: e.target.value })} placeholder="örn. 28.554" required />
                </label>
              </div>
              <div className="add-row">
                <label>
                  Aylık katkı (ops.)
                  <input inputMode="decimal" value={form.monthlyAmount} onChange={(e) => set({ monthlyAmount: e.target.value })} placeholder="örn. 7.500" />
                </label>
                <label>
                  Ödeme günü (1–28)
                  <input type="number" min={1} max={28} value={form.contributionDay} onChange={(e) => set({ contributionDay: e.target.value })} placeholder="1" />
                </label>
              </div>
              <p className="form-hint">
                Aylık katkı + ödeme günü girersen <b>düzenli plan</b> kurulur; ay geldikçe katkı kaydı
                otomatik eklenir. Devlet katkısı, kendi katkı ödemeni izleyen ayın sonunda yatmış sayılır.
              </p>
            </>
          ) : (
            <>
              <div className="add-row">
                <label>
                  Sembol (ops.)
                  <input value={form.symbol} onChange={(e) => set({ symbol: e.target.value })} placeholder="XAU" />
                </label>
                <label>
                  Para birimi
                  <select value={form.currency} onChange={(e) => set({ currency: e.target.value as CurrencyCode })}>
                    {CURRENCIES.map((c) => (<option key={c} value={c}>{c}</option>))}
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
                  <input inputMode="decimal" value={form.quantity} onChange={(e) => set({ quantity: e.target.value })} placeholder="40" required />
                </label>
                <label>
                  Alış birim fiyatı ({form.currency})
                  <input inputMode="decimal" value={form.unitPrice} onChange={(e) => set({ unitPrice: e.target.value })} placeholder="4546,275" required />
                </label>
              </div>
            </>
          )}

          {isError && (
            <p className="neg" role="alert">
              {err instanceof Error ? err.message : "Eklenemedi."}
            </p>
          )}

          {!valid && !isError && (
            <p className="form-hint">
              {isBes
                ? "Plan adı, başlangıç tarihi, fon değeri ve katkı toplamları zorunlu."
                : "Ad, miktar ve alış fiyatı zorunlu. Miktar 0'dan büyük olmalı."}
            </p>
          )}

          <div className="add-actions">
            <button type="button" className="btn-ghost" onClick={onClose}>
              Vazgeç
            </button>
            <button type="submit" disabled={!valid || pending}>
              {pending ? "Ekleniyor…" : "Ekle"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

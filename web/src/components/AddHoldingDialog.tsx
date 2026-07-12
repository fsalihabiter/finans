import { useEffect, useRef, useState } from "react";
import type { AssetType, CreateBesInput, CreateHoldingInput, CurrencyCode } from "@finans/shared";
import { formatCurrency } from "@finans/shared";
import { api } from "../lib/api";
import { useCreateBes, useCreateHolding, usePrices } from "../lib/hooks";
import { useToast } from "./Toast";
import { withViewTransition } from "../lib/viewTransition";
import { ASSET_META } from "../lib/assetMeta";
import { DateField } from "./DateField";

const ASSET_TYPES: { value: AssetType; label: string }[] = [
  { value: "Gold", label: "Altın" },
  { value: "Fx", label: "Döviz" },
  { value: "Stock", label: "Hisse" },
  { value: "Fund", label: "Fon" },
  { value: "Cash", label: "Nakit" },
  { value: "Bes", label: "BES" },
];

const CURRENCIES: CurrencyCode[] = ["TRY", "USD", "EUR"];
type FxCcy = "USD" | "EUR";

/** Tür-varsayılan ad — kullanıcı elle değiştirmediyse tür/döviz değişince tazelenir. */
const defaultNameFor = (type: AssetType, fxCcy: FxCcy): string => {
  switch (type) {
    case "Gold": return "Altın (gram)";
    case "Fx": return fxCcy === "USD" ? "ABD Doları" : "Euro";
    case "Cash": return "Nakit (TL)";
    default: return "";
  }
};

interface FormState {
  assetType: AssetType;
  name: string;
  symbol: string;
  currency: CurrencyCode;
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
  name: defaultNameFor("Gold", "USD"),
  symbol: "",
  currency: "TRY",
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

/** Sayıyı girdi alanına TR ondalık virgülüyle yazar (hesap değil, ön-doldurma). */
const toInput = (n: number) => String(n).replace(".", ",");

const FOCUSABLE =
  'a[href], button:not([disabled]), input, select, textarea, [tabindex]:not([tabindex="-1"])';

/**
 * "Varlık Ekle" modalı (13 §4, FR-1.1). Alanlar TÜRE GÖRE uyarlanır (kullanıcı isteği
 * 2026-07-12 — tek tip form her varlığa uymuyor):
 * - Altın: ad + gram + ₺/gram (sembol XAU, birim gram, pb TRY otomatik; canlı gram fiyatı ön-dolar)
 * - Döviz: USD/EUR seçimi + miktar + alış kuru (canlı kur ön-dolar; ad/sembol/birim otomatik)
 * - Hisse: sembol (alan terkinde ad + güncel $ fiyatı otomatik getirilir) + adet + fiyat
 * - Fon: ad + sembol (ops.) + adet + birim fiyat (canlı kaynak yok — elle)
 * - Nakit: yalnız ad + tutar (birim TRY, fiyat 1 otomatik)
 * Ön-dolan fiyat DÜZENLENEBİLİR — sayı üretmez, yol gösterir (CLAUDE.md §3.1: hesap kodda).
 * **BES** seçilince açılış bakiyesi formu (T-BES.8). İlk alana autofocus, Tab odak tuzağı;
 * kullanıcı bir şey girdiyse dışına tıklama kapatmaz.
 */
export function AddHoldingDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const create = useCreateHolding();
  const createBes = useCreateBes();
  const prices = usePrices();
  const { notify } = useToast();
  const [form, setForm] = useState<FormState>(INITIAL);
  const [fxCcy, setFxCcy] = useState<FxCcy>("USD");
  const [stockCcy, setStockCcy] = useState<CurrencyCode>("USD");
  const [touched, setTouched] = useState(false);      // kullanıcı eliyle bir şey girdi mi (overlay kapatma)
  const [nameTouched, setNameTouched] = useState(false);
  const [priceTouched, setPriceTouched] = useState(false);
  const [autoPrice, setAutoPrice] = useState<{ value: number; stale: boolean } | null>(null);
  const [stockLookup, setStockLookup] = useState<{ loading: boolean; error: string | null; last: string }>(
    { loading: false, error: null, last: "" });
  const dialogRef = useRef<HTMLDivElement>(null);
  const firstFieldRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (open) requestAnimationFrame(() => firstFieldRef.current?.focus());
  }, [open]);

  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        withViewTransition(onClose);
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

  // ── Canlı fiyat ön-doldurma: altın (₺/gram) ve döviz (kur) — kullanıcı fiyatı
  // elle DEĞİŞTİRMEDİYSE tür/döviz değişiminde ve fiyat geldiğinde tazelenir.
  const livePrice = (() => {
    const list = prices.data?.prices ?? [];
    if (form.assetType === "Gold") {
      const p = list.find((x) => x.kind === "Gold");
      return p ? { value: p.price, stale: p.stale } : null;
    }
    if (form.assetType === "Fx") {
      const p = list.find((x) => x.kind === "Currency" && x.currency === fxCcy);
      return p ? { value: p.price, stale: p.stale } : null;
    }
    return null;
  })();

  useEffect(() => {
    if (!open || priceTouched || !livePrice) return;
    setForm((f) => ({ ...f, unitPrice: toInput(livePrice.value) }));
    setAutoPrice(livePrice);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, priceTouched, form.assetType, fxCcy, livePrice?.value]);

  if (!open) return null;

  const set = (patch: Partial<FormState>) => {
    setTouched(true);
    setForm((f) => ({ ...f, ...patch }));
  };
  const type = form.assetType;
  const isBes = type === "Bes";
  const isCash = type === "Cash";
  const today = new Date().toISOString().slice(0, 10);
  const thisYear = new Date().getFullYear();

  const onAssetTypeChange = (value: AssetType) => {
    setForm((f) => ({
      ...f,
      assetType: value,
      name: defaultNameFor(value, fxCcy),
      symbol: "",
      quantity: "",
      unitPrice: "",
    }));
    setNameTouched(false);
    setPriceTouched(false);
    setAutoPrice(null);
    setStockLookup({ loading: false, error: null, last: "" });
  };

  const onFxCcyChange = (ccy: FxCcy) => {
    setFxCcy(ccy);
    setPriceTouched(false);
    setForm((f) => ({
      ...f,
      name: nameTouched ? f.name : defaultNameFor("Fx", ccy),
      unitPrice: "",
    }));
  };

  // ── Hisse: sembol alanı terk edilince ad + güncel fiyat otomatik getirilir. ──
  const lookupStock = async () => {
    const sym = form.symbol.trim().toUpperCase();
    if (sym === "" || sym === stockLookup.last) return;
    setStockLookup({ loading: true, error: null, last: sym });
    try {
      const m = await api.getStockMetrics(sym);
      setForm((f) => ({
        ...f,
        name: nameTouched && f.name.trim() !== "" ? f.name : m.name,
        unitPrice: !priceTouched && m.price != null ? toInput(m.price) : f.unitPrice,
      }));
      if (m.price != null && !priceTouched) setAutoPrice({ value: m.price, stale: false });
      if (CURRENCIES.includes(m.currency as CurrencyCode)) setStockCcy(m.currency as CurrencyCode);
      setStockLookup({ loading: false, error: null, last: sym });
    } catch {
      setStockLookup({
        loading: false,
        error: "Sembol bilgisi alınamadı — ad ve fiyatı elle girebilirsin.",
        last: sym,
      });
    }
  };

  // ── Türe göre gönderilecek sabitler (gereksiz alan sorulmaz, otomatik gider) ──
  const derived = (() => {
    switch (type) {
      case "Gold": return { symbol: "XAU", unit: "gram", currency: "TRY" as CurrencyCode };
      case "Fx": return { symbol: fxCcy, unit: fxCcy, currency: "TRY" as CurrencyCode };
      case "Stock": return { symbol: form.symbol.trim().toUpperCase() || null, unit: "adet", currency: stockCcy };
      case "Fund": return { symbol: form.symbol.trim() || null, unit: "adet", currency: "TRY" as CurrencyCode };
      default: return { symbol: null, unit: "TRY", currency: "TRY" as CurrencyCode }; // Cash
    }
  })();

  const ccySymbol = derived.currency === "TRY" ? "₺" : derived.currency === "USD" ? "$" : "€";
  const quantityLabel = type === "Gold" ? "Miktar (gram)"
    : type === "Fx" ? `Miktar (${fxCcy})`
    : isCash ? "Tutar (₺)"
    : "Adet";

  // ── Doğrulama: türe göre ──
  const quantity = toNumber(form.quantity);
  const unitPrice = isCash ? 1 : toNumber(form.unitPrice);
  const standardValid =
    form.name.trim() !== "" &&
    Number.isFinite(quantity) && quantity > 0 &&
    (isCash || (Number.isFinite(unitPrice) && unitPrice >= 0)) &&
    (type !== "Stock" || form.symbol.trim() !== "");

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

  const invalidHint = isBes
    ? "Plan adı, başlangıç tarihi, fon değeri ve katkı toplamları zorunlu."
    : isCash
      ? "Tutar 0'dan büyük olmalı."
      : type === "Stock"
        ? "Sembol, adet ve alış fiyatı zorunlu."
        : "Ad, miktar ve alış fiyatı zorunlu. Miktar 0'dan büyük olmalı.";

  const onOverlayClick = () => {
    if (!touched) withViewTransition(onClose);
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
      assetType: type,
      name: form.name.trim(),
      symbol: derived.symbol,
      currency: derived.currency,
      unit: derived.unit,
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
          <button type="button" className="modal-close" aria-label="Kapat" onClick={() => withViewTransition(onClose)}>✕</button>
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
                  aria-checked={type === a.value}
                  className={type === a.value ? "sel" : ""}
                  onClick={() => onAssetTypeChange(a.value)}
                >
                  <span aria-hidden="true">{ASSET_META[a.value].icon}</span> {a.label}
                </button>
              ))}
            </div>
          </div>

          {type === "Fx" && (
            <div className="field-group">
              <span className="field-label">Döviz</span>
              <div className="type-chips" role="radiogroup" aria-label="Döviz türü">
                {(["USD", "EUR"] as const).map((c) => (
                  <button
                    key={c}
                    type="button"
                    role="radio"
                    aria-checked={fxCcy === c}
                    className={fxCcy === c ? "sel" : ""}
                    onClick={() => onFxCcyChange(c)}
                  >
                    {c === "USD" ? "🇺🇸 ABD Doları" : "🇪🇺 Euro"}
                  </button>
                ))}
              </div>
            </div>
          )}

          {type === "Stock" && (
            <label>
              Sembol
              <input
                value={form.symbol}
                onChange={(e) => set({ symbol: e.target.value.toUpperCase() })}
                onBlur={() => void lookupStock()}
                placeholder="AAPL"
                required
              />
            </label>
          )}
          {type === "Stock" && stockLookup.loading && (
            <p className="form-hint">Sembol bilgisi getiriliyor…</p>
          )}
          {type === "Stock" && stockLookup.error && (
            <p className="form-hint">{stockLookup.error}</p>
          )}

          <label>
            {isBes ? "Plan / şirket adı" : "Ad"}
            <input
              ref={firstFieldRef}
              value={form.name}
              onChange={(e) => {
                setNameTouched(true);
                set({ name: e.target.value });
              }}
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
              {type === "Fund" && (
                <label>
                  Sembol (ops.)
                  <input value={form.symbol} onChange={(e) => set({ symbol: e.target.value })} placeholder="TEKFON" />
                </label>
              )}

              <div className="add-row">
                <label>
                  {quantityLabel}
                  <input
                    inputMode="decimal"
                    value={form.quantity}
                    onChange={(e) => set({ quantity: e.target.value })}
                    placeholder={isCash ? "15.000" : type === "Gold" ? "40" : type === "Fx" ? "2.000" : "12"}
                    required
                  />
                </label>
                {!isCash && (
                  <label>
                    {type === "Fx" ? `Alış kuru (₺/${fxCcy})` : `Alış birim fiyatı (${ccySymbol})`}
                    <input
                      inputMode="decimal"
                      value={form.unitPrice}
                      onChange={(e) => {
                        setPriceTouched(true);
                        setAutoPrice(null);
                        set({ unitPrice: e.target.value });
                      }}
                      placeholder="4546,275"
                      required
                    />
                  </label>
                )}
              </div>

              {isCash && (
                <p className="form-hint">
                  Nakit TL olarak tutulur — tutarı girmen yeterli; birim ve fiyat otomatik.
                </p>
              )}
              {autoPrice && !priceTouched && (
                <p className="form-hint">
                  Güncel fiyat otomatik geldi: <b className="tnum">{formatCurrency(autoPrice.value, derived.currency)}</b>
                  {autoPrice.stale && " (yaklaşık)"} — geçmiş bir alış giriyorsan fiyatı düzenle.
                </p>
              )}
              {type === "Fund" && (
                <p className="form-hint">
                  Fon fiyatı için canlı kaynak yok — alış birim fiyatını fon platformundan bakarak gir.
                </p>
              )}
            </>
          )}

          {isError && (
            <p className="neg" role="alert">
              {err instanceof Error ? err.message : "Eklenemedi."}
            </p>
          )}

          {!valid && !isError && <p className="form-hint">{invalidHint}</p>}

          <div className="add-actions">
            <button type="button" className="btn-ghost" onClick={() => withViewTransition(onClose)}>
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

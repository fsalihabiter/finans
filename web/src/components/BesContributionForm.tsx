import { useState } from "react";
import { formatCurrency } from "@finans/shared";
import { useAddBesContribution } from "../lib/hooks";

const toNumber = (s: string) => Number(s.replace(",", "."));

/** Devlet katkısı oranı ödeme tarihine göre (2026-01-01 öncesi %30, sonrası %20 — geriye dönük değil). */
function stateRateOn(dateStr: string): number {
  return new Date(dateStr) < new Date("2026-01-01T00:00:00Z") ? 0.3 : 0.2;
}

/**
 * BES'e **aylık katkı** ekler (kendi katkı + devlet katkısı). BES nominal hesaptır;
 * alış/satış değil — maliyet tabanı (kendi+devlet) büyür, fon getirisi güncel değerden gelir.
 * Devlet katkısı **ödeme tarihindeki orana** göre hesaplanır (2026 öncesi %30, sonrası %20);
 * burada da önizlenir.
 */
export function BesContributionForm({
  holdingId,
  onDone,
}: {
  holdingId: string;
  onDone?: () => void;
}) {
  const add = useAddBesContribution(holdingId);
  const today = new Date().toISOString().slice(0, 10);
  const [own, setOwn] = useState("");
  const [paidAt, setPaidAt] = useState(today);

  const ownAmount = toNumber(own);
  const valid = Number.isFinite(ownAmount) && ownAmount > 0 && paidAt !== "";
  const rate = stateRateOn(paidAt);
  const estimatedState = valid ? Math.round(ownAmount * rate * 100) / 100 : 0;

  const onSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!valid) return;
    add.mutate(
      { ownAmount, paidAtUtc: `${paidAt}T00:00:00Z` },
      { onSuccess: () => { setOwn(""); onDone?.(); } },
    );
  };

  return (
    <form className="tx-form" onSubmit={onSubmit} aria-label="Aylık katkı ekle">
      <div className="tx-row">
        <label>
          Kendi katkın (TRY)
          <input
            inputMode="decimal"
            value={own}
            onChange={(e) => setOwn(e.target.value)}
            placeholder="örn. 2.000"
            required
          />
        </label>
        <label>
          Ödeme tarihi
          <input
            type="date"
            max={today}
            value={paidAt}
            onChange={(e) => setPaidAt(e.target.value)}
            required
          />
        </label>
        <button type="submit" disabled={!valid || add.isPending}>
          {add.isPending ? "Ekleniyor…" : "Katkı ekle"}
        </button>
      </div>
      {valid && (
        <p className="muted">
          Tahmini devlet katkısı (%{Math.round(rate * 100)}):{" "}
          <strong>{formatCurrency(estimatedState, "TRY")}</strong> — toplam maliyete eklenir.
        </p>
      )}
      {add.isError && (
        <p className="neg" role="alert">
          {add.error instanceof Error ? add.error.message : "Katkı eklenemedi."}
        </p>
      )}
    </form>
  );
}

import { useState } from "react";
import { formatCurrency } from "@finans/shared";
import { useAddBesContribution } from "../lib/hooks";

const toNumber = (s: string) => Number(s.replace(",", "."));

/**
 * BES'e **aylık katkı** ekler (kendi katkı + devlet katkısı). BES nominal hesaptır;
 * alış/satış değil — maliyet tabanı (kendi+devlet) büyür, fon getirisi güncel değerden gelir.
 * Devlet katkısı boşsa backend %30 hesaplar (TR kuralı); burada da önizlenir.
 */
export function BesContributionForm({
  holdingId,
  onDone,
}: {
  holdingId: string;
  onDone?: () => void;
}) {
  const add = useAddBesContribution(holdingId);
  const [own, setOwn] = useState("");

  const ownAmount = toNumber(own);
  const valid = Number.isFinite(ownAmount) && ownAmount > 0;
  const estimatedState = valid ? Math.round(ownAmount * 0.3 * 100) / 100 : 0;

  const onSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!valid) return;
    add.mutate({ ownAmount }, { onSuccess: () => { setOwn(""); onDone?.(); } });
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
        <button type="submit" disabled={!valid || add.isPending}>
          {add.isPending ? "Ekleniyor…" : "Katkı ekle"}
        </button>
      </div>
      {valid && (
        <p className="muted">
          Tahmini devlet katkısı (%30): <strong>{formatCurrency(estimatedState, "TRY")}</strong> —
          toplam maliyete eklenir.
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

import { useState } from "react";
import { useGenerateBesContributions } from "../lib/hooks";
import { DateField } from "./DateField";

const toNumber = (s: string) => Number(s.replace(",", "."));

/**
 * Düzenli BES katkısını **tarih aralığından** üretir (T-BES.6): aralıktaki her ay, seçilen
 * günde, girilen tutarda katkı kaydı oluşturulur (gelecek aylar hariç; zaten kayıtlı ay atlanır).
 * Devlet katkısı her ayın tarihindeki orana göre eklenir. Geçmişi doldurmak ve bugüne dek
 * "devam ettirmek" için kullanılır.
 */
export function BesContributionPlanForm({
  holdingId,
  onDone,
}: {
  holdingId: string;
  onDone?: () => void;
}) {
  const gen = useGenerateBesContributions(holdingId);
  const today = new Date().toISOString().slice(0, 10);
  const [amount, setAmount] = useState("");
  const [day, setDay] = useState("5");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState(today);

  const amt = toNumber(amount);
  const dayNum = Number(day);
  const valid =
    Number.isFinite(amt) && amt > 0 &&
    Number.isInteger(dayNum) && dayNum >= 1 && dayNum <= 28 &&
    from !== "" && to !== "" && from <= to;

  const onSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!valid) return;
    gen.mutate(
      { monthlyAmount: amt, day: dayNum, fromUtc: `${from}T00:00:00Z`, toUtc: `${to}T00:00:00Z` },
      { onSuccess: () => onDone?.() },
    );
  };

  return (
    <form className="tx-form" onSubmit={onSubmit} aria-label="Düzenli katkı oluştur">
      <p className="muted">
        Aralıktaki her ay seçtiğin günde, girdiğin tutarda katkı kaydı oluşturulur (gelecek aylar
        hariç; zaten kayıtlı aylar atlanır). Devlet katkısı her ayın tarihindeki orana göre eklenir.
      </p>
      <div className="tx-row">
        <label>
          Aylık tutar (TRY)
          <input inputMode="decimal" value={amount} onChange={(e) => setAmount(e.target.value)} placeholder="2.000" required />
        </label>
        <label>
          Ödeme günü (1–28)
          <input type="number" min={1} max={28} value={day} onChange={(e) => setDay(e.target.value)} required />
        </label>
      </div>
      <div className="tx-row">
        <label>
          Başlangıç
          <DateField value={from} onChange={setFrom} max={today} required ariaLabel="Başlangıç" />
        </label>
        <label>
          Bitiş
          <DateField value={to} onChange={setTo} max={today} required ariaLabel="Bitiş" />
        </label>
        <button type="submit" disabled={!valid || gen.isPending}>
          {gen.isPending ? "Oluşturuluyor…" : "Katkıları oluştur"}
        </button>
      </div>
      {gen.isError && (
        <p className="neg" role="alert">
          {gen.error instanceof Error ? gen.error.message : "Katkılar oluşturulamadı."}
        </p>
      )}
    </form>
  );
}

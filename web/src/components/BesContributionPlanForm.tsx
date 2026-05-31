import { useMemo, useState } from "react";
import type { BesContribution } from "@finans/shared";
import { useGenerateBesContributions } from "../lib/hooks";
import { DateField } from "./DateField";

const toNumber = (s: string) => Number(s.replace(",", "."));

/** "YYYY-MM" anahtarı — mevcut katkı ayını set'te tutmak için. */
const monthKey = (iso: string) => iso.slice(0, 7);

/**
 * Düzenli BES katkısını **tarih aralığından** üretir (T-BES.6, T-BES.9):
 * <ul>
 *   <li>Aralıktaki <b>her ay</b> seçilen günde, girilen tutarda katkı kaydı oluşturulur
 *     (gün 1–28'e kıskaçlanır). Aralık ileri tarihli olabilir (planlama).</li>
 *   <li><b>Zaten kayıtlı aylar atlanır</b> (idempotent). Form, gönder ÖNCESİ bir önizleme
 *     gösterir: "Aralıkta N ay var · K yeni eklenecek · M zaten kayıtlı" — kullanıcı
 *     sessizce 0 kayıt eklenmesini yaşamaz (önceki UX hatası, T-BES.9 fix).</li>
 *   <li>Devlet katkısı her ayın tarihindeki orana göre hesaplanır.</li>
 * </ul>
 */
export function BesContributionPlanForm({
  holdingId,
  existingContributions,
  onDone,
}: {
  holdingId: string;
  existingContributions: BesContribution[];
  /** Eklenmiş kayıt sayısı (zaten kayıtlı aylar atlandığı için 0 olabilir). */
  onDone?: (addedCount: number) => void;
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

  // Önizleme: aralıktaki ay sayısı + zaten kayıtlı ay sayısı + yeni eklenecek sayısı.
  // Backend planner ile birebir aynı kural (her ay, gün 1–28'e clamp, idempotent ay bazlı).
  const preview = useMemo(() => {
    if (!valid) return null;
    const fromD = new Date(`${from}T00:00:00Z`);
    const toD = new Date(`${to}T00:00:00Z`);
    if (toD < fromD) return null;
    const existingMonths = new Set(existingContributions.map((c) => monthKey(c.paidAtUtc)));
    let total = 0, willAdd = 0, alreadyExists = 0;
    let y = fromD.getUTCFullYear(), m = fromD.getUTCMonth();
    const endY = toD.getUTCFullYear(), endM = toD.getUTCMonth();
    while (y < endY || (y === endY && m <= endM)) {
      total++;
      const key = `${y}-${String(m + 1).padStart(2, "0")}`;
      if (existingMonths.has(key)) alreadyExists++; else willAdd++;
      m++; if (m > 11) { m = 0; y++; }
      if (total > 600) break; // güvenlik (50 yıllık tavan)
    }
    return { total, willAdd, alreadyExists };
  }, [valid, from, to, existingContributions]);

  const onSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!valid) return;
    gen.mutate(
      { monthlyAmount: amt, day: dayNum, fromUtc: `${from}T00:00:00Z`, toUtc: `${to}T00:00:00Z` },
      { onSuccess: () => onDone?.(preview?.willAdd ?? 0) },
    );
  };

  return (
    <form className="tx-form" onSubmit={onSubmit} aria-label="Düzenli katkı oluştur">
      <p className="muted">
        Aralıktaki <b>her ay</b>, seçtiğin günde girdiğin tutarda katkı kaydı oluşturulur. Zaten
        kayıtlı aylar atlanır (çiftleme yok); aralık ileri tarihli olabilir. Devlet katkısı her ayın
        tarihindeki orana göre eklenir.
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
          <DateField value={from} onChange={setFrom} required ariaLabel="Başlangıç" />
        </label>
        <label>
          Bitiş
          <DateField value={to} onChange={setTo} required ariaLabel="Bitiş" />
        </label>
      </div>
      {preview && (
        <p className={`muted preview${preview.willAdd === 0 ? " warn" : ""}`} role="status">
          Aralıkta <b>{preview.total}</b> ay · <b>{preview.willAdd}</b> yeni kayıt eklenecek ·{" "}
          <b>{preview.alreadyExists}</b> ay zaten kayıtlı (atlanacak).
          {preview.willAdd === 0 && " — Tarih aralığını değiştir veya tutar/günü güncelle."}
        </p>
      )}
      <button type="submit" disabled={!valid || gen.isPending || preview?.willAdd === 0}>
        {gen.isPending ? "Oluşturuluyor…" : `Katkıları oluştur${preview && preview.willAdd > 0 ? ` (${preview.willAdd})` : ""}`}
      </button>
      {gen.isError && (
        <p className="neg" role="alert">
          {gen.error instanceof Error ? gen.error.message : "Katkılar oluşturulamadı."}
        </p>
      )}
    </form>
  );
}

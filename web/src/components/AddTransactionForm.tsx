import { useState } from "react";
import type { CurrencyCode, TransactionType } from "@finans/shared";
import { useAddTransaction } from "../lib/hooks";

const toNumber = (s: string) => Number(s.replace(",", "."));

/**
 * Mevcut pozisyona **alış (ekleme) / satış (çıkarma)** işlemi ekler
 * (FR-1.1, `POST /holdings/{id}/transactions`). Backend ort. maliyet/miktarı
 * işlemlerden yeniden türetir (T1.5); satış eldekinden fazlaysa 400 döner.
 */
export function AddTransactionForm({
  holdingId,
  currency,
  unit,
  onDone,
}: {
  holdingId: string;
  currency: CurrencyCode;
  unit: string;
  onDone?: (type: TransactionType) => void;
}) {
  const add = useAddTransaction(holdingId);
  const [type, setType] = useState<TransactionType>("Buy");
  const [quantity, setQuantity] = useState("");
  const [unitPrice, setUnitPrice] = useState("");

  const qty = toNumber(quantity);
  const price = toNumber(unitPrice);
  const valid =
    Number.isFinite(qty) && qty > 0 && Number.isFinite(price) && price >= 0;

  const onSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!valid) return;
    add.mutate(
      { type, quantity: qty, unitPrice: price },
      {
        onSuccess: () => {
          setQuantity("");
          setUnitPrice("");
          onDone?.(type);
        },
      },
    );
  };

  return (
    <form className="tx-form" onSubmit={onSubmit} aria-label="İşlem ekle">
      <div className="tx-type" role="group" aria-label="İşlem türü">
        <button
          type="button"
          className={type === "Buy" ? "active" : ""}
          aria-pressed={type === "Buy"}
          onClick={() => setType("Buy")}
        >
          Alış
        </button>
        <button
          type="button"
          className={type === "Sell" ? "active sell" : ""}
          aria-pressed={type === "Sell"}
          onClick={() => setType("Sell")}
        >
          Satış
        </button>
      </div>

      <div className="tx-row">
        <label>
          Miktar ({unit})
          <input
            inputMode="decimal"
            value={quantity}
            onChange={(e) => setQuantity(e.target.value)}
            placeholder="10"
            required
          />
        </label>
        <label>
          Birim fiyat ({currency})
          <input
            inputMode="decimal"
            value={unitPrice}
            onChange={(e) => setUnitPrice(e.target.value)}
            placeholder="0,00"
            required
          />
        </label>
        <button type="submit" disabled={!valid || add.isPending}>
          {add.isPending ? "Ekleniyor…" : "Ekle"}
        </button>
      </div>

      {add.isError && (
        <p className="neg" role="alert">
          {add.error instanceof Error ? add.error.message : "İşlem eklenemedi."}
        </p>
      )}
    </form>
  );
}

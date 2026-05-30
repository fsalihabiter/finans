import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { TransactionHistory } from "./TransactionHistory";
import type { Transaction } from "@finans/shared";

const tx: Transaction = {
  id: "t1",
  type: "Buy",
  quantity: 40,
  unitPrice: 4546.275,
  fee: 0,
  transactedAtUtc: "2024-06-01T00:00:00Z",
};

describe("TransactionHistory", () => {
  it("işlemi tür/miktar/tutar ile gösterir", () => {
    render(<TransactionHistory transactions={[tx]} currency="TRY" unit="gram" />);
    expect(screen.getByText("Alış")).toBeInTheDocument();
    // Tutar = 40 × 4.546,275 = 181.851,00
    expect(screen.getByText(/181\.851,00/)).toBeInTheDocument();
  });

  it("boş geçmişte 'henüz işlem yok' der", () => {
    render(<TransactionHistory transactions={[]} currency="TRY" unit="birim" />);
    expect(screen.getByText(/Henüz işlem yok/)).toBeInTheDocument();
  });
});

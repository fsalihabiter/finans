import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { PriceTicker } from "./PriceTicker";
import type { PriceDto } from "@finans/shared";

const prices: PriceDto[] = [
  {
    kind: "Gold", currency: "TRY", price: 6687.67, quoteCurrency: "TRY",
    asOfUtc: "2026-05-31T08:00:00Z", source: "truncgil", stale: false,
  },
  {
    kind: "Currency", currency: "USD", price: 45.886, quoteCurrency: "TRY",
    asOfUtc: "2026-05-31T08:00:00Z", source: "frankfurter", stale: true,
  },
];

describe("PriceTicker", () => {
  it("altın/döviz değerlerini, kaynağı ve bayat işaretini gösterir", () => {
    render(<PriceTicker prices={prices} />);
    // Statik şerit → her değer tek kez.
    expect(screen.getByText("Gram altın")).toBeInTheDocument();
    expect(screen.getByText("USD")).toBeInTheDocument();
    expect(screen.getByText(/Kaynak: Frankfurter/)).toBeInTheDocument();
    expect(screen.getByText(/yaklaşık/)).toBeInTheDocument();
  });

  it("boş listede hiçbir şey çizmez", () => {
    const { container } = render(<PriceTicker prices={[]} />);
    expect(container.firstChild).toBeNull();
  });
});

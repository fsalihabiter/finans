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
    // İçerik kesintisiz döngü için iki kez sunulur → getAllByText.
    expect(screen.getAllByText("Gram altın").length).toBeGreaterThan(0);
    expect(screen.getAllByText("USD").length).toBeGreaterThan(0);
    expect(screen.getAllByText(/Kaynak: Frankfurter/).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/yaklaşık/).length).toBeGreaterThan(0);
  });

  it("boş listede hiçbir şey çizmez", () => {
    const { container } = render(<PriceTicker prices={[]} />);
    expect(container.firstChild).toBeNull();
  });
});

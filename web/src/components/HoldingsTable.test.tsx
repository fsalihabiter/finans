import { describe, it, expect } from "vitest";
import { render, screen, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { HoldingsTable } from "./HoldingsTable";
import type { Holding } from "@finans/shared";

const gold: Holding = {
  id: "h1",
  assetType: "Gold",
  name: "Altın",
  symbol: "XAU",
  currency: "TRY",
  unit: "gram",
  quantity: 40,
  avgCost: 4546.275,
  currentPrice: 6500,
  totalCost: 181851,
  currentValue: 260000,
  profit: 78149,
  returnRatio: 0.43,
  weight: 0.405,
  bes: null,
};

describe("HoldingsTable", () => {
  it("kalemi tr-TR biçimiyle ve detay bağlantısıyla gösterir", () => {
    render(
      <MemoryRouter>
        <HoldingsTable holdings={[gold]} baseCurrency="TRY" />
      </MemoryRouter>,
    );

    const link = screen.getByRole("link", { name: "Altın" });
    expect(link).toHaveAttribute("href", "/holdings/h1");

    const row = link.closest("tr")!;
    expect(within(row).getByText(/260\.000,00/)).toBeInTheDocument(); // değer
    expect(within(row).getByText("+%43,0")).toBeInTheDocument(); // getiri
    expect(within(row).getByText("%40,5")).toBeInTheDocument(); // ağırlık (işaretsiz)
  });

  it("boş listede tablo çizmez", () => {
    const { container } = render(
      <MemoryRouter>
        <HoldingsTable holdings={[]} baseCurrency="TRY" />
      </MemoryRouter>,
    );
    expect(container.querySelector("table")).toBeNull();
  });
});

import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { AllocationDonut } from "./AllocationDonut";
import type { AllocationSlice } from "@finans/shared";

const allocation: AllocationSlice[] = [
  { assetType: "Gold", name: "Altın", value: 260000, weight: 0.405 },
  { assetType: "Bes", name: "BES", value: 279378, weight: 0.436 },
];

describe("AllocationDonut", () => {
  it("her dilim için ad ve işaretsiz yüzde gösterir", () => {
    render(<AllocationDonut allocation={allocation} baseCurrency="TRY" />);
    expect(screen.getByText("Altın")).toBeInTheDocument();
    // Ağırlıkta "+" olmamalı (getiri değil)
    expect(screen.getByText("%40,5")).toBeInTheDocument();
    expect(screen.getByText("%43,6")).toBeInTheDocument();
  });

  it("erişilebilir özet (role=img + aria-label) sunar", () => {
    render(<AllocationDonut allocation={allocation} baseCurrency="TRY" />);
    const img = screen.getByRole("img", { name: /Altın %40,5/ });
    expect(img).toBeInTheDocument();
  });

  it("boş dağılımda hiçbir şey çizmez", () => {
    const { container } = render(<AllocationDonut allocation={[]} baseCurrency="TRY" />);
    expect(container.firstChild).toBeNull();
  });
});

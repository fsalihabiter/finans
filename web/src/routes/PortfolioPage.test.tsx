import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { PortfolioPage } from "./PortfolioPage";

// Web → @finans/shared bağının çalıştığını ve tr-TR biçimlemenin DOM'a ulaştığını
// doğrular (T0.10/T0.11 web ayağı). Ağ gerektirmez.
describe("PortfolioPage", () => {
  it("paylaşılan format util ile tr-TR para biçimini gösterir", () => {
    render(<PortfolioPage />);
    expect(screen.getByRole("heading", { name: "Portföy" })).toBeInTheDocument();
    expect(screen.getByText(/641\.403,00/)).toBeInTheDocument();
    expect(screen.getByText(/\+%51,6/)).toBeInTheDocument();
  });
});

import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { AnalysisPage } from "./AnalysisPage";

// SC-W2 (NFR-2): Analiz sayfasında "yatırım tavsiyesi değildir" çerçevesi HER ZAMAN görünür.
describe("AnalysisPage", () => {
  it("disclaimer'ı (yatırım tavsiyesi değildir) gösterir", () => {
    render(<AnalysisPage />);
    expect(screen.getByText(/Yatırım tavsiyesi değildir/i)).toBeInTheDocument();
    expect(screen.getByRole("note")).toBeInTheDocument();
  });
});

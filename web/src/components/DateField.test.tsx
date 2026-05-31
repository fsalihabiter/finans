import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { DateField } from "./DateField";

describe("DateField (native)", () => {
  it("native date input render eder ve ISO değeri taşır", () => {
    render(<DateField value="2026-03-01" onChange={() => {}} ariaLabel="t" />);
    const input = screen.getByLabelText("t");
    expect(input).toHaveAttribute("type", "date");
    expect(input).toHaveValue("2026-03-01");
  });

  it("değişimde ISO emit eder", () => {
    const onChange = vi.fn();
    render(<DateField value="" onChange={onChange} ariaLabel="t" />);
    fireEvent.change(screen.getByLabelText("t"), { target: { value: "2026-03-05" } });
    expect(onChange).toHaveBeenLastCalledWith("2026-03-05");
  });

  it("varsayılan olarak üst sınır yok (ileri tarih serbest)", () => {
    render(<DateField value="" onChange={() => {}} ariaLabel="t" />);
    expect(screen.getByLabelText("t")).not.toHaveAttribute("max");
  });

  it("max verilince native max niteliğine yansır", () => {
    render(<DateField value="" onChange={() => {}} max="2026-05-31" ariaLabel="t" />);
    expect(screen.getByLabelText("t")).toHaveAttribute("max", "2026-05-31");
  });
});

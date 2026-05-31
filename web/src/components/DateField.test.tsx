import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { DateField } from "./DateField";
import { dottedToIso } from "../lib/dateMask";

describe("dottedToIso", () => {
  it("gg.aa.yyyy → ISO", () => {
    expect(dottedToIso("01.03.2026")).toBe("2026-03-01");
  });
  it("geçersiz tarih (31.02) → boş", () => {
    expect(dottedToIso("31.02.2026")).toBe("");
  });
  it("eksik → boş", () => {
    expect(dottedToIso("1.3.26")).toBe("");
  });
});

describe("DateField", () => {
  it("ISO değeri gg.aa.yyyy gösterir", () => {
    render(<DateField value="2026-03-01" onChange={() => {}} ariaLabel="t" />);
    expect(screen.getByLabelText("t")).toHaveValue("01.03.2026");
  });

  it("rakam yazınca maskeler ve geçerli olunca ISO emit eder", () => {
    const onChange = vi.fn();
    render(<DateField value="" onChange={onChange} ariaLabel="t" />);
    const input = screen.getByLabelText("t");
    fireEvent.change(input, { target: { value: "05032026" } });
    expect(input).toHaveValue("05.03.2026");
    expect(onChange).toHaveBeenLastCalledWith("2026-03-05");
  });

  it("max aşımında (ileri tarih) boş emit eder", () => {
    const onChange = vi.fn();
    render(<DateField value="" onChange={onChange} max="2026-05-31" ariaLabel="t" />);
    fireEvent.change(screen.getByLabelText("t"), { target: { value: "01.01.2030" } });
    expect(onChange).toHaveBeenLastCalledWith("");
  });
});

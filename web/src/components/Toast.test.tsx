import { describe, expect, it } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { ToastProvider, useToast } from "./Toast";

function Trigger() {
  const { notify } = useToast();
  return (
    <button type="button" onClick={() => notify("Varlık eklendi.", "success")}>
      tetikle
    </button>
  );
}

describe("Toast", () => {
  it("notify çağrılınca bildirim gösterir ve kapatılabilir", () => {
    render(
      <ToastProvider>
        <Trigger />
      </ToastProvider>,
    );

    expect(screen.queryByText("Varlık eklendi.")).toBeNull();

    fireEvent.click(screen.getByRole("button", { name: "tetikle" }));
    expect(screen.getByText("Varlık eklendi.")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Kapat" }));
    expect(screen.queryByText("Varlık eklendi.")).toBeNull();
  });

  it("sağlayıcı yoksa notify no-op (çökmemeli)", () => {
    // useToast varsayılan context'i no-op döndürür → render hatası yok.
    expect(() => render(<Trigger />)).not.toThrow();
  });
});

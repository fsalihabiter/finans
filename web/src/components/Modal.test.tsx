import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { Modal } from "./Modal";

describe("Modal", () => {
  it("başlık + içerik gösterir; kapat ve Escape onClose çağırır", () => {
    const onClose = vi.fn();
    render(
      <Modal title="İşlem ekle" onClose={onClose}>
        <p>gövde</p>
      </Modal>,
    );

    expect(screen.getByRole("dialog", { name: "İşlem ekle" })).toBeInTheDocument();
    expect(screen.getByText("gövde")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Kapat" }));
    expect(onClose).toHaveBeenCalledTimes(1);

    fireEvent.keyDown(window, { key: "Escape" });
    expect(onClose).toHaveBeenCalledTimes(2);
  });
});

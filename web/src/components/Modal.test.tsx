import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { Modal } from "./Modal";

/** Bir animasyon karesini bekler (Modal odağı rAF içinde verir). */
const nextFrame = () => new Promise<number>(requestAnimationFrame);

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

  it("açılışta odak içerik alanındaki ilk alana gelir (kapat butonuna değil)", async () => {
    render(
      <Modal title="Fiyatı güncelle" onClose={() => {}}>
        <input aria-label="fiyat" />
      </Modal>,
    );
    await nextFrame();
    expect(screen.getByLabelText("fiyat")).toHaveFocus();
  });

  it("ebeveyn yeniden render'ında (onClose kimliği değişse de) odağı çalmaz", async () => {
    // Gerçek hata: her tuşta setState → yeni onClose → effect yeniden çalışıp odağı sıçratıyordu.
    const { rerender } = render(
      <Modal title="Fiyatı güncelle" onClose={() => {}}>
        <input aria-label="fiyat" />
      </Modal>,
    );
    const input = screen.getByLabelText("fiyat");
    await nextFrame();
    input.focus();

    rerender(
      <Modal title="Fiyatı güncelle" onClose={() => {}}>
        <input aria-label="fiyat" />
      </Modal>,
    );
    await nextFrame();

    expect(input).toHaveFocus();
    expect(screen.getByRole("button", { name: "Kapat" })).not.toHaveFocus();
  });
});

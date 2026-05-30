import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { EmptyState } from "./EmptyState";

describe("EmptyState", () => {
  it("başlık, açıklama ve eylemi gösterir; eylem tıklanabilir", () => {
    const onClick = vi.fn();
    render(
      <EmptyState
        icon="📂"
        title="Portföyün henüz boş"
        description="İlk varlığını ekle."
        action={
          <button type="button" onClick={onClick}>
            İlk varlığını ekle
          </button>
        }
      />,
    );

    expect(screen.getByRole("heading", { name: "Portföyün henüz boş" })).toBeInTheDocument();
    expect(screen.getByText("İlk varlığını ekle.")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "İlk varlığını ekle" }));
    expect(onClick).toHaveBeenCalledOnce();
  });
});

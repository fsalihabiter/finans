import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { BesContributionHistory } from "./BesContributionHistory";
import type { BesContribution } from "@finans/shared";

const items: BesContribution[] = [
  { id: "c1", ownAmount: 1000, stateAmount: 200, paidAtUtc: "2026-03-05T00:00:00Z", source: "Plan" },
  { id: "c2", ownAmount: 1000, stateAmount: 300, paidAtUtc: "2025-12-05T00:00:00Z", source: "Manual" },
];

describe("BesContributionHistory", () => {
  it("düzenle/sil ikon butonlarını gösterir ve geri çağırır", () => {
    const onEdit = vi.fn();
    const onDelete = vi.fn();
    render(<BesContributionHistory contributions={items} onEdit={onEdit} onDelete={onDelete} />);

    const editButtons = screen.getAllByRole("button", { name: "Düzenle" });
    const deleteButtons = screen.getAllByRole("button", { name: "Sil" });
    expect(editButtons).toHaveLength(2);
    expect(deleteButtons).toHaveLength(2);

    fireEvent.click(editButtons[0]);
    expect(onEdit).toHaveBeenCalledWith(items[0]);
    fireEvent.click(deleteButtons[1]);
    expect(onDelete).toHaveBeenCalledWith(items[1]);
  });

  it("boşsa bilgi metni gösterir", () => {
    render(<BesContributionHistory contributions={[]} />);
    expect(screen.getByText(/Henüz katkı kaydı yok/)).toBeInTheDocument();
  });
});

import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { BesContributionHistory } from "./BesContributionHistory";
import type { BesContribution } from "@finans/shared";

const rows: BesContribution[] = [
  {
    id: "c1", ownAmount: 1000, stateAmount: 200, paidAtUtc: "2026-03-05T00:00:00Z",
    source: "Manual", status: "Deposited", stateDepositDate: "2026-04-30T00:00:00Z",
  },
  {
    id: "c2", ownAmount: 1000, stateAmount: 300, paidAtUtc: "2025-12-05T00:00:00Z",
    source: "Plan", status: "Deposited", stateDepositDate: "2026-01-31T00:00:00Z",
  },
];

describe("BesContributionHistory", () => {
  it("satırları render eder + düzenle/sil çağrılır", () => {
    const onEdit = vi.fn();
    const onDelete = vi.fn();
    render(<BesContributionHistory contributions={rows} onEdit={onEdit} onDelete={onDelete} />);
    // Fixture: paidAt 2026-03-05 → 05.03.2026; 2025-12-05 → 05.12.2025.
    expect(screen.getByText("05.03.2026")).toBeInTheDocument();
    expect(screen.getByText("05.12.2025")).toBeInTheDocument();
    fireEvent.click(screen.getAllByLabelText("Düzenle")[0]);
    expect(onEdit).toHaveBeenCalledWith(rows[0]);
    fireEvent.click(screen.getAllByLabelText("Sil")[1]);
    expect(onDelete).toHaveBeenCalledWith(rows[1]);
  });

  it("durumu sol şerit (CSS sınıfı) ile gösterir — ayrı kolon YOK", () => {
    const mixed: BesContribution[] = [
      { ...rows[0], id: "d", status: "Deposited" },
      { ...rows[0], id: "p", status: "StatePending" },
      { ...rows[0], id: "f", status: "Future" },
    ];
    const { container } = render(<BesContributionHistory contributions={mixed} />);
    // Lejant durumu zaten açıklar; satırda durum metin sütunu olmamalı (Tarih/Katkı/Devlet/İşlem).
    expect(container.querySelectorAll("thead th")).toHaveLength(4);
    // Renk durumu sınıf üzerinden uygulanır.
    expect(container.querySelector("tr.hist-deposited")).not.toBeNull();
    expect(container.querySelector("tr.hist-pending")).not.toBeNull();
    expect(container.querySelector("tr.hist-future")).not.toBeNull();
  });

  it("açılış kaydını 'Açılış' olarak gösterir", () => {
    const opening: BesContribution[] = [{ ...rows[0], id: "o", source: "Opening" }];
    render(<BesContributionHistory contributions={opening} />);
    expect(screen.getByText("Açılış")).toBeInTheDocument();
  });

  it("boşsa bilgi metni gösterir", () => {
    render(<BesContributionHistory contributions={[]} />);
    expect(screen.getByText("Henüz katkı kaydı yok.")).toBeInTheDocument();
  });

  it("devlet katkısının etkin oranını satırda ve ödenmiş toplamda gösterir", () => {
    render(<BesContributionHistory contributions={rows} />);
    // c1: 200/1000 → %20; c2: 300/1000 → %30.
    expect(screen.getByText("%20")).toBeInTheDocument();
    // %30 hem c2 satırında hem değil — toplam 500/2000 → %25 toplam satırında.
    expect(screen.getByText("%30")).toBeInTheDocument();
    expect(screen.getByText("%25")).toBeInTheDocument();
  });
});

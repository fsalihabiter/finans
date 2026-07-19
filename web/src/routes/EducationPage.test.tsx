import { fireEvent, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { renderWithProviders } from "../test/renderWithProviders";
import { EducationPage } from "./EducationPage";

const tracks = [
  { id: "t1", slug: "temeller", title: "Temeller", description: null, level: "Beginner", lessonCount: 3 },
];

const lessons = [
  { id: "l1", slug: "enflasyon", order: 1, title: "Enflasyon ve Reel Getiri", summary: "Param büyüdü mü?", estimatedMinutes: 4, level: "Beginner", status: "Completed", progressPercent: 100, locked: false },
  { id: "l2", slug: "cesit", order: 2, title: "Çeşitlendirme", summary: "Tek sepete koyma", estimatedMinutes: 5, level: "Beginner", status: "InProgress", progressPercent: 0, locked: false },
  { id: "l5", slug: "bilesik", order: 5, title: "Bileşik Getiri", summary: "Zaman dostun", estimatedMinutes: 5, level: "Beginner", status: "NotStarted", progressPercent: 0, locked: true },
];

const lessonDetail = {
  ...lessons[0],
  bodyMarkdown: "## Nominal mi, reel mi?\n\nGövde metni burada.",
  sections: [],
  quiz: null,
  conceptTags: [{ key: "real-return", label: "Reel Getiri" }],
};

function ok(body: unknown) {
  return Promise.resolve({ ok: true, status: 200, json: async () => body } as Response);
}

function mockApi() {
  vi.stubGlobal(
    "fetch",
    vi.fn((url: string) => {
      if (url.endsWith("/api/education/tracks")) return ok(tracks);
      if (url.includes("/tracks/temeller/lessons")) return ok(lessons);
      if (url.includes("/api/education/lessons/")) return ok(lessonDetail);
      return Promise.reject(new Error(`beklenmeyen istek: ${url}`));
    }),
  );
}

afterEach(() => vi.restoreAllMocks());

describe("EducationPage", () => {
  it("dersleri ilerleme + kilit durumuyla listeler", async () => {
    mockApi();
    renderWithProviders(<EducationPage />);

    expect(await screen.findByText("Enflasyon ve Reel Getiri")).toBeInTheDocument();
    expect(screen.getByText("✓ Tamamlandı")).toBeInTheDocument();
    expect(screen.getByText("🔒 Kilitli")).toBeInTheDocument();
    expect(screen.getByText("1/3 ders tamamlandı")).toBeInTheDocument();
  });

  it("kilitli ders tıklanamaz (disabled)", async () => {
    mockApi();
    renderWithProviders(<EducationPage />);

    await screen.findByText("Bileşik Getiri");
    expect(screen.getByText("Bileşik Getiri").closest("button")).toBeDisabled();
  });

  it("derse tıklayınca okuma görünümünü (Markdown gövde + kavram) açar", async () => {
    mockApi();
    renderWithProviders(<EducationPage />);

    fireEvent.click(await screen.findByText("Enflasyon ve Reel Getiri"));

    expect(await screen.findByText("Gövde metni burada.")).toBeInTheDocument();
    expect(screen.getByRole("heading", { level: 3, name: "Nominal mi, reel mi?" })).toBeInTheDocument();
    expect(screen.getByText("Reel Getiri")).toBeInTheDocument(); // kavram etiketi
    expect(screen.getByText("← Derslere dön")).toBeInTheDocument();
  });

  // ── T6.1/T6.2: katmanlı bölümler + bağlam rozeti + ilerleme akışı ──────────

  /** Bölümlü ders detayı; `contextState` ile bağlam rozeti senaryosu kurulur. */
  function detailWithSections(over: Record<string, unknown> = {}) {
    return {
      ...lessonDetail,
      sections: [
        { order: 1, heading: null, bodyMarkdown: "Çekirdek anlatım.", depthTier: "Core", kind: "Explain" },
        { order: 2, heading: null, bodyMarkdown: "Derin katman.", depthTier: "Deep", kind: "Explain" },
        { order: 3, heading: null, bodyMarkdown: "Yoğunlaşman %80.", depthTier: "Core", kind: "LiveContext" },
      ],
      contextState: "Own",
      contextAsOf: null,
      nextLesson: null,
      ...over,
    };
  }

  function mockDetail(detail: unknown) {
    vi.stubGlobal(
      "fetch",
      vi.fn((url: string) => {
        if (url.endsWith("/api/education/tracks")) return ok(tracks);
        if (url.includes("/tracks/temeller/lessons")) return ok(lessons);
        if (url.includes("/api/education/lessons/")) return ok(detail);
        return Promise.reject(new Error(`beklenmeyen istek: ${url}`));
      }),
    );
  }

  it("katmanlı bölümleri render eder (bodyMarkdown yerine)", async () => {
    mockDetail(detailWithSections());
    renderWithProviders(<EducationPage />);

    fireEvent.click(await screen.findByText("Enflasyon ve Reel Getiri"));

    expect(await screen.findByText("Çekirdek anlatım.")).toBeInTheDocument();
    expect(screen.getByText("Derin katman.")).toBeInTheDocument();
    expect(screen.getByText("Yoğunlaşman %80.")).toBeInTheDocument();
    // Bölüm varken fallback gövde GÖSTERİLMEZ (çift içerik olmaz).
    expect(screen.queryByText("Gövde metni burada.")).not.toBeInTheDocument();
  });

  it("kendi verisinde bağlam rozeti göstermez, demo verisinde gösterir", async () => {
    mockDetail(detailWithSections()); // contextState: "Own"
    const own = renderWithProviders(<EducationPage />);
    fireEvent.click(await screen.findByText("Enflasyon ve Reel Getiri"));
    await screen.findByText("Yoğunlaşman %80.");
    expect(screen.queryByText(/Örnek portföy/)).not.toBeInTheDocument();
    own.unmount();

    mockDetail(detailWithSections({ contextState: "Demo" }));
    renderWithProviders(<EducationPage />);
    fireEvent.click(await screen.findByText("Enflasyon ve Reel Getiri"));
    expect(await screen.findByText(/Örnek portföy/)).toBeInTheDocument();
  });

  it("tamamlanmış derste sonraki derse geçiş sunar; set sonunda sunmaz", async () => {
    mockDetail(
      detailWithSections({
        status: "Completed",
        nextLesson: { id: "l2", slug: "cesit", title: "Çeşitlendirme", locked: false },
      }),
    );
    const withNext = renderWithProviders(<EducationPage />);
    fireEvent.click(await screen.findByText("Enflasyon ve Reel Getiri"));

    const nextBtn = await screen.findByRole("button", { name: /Sonraki ders: Çeşitlendirme/ });
    expect(nextBtn).toBeEnabled();
    withNext.unmount();

    // Set sonu: sonraki ders yok → geçiş butonu yerine tamamlama rozeti.
    mockDetail(detailWithSections({ status: "Completed", nextLesson: null }));
    renderWithProviders(<EducationPage />);
    fireEvent.click(await screen.findByText("Enflasyon ve Reel Getiri"));

    expect(await screen.findByText("🎉 Seti tamamladın")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Sonraki ders/ })).not.toBeInTheDocument();
  });
});

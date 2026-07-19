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

  // ── T6.7: seviyeye göre katlama + figür ────────────────────────────────────

  /** Üç derinlik katmanı + figürlü örnek bloğu taşıyan ders. */
  function layeredDetail() {
    return {
      ...lessonDetail,
      sections: [
        { order: 1, heading: null, bodyMarkdown: "Çekirdek anlatım.", depthTier: "Core", kind: "Explain", figureKey: null },
        { order: 2, heading: null, bodyMarkdown: "Bağlam katmanı.", depthTier: "Context", kind: "Explain", figureKey: null },
        { order: 3, heading: null, bodyMarkdown: "Uzman katmanı.", depthTier: "Deep", kind: "Explain", figureKey: null },
        { order: 4, heading: null, bodyMarkdown: "Örnek metni.", depthTier: "Core", kind: "Example", figureKey: "real-vs-nominal" },
      ],
      contextState: null,
      contextAsOf: null,
      nextLesson: null,
    };
  }

  /** Profil ölçülmüş kullanıcı (onboarding atlanır) + katmanlı ders. */
  function mockAtLevel(level: string) {
    vi.stubGlobal(
      "fetch",
      vi.fn((url: string) => {
        if (url.endsWith("/api/education/profile"))
          return ok({ literacyLevel: level, profiled: true });
        if (url.endsWith("/api/education/tracks")) return ok(tracks);
        if (url.includes("/tracks/temeller/lessons")) return ok(lessons);
        if (url.includes("/api/education/lessons/")) return ok(layeredDetail());
        return Promise.reject(new Error(`beklenmeyen istek: ${url}`));
      }),
    );
  }

  it("başlangıç seviyesinde üst katmanları katlar, çekirdeği açık gösterir", async () => {
    mockAtLevel("Beginner");
    renderWithProviders(<EducationPage />);
    fireEvent.click(await screen.findByText("Enflasyon ve Reel Getiri"));

    // Core açık; Context/Deep <details> içinde (kapalı ama DOM'da).
    expect(await screen.findByText("Çekirdek anlatım.")).toBeVisible();
    expect(screen.getByText("Daha derine in")).toBeInTheDocument();
    expect(screen.getByText("Uzman katmanı")).toBeInTheDocument();
    expect(screen.getByText("Bağlam katmanı.").closest("details")).not.toHaveAttribute("open");
  });

  it("ileri seviyede derin katman katlanmaz (tavan kapatılmaz)", async () => {
    mockAtLevel("Advanced");
    renderWithProviders(<EducationPage />);
    fireEvent.click(await screen.findByText("Enflasyon ve Reel Getiri"));

    await screen.findByText("Çekirdek anlatım.");
    // Hiçbir katman katlanmadı → "Daha derine in" başlığı yok.
    expect(screen.queryByText("Daha derine in")).not.toBeInTheDocument();
    expect(screen.getByText("Uzman katmanı.")).toBeVisible();
  });

  it("katlanmış katman açılabilir — içerik kimseden gizlenmez", async () => {
    mockAtLevel("Beginner");
    renderWithProviders(<EducationPage />);
    fireEvent.click(await screen.findByText("Enflasyon ve Reel Getiri"));

    const details = (await screen.findByText("Bağlam katmanı.")).closest("details")!;
    expect(details).not.toHaveAttribute("open");
    fireEvent.click(screen.getByText("Daha derine in"));
    expect(details).toHaveAttribute("open");
  });

  it("figür anahtarı olan bölümde erişilebilir görsel çizer", async () => {
    mockAtLevel("Beginner");
    renderWithProviders(<EducationPage />);
    fireEvent.click(await screen.findByText("Enflasyon ve Reel Getiri"));

    expect(await screen.findByRole("img", { name: /nominal ve reel getirisi/i })).toBeInTheDocument();
  });

  it("bilinmeyen figür anahtarı içeriği bozmaz", async () => {
    const detail = layeredDetail();
    detail.sections[3].figureKey = "boyle-bir-figur-yok";
    vi.stubGlobal(
      "fetch",
      vi.fn((url: string) => {
        if (url.endsWith("/api/education/profile")) return ok({ literacyLevel: "Beginner", profiled: true });
        if (url.endsWith("/api/education/tracks")) return ok(tracks);
        if (url.includes("/tracks/temeller/lessons")) return ok(lessons);
        if (url.includes("/api/education/lessons/")) return ok(detail);
        return Promise.reject(new Error(`beklenmeyen istek: ${url}`));
      }),
    );
    renderWithProviders(<EducationPage />);
    fireEvent.click(await screen.findByText("Enflasyon ve Reel Getiri"));

    // Metin yerinde, figür sessizce atlandı.
    expect(await screen.findByText("Örnek metni.")).toBeInTheDocument();
    expect(screen.queryByRole("img", { name: /getiri/i })).not.toBeInTheDocument();
  });

  // ── T6.6: tanılama onboarding'i ────────────────────────────────────────────

  const diagnosticQuestions = [
    {
      key: "real-return",
      kind: "Knowledge",
      prompt: "Faiz %40, enflasyon %50. Alım gücün ne olur?",
      options: [
        { key: "increased", text: "Arttı" },
        { key: "decreased", text: "Azaldı" },
        { key: "same", text: "Aynı kaldı" },
      ],
    },
    {
      key: "drawdown",
      kind: "Scenario",
      prompt: "Portföyün %20 düştü. İlk tepkin?",
      options: [
        { key: "sell", text: "Satarım" },
        { key: "wait", text: "Beklerim" },
        { key: "buy", text: "Eklerim" },
      ],
    },
  ];

  /** Profil ölçülmemiş kullanıcı → onboarding görünmeli. */
  function mockUnprofiled(result?: unknown) {
    vi.stubGlobal(
      "fetch",
      vi.fn((url: string, init?: RequestInit) => {
        if (url.endsWith("/api/education/profile"))
          return ok({ literacyLevel: null, profiled: false });
        if (url.endsWith("/api/education/diagnostic") && init?.method === "POST")
          return ok(result ?? { literacyLevel: "Beginner", message: "Baştan başlayalım." });
        if (url.endsWith("/api/education/diagnostic")) return ok(diagnosticQuestions);
        if (url.endsWith("/api/education/tracks")) return ok(tracks);
        if (url.includes("/tracks/temeller/lessons")) return ok(lessons);
        if (url.includes("/api/education/lessons/")) return ok(lessonDetail);
        return Promise.reject(new Error(`beklenmeyen istek: ${url}`));
      }),
    );
  }

  it("profil ölçülmemişse önce tanılama testini gösterir", async () => {
    mockUnprofiled();
    renderWithProviders(<EducationPage />);

    expect(await screen.findByText("Haritada neredesin?")).toBeInTheDocument();
    expect(screen.getByText(/Faiz %40, enflasyon %50/)).toBeInTheDocument();
    // Ders listesi henüz gösterilmez.
    expect(screen.queryByText("Enflasyon ve Reel Getiri")).not.toBeInTheDocument();
  });

  it("tanılama sonucu risk tutumunu ASLA göstermez (SPK sınırı)", async () => {
    mockUnprofiled();
    renderWithProviders(<EducationPage />);

    fireEvent.click(await screen.findByText("Azaldı"));
    fireEvent.click(screen.getByText("Eklerim"));
    fireEvent.click(screen.getByRole("button", { name: "Bitir" }));

    expect(await screen.findByText("Baştan başlayalım.")).toBeInTheDocument();
    // 🔒 15 §1.1 — tutum etiketi hiçbir yerde geçmemeli.
    for (const label of ["Temkinli", "Dengeli", "Atılgan", "Atilgan"]) {
      expect(screen.queryByText(new RegExp(label, "i"))).not.toBeInTheDocument();
    }
  });

  it("tanılama atlanabilir ve derslere düşer", async () => {
    mockUnprofiled();
    renderWithProviders(<EducationPage />);

    fireEvent.click(await screen.findByRole("button", { name: "Atla" }));
    fireEvent.click(await screen.findByRole("button", { name: /Derslere başla/ }));

    expect(await screen.findByText("Enflasyon ve Reel Getiri")).toBeInTheDocument();
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

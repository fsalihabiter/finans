import { fireEvent, screen, within } from "@testing-library/react";
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

  // ── T6.10: aşamalı adım akışı + yol haritası ──────────────────────────────

  /** Başlıklı, üç derinlik katmanlı, figürlü ve testli ders. */
  function steppedDetail(over: Record<string, unknown> = {}) {
    return {
      ...lessonDetail,
      sections: [
        { order: 1, heading: "Kavram nedir?", bodyMarkdown: "Çekirdek anlatım.", depthTier: "Core", kind: "Explain", figureKey: null },
        { order: 2, heading: "Nasıl hesaplanır?", bodyMarkdown: "Bağlam katmanı.", depthTier: "Context", kind: "Explain", figureKey: null },
        { order: 3, heading: "İşin matematiği", bodyMarkdown: "Uzman katmanı.", depthTier: "Deep", kind: "Explain", figureKey: null },
        { order: 4, heading: "Örnek", bodyMarkdown: "Örnek metni.", depthTier: "Core", kind: "Example", figureKey: "real-vs-nominal" },
      ],
      quiz: {
        id: "q1",
        title: "Mini Test",
        passingScore: 60,
        questions: [
          { id: "qq1", order: 1, type: "SingleChoice", prompt: "Soru?", options: [{ id: "o1", order: 1, text: "Şık" }] },
        ],
      },
      contextState: null,
      contextAsOf: null,
      nextLesson: null,
      ...over,
    };
  }

  function mockStepped(level: string, detail: unknown = steppedDetail()) {
    vi.stubGlobal(
      "fetch",
      vi.fn((url: string) => {
        if (url.endsWith("/api/education/profile")) return ok({ literacyLevel: level, profiled: true });
        if (url.endsWith("/api/education/tracks")) return ok(tracks);
        if (url.includes("/tracks/temeller/lessons")) return ok(lessons);
        if (url.includes("/api/education/lessons/")) return ok(detail);
        return Promise.reject(new Error(`beklenmeyen istek: ${url}`));
      }),
    );
  }

  async function openLesson(level = "Beginner", detail: unknown = steppedDetail()) {
    mockStepped(level, detail);
    renderWithProviders(<EducationPage />);
    fireEvent.click(await screen.findByText("Enflasyon ve Reel Getiri"));
    return screen.findByText("Çekirdek anlatım.");
  }

  it("tek seferde tek adım gösterir (hepsini birden dökmez)", async () => {
    await openLesson();

    expect(screen.getByText("Çekirdek anlatım.")).toBeInTheDocument();
    // Sonraki adımların GÖVDESİ henüz yok — yalnız başlıkları yol haritasında.
    expect(screen.queryByText("Bağlam katmanı.")).not.toBeInTheDocument();
    expect(screen.queryByText("Uzman katmanı.")).not.toBeInTheDocument();
    expect(screen.getByText("Adım 1/5")).toBeInTheDocument(); // 4 bölüm + test
  });

  it("yol haritasında ilerideki başlıklar GÖRÜNÜR ama kilitli", async () => {
    await openLesson();

    const roadmap = screen.getByRole("navigation", { name: "Ders adımları" });
    // Başlıklar okunuyor → kullanıcı ne öğreneceğini biliyor.
    expect(within(roadmap).getByText("Nasıl hesaplanır?")).toBeInTheDocument();
    expect(within(roadmap).getByText("İşin matematiği")).toBeInTheDocument();
    expect(within(roadmap).getByText("Mini test")).toBeInTheDocument();

    // …ama detaya atlayamıyor (kilitli).
    expect(within(roadmap).getByText("İşin matematiği").closest("button")).toBeDisabled();
    expect(within(roadmap).getByText("Mini test").closest("button")).toBeDisabled();
  });

  it("devam düğmesi bir sonraki adımın adını söyler ve ilerletir", async () => {
    await openLesson();

    fireEvent.click(screen.getByRole("button", { name: /Devam: Nasıl hesaplanır\?/ }));

    expect(await screen.findByText("Bağlam katmanı.")).toBeInTheDocument();
    expect(screen.getByText("Adım 2/5")).toBeInTheDocument();
    expect(screen.queryByText("Çekirdek anlatım.")).not.toBeInTheDocument();
  });

  it("tamamlanan adıma yol haritasından geri dönülebilir", async () => {
    await openLesson();
    fireEvent.click(screen.getByRole("button", { name: /Devam: Nasıl hesaplanır\?/ }));
    await screen.findByText("Bağlam katmanı.");

    const roadmap = screen.getByRole("navigation", { name: "Ders adımları" });
    fireEvent.click(within(roadmap).getByText("Kavram nedir?"));

    expect(await screen.findByText("Çekirdek anlatım.")).toBeInTheDocument();
  });

  it("test AYRI ve SON adımdır — okuma sayfasının devamı değil", async () => {
    await openLesson();

    // Test içeriği hiçbir okuma adımında görünmez.
    expect(screen.queryByText("Soru?")).not.toBeInTheDocument();

    for (const name of [/Devam: Nasıl hesaplanır\?/, /Devam: İşin matematiği/, /Devam: Örnek/]) {
      fireEvent.click(screen.getByRole("button", { name }));
    }

    // Son okuma adımından teste YÖNLENDİRME ile geçilir.
    const toQuiz = screen.getByRole("button", { name: /Mini teste geç/ });
    fireEvent.click(toQuiz);

    expect(await screen.findByText("Soru?")).toBeInTheDocument();
    expect(screen.getByText("Adım 5/5")).toBeInTheDocument();
  });

  it("seviyenin üstündeki adım 'ileri seviye' olarak işaretlenir", async () => {
    await openLesson("Beginner");
    fireEvent.click(screen.getByRole("button", { name: /Devam: Nasıl hesaplanır\?/ }));

    expect(await screen.findByText(/ileri seviye — istersen atla/)).toBeInTheDocument();
  });

  it("ileri seviyede hiçbir adım 'ileri seviye' işaretini taşımaz", async () => {
    await openLesson("Advanced");
    fireEvent.click(screen.getByRole("button", { name: /Devam: Nasıl hesaplanır\?/ }));

    await screen.findByText("Bağlam katmanı.");
    expect(screen.queryByText(/ileri seviye — istersen atla/)).not.toBeInTheDocument();
  });

  it("figür kendi adımında erişilebilir görsel olarak çizilir", async () => {
    await openLesson();
    for (const name of [/Devam: Nasıl hesaplanır\?/, /Devam: İşin matematiği/, /Devam: Örnek/]) {
      fireEvent.click(screen.getByRole("button", { name }));
    }

    expect(await screen.findByRole("img", { name: /nominal ve reel getirisi/i })).toBeInTheDocument();
  });

  it("bölümsüz ders tek parça okunur (geriye dönük uyum)", async () => {
    mockStepped("Beginner", {
      ...lessonDetail,
      sections: [],
      quiz: null,
      contextState: null,
      contextAsOf: null,
      nextLesson: null,
    });
    renderWithProviders(<EducationPage />);
    fireEvent.click(await screen.findByText("Enflasyon ve Reel Getiri"));

    expect(await screen.findByText("Gövde metni burada.")).toBeInTheDocument();
    // Adım gezgini yok — tek parça okuma.
    expect(screen.queryByRole("navigation", { name: "Ders adımları" })).not.toBeInTheDocument();
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
    mockStepped("Beginner", steppedDetail({
      status: "Completed",
      nextLesson: { id: "l2", slug: "cesit", title: "Çeşitlendirme", locked: false },
    }));
    const withNext = renderWithProviders(<EducationPage />);
    fireEvent.click(await screen.findByText("Enflasyon ve Reel Getiri"));

    expect(await screen.findByRole("button", { name: /Sonraki ders: Çeşitlendirme/ })).toBeEnabled();
    withNext.unmount();

    // Set sonu: sonraki ders yok → geçiş butonu yerine tamamlama rozeti.
    mockStepped("Beginner", steppedDetail({ status: "Completed", nextLesson: null }));
    renderWithProviders(<EducationPage />);
    fireEvent.click(await screen.findByText("Enflasyon ve Reel Getiri"));

    expect(await screen.findByText("🎉 Seti tamamladın")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Sonraki ders/ })).not.toBeInTheDocument();
  });
});

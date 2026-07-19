import { useMemo, useState } from "react";
import type {
  DepthTier,
  DiagnosticOption,
  LessonContextState,
  LessonLevel,
  LessonDetail,
  LessonListItem,
  LessonSection,
  SectionKind,
  LessonStatus,
  Quiz,
} from "@finans/shared";
import { EmptyState } from "../components/EmptyState";
import { LessonFigure } from "../components/LessonFigure";
import { MiniMarkdown } from "../components/MiniMarkdown";
import { Skeleton } from "../components/Skeleton";
import { useToast } from "../components/Toast";
import {
  useDiagnosticQuestions,
  useEducationTracks,
  useLiteracyProfile,
  useSubmitDiagnostic,
  useLesson,
  useSubmitQuizAttempt,
  useTrackLessons,
  useUpdateLessonProgress,
} from "../lib/hooks";

/** Ders satırı durum rozeti (kilit ön-koşuldan türetilir — backend). */
function statusMeta(status: LessonStatus, locked: boolean): { text: string; cls: string } {
  if (locked) return { text: "🔒 Kilitli", cls: "locked" };
  if (status === "Completed") return { text: "✓ Tamamlandı", cls: "done" };
  if (status === "InProgress") return { text: "● Devam ediyor", cls: "inprogress" };
  return { text: "Başla →", cls: "start" };
}

/**
 * Eğitim (Faz 6 — 04 §7.5): "Temeller" seti → ders listesi (ilerleme çubuğu + kilit) →
 * ders okuma (Markdown) + mini test. İçerik backend'den; kilit/durum kullanıcıya özel.
 * Sayfa-içi master-detail (local seçim) — ayrı rota gerekmez.
 */
export function EducationPage() {
  const tracks = useEducationTracks();
  const track = tracks.data?.[0]; // MVP: tek set ("Temeller")
  const trackSlug = track?.slug ?? "";
  const lessons = useTrackLessons(trackSlug);
  const [selectedSlug, setSelectedSlug] = useState<string | null>(null);

  // Onboarding (T6.6): profil ölçülmemişse önce tanılama. Kullanıcı "Atla" derse
  // veya bitirirse bu tur için kapanır; profil yazıldığı için tekrar sorulmaz.
  const profile = useLiteracyProfile();
  const [skippedDiagnostic, setSkippedDiagnostic] = useState(false);
  const showDiagnostic = !skippedDiagnostic && profile.data?.profiled === false;

  return (
    <section className="page edu-page">
      <header className="page-head">
        <div className="kicker">Kendi hızında</div>
        <h1>
          Eğitim <span aria-hidden="true">📚</span>
        </h1>
        <p className="page-lead">
          Temellerden ileri seviyeye — portföyünde gördüğün kavramlara bağlanarak, sade dille.
        </p>
      </header>

      {showDiagnostic ? (
        <DiagnosticOnboarding onDone={() => setSkippedDiagnostic(true)} />
      ) : selectedSlug ? (
        <LessonReader
          slug={selectedSlug}
          onBack={() => setSelectedSlug(null)}
          onNavigate={setSelectedSlug}
        />
      ) : (
        <LessonList
          loading={tracks.isLoading || lessons.isLoading}
          error={tracks.isError || lessons.isError}
          trackTitle={track?.title}
          lessons={lessons.data ?? []}
          onOpen={setSelectedSlug}
          onRetry={() => void lessons.refetch()}
        />
      )}
    </section>
  );
}

// ── Ders listesi + ilerleme çubuğu ──────────────────────────────────────────

function LessonList({
  loading,
  error,
  trackTitle,
  lessons,
  onOpen,
  onRetry,
}: {
  loading: boolean;
  error: boolean;
  trackTitle?: string;
  lessons: LessonListItem[];
  onOpen: (slug: string) => void;
  onRetry: () => void;
}) {
  const completed = lessons.filter((l) => l.status === "Completed").length;

  if (loading) {
    return (
      <div className="card">
        <Skeleton width="40%" height={18} />
        <div style={{ marginTop: 14, display: "grid", gap: 12 }}>
          {[0, 1, 2, 3, 4].map((i) => (
            <Skeleton key={i} height={64} />
          ))}
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="card empty-state" role="alert">
        <h3>Dersler yüklenemedi</h3>
        <p>Bağlantını kontrol edip tekrar dene.</p>
        <button type="button" className="btn-primary" onClick={onRetry}>
          Tekrar dene
        </button>
      </div>
    );
  }

  if (lessons.length === 0) {
    return (
      <EmptyState
        icon="🎓"
        title="Ders içeriği hazırlanıyor"
        description="Yakında burada kısa, portföyüne bağlı dersler göreceksin."
      />
    );
  }

  return (
    <div className="card">
      <div className="card-head">
        <h3>{trackTitle ?? "Temeller"}</h3>
        <span className="mini">
          {completed}/{lessons.length} ders tamamlandı
        </span>
      </div>

      {/* İlerleme çubuğu: her ders bir segment; tamamlananlar dolu. */}
      <div className="edu-track" role="img" aria-label={`${completed}/${lessons.length} ders tamamlandı`}>
        {lessons.map((l) => (
          <span key={l.id} className={`edu-seg${l.status === "Completed" ? " done" : ""}`} />
        ))}
      </div>

      <ul className="lesson-list">
        {lessons.map((l) => {
          const meta = statusMeta(l.status, l.locked);
          return (
            <li key={l.id}>
              <button
                type="button"
                className={`lesson${l.locked ? " is-locked" : ""}`}
                onClick={() => !l.locked && onOpen(l.slug)}
                disabled={l.locked}
                aria-disabled={l.locked}
              >
                <span className="lesson-num">{String(l.order).padStart(2, "0")}</span>
                <span className="lesson-info">
                  <span className="lesson-ti">{l.title}</span>
                  <span className="lesson-de">{l.summary}</span>
                  <span className="lesson-mt">
                    <span>⏱ {l.estimatedMinutes} dk</span>
                    <span className={`lesson-status ${meta.cls}`}>{meta.text}</span>
                  </span>
                </span>
                {!l.locked && <span className="lesson-chev" aria-hidden="true">→</span>}
              </button>
            </li>
          );
        })}
      </ul>
    </div>
  );
}

// ── Ders okuma + tamamla + quiz ─────────────────────────────────────────────

/**
 * Tanılama testi (T6.6, 15 §4) — eğitime başlamadan önce 8 soru.
 * 4 bilgi sorusu içerik derinliğini, 4 senaryo sorusu ders sırasını belirler.
 *
 * ⚠ Risk tutumu kullanıcıya **gösterilmez** (15 §1.1): sonuç ekranında yalnız
 * seviye ve yönlendirme mesajı var; "Temkinli/Dengeli/Atılgan" etiketi hiç geçmez.
 * ⚠ "Utandırmayan" ilke (14 §4-A2): doğru/yanlış sayısı gösterilmez.
 */
function DiagnosticOnboarding({ onDone }: { onDone: () => void }) {
  const questions = useDiagnosticQuestions(true);
  const submit = useSubmitDiagnostic();
  const [answers, setAnswers] = useState<Record<string, string>>({});

  const items = questions.data ?? [];
  const answered = items.filter((q) => answers[q.key]).length;

  if (questions.isLoading)
    return (
      <div className="card" style={{ marginTop: 14 }}>
        <Skeleton width="45%" height={22} />
        <div style={{ marginTop: 14, display: "grid", gap: 10 }}>
          <Skeleton height={14} />
          <Skeleton height={14} />
        </div>
      </div>
    );

  // Sorular gelmezse eğitim engellenmez — doğrudan derslere düş (NFR-5).
  if (questions.isError || items.length === 0) {
    onDone();
    return null;
  }

  if (submit.data)
    return (
      <div className="card diagnostic-result" style={{ marginTop: 14 }}>
        <h3>Hazırız</h3>
        <p className="page-lead">{submit.data.message}</p>
        <button type="button" className="btn-primary" onClick={onDone}>
          Derslere başla →
        </button>
      </div>
    );

  return (
    <div className="card diagnostic" style={{ marginTop: 14 }}>
      <div className="page-head" style={{ marginBottom: 4 }}>
        <span className="kicker">Başlamadan önce</span>
        <h2>Haritada neredesin?</h2>
      </div>
      <p className="page-lead">
        8 kısa soru — doğru cevap aranmıyor. Amacı dersleri sana uygun derinlikte
        göstermek. İstersen atlayabilirsin.
      </p>

      <p className="diagnostic-progress mini">
        {answered}/{items.length} soru yanıtlandı
      </p>

      <ol className="diagnostic-list">
        {items.map((q) => (
          <li key={q.key} className="diagnostic-q">
            <p className="diagnostic-prompt">{q.prompt}</p>
            <div className="diagnostic-options">
              {q.options.map((o: DiagnosticOption) => (
                <label key={o.key} className={answers[q.key] === o.key ? "picked" : undefined}>
                  <input
                    type="radio"
                    name={q.key}
                    value={o.key}
                    checked={answers[q.key] === o.key}
                    onChange={() => setAnswers((prev) => ({ ...prev, [q.key]: o.key }))}
                  />
                  <span>{o.text}</span>
                </label>
              ))}
            </div>
          </li>
        ))}
      </ol>

      <div className="lesson-actions">
        <button
          type="button"
          className="btn-primary"
          disabled={submit.isPending}
          onClick={() =>
            submit.mutate({
              answers: Object.entries(answers).map(([questionKey, optionKey]) => ({
                questionKey,
                optionKey,
              })),
            })
          }
        >
          {submit.isPending ? "Değerlendiriliyor…" : "Bitir"}
        </button>
        <button
          type="button"
          className="btn-ghost"
          disabled={submit.isPending}
          onClick={() => submit.mutate({ answers: [] })}
        >
          Atla
        </button>
      </div>
    </div>
  );
}

/** "Senin portföyünde" bloğunun veri kaynağı rozeti (15 §3.2). Own'da rozet YOK. */
function ContextBadge({ state, asOf }: { state: LessonContextState; asOf: string | null }) {
  if (state === "Own") return null;

  if (state === "Demo") {
    return (
      <p className="context-badge demo">
        <strong>Örnek portföy.</strong> Henüz kendi varlığın olmadığı için bu bölümdeki
        sayılar örnek bir portföye ait — kendi rakamların değil.
      </p>
    );
  }

  const stamp = asOf ? new Date(asOf).toLocaleDateString("tr-TR") : null;
  return (
    <p className="context-badge stale">
      Bu bölümdeki sayılar {stamp ? `${stamp} tarihine` : "eski bir tarihe"} ait —
      fiyatlar o günden beri güncellenmedi.
    </p>
  );
}

/**
 * Ders gövdesi. Katmanlı bölüm varsa onlar render edilir; yoksa `bodyMarkdown`'a
 * düşülür (geriye dönük uyum, 15 §2.1 / SC-E2).
 *
 * NOT: Şu an TÜM derinlik katmanları açık gösterilir. Seviyeye göre katlama +
 * "Daha derine in" T6.7'nin işi (tanılama testi T6.6'ya bağlı).
 */
/** Derinlik katmanının sıra değeri — kullanıcının seviyesiyle karşılaştırmak için. */
const TIER_RANK: Record<DepthTier, number> = { Core: 0, Context: 1, Deep: 2 };

/** Kullanıcının seviyesi kaçıncı katmana kadar ANA YOLDA sayılır (15 §2.2). */
const LEVEL_RANK: Record<LessonLevel, number> = { Beginner: 0, Intermediate: 1, Advanced: 2 };

/** Derinlik rozeti — yol haritasında "beni neler bekliyor" sinyali. */
const TIER_BADGE: Record<DepthTier, string | null> = {
  Core: null,
  Context: "derinleşme",
  Deep: "ileri",
};

/** Blok türü rozeti — adımın ne tür bir içerik olduğunu önceden belli eder. */
const KIND_BADGE: Record<SectionKind, string | null> = {
  Explain: null,
  Example: "örnek",
  Trap: "tuzak",
  LiveContext: "senin verin",
  Source: "kaynak",
};

/** Yol haritasındaki bir adım (bölümler + varsa kapanış testi). */
type Step = {
  key: string;
  title: string;
  badge: string | null;
  optional: boolean;
  section: LessonSection | null; // null = test adımı
};

/**
 * Ders adımlarını kurar (T6.10): her bölüm bir adım, test EN SONA ayrı adım olarak
 * eklenir — böylece test "sayfanın devamı" değil, **ulaşılan bir hedef** olur.
 */
function buildSteps(lesson: LessonDetail, level: LessonLevel | null): Step[] {
  const reach = LEVEL_RANK[level ?? "Beginner"];

  const steps: Step[] = lesson.sections.map((s) => ({
    key: `s${s.order}`,
    title: s.heading ?? `Bölüm ${s.order}`,
    badge: KIND_BADGE[s.kind] ?? TIER_BADGE[s.depthTier],
    // Seviyenin üstündeki katman ZORUNLU değil — atlanabilir ama gizlenmez (15 §2.2).
    optional: TIER_RANK[s.depthTier] > reach,
    section: s,
  }));

  if (lesson.quiz) {
    steps.push({
      key: "quiz",
      title: "Mini test",
      badge: "kapanış",
      optional: false,
      section: null,
    });
  }

  return steps;
}

/**
 * Yol haritası — dersin tüm adımlarını başlıklarıyla gösterir.
 *
 * Tamamlananlar tıklanabilir (geri dönüş), sıradaki vurgulu, **ileridekiler
 * başlığıyla görünür ama kilitli**: kullanıcı ne öğreneceğini bilir, ama detay
 * için ilerlemesi gerekir (merak + ilerleme hissi).
 */
function LessonRoadmap({
  steps,
  current,
  onJump,
}: {
  steps: Step[];
  current: number;
  onJump: (index: number) => void;
}) {
  return (
    <nav className="lesson-roadmap" aria-label="Ders adımları">
      <ol>
        {steps.map((step, i) => {
          const state = i < current ? "done" : i === current ? "current" : "upcoming";
          const reachable = i <= current;
          return (
            <li key={step.key} className={`roadmap-step is-${state}`}>
              <button
                type="button"
                onClick={() => reachable && onJump(i)}
                disabled={!reachable}
                aria-current={state === "current" ? "step" : undefined}
              >
                <span className="roadmap-mark" aria-hidden="true">
                  {state === "done" ? "✓" : state === "current" ? "●" : "🔒"}
                </span>
                <span className="roadmap-title">{step.title}</span>
                {step.badge && <span className="roadmap-badge">{step.badge}</span>}
              </button>
            </li>
          );
        })}
      </ol>
    </nav>
  );
}

/** Tek adımın içeriği. */
function StepContent({
  step,
  lesson,
}: {
  step: Step;
  lesson: LessonDetail;
}) {
  const s = step.section!;
  return (
    <section className={`lesson-section kind-${s.kind.toLowerCase()} tier-${s.depthTier.toLowerCase()}`}>
      {s.kind === "LiveContext" && lesson.contextState && (
        <ContextBadge state={lesson.contextState} asOf={lesson.contextAsOf} />
      )}
      <MiniMarkdown className="markdown-body" markdown={s.bodyMarkdown} />
      <LessonFigure figureKey={s.figureKey} />
    </section>
  );
}

function LessonReader({
  slug,
  onBack,
  onNavigate,
}: {
  slug: string;
  onBack: () => void;
  onNavigate: (slug: string) => void;
}) {
  const lesson = useLesson(slug);
  const complete = useUpdateLessonProgress(lesson.data?.id ?? "");
  const profile = useLiteracyProfile();
  const { notify } = useToast();
  const [stepIndex, setStepIndex] = useState(0);

  // Ders değişince baştan başla (sonraki derse geçişte adım sayacı sıfırlanmalı).
  const [loadedSlug, setLoadedSlug] = useState(slug);
  if (loadedSlug !== slug) {
    setLoadedSlug(slug);
    setStepIndex(0);
  }

  const steps = useMemo(
    () => (lesson.data ? buildSteps(lesson.data, profile.data?.literacyLevel ?? null) : []),
    [lesson.data, profile.data?.literacyLevel],
  );

  const onComplete = () => {
    complete.mutate(
      { status: "Completed", progressPercent: 100 },
      {
        onSuccess: () => {
          const next = lesson.data?.nextLesson;
          notify(
            next
              ? `Ders tamamlandı — "${next.title}" açıldı.`
              : "Ders tamamlandı. Seti bitirdin 🎉",
            "success",
          );
        },
      },
    );
  };

  const step = steps[stepIndex];
  const isLast = stepIndex === steps.length - 1;
  const nextStep = steps[stepIndex + 1];

  return (
    <div className="lesson-reader">
      <button type="button" className="btn-ghost" onClick={onBack}>
        ← Derslere dön
      </button>

      {lesson.isLoading && (
        <div className="card" style={{ marginTop: 14 }}>
          <Skeleton width="55%" height={24} />
          <div style={{ marginTop: 14, display: "grid", gap: 10 }}>
            <Skeleton height={14} />
            <Skeleton height={14} />
            <Skeleton width="80%" height={14} />
          </div>
        </div>
      )}

      {lesson.isError && (
        <div className="card empty-state" role="alert" style={{ marginTop: 14 }}>
          <h3>Ders yüklenemedi</h3>
          <button type="button" className="btn-primary" onClick={() => void lesson.refetch()}>
            Tekrar dene
          </button>
        </div>
      )}

      {lesson.data && (
        <article className="card lesson-body" style={{ marginTop: 14 }}>
          <div className="lesson-head">
            <h2>{lesson.data.title}</h2>
            <span className="mini">⏱ {lesson.data.estimatedMinutes} dk</span>
          </div>
          {lesson.data.conceptTags.length > 0 && (
            <div className="concept-tags">
              {lesson.data.conceptTags.map((t) => (
                <span key={t.key} className="concept-tag">
                  {t.label}
                </span>
              ))}
            </div>
          )}

          {/* Bölümsüz ders (eski içerik / topluluk katkısı) → tek parça oku (SC-E2). */}
          {steps.length === 0 ? (
            <>
              <MiniMarkdown className="markdown-body" markdown={lesson.data.bodyMarkdown} />
              <div className="lesson-actions">
                {lesson.data.status === "Completed" ? (
                  <span className="lesson-done-badge">✓ Bu dersi tamamladın</span>
                ) : (
                  <button
                    type="button"
                    className="btn-primary"
                    onClick={onComplete}
                    disabled={complete.isPending}
                  >
                    {complete.isPending ? "Kaydediliyor…" : "Dersi tamamla"}
                  </button>
                )}
              </div>
            </>
          ) : (
            <>
              <div className="lesson-progress">
                <div className="lesson-progress-bar" role="img" aria-label={`${stepIndex + 1}/${steps.length} adım`}>
                  {steps.map((st, i) => (
                    <span key={st.key} className={`lesson-progress-seg${i <= stepIndex ? " done" : ""}`} />
                  ))}
                </div>
                <span className="mini">
                  Adım {stepIndex + 1}/{steps.length}
                </span>
              </div>

              {/* Geniş ekranda iki sütun: solda yol haritası (yapışkan), sağda adım.
                  Böylece metin kendi okuma genişliğini korurken sayfa boş kalmaz. */}
              <div className="lesson-layout">
                <LessonRoadmap steps={steps} current={stepIndex} onJump={setStepIndex} />

                <div className="lesson-step">
                  <div className="lesson-step-head">
                    <h3>{step.title}</h3>
                    {step.optional && (
                      <span className="step-optional">ileri seviye — istersen atla</span>
                    )}
                  </div>

                  {step.section ? (
                    <StepContent step={step} lesson={lesson.data} />
                  ) : (
                    <QuizPanel quiz={lesson.data.quiz!} />
                  )}
                </div>
              </div>

              <div className="lesson-actions">
                {stepIndex > 0 && (
                  <button type="button" className="btn-ghost" onClick={() => setStepIndex(stepIndex - 1)}>
                    ← Geri
                  </button>
                )}

                {/* Son adımdan ÖNCE: sonraki adıma yönlendir. Test adımı da böyle
                    ulaşılır — "sayfanın devamı" değil, varılan bir hedef. */}
                {!isLast && (
                  <button
                    type="button"
                    className="btn-primary next-lesson"
                    onClick={() => setStepIndex(stepIndex + 1)}
                  >
                    {nextStep?.key === "quiz" ? "Mini teste geç →" : `Devam: ${nextStep?.title} →`}
                  </button>
                )}

                {/* Testi olmayan derste son adımda tamamlama düğmesi. */}
                {isLast && !lesson.data.quiz && lesson.data.status !== "Completed" && (
                  <button
                    type="button"
                    className="btn-primary next-lesson"
                    onClick={onComplete}
                    disabled={complete.isPending}
                  >
                    {complete.isPending ? "Kaydediliyor…" : "Dersi tamamla"}
                  </button>
                )}

                {lesson.data.status === "Completed" && lesson.data.nextLesson && (
                  <button
                    type="button"
                    className="btn-primary next-lesson"
                    onClick={() => onNavigate(lesson.data!.nextLesson!.slug)}
                    disabled={lesson.data.nextLesson.locked}
                  >
                    Sonraki ders: {lesson.data.nextLesson.title} →
                  </button>
                )}

                {lesson.data.status === "Completed" && !lesson.data.nextLesson && (
                  <span className="lesson-done-badge">🎉 Seti tamamladın</span>
                )}
              </div>
            </>
          )}
        </article>
      )}
    </div>
  );
}

// ── Mini test ────────────────────────────────────────────────────────────────

function QuizPanel({ quiz }: { quiz: Quiz }) {
  const attempt = useSubmitQuizAttempt(quiz.id);
  const [answers, setAnswers] = useState<Record<string, string[]>>({});

  const resultByQuestion = useMemo(() => {
    const map = new Map(attempt.data?.results.map((r) => [r.questionId, r]) ?? []);
    return map;
  }, [attempt.data]);
  const submitted = attempt.data != null;

  const select = (questionId: string, optionId: string, multi: boolean) => {
    if (submitted) return;
    setAnswers((prev) => {
      const current = prev[questionId] ?? [];
      if (multi) {
        const next = current.includes(optionId)
          ? current.filter((id) => id !== optionId)
          : [...current, optionId];
        return { ...prev, [questionId]: next };
      }
      return { ...prev, [questionId]: [optionId] };
    });
  };

  const onSubmit = () => {
    attempt.mutate({
      answers: Object.entries(answers).map(([questionId, selectedOptionIds]) => ({
        questionId,
        selectedOptionIds,
      })),
    });
  };

  const reset = () => {
    setAnswers({});
    attempt.reset();
  };

  return (
    <div className="quiz-panel">
      <div className="card-head">
        <h3>{quiz.title}</h3>
        {submitted && (
          <span className={`quiz-score ${attempt.data!.passed ? "done" : "fail"}`}>
            {attempt.data!.score} puan · {attempt.data!.passed ? "Geçti ✓" : "Tekrar dene"}
          </span>
        )}
      </div>

      <ol className="quiz-questions">
        {quiz.questions.map((q) => {
          const multi = q.type === "MultipleChoice";
          const result = resultByQuestion.get(q.id);
          const selected = answers[q.id] ?? [];
          return (
            <li key={q.id} className="quiz-q">
              <p className="quiz-prompt">{q.prompt}</p>
              <div className="quiz-options">
                {q.options.map((o) => {
                  const isSelected = selected.includes(o.id);
                  const isCorrect = result?.correctOptionIds.includes(o.id) ?? false;
                  const cls = [
                    "quiz-option",
                    isSelected ? "selected" : "",
                    submitted && isCorrect ? "correct" : "",
                    submitted && isSelected && !isCorrect ? "wrong" : "",
                  ]
                    .filter(Boolean)
                    .join(" ");
                  return (
                    <button
                      key={o.id}
                      type="button"
                      className={cls}
                      onClick={() => select(q.id, o.id, multi)}
                      disabled={submitted}
                      aria-pressed={isSelected}
                    >
                      <span className="quiz-mark" aria-hidden="true">
                        {submitted && isCorrect ? "✓" : submitted && isSelected ? "✕" : multi ? "☐" : "○"}
                      </span>
                      {o.text}
                    </button>
                  );
                })}
              </div>
              {submitted && result && (
                <p className={`quiz-explanation ${result.correct ? "correct" : "wrong"}`}>
                  {result.correct ? "Doğru. " : "Tekrar bak. "}
                  {result.explanation}
                </p>
              )}
            </li>
          );
        })}
      </ol>

      <div className="quiz-actions">
        {submitted ? (
          <button type="button" className="btn-ghost" onClick={reset}>
            ↻ Yeniden çöz
          </button>
        ) : (
          <button
            type="button"
            className="btn-primary"
            onClick={onSubmit}
            disabled={attempt.isPending || Object.keys(answers).length < quiz.questions.length}
          >
            {attempt.isPending ? "Değerlendiriliyor…" : "Testi gönder"}
          </button>
        )}
        {attempt.isError && (
          <p className="neg" role="alert">
            Test gönderilemedi. Tekrar dene.
          </p>
        )}
      </div>
    </div>
  );
}

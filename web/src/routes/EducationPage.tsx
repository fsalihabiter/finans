import { useMemo, useState } from "react";
import type {
  LessonContextState,
  LessonDetail,
  LessonListItem,
  LessonSection,
  LessonStatus,
  Quiz,
} from "@finans/shared";
import { EmptyState } from "../components/EmptyState";
import { MiniMarkdown } from "../components/MiniMarkdown";
import { Skeleton } from "../components/Skeleton";
import { useToast } from "../components/Toast";
import {
  useEducationTracks,
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

      {selectedSlug ? (
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
function LessonBody({ lesson }: { lesson: LessonDetail }) {
  if (lesson.sections.length === 0)
    return <MiniMarkdown className="markdown-body" markdown={lesson.bodyMarkdown} />;

  return (
    <>
      {lesson.sections.map((s: LessonSection) => (
        <section key={s.order} className={`lesson-section kind-${s.kind.toLowerCase()}`}>
          {s.kind === "LiveContext" && lesson.contextState && (
            <ContextBadge state={lesson.contextState} asOf={lesson.contextAsOf} />
          )}
          <MiniMarkdown className="markdown-body" markdown={s.bodyMarkdown} />
        </section>
      ))}
    </>
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
  const { notify } = useToast();

  const onComplete = () => {
    complete.mutate(
      { status: "Completed", progressPercent: 100 },
      {
        onSuccess: () => {
          const next = lesson.data?.nextLesson;
          // İlerleme akışı (T6.2): tamamlanan ders sonraki dersin ön-koşuluydu →
          // kilit açıldı. Kullanıcıya bunu SÖYLE, listeye dönüp aramasını bekleme.
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

          <LessonBody lesson={lesson.data} />

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

            {/* İlerleme akışı (T6.2): ders tamamlandıysa sonrakine doğrudan geç.
                Kilit ön-koşuldan türetildiği için tamamlama anında açılmış olur. */}
            {lesson.data.status === "Completed" && lesson.data.nextLesson && (
              <button
                type="button"
                className="btn-primary next-lesson"
                onClick={() => onNavigate(lesson.data!.nextLesson!.slug)}
                disabled={lesson.data.nextLesson.locked}
                title={
                  lesson.data.nextLesson.locked
                    ? "Bu ders başka bir ön koşul bekliyor"
                    : undefined
                }
              >
                Sonraki ders: {lesson.data.nextLesson.title} →
              </button>
            )}

            {lesson.data.status === "Completed" && !lesson.data.nextLesson && (
              <span className="lesson-done-badge">🎉 Seti tamamladın</span>
            )}
          </div>

          {lesson.data.quiz && <QuizPanel quiz={lesson.data.quiz} />}
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

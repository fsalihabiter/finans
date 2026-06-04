import { CommentaryCardList } from "../components/CommentaryCardList";
import { Disclaimer } from "../components/Disclaimer";
import { useCommentary } from "../lib/hooks";

/**
 * Analiz — eğitici LLM yorum kartları (T3.8 — 07). Disclaimer (NFR-2 / CLAUDE.md §2) sayfa
 * üstünde **her zaman** görünür: yorum gelmeden önce, gelirken (loading), geldiğinde, fallback'te.
 *
 * <p>LLM kapalıyken (API anahtarı yok) backend `source="fallback"` ile tek bilgilendirme kartı döner —
 * uygulama çökmez (NFR-5). Kullanıcı "Yenile" ile elle tetikler (pahalı çağrı, otomatik refetch yok).</p>
 */
export function AnalysisPage() {
  const { data, isLoading, isFetching, isError, refetch } = useCommentary();

  return (
    <section className="page analysis-page">
      <header className="page-head">
        <p className="kicker">Yapay zekâ destekli</p>
        <h1>Portföyün ne anlatıyor?</h1>
        <p className="page-lead">
          Sayılar koddan, yorum sade dille. Aşağıdaki kartlar mevcut durumun üzerine farkındalık
          sunar — alım-satım yönlendirmesi içermez.
        </p>
        <Disclaimer />
      </header>

      <div className="analysis-toolbar">
        <span className="mini">
          {data?.source === "llm" && "LLM tarafından üretildi"}
          {data?.source === "fallback" && "Yorum şu an üretilemedi — sayıların etkilenmedi"}
          {data?.source === "cache" && "Önbellekten gösteriliyor"}
        </span>
        <button
          type="button"
          className="btn btn-secondary"
          onClick={() => void refetch()}
          disabled={isFetching}
          aria-busy={isFetching}
        >
          {isFetching ? "Yenileniyor…" : "↻ Yenile"}
        </button>
      </div>

      {isLoading ? (
        <div className="commentary-skeleton" aria-label="Yorum kartları yükleniyor">
          {[0, 1, 2].map((i) => (
            <div key={i} className="commentary-card is-skeleton">
              <div className="sk-line sk-line-title" />
              <div className="sk-line" />
              <div className="sk-line sk-line-short" />
            </div>
          ))}
        </div>
      ) : isError ? (
        <div className="card empty-state" role="alert">
          <h3>Yorum yüklenemedi</h3>
          <p>Bağlantı ya da sunucu kaynaklı geçici bir sorun olabilir. Tekrar dene.</p>
          <button type="button" className="btn" onClick={() => void refetch()}>Tekrar dene</button>
        </div>
      ) : (
        <CommentaryCardList cards={data?.cards ?? []} source={data?.source ?? "fallback"} />
      )}
    </section>
  );
}

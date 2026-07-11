import { useState } from "react";
import { ApiError, formatCurrency, formatNumber, formatPercent } from "@finans/shared";
import type { CurrencyCode, StockMetrics } from "@finans/shared";
import { CommentaryCardList } from "../components/CommentaryCardList";
import { Disclaimer } from "../components/Disclaimer";
import { InfoTip } from "../components/InfoTip";
import { useStockExplain, useStockMetrics } from "../lib/hooks";

/** Hızlı deneme çipleri — tanıdık ABD sembolleri (BIST ileri fazda, T4.1 kararı). */
const POPULAR = ["AAPL", "MSFT", "NVDA", "GOOGL", "AMZN", "TSLA"] as const;

/** Kaba bant etiketi → Türkçe gösterim (etiketler tavsiye değil; bandın adı). */
const BAND_TR: Record<string, string> = {
  low: "düşük bant",
  moderate: "orta bant",
  above: "yüksek bant",
  high: "yüksek bant",
  none: "temettü yok",
  negative: "negatif",
  flat: "yatay",
  positive: "pozitif",
};

/** Bant → renk tonu sınıfı (bilgilendirici; iyi/kötü hükmü İÇERMEZ). */
const BAND_CLS: Record<string, string> = {
  low: "band-cool",
  moderate: "band-mid",
  above: "band-warm",
  high: "band-warm",
  none: "band-mute",
  negative: "band-warm",
  flat: "band-mid",
  positive: "band-cool",
};

function MetricCard({
  label,
  value,
  band,
  tip,
}: {
  label: string;
  value: string | null;
  band: string | null | undefined;
  tip: string;
}) {
  return (
    <div className="metric-card">
      <div className="metric-k">
        {label}
        <InfoTip label={label}>{tip}</InfoTip>
      </div>
      <div className="metric-v tnum">{value ?? "—"}</div>
      {value === null ? (
        <span className="metric-band band-mute">veri yok</span>
      ) : band ? (
        <span className={`metric-band ${BAND_CLS[band] ?? "band-mute"}`}>{BAND_TR[band] ?? band}</span>
      ) : null}
    </div>
  );
}

function MetricGrid({ stock }: { stock: StockMetrics }) {
  const m = stock.metrics;
  const c = stock.sectorContext;
  return (
    <div className="metric-grid">
      <MetricCard
        label="F/K"
        value={m.peRatio == null ? null : formatNumber(m.peRatio, 2)}
        band={c.peRatio}
        tip="Fiyat/Kazanç: hisse fiyatının, hisse başına yıllık kâra oranı. Ödenen fiyatın kaç yıllık kâra denk geldiğini gösterir."
      />
      <MetricCard
        label="PD/DD"
        value={m.pbRatio == null ? null : formatNumber(m.pbRatio, 2)}
        band={c.pbRatio}
        tip="Piyasa Değeri/Defter Değeri: fiyatın, şirketin bilançodaki net varlık değerine oranı."
      />
      <MetricCard
        label="Temettü Verimi"
        value={m.dividendYield == null ? null : formatPercent(m.dividendYield, 2, true, false)}
        band={c.dividendYield}
        tip="Hisse başına dağıtılan yıllık temettünün fiyata oranı — hisseyi tutarak elde edilen nakit getiri."
      />
      <MetricCard
        label="Kâr Büyümesi"
        value={m.earningsGrowth == null ? null : formatPercent(m.earningsGrowth)}
        band={c.earningsGrowth}
        tip="Hisse başına kârın yıllık değişimi — şirketin kazancını ne hızla büyüttüğü."
      />
    </div>
  );
}

/**
 * Hisse Analizi (T4.4 — 04 §7, 13 §4): sembol ara → metrik ızgarası (KODUN çektiği sayılar +
 * kaba bant etiketleri) → LLM'in eğitici açıklama kartları. **Al/sat/öneri YOK** (CLAUDE.md §2);
 * disclaimer her durumda görünür. Açıklama, metrikler başarıyla gelince tetiklenir (LLM pahalı).
 */
export function StocksPage() {
  const [input, setInput] = useState("");
  const [symbol, setSymbol] = useState("");

  const metrics = useStockMetrics(symbol);
  const explain = useStockExplain(symbol, metrics.isSuccess);

  const search = (s: string) => {
    const normalized = s.trim().toUpperCase();
    if (normalized) {
      setInput(normalized);
      setSymbol(normalized);
    }
  };

  const metricsError =
    metrics.error instanceof ApiError
      ? metrics.error.message
      : "Bağlantı ya da sunucu kaynaklı geçici bir sorun olabilir. Tekrar dene.";

  return (
    <section className="page stocks-page">
      <header className="page-head">
        <p className="kicker">Temel analiz</p>
        <h1>Rakamların ne anlama geldiğini öğren</h1>
        <p className="page-lead">
          Bir sembol yaz; F/K, PD/DD, temettü verimi ve kâr büyümesini çekip sade dille
          açıklayalım — "al/sat" ya da "yükselir" demeden, hissenin karakterini anlatarak.
        </p>
        <Disclaimer />
      </header>

      <form
        className="stock-search"
        onSubmit={(e) => {
          e.preventDefault();
          search(input);
        }}
        role="search"
        aria-label="Hisse sembolü ara"
      >
        <input
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder="Sembol yaz — örn. AAPL"
          aria-label="Hisse sembolü"
          autoComplete="off"
          spellCheck={false}
        />
        <button type="submit" className="btn-primary" disabled={!input.trim()}>
          İncele
        </button>
      </form>
      <div className="stock-chips" role="group" aria-label="Popüler semboller">
        {POPULAR.map((s) => (
          <button
            key={s}
            type="button"
            className={`chip${symbol === s ? " sel" : ""}`}
            onClick={() => search(s)}
          >
            {s}
          </button>
        ))}
      </div>

      {symbol === "" && (
        <div className="card empty-state">
          <div className="empty-ic" aria-hidden="true">📊</div>
          <h3 className="empty-title">Bir sembolle başla</h3>
          <p className="empty-desc">
            Yukarıya bir ABD hisse sembolü yaz ya da hazır çiplerden birini seç. BIST hisseleri
            güvenilir veri kaynağı netleşince eklenecek.
          </p>
        </div>
      )}

      {metrics.isLoading && symbol !== "" && (
        <div className="card">
          <div className="sk-line sk-line-title" />
          <div className="sk-line" />
          <div className="sk-line sk-line-short" />
        </div>
      )}

      {metrics.isError && (
        <div className="card empty-state" role="alert">
          <h3>{symbol} için veri alınamadı</h3>
          <p>{metricsError}</p>
          <button type="button" className="btn-primary" onClick={() => void metrics.refetch()}>
            Tekrar dene
          </button>
        </div>
      )}

      {metrics.data && (
        <>
          <div className="card stock-head-card">
            <div className="stock-title">
              <div>
                <h2>
                  {metrics.data.name} <span className="muted">{metrics.data.symbol}</span>
                </h2>
                <div className="stock-sub">
                  {metrics.data.exchange ?? "—"} · kaynak: {metrics.data.source}
                </div>
              </div>
              <div className="stock-price tnum">
                {metrics.data.price == null
                  ? "—"
                  : formatCurrency(metrics.data.price, metrics.data.currency as CurrencyCode)}
                {metrics.data.changeRatio != null && (
                  <span
                    className={`stock-change ${metrics.data.changeRatio >= 0 ? "up" : "down"}`}
                  >
                    {formatPercent(metrics.data.changeRatio)}
                  </span>
                )}
              </div>
            </div>
            <MetricGrid stock={metrics.data} />
          </div>

          <div className="card">
            <div className="card-head">
              <h3>Bu rakamlar ne anlatıyor?</h3>
              <span className="mini">
                {explain.isFetching && "Açıklama hazırlanıyor…"}
                {explain.data?.source === "llm" && "LLM tarafından üretildi"}
                {explain.data?.source === "cache" && "Önbellekten gösteriliyor"}
                {explain.data?.source === "fallback" &&
                  "Açıklama şu an üretilemedi — sayıların etkilenmedi"}
              </span>
            </div>
            {explain.isLoading ? (
              <div className="commentary-skeleton" aria-label="Açıklama kartları yükleniyor">
                {[0, 1, 2].map((i) => (
                  <div key={i} className="commentary-card is-skeleton">
                    <div className="sk-line sk-line-title" />
                    <div className="sk-line" />
                    <div className="sk-line sk-line-short" />
                  </div>
                ))}
              </div>
            ) : explain.isError ? (
              <div className="empty-state" role="alert">
                <p>Açıklama yüklenemedi. Metrikler yukarıda doğru ve güncel.</p>
                <button type="button" className="btn-ghost" onClick={() => void explain.refetch()}>
                  Tekrar dene
                </button>
              </div>
            ) : (
              <CommentaryCardList
                cards={explain.data?.cards ?? []}
                source={explain.data?.source ?? "fallback"}
              />
            )}
          </div>
        </>
      )}
    </section>
  );
}

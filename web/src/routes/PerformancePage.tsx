import { useState } from "react";
import { formatPercent } from "@finans/shared";
import type { Holding } from "@finans/shared";
import { ASSET_META } from "../lib/assetMeta";
import { KpiGrid } from "../components/KpiGrid";
import { PortfolioSkeleton } from "../components/Skeleton";
import { EmptyState } from "../components/EmptyState";
import { useHoldings, usePortfolioSummary } from "../lib/hooks";
import { useAppShell } from "../lib/appShell";

const PERIODS = ["1A", "3A", "1Y", "Tümü"] as const;

/** Kalem bazında getiri çubuğu (tüm zamanlar) — gerçek veriden, en abartılıdan sırala. */
function ReturnBars({ holdings }: { holdings: Holding[] }) {
  const withReturn = holdings
    .filter((h) => h.returnRatio !== null)
    .sort((a, b) => (b.returnRatio ?? 0) - (a.returnRatio ?? 0));
  if (withReturn.length === 0) return <p className="muted">Henüz getiri hesaplanabilen pozisyon yok.</p>;

  const maxAbs = Math.max(...withReturn.map((h) => Math.abs(h.returnRatio ?? 0)), 0.0001);

  return (
    <div className="perf-bars">
      {withReturn.map((h) => {
        const r = h.returnRatio ?? 0;
        const meta = ASSET_META[h.assetType];
        const pct = (Math.abs(r) / maxAbs) * 50; // merkezden ±%50 genişlik
        const positive = r >= 0;
        return (
          <div className="perf-bar" key={h.id}>
            <div className="pb-top">
              <span className="pb-name">{meta.icon} {h.name}</span>
              <span className={`tnum ${positive ? "up" : "down"}`}>{formatPercent(r)}</span>
            </div>
            <div className="perf-track">
              <i
                className="pb-fill"
                style={{
                  left: positive ? "50%" : `${50 - pct}%`,
                  width: `${pct}%`,
                  background: positive ? "var(--mint, #5fc9a0)" : "var(--coral, #e58e6e)",
                  // Merkez çizgisinden dışa doğru genişler (App.css @keyframes bar-grow).
                  transformOrigin: positive ? "left center" : "right center",
                }}
              />
              <i style={{ left: "50%", width: 1, background: "var(--muted-2, #6f6557)" }} />
            </div>
          </div>
        );
      })}
    </div>
  );
}

/**
 * Performans (#4) — dönem seçici + zaman-serisi grafiği (canlı fiyat geçmişi Faz 2'de
 * birikecek) + kalem bazında **gerçek** getiri dağılımı. Geleceği tahmin etmez;
 * mevcut/birikmiş veriyi gösterir (CLAUDE.md §2).
 */
export function PerformancePage() {
  const summary = usePortfolioSummary();
  const holdings = useHoldings();
  const { openAddHolding } = useAppShell();
  const [period, setPeriod] = useState<(typeof PERIODS)[number]>("Tümü");

  const holdingList = Array.isArray(holdings.data) ? holdings.data : [];

  return (
    <section className="page">
      <div className="topbar">
        <div>
          <div className="greet-hi">Zaman içinde</div>
          <h1>Performans</h1>
        </div>
      </div>

      {summary.isLoading && <PortfolioSkeleton />}
      {summary.isError && (
        <div className="state-error" role="alert">
          <p>Performans verisi yüklenemedi.</p>
          <button type="button" className="btn-primary" onClick={() => summary.refetch()}>
            Tekrar dene
          </button>
        </div>
      )}

      {summary.data && summary.data.allocation.length === 0 && (
        <EmptyState
          icon="📈"
          title="Performans için önce varlık ekle"
          description="Pozisyon ekledikçe getiriler ve zaman içindeki seyir burada görünecek."
          action={
            <button type="button" className="btn-primary lg" onClick={openAddHolding}>
              ＋ İlk varlığını ekle
            </button>
          }
        />
      )}

      {summary.data && summary.data.allocation.length > 0 && (
        <>
          <KpiGrid summary={summary.data} positionCount={holdingList.length} />

          <div className="card">
            <div className="card-head">
              <h3>Değer Seyri</h3>
              <div className="periods" role="group" aria-label="Dönem">
                {PERIODS.map((p) => (
                  <button
                    key={p}
                    type="button"
                    className={p === period ? "on" : ""}
                    aria-pressed={p === period}
                    onClick={() => setPeriod(p)}
                  >
                    {p}
                  </button>
                ))}
              </div>
            </div>
            <div className="chart-frame">
              <div className="cf-empty">
                <div className="cf-ic" aria-hidden="true">🕒</div>
                <p>
                  Zaman içindeki değer grafiği, <b>canlı fiyat geçmişi</b> biriktikçe burada
                  görünecek (Faz 2). Şimdilik aşağıda kalemlerin <b>tüm zamanlar</b> getiri
                  dağılımı var.
                </p>
              </div>
            </div>
          </div>

          <div className="card">
            <div className="card-head">
              <h3>Kalem Bazında Getiri</h3>
              <span className="mini">tüm zamanlar</span>
            </div>
            <ReturnBars holdings={holdingList} />
          </div>
        </>
      )}
    </section>
  );
}

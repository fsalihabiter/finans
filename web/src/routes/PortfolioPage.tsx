import type { CurrencyCode } from "@finans/shared";
import { KpiGrid } from "../components/KpiGrid";
import { AllocationDonut } from "../components/AllocationDonut";
import { PortfolioInsights } from "../components/PortfolioInsights";
import { CurrencySelector } from "../components/CurrencySelector";
import { HoldingsTable } from "../components/HoldingsTable";
import { useHoldings, usePortfolioSummary, useSettings, useUpdateSettings } from "../lib/hooks";

/**
 * Genel Bakış panosu (13 §4): KPI'lar + dağılım donut + içgörüler + pozisyon tablosu.
 * Tüm sayılar backend'den; burada yalnızca veri bağlama + tr-TR biçimleme (NFR-1/7).
 */
export function PortfolioPage() {
  const settings = useSettings();
  const summary = usePortfolioSummary();
  const holdings = useHoldings();
  const updateSettings = useUpdateSettings();

  const baseCurrency = settings.data?.baseCurrency;
  const onCurrencyChange = (currency: CurrencyCode) =>
    updateSettings.mutate({ baseCurrency: currency });

  const holdingList = Array.isArray(holdings.data) ? holdings.data : [];

  return (
    <section>
      <div className="topbar">
        <div>
          <div className="greet-hi">İyi günler,</div>
          <h1>Genel Bakış</h1>
        </div>
        <div className="tools">
          {summary.data && <span className="badge">{summary.data.allocation.length} varlık</span>}
          {baseCurrency && (
            <CurrencySelector
              value={baseCurrency}
              onChange={onCurrencyChange}
              disabled={updateSettings.isPending}
            />
          )}
        </div>
      </div>

      {summary.isLoading && <p className="muted">Yükleniyor…</p>}
      {summary.isError && (
        <p className="neg" role="alert">
          Portföy özeti yüklenemedi. Bağlantıyı kontrol edip tekrar deneyin.
        </p>
      )}

      {summary.data && (
        <>
          <KpiGrid summary={summary.data} positionCount={holdingList.length} />

          {summary.data.allocation.length > 0 ? (
            <>
              <div className="card" style={{ marginBottom: 16 }}>
                <div className="card-head"><h3>Varlık Dağılımı</h3></div>
                <AllocationDonut allocation={summary.data.allocation} />
              </div>

              <PortfolioInsights summary={summary.data} holdings={holdingList} />

              <div className="card">
                <div className="card-head">
                  <h3>Varlıklarım</h3>
                  <span className="mini">Detay için satıra tıkla</span>
                </div>
                <HoldingsTable holdings={holdingList} baseCurrency={summary.data.baseCurrency} />
              </div>
            </>
          ) : (
            <p className="muted empty-hint">
              Henüz pozisyonun yok. Soldaki <b>＋ Varlık Ekle</b> ile başla.
            </p>
          )}
        </>
      )}
    </section>
  );
}

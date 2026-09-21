import { formatCurrency, formatPercent } from "@finans/shared";
import { HoldingsTable } from "../components/HoldingsTable";
import { PortfolioSkeleton } from "../components/Skeleton";
import { EmptyState } from "../components/EmptyState";
import { useHoldings, usePortfolioSummary } from "../lib/hooks";
import { useAppShell } from "../lib/appShell";

/**
 * Varlıklarım (kullanıcı isteği 2026-07-12): pozisyon tablosu panonun dibinde sönük
 * kalıyordu → kendi sayfası. "Varlık Ekle" butonu ve akışı da YALNIZ burada — konu
 * bütünlüğü (varlık yönetimi tek yerde). Satıra tıklayınca pozisyon detayı.
 */
export function HoldingsPage() {
  const holdings = useHoldings();
  const summary = usePortfolioSummary();
  const { openAddHolding } = useAppShell();

  const list = Array.isArray(holdings.data) ? holdings.data : [];
  const baseCurrency = summary.data?.baseCurrency ?? "TRY";

  return (
    <section className="page">
      <div className="topbar">
        <div>
          <div className="greet-hi">Portföy</div>
          <h1>Varlıklarım</h1>
        </div>
        <div className="tools">
          <button type="button" className="btn-primary" onClick={openAddHolding}>
            ＋ Varlık Ekle
          </button>
        </div>
      </div>

      {holdings.isLoading && <PortfolioSkeleton />}

      {holdings.isError && (
        <div className="state-error" role="alert">
          <p>Pozisyonlar yüklenemedi. Bağlantını kontrol edip tekrar dene.</p>
          <button type="button" className="btn-primary" onClick={() => holdings.refetch()}>
            Tekrar dene
          </button>
        </div>
      )}

      {holdings.data &&
        (list.length > 0 ? (
          <div className="card">
            <div className="card-head">
              <h3>Pozisyonlar</h3>
              <span className="mini">{list.length} pozisyon · Detay için satıra tıkla</span>
            </div>
            <HoldingsTable holdings={list} baseCurrency={baseCurrency} />
            {/* Genel toplam (kullanıcı isteği 2026-09-20). İstemcide TOPLANMAZ — özet
                ucunun backend'de hesapladığı değerler gösterilir (D-001: parasal hesap
                kodda, tek yerde). Liste ile aynı kuralla (hak edilmiş BES dahil) hesaplandığı
                için satırlarla tutarlıdır. */}
            {summary.data && (
              <div className="holdings-total" data-testid="holdings-total">
                <div className="ht-item">
                  <span className="ht-k">Toplam değer</span>
                  <span className="ht-v tnum">{formatCurrency(summary.data.totalValue, baseCurrency)}</span>
                </div>
                <div className="ht-item">
                  <span className="ht-k">Toplam maliyet</span>
                  <span className="ht-v tnum">{formatCurrency(summary.data.totalCost, baseCurrency)}</span>
                </div>
                <div className="ht-item">
                  <span className="ht-k">Kâr / zarar</span>
                  <span className={`ht-v tnum ${summary.data.netProfit > 0 ? "up" : summary.data.netProfit < 0 ? "down" : ""}`}>
                    {summary.data.netProfit > 0 ? "+" : ""}
                    {formatCurrency(summary.data.netProfit, baseCurrency)}
                    {summary.data.returnRatio !== null && <> · {formatPercent(summary.data.returnRatio, 1, true)}</>}
                  </span>
                </div>
              </div>
            )}
          </div>
        ) : (
          <EmptyState
            icon="📂"
            title="Portföyün henüz boş"
            description={
              <>
                İlk varlığını ekle; toplam değer, dağılım ve getiri otomatik hesaplansın.
                Altın, döviz, hisse, fon, nakit ve BES ekleyebilirsin.
              </>
            }
            action={
              <button type="button" className="btn-primary lg" onClick={openAddHolding}>
                ＋ İlk varlığını ekle
              </button>
            }
          />
        ))}
    </section>
  );
}

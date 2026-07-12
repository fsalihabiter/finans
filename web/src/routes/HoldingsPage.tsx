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

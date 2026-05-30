import { ComingSoonPage } from "../components/ComingSoonPage";

/**
 * Hisse Analizi — F/K, PD/DD, temettü, kâr büyümesi gibi temel metrikleri çeker ve
 * **ne anlama geldiğini** sade dille açıklar; al/sat/öneri YOK (CLAUDE.md §2, Faz 4).
 */
export function StocksPage() {
  return (
    <ComingSoonPage
      kicker="Temel analiz"
      title="Hisse Analizi"
      icon="📊"
      heading="Rakamların ne anlama geldiğini öğren"
      description="F/K, PD/DD, temettü verimi ve kâr büyümesi gibi metrikler çekilip sade dille açıklanacak — “al/sat” ya da “yükselir” demeden, hissenin karakterini anlatarak."
      phase="Faz 4"
      withDisclaimer
    />
  );
}

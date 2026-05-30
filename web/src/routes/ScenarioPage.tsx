import { ComingSoonPage } from "../components/ComingSoonPage";

/**
 * Senaryo — "şunu değiştirseydim ne olurdu?" geçmişe dönük simülatör. Geleceği
 * tahmin etmez; geçmiş veriyle karşılaştırır (CLAUDE.md §2). Fiyat geçmişi gerekir.
 */
export function ScenarioPage() {
  return (
    <ComingSoonPage
      kicker="Geçmişe dönük simülatör"
      title="Senaryo"
      icon="⚖"
      heading="Farklı dağılımları güvenle dene"
      description="“Kripto yerine fon tutsaydım?” gibi değişikliklerin geçmiş veriyle nasıl sonuçlanacağını gösterir — geleceği öngörmez, öğrenmek için güvenli bir alandır."
      phase="Faz 5"
      withDisclaimer
    />
  );
}

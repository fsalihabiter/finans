import { ComingSoonPage } from "../components/ComingSoonPage";

/**
 * Analiz — eğitici LLM yorum kartları Faz 3'te. Disclaimer (NFR-2) bu yüzeyde
 * **her zaman** görünür (yorum içeriği gelmeden önce bile).
 */
export function AnalysisPage() {
  return (
    <ComingSoonPage
      kicker="Yapay zekâ destekli"
      title="Analiz"
      icon="✦"
      heading="Portföyün ne anlatıyor?"
      description="Hesaplar sistemde yapılır, yorum sade dille üretilir. Çeşitlendirme, yoğunlaşma, döviz maruziyeti ve reel getiri üzerine eğitici çerçeveler bu ekranda gelecek."
      phase="Faz 3"
      withDisclaimer
    />
  );
}

import { Disclaimer } from "../components/Disclaimer";

/**
 * Analiz sayfası — eğitici yorum kartları Faz 3'te (LLM). Disclaimer (NFR-2) bu
 * yüzeyte **her zaman** görünür — yorum içeriği gelmeden önce bile.
 */
export function AnalysisPage() {
  return (
    <section>
      <h1>Analiz</h1>
      <Disclaimer />
      <p className="muted">
        Eğitici yorum kartları (portföyünü sade dille açıklayan çerçeveler) Faz 3'te
        burada gelecek.
      </p>
    </section>
  );
}

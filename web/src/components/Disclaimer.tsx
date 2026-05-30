/**
 * "Yatırım tavsiyesi değildir" çerçevesi (NFR-2, CLAUDE.md §2 — projenin #1 kuralı).
 * Analiz/Hisse gibi yorum içeren ekranlarda **her zaman** görünür. Faz 3'teki LLM
 * yorum kartları da bu bileşeni kullanır.
 */
export function Disclaimer() {
  return (
    <p className="disclaimer" role="note">
      <strong>Yatırım tavsiyesi değildir.</strong> Buradaki bilgiler eğitici ve
      bilgilendirici amaçlıdır; kişiye özel alım-satım yönlendirmesi içermez.
    </p>
  );
}

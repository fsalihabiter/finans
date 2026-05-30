import type { ReactNode } from "react";
import { Disclaimer } from "./Disclaimer";

/**
 * Taslaktaki menüler için ortak yer-tutucu sayfa (#3): topbar + (ops.) disclaimer +
 * "Yakında" bloğu. Hangi fazda geleceği `phase` ile belirtilir. İçerik geldikçe
 * her sayfa kendi gerçek bileşeniyle değiştirilir.
 */
export function ComingSoonPage({
  kicker,
  title,
  icon,
  heading,
  description,
  phase,
  withDisclaimer = false,
  children,
}: {
  kicker: string;
  title: string;
  icon: string;
  heading: string;
  description: ReactNode;
  phase: string;
  withDisclaimer?: boolean;
  children?: ReactNode;
}) {
  return (
    <section className="page">
      <div className="topbar">
        <div>
          <div className="greet-hi">{kicker}</div>
          <h1>{title}</h1>
        </div>
        <span className="badge">{phase}</span>
      </div>

      {withDisclaimer && <Disclaimer />}

      <div className="coming-soon">
        <div className="cs-orb" aria-hidden="true">{icon}</div>
        <h2 className="cs-title">{heading}</h2>
        <p className="cs-desc">{description}</p>
      </div>

      {children}
    </section>
  );
}

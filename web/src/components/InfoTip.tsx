import { useId, useState } from "react";

/**
 * Erişilebilir bilgi ipucu — metrik adının yanında küçük "i". "Sıfır bilgi"
 * kullanıcıya terimi sade dille açıklar (ürün vizyonu: eğitici). Hover + odak +
 * dokunuş ile açılır; içerik yatırım tavsiyesi değil, tanım/çerçeve sunar.
 */
export function InfoTip({ label, children }: { label: string; children: string }) {
  const [open, setOpen] = useState(false);
  const id = useId();

  return (
    <span className="infotip">
      <button
        type="button"
        className="infotip-btn"
        aria-label={`${label} nedir?`}
        aria-expanded={open}
        aria-describedby={open ? id : undefined}
        onClick={() => setOpen((v) => !v)}
        onMouseEnter={() => setOpen(true)}
        onMouseLeave={() => setOpen(false)}
        onBlur={() => setOpen(false)}
      >
        i
      </button>
      {open && (
        <span role="tooltip" id={id} className="infotip-pop">
          {children}
        </span>
      )}
    </span>
  );
}

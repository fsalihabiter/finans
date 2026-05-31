import { useEffect, useRef } from "react";
import type { ReactNode } from "react";

const FOCUSABLE =
  'a[href], button:not([disabled]), input, select, textarea, [tabindex]:not([tabindex="-1"])';

/**
 * Genel modal kabuğu — başlık + kapat + odak tuzağı + Escape + odak iadesi.
 * Detay sayfasındaki işlem/fiyat/katkı formları bununla modale taşınır (#2),
 * böylece sayfa yoğunluğu azalır. `AddHoldingDialog` kendi yapısını korur.
 */
export function Modal({
  title,
  onClose,
  children,
}: {
  title: string;
  onClose: () => void;
  children: ReactNode;
}) {
  const ref = useRef<HTMLDivElement>(null);
  const restore = useRef<HTMLElement | null>(null);
  // onClose'u ref'te tut: ebeveyn her render'da yeni kapatma fonksiyonu verse bile asıl
  // effect YENİDEN çalışmasın (yoksa her tuşta odak ilk öğeye sıçrardı). Ref güncellemesi
  // render'da değil, ayrı bir effect'te (react-hooks/refs).
  const onCloseRef = useRef(onClose);
  useEffect(() => {
    onCloseRef.current = onClose;
  }, [onClose]);

  useEffect(() => {
    restore.current = document.activeElement as HTMLElement | null;
    requestAnimationFrame(() => {
      // Odağı İÇERİK alanındaki ilk alana ver (kapat butonu .modal-top'ta, DOM'da
      // ondan önce gelir → tüm modalda querySelector onu seçerdi). Yoksa modal geneline düş.
      const body = ref.current?.querySelector<HTMLElement>(".sheet-body");
      const first =
        body?.querySelector<HTMLElement>(FOCUSABLE) ?? ref.current?.querySelector<HTMLElement>(FOCUSABLE);
      first?.focus();
    });
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        onCloseRef.current();
        return;
      }
      if (e.key !== "Tab" || !ref.current) return;
      const items = Array.from(ref.current.querySelectorAll<HTMLElement>(FOCUSABLE)).filter(
        (el) => el.offsetParent !== null,
      );
      if (items.length === 0) return;
      const first = items[0];
      const last = items[items.length - 1];
      if (e.shiftKey && document.activeElement === first) {
        e.preventDefault();
        last.focus();
      } else if (!e.shiftKey && document.activeElement === last) {
        e.preventDefault();
        first.focus();
      }
    };
    window.addEventListener("keydown", onKey);
    document.body.classList.add("drawer-lock");
    return () => {
      window.removeEventListener("keydown", onKey);
      document.body.classList.remove("drawer-lock");
      restore.current?.focus?.();
    };
  }, []); // yalnız mount/unmount — onClose ref ile okunur (yukarıdaki açıklama)

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div
        ref={ref}
        className="modal"
        role="dialog"
        aria-modal="true"
        aria-label={title}
        onClick={(e) => e.stopPropagation()}
      >
        <div className="modal-top">
          <h2>{title}</h2>
          <button type="button" className="modal-close" aria-label="Kapat" onClick={onClose}>
            ✕
          </button>
        </div>
        <div className="sheet-body">{children}</div>
      </div>
    </div>
  );
}

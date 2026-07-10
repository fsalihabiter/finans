import { createContext, useCallback, useContext, useMemo, useRef, useState } from "react";
import type { ReactNode } from "react";
import { withViewTransition } from "../lib/viewTransition";

export type ToastTone = "success" | "error" | "info";

interface ToastItem {
  id: number;
  message: string;
  tone: ToastTone;
  /** Çıkış animasyonu oynuyor (toast-out keyframe) — DOM'dan silinmeden hemen önce. */
  leaving?: boolean;
}

interface ToastApi {
  /** Geçici bildirim göster (başarı/hata/bilgi). Sağlayıcı yoksa no-op. */
  notify: (message: string, tone?: ToastTone) => void;
}

const ToastContext = createContext<ToastApi>({ notify: () => {} });

/** Aksiyon geri bildirimi (varlık eklendi, fiyat güncellendi, silindi…). */
// eslint-disable-next-line react-refresh/only-export-components
export function useToast(): ToastApi {
  return useContext(ToastContext);
}

const TONE_ICON: Record<ToastTone, string> = {
  success: "✓",
  error: "!",
  info: "i",
};

const AUTO_DISMISS_MS = 3800;
/** Çıkış keyframe süresi (App.css `toast-out` = 440ms) — otomatik kapanışta silmeden önce oynar. */
const EXIT_MS = 440;

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([]);
  const seq = useRef(0);

  const dismiss = useCallback((id: number) => {
    setToasts((list) => list.filter((t) => t.id !== id));
  }, []);

  const beginLeave = useCallback((id: number) => {
    setToasts((list) => list.map((t) => (t.id === id ? { ...t, leaving: true } : t)));
  }, []);

  const notify = useCallback(
    (message: string, tone: ToastTone = "success") => {
      const id = ++seq.current;
      setToasts((list) => [...list, { id, message, tone }]);
      // Zamanlayıcı sürümlü yaşam döngüsü: görsel çıkış CSS'te, silme timer'da
      // (jsdom animasyon olayı üretmez — davranış animasyona bağlanmaz).
      window.setTimeout(() => beginLeave(id), AUTO_DISMISS_MS - EXIT_MS);
      window.setTimeout(() => dismiss(id), AUTO_DISMISS_MS);
    },
    [beginLeave, dismiss],
  );

  const api = useMemo(() => ({ notify }), [notify]);

  return (
    <ToastContext.Provider value={api}>
      {children}
      <div className="toast-region" role="status" aria-live="polite" aria-atomic="false">
        {toasts.map((t) => (
          <div key={t.id} className={`toast toast-${t.tone}${t.leaving ? " toast-leaving" : ""}`}>
            <span className="toast-ic" aria-hidden="true">{TONE_ICON[t.tone]}</span>
            <span className="toast-msg">{t.message}</span>
            <button
              type="button"
              className="toast-close"
              aria-label="Kapat"
              onClick={() => withViewTransition(() => dismiss(t.id))}
            >
              ✕
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

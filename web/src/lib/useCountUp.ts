import { useEffect, useRef, useState } from "react";

/** easeOutCubic — sayaç sona yaklaşırken yavaşlar (doğal "yerleşme" hissi). */
const easeOut = (t: number) => 1 - Math.pow(1 - t, 3);

/**
 * Gerçek tarayıcı mı? Test ortamı (jsdom) matchMedia bilmez → animasyonsuz, hedef
 * anında (deterministik testler). OS "hareketi azalt" tercihi BİLEREK dikkate
 * alınmaz — kullanıcı kararı 2026-07-11: animasyonlar her açılış/yenilemede oynar.
 */
function canAnimate(): boolean {
  return typeof window !== "undefined" && typeof window.matchMedia === "function";
}

/**
 * Sayısal değeri sayaç animasyonuyla hedefe taşır: her mount'ta (sayfa yenileme
 * dahil) 0'dan, sonraki güncellemelerde o anki görünen değerden başlar. Testte
 * hedefi anında döndürür — sayılar deterministik kalır, testler beklemez.
 * Yalnızca GÖSTERİM içindir; hesap her zaman backend'in tam değeriyle yapılır.
 */
export function useCountUp(target: number, durationMs = 900): number {
  const [display, setDisplay] = useState(() => (canAnimate() ? 0 : target));
  const displayRef = useRef(display);

  useEffect(() => {
    if (!canAnimate()) {
      displayRef.current = target;
      setDisplay(target);
      return;
    }
    const from = displayRef.current;
    if (from === target) return;

    let raf = 0;
    const start = performance.now();
    const tick = (now: number) => {
      const t = Math.min(1, (now - start) / durationMs);
      const value = t === 1 ? target : from + (target - from) * easeOut(t);
      displayRef.current = value;
      setDisplay(value);
      if (t < 1) raf = requestAnimationFrame(tick);
    };
    raf = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf);
  }, [target, durationMs]);

  return display;
}

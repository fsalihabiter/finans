import { useCallback, useState } from "react";

/**
 * Grafik hover durumu (T5.3 devamı): imlecin yatay konumunu en yakın veri noktasının
 * indeksine eşler. Grafikler eşit aralıklı noktalar çizer → oran × (n−1) yeterli.
 * Pointer olayları hem fare hem dokunmatik için çalışır.
 */
export function useChartHover(pointCount: number) {
  const [index, setIndex] = useState<number | null>(null);

  const onPointerMove = useCallback(
    (e: React.PointerEvent<HTMLElement>) => {
      const rect = e.currentTarget.getBoundingClientRect();
      if (rect.width <= 0 || pointCount < 2) return;
      const ratio = (e.clientX - rect.left) / rect.width;
      if (!Number.isFinite(ratio)) return;
      const nearest = Math.round(ratio * (pointCount - 1));
      setIndex(Math.max(0, Math.min(pointCount - 1, nearest)));
    },
    [pointCount],
  );

  const onPointerLeave = useCallback(() => setIndex(null), []);

  return { index, onPointerMove, onPointerLeave };
}

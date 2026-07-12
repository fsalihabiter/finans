import { useCallback, useState } from "react";

/**
 * Grafik hover durumu (T5.3 devamı): imlecin yatay konumunu en yakın veri noktasının
 * indeksine eşler. Grafikler eşit aralıklı noktalar çizer → oran × (n−1) yeterli.
 * Pointer olayları hem fare hem dokunmatik için çalışır.
 *
 * `insetRatio`: çizim alanının yatay kenar boşluğu (padX/viewBoxWidth). Çizgiler bu
 * boşluğun İÇİNE çizilir; imleç oranı da aynı aralığa normalize edilmezse nokta
 * imleçten ~10px kayar ve dik segmentlerde "çizgiyle hizasız" görünür (2026-07-12
 * geri bildirimi — isabet düzeltmesi).
 */
export function useChartHover(pointCount: number, insetRatio = 0) {
  const [index, setIndex] = useState<number | null>(null);

  const onPointerMove = useCallback(
    (e: React.PointerEvent<HTMLElement>) => {
      const rect = e.currentTarget.getBoundingClientRect();
      if (rect.width <= 0 || pointCount < 2) return;
      const raw = (e.clientX - rect.left) / rect.width;
      const ratio = insetRatio > 0 ? (raw - insetRatio) / (1 - 2 * insetRatio) : raw;
      if (!Number.isFinite(ratio)) return;
      const nearest = Math.round(ratio * (pointCount - 1));
      setIndex(Math.max(0, Math.min(pointCount - 1, nearest)));
    },
    [pointCount, insetRatio],
  );

  const onPointerLeave = useCallback(() => setIndex(null), []);

  return { index, onPointerMove, onPointerLeave };
}

import { describe, expect, it } from "vitest";
import { renderHook } from "@testing-library/react";
import { useCountUp } from "./useCountUp";

/**
 * jsdom'da matchMedia yok → hook test yoluna düşer ve hedefi ANINDA döndürür.
 * Bu bilinçli sözleşmedir: testlerde para değerleri deterministiktir, hiçbir
 * assert sayaç animasyonunu beklemek zorunda kalmaz (NFR-1: yanlış/ara rakam
 * gösterimi kabul edilemez). Tarayıcıda ise her mount'ta animasyon oynar.
 */
describe("useCountUp", () => {
  it("test ortamında (matchMedia yok) hedef değeri anında döndürür", () => {
    const { result } = renderHook(({ v }) => useCountUp(v), {
      initialProps: { v: 641403 },
    });
    expect(result.current).toBe(641403);
  });

  it("hedef değişince yeni hedefi anında yansıtır (test ortamı)", () => {
    const { result, rerender } = renderHook(({ v }) => useCountUp(v), {
      initialProps: { v: 100 },
    });
    rerender({ v: -250.5 });
    expect(result.current).toBe(-250.5);
  });
});

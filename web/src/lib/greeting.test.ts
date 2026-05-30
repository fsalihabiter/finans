import { describe, expect, it } from "vitest";
import { greetingFor } from "./greeting";

describe("greetingFor", () => {
  it("saate göre doğru Türkçe selamlamayı verir", () => {
    expect(greetingFor(2)).toBe("İyi geceler");
    expect(greetingFor(9)).toBe("Günaydın");
    expect(greetingFor(14)).toBe("İyi günler");
    expect(greetingFor(21)).toBe("İyi akşamlar");
  });

  it("sınır saatlerini doğru sınıflandırır", () => {
    expect(greetingFor(0)).toBe("İyi geceler");
    expect(greetingFor(6)).toBe("Günaydın");
    expect(greetingFor(12)).toBe("İyi günler");
    expect(greetingFor(18)).toBe("İyi akşamlar");
  });
});

/** Saate göre Türkçe selamlama (topbar). Saf — yalnızca saat bilgisinden türer. */
export function greetingFor(hour: number): string {
  if (hour < 6) return "İyi geceler";
  if (hour < 12) return "Günaydın";
  if (hour < 18) return "İyi günler";
  return "İyi akşamlar";
}

export function currentGreeting(now: Date = new Date()): string {
  return greetingFor(now.getHours());
}

// gg.aa.yyyy ↔ ISO (YYYY-MM-DD) dönüşümü + maske — özel DateField için saf yardımcılar.

/** ISO (YYYY-MM-DD) → görünen "gg.aa.yyyy". Geçersizse boş. */
export function isoToDotted(iso: string): string {
  const m = /^(\d{4})-(\d{2})-(\d{2})/.exec(iso);
  return m ? `${m[3]}.${m[2]}.${m[1]}` : "";
}

/** "gg.aa.yyyy" → ISO (YYYY-MM-DD); geçersiz/eksikse boş (round-trip ile 31.02 gibi reddedilir). */
export function dottedToIso(text: string): string {
  const m = /^(\d{2})\.(\d{2})\.(\d{4})$/.exec(text);
  if (!m) return "";
  const [, dd, mm, yyyy] = m;
  const day = Number(dd);
  const mon = Number(mm);
  const year = Number(yyyy);
  if (mon < 1 || mon > 12 || day < 1 || day > 31) return "";
  const d = new Date(Date.UTC(year, mon - 1, day));
  if (d.getUTCFullYear() !== year || d.getUTCMonth() !== mon - 1 || d.getUTCDate() !== day) return "";
  return `${yyyy}-${mm}-${dd}`;
}

/** Yazılan metni gg.aa.yyyy maskesine sokar (yalnız rakam; otomatik nokta). */
export function maskDate(raw: string): string {
  const digits = raw.replace(/\D/g, "").slice(0, 8);
  const parts = [digits.slice(0, 2), digits.slice(2, 4), digits.slice(4, 8)].filter((p) => p.length > 0);
  return parts.join(".");
}

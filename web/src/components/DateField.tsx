/**
 * Tarih girişi — native `<input type="date">`.
 *
 * Tarayıcı tek başına şunları sağlar: **takvim açılır** (autocomplete), **↑/↓** ile
 * gün/ay/yıl değerini artır-azalt, **←/→** ile gün↔ay↔yıl segmentleri arası geçiş ve
 * **Tab** ile alanlar arası gezinme. Değer sözleşmesi ISO `YYYY-MM-DD` (native ile aynı):
 * `value` ISO alır, `onChange` ISO döner (boş/geçersizde "").
 *
 * Not: Gösterilen biçim tarayıcı diline bağlıdır (TR → gg.aa.yyyy). Uygulama genelindeki
 * salt-gösterim tarihleri `formatDate` ile her durumda gg.aa.yyyy basılır (NFR-7).
 */
export function DateField({
  value,
  onChange,
  max,
  min,
  id,
  autoFocus,
  required,
  ariaLabel,
}: {
  value: string;
  onChange: (isoDate: string) => void;
  /** Üst sınır (ISO). Verilmezse ileri tarih serbest. */
  max?: string;
  /** Alt sınır (ISO). */
  min?: string;
  id?: string;
  autoFocus?: boolean;
  required?: boolean;
  ariaLabel?: string;
}) {
  return (
    <input
      id={id}
      type="date"
      className="date-input"
      max={max}
      min={min}
      autoFocus={autoFocus}
      required={required}
      aria-label={ariaLabel}
      value={value}
      onChange={(e) => onChange(e.target.value)}
    />
  );
}

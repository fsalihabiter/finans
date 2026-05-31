import { useState } from "react";
import { dottedToIso, isoToDotted, maskDate } from "../lib/dateMask";

/**
 * Özel tarih girişi — her OS/locale'de **gg.aa.yyyy** (noktalı) gösterir/alır (native
 * `input[type=date]` biçimi tarayıcıya bağlı olduğundan). `value`/`onChange` native ile
 * AYNI sözleşme: ISO `YYYY-MM-DD` (geçersiz/eksik/`max` aşımında ""). Salt metin (maskeli).
 */
export function DateField({
  value,
  onChange,
  max,
  id,
  autoFocus,
  required,
  ariaLabel,
}: {
  value: string;
  onChange: (isoDate: string) => void;
  max?: string;
  id?: string;
  autoFocus?: boolean;
  required?: boolean;
  ariaLabel?: string;
}) {
  const [text, setText] = useState(() => isoToDotted(value));

  const handle = (raw: string) => {
    const formatted = maskDate(raw);
    setText(formatted);
    const iso = dottedToIso(formatted);
    onChange(iso && max && iso > max ? "" : iso);
  };

  return (
    <input
      id={id}
      type="text"
      inputMode="numeric"
      placeholder="gg.aa.yyyy"
      maxLength={10}
      autoFocus={autoFocus}
      required={required}
      aria-label={ariaLabel}
      value={text}
      onChange={(e) => handle(e.target.value)}
    />
  );
}

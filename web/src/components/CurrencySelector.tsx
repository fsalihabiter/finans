import type { CurrencyCode } from "@finans/shared";

const OPTIONS: CurrencyCode[] = ["TRY", "USD", "EUR"];

/**
 * Baz para birimi seçici (FR-1.4). Seçim kullanıcı tercihini günceller; tüm
 * parasal görünümler seçilen baz pb'ye göre yeniden hesaplanır (backend).
 */
export function CurrencySelector({
  value,
  onChange,
  disabled = false,
}: {
  value: CurrencyCode;
  onChange: (currency: CurrencyCode) => void;
  disabled?: boolean;
}) {
  return (
    <div className="ccy-selector" role="group" aria-label="Baz para birimi">
      {OPTIONS.map((code) => (
        <button
          key={code}
          type="button"
          className={code === value ? "active" : ""}
          aria-pressed={code === value}
          disabled={disabled}
          onClick={() => onChange(code)}
        >
          {code}
        </button>
      ))}
    </div>
  );
}

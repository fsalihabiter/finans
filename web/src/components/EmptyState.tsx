import type { ReactNode } from "react";

/**
 * Boş/sıfır durum bloğu — ikon + başlık + açıklama + (ops.) eylem butonu.
 * Cihazdan bağımsız çalışır; "soldaki menü" gibi platforma bağlı yönlendirme YOK.
 */
export function EmptyState({
  icon,
  title,
  description,
  action,
}: {
  icon: string;
  title: string;
  description: ReactNode;
  action?: ReactNode;
}) {
  return (
    <div className="empty-state">
      <div className="empty-ic" aria-hidden="true">{icon}</div>
      <h2 className="empty-title">{title}</h2>
      <p className="empty-desc">{description}</p>
      {action && <div className="empty-action">{action}</div>}
    </div>
  );
}

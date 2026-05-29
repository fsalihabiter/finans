import { useQuery } from "@tanstack/react-query";
import { api } from "../lib/api";

/**
 * Backend `/api/health` ucundan canlı veri çeker ve durumunu gösterir (T0.10).
 * Faz 0 mini deneme: web → @finans/shared api → backend zincirini doğrular.
 */
export function HealthBadge() {
  const { data, isLoading, isError } = useQuery({
    queryKey: ["health"],
    queryFn: () => api.getHealth(),
  });

  let label = "Backend kontrol ediliyor…";
  let tone = "pending";
  if (isError) {
    label = "Backend'e ulaşılamadı";
    tone = "error";
  } else if (!isLoading && data) {
    label = `Backend: ${data.status}`;
    tone = "ok";
  }

  return (
    <span className="health-badge" data-tone={tone} role="status">
      <span className="health-dot" aria-hidden="true" />
      {label}
    </span>
  );
}

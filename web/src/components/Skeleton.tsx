/**
 * Yükleme iskeletleri — düz "Yükleniyor…" metni yerine (layout shift'i önler,
 * algılanan hızı artırır). Saf görsel; `prefers-reduced-motion`'da parıltı durur.
 */
export function Skeleton({
  width,
  height = 14,
  radius = 8,
  className = "",
}: {
  width?: number | string;
  height?: number | string;
  radius?: number | string;
  className?: string;
}) {
  return (
    <span
      className={`skel ${className}`}
      style={{ width, height, borderRadius: radius }}
      aria-hidden="true"
    />
  );
}

/** Genel Bakış panosu için tam-sayfa iskelet (KPI + dağılım + tablo). */
export function PortfolioSkeleton() {
  return (
    <div className="page" aria-busy="true" aria-label="Portföy yükleniyor">
      <div className="kpis">
        {Array.from({ length: 4 }).map((_, i) => (
          <div className="kpi" key={i}>
            <Skeleton width="55%" height={11} />
            <Skeleton width="80%" height={26} radius={10} className="skel-mt" />
            <Skeleton width="40%" height={11} className="skel-mt" />
          </div>
        ))}
      </div>
      <div className="grid-2">
        <div className="card">
          <Skeleton width={150} height={150} radius="50%" />
        </div>
        <div className="card">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} width="100%" height={16} className="skel-mt" />
          ))}
        </div>
      </div>
      <div className="card">
        {Array.from({ length: 4 }).map((_, i) => (
          <div className="skel-row" key={i}>
            <Skeleton width={37} height={37} radius={11} />
            <Skeleton width="40%" height={14} />
            <Skeleton width="20%" height={14} />
          </div>
        ))}
      </div>
    </div>
  );
}

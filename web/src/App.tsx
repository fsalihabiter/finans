import { useState } from "react";
import { NavLink, Outlet } from "react-router-dom";
import { HealthBadge } from "./components/HealthBadge";
import { AddHoldingDialog } from "./components/AddHoldingDialog";
import "./App.css";

const ICONS = {
  home: "M3 9.5 12 3l9 6.5V20a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1V9.5Z",
  chart: "M4 19V5m0 14h16M8 16l3-4 3 2 4-6",
  gear: "M12 2v3m0 14v3M2 12h3m14 0h3M5 5l2 2m10 10 2 2M19 5l-2 2M7 17l-2 2",
};

function NavIcon({ d }: { d: string }) {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      {d === ICONS.gear && <circle cx="12" cy="12" r="3" />}
      <path d={d} />
    </svg>
  );
}

/**
 * AppShell (13 §4): sol sidebar (marka + ikonlu gezinme + "Varlık Ekle" + kullanıcı),
 * ortada içerik. "Varlık Ekle" modalı kabuk seviyesinde — her ekrandan açılır.
 */
export default function App() {
  const [addOpen, setAddOpen] = useState(false);

  return (
    <div className="app-shell">
      <aside className="app-sidebar">
        <div className="app-brand">
          <div className="brand-mark" aria-hidden="true">◆</div>
          <div className="brand-name">Fin<span>ans</span></div>
        </div>

        <div className="nav-label">Portföy</div>
        <nav className="app-nav">
          <NavLink to="/" end>
            <NavIcon d={ICONS.home} /> Genel Bakış
          </NavLink>
          <NavLink to="/analiz">
            <NavIcon d={ICONS.chart} /> Analiz
          </NavLink>
        </nav>

        <div className="sidebar-spacer" />

        <nav className="app-nav">
          <NavLink to="/ayarlar">
            <NavIcon d={ICONS.gear} /> Ayarlar
          </NavLink>
        </nav>
        <button type="button" className="sidebar-add" onClick={() => setAddOpen(true)}>
          ＋ Varlık Ekle
        </button>
        <div className="sidebar-user">
          <div className="avatar" aria-hidden="true">👤</div>
          <div>
            <div className="su-name">Yatırımcı</div>
            <HealthBadge />
          </div>
        </div>
      </aside>

      <main className="app-content">
        <Outlet />
      </main>

      <AddHoldingDialog open={addOpen} onClose={() => setAddOpen(false)} />
    </div>
  );
}

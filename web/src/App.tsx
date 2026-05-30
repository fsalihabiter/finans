import { NavLink, Outlet } from "react-router-dom";
import { HealthBadge } from "./components/HealthBadge";
import "./App.css";

/**
 * AppShell (13 §4): geniş ekranda sol sidebar (marka + gezinme + durum), dar
 * ekranda üstte yatay bar (responsive grid). İçerik ortalı kolonda (Outlet).
 */
export default function App() {
  return (
    <div className="app-shell">
      <aside className="app-sidebar">
        <span className="app-brand">Finans</span>
        <nav className="app-nav">
          <NavLink to="/" end>
            Portföy
          </NavLink>
          <NavLink to="/analiz">Analiz</NavLink>
        </nav>
        <div className="app-sidebar-foot">
          <HealthBadge />
        </div>
      </aside>
      <main className="app-content">
        <Outlet />
      </main>
    </div>
  );
}

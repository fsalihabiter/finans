import { NavLink, Outlet } from "react-router-dom";
import { HealthBadge } from "./components/HealthBadge";
import "./App.css";

/**
 * AppShell — Faz 0 iskelet kabuğu: sol/üst gezinme + içerik alanı (Outlet).
 * Tam sidebar/topbar tasarımı (DESIGN.md) T0.9/T1.11'de oturur.
 */
export default function App() {
  return (
    <div className="app-shell">
      <header className="app-topbar">
        <span className="app-brand">Finans</span>
        <nav className="app-nav">
          <NavLink to="/" end>
            Portföy
          </NavLink>
          <NavLink to="/analiz">Analiz</NavLink>
        </nav>
        <HealthBadge />
      </header>
      <main className="app-content">
        <Outlet />
      </main>
    </div>
  );
}

import { useEffect, useState } from "react";
import { NavLink, Outlet } from "react-router-dom";
import { HealthBadge } from "./components/HealthBadge";
import { AddHoldingDialog } from "./components/AddHoldingDialog";
import { BrandMark } from "./components/BrandMark";
import { ToastProvider } from "./components/Toast";
import { AppShellContext } from "./lib/appShell";
import "./App.css";

const ICONS = {
  home: "M3 9.5 12 3l9 6.5V20a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1V9.5Z",
  wallet: "M4 8h16v11H4V8Zm4 0V6h8v2M4 12h16",
  tx: "M4 7h16M4 12h16M4 17h10",
  perf: "M4 19V5m0 14h16M8 16l3-4 3 2 4-6",
  chart: "M4 19V5m0 14h16M8 13l3 3 3-5 4 4",
  scenario: "M12 3v18M3 12h18M5 7l3 3M19 7l-3 3",
  stock: "M3 17l5-5 4 3 6-7M21 8h-4m4 0v4",
  edu: "M3 6.5 12 3l9 3.5L12 10 3 6.5Zm0 0V14m18-7.5V14M7 8.7V14c0 1.1 2.2 2 5 2s5-.9 5-2V8.7",
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
 * AppShell (13 §4): masaüstünde sol sidebar; ≤1040px'te sidebar bir **drawer**'a
 * dönüşür ve üstte mobil bar gelir. "Varlık Ekle" modalı kabuk seviyesinde kalır
 * (context) ama tetikleyicisi YALNIZ Varlıklarım sayfasında — konu bütünlüğü
 * (kullanıcı isteği 2026-07-12).
 */
export default function App() {
  const [addOpen, setAddOpen] = useState(false);
  const [drawerOpen, setDrawerOpen] = useState(false);

  const closeDrawer = () => setDrawerOpen(false);

  // Drawer açıkken Escape ile kapat + arka plan kaydırmasını kilitle.
  useEffect(() => {
    if (!drawerOpen) return;
    const onKey = (e: KeyboardEvent) => e.key === "Escape" && setDrawerOpen(false);
    window.addEventListener("keydown", onKey);
    document.body.classList.add("drawer-lock");
    return () => {
      window.removeEventListener("keydown", onKey);
      document.body.classList.remove("drawer-lock");
    };
  }, [drawerOpen]);

  const openAdd = () => setAddOpen(true);

  return (
    <ToastProvider>
      <AppShellContext.Provider value={{ openAddHolding: openAdd }}>
        <a href="#main-content" className="skip-link">İçeriğe geç</a>

        {/* Mobil üst bar (≤1040px) */}
        <header className="mobile-topbar">
          <button
            type="button"
            className="hamburger"
            aria-label="Menüyü aç"
            aria-expanded={drawerOpen}
            onClick={() => setDrawerOpen(true)}
          >
            <span /><span /><span />
          </button>
          <div className="app-brand mobile-brand">
            <div className="brand-mark" aria-hidden="true"><BrandMark /></div>
            <div className="brand-name">Ni<span>rengi</span></div>
          </div>
        </header>

        <div className="app-shell">
          {/* Drawer arka planı (mobil) */}
          {drawerOpen && (
            <div className="drawer-scrim" onClick={() => setDrawerOpen(false)} aria-hidden="true" />
          )}

          <aside className={`app-sidebar ${drawerOpen ? "open" : ""}`}>
            <div className="app-brand">
              <div className="brand-mark" aria-hidden="true"><BrandMark /></div>
              <div className="brand-name">Ni<span>rengi</span></div>
            </div>

            <div className="nav-label">Portföy</div>
            <nav className="app-nav" onClick={closeDrawer}>
              <NavLink viewTransition to="/" end>
                <NavIcon d={ICONS.home} /> Genel Bakış
              </NavLink>
              <NavLink viewTransition to="/varliklar">
                <NavIcon d={ICONS.wallet} /> Varlıklarım
              </NavLink>
              <NavLink viewTransition to="/islemler">
                <NavIcon d={ICONS.tx} /> İşlemler
              </NavLink>
              <NavLink viewTransition to="/performans">
                <NavIcon d={ICONS.perf} /> Performans
              </NavLink>
            </nav>

            <div className="nav-label">Akıl &amp; Öğren</div>
            <nav className="app-nav" onClick={closeDrawer}>
              <NavLink viewTransition to="/analiz">
                <NavIcon d={ICONS.chart} /> Analiz
              </NavLink>
              <NavLink viewTransition to="/senaryo">
                <NavIcon d={ICONS.scenario} /> Senaryo
              </NavLink>
              <NavLink viewTransition to="/hisse">
                <NavIcon d={ICONS.stock} /> Hisse Analizi
              </NavLink>
              <NavLink viewTransition to="/egitim">
                <NavIcon d={ICONS.edu} /> Eğitim
              </NavLink>
            </nav>

            <div className="sidebar-spacer" />

            <nav className="app-nav" onClick={closeDrawer}>
              <NavLink viewTransition to="/ayarlar">
                <NavIcon d={ICONS.gear} /> Ayarlar
              </NavLink>
            </nav>
            <div className="sidebar-user">
              <div className="avatar" aria-hidden="true">👤</div>
              <div>
                <div className="su-name">Yatırımcı</div>
                <HealthBadge />
              </div>
            </div>
          </aside>

          <main className="app-content" id="main-content">
            <Outlet />
          </main>
        </div>

        {addOpen && <AddHoldingDialog open onClose={() => setAddOpen(false)} />}
      </AppShellContext.Provider>
    </ToastProvider>
  );
}

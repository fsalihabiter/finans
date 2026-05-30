import { createContext, useContext } from "react";

/**
 * Kabuk seviyesi eylemleri (her ekrandan erişilebilir): "Varlık Ekle" modalını aç.
 * Boş durum CTA'sı, mobil üst bar ve sidebar bu opener'ı kullanır — böylece modal
 * tek yerde (App) yönetilir. Sağlayıcı yoksa no-op (testlerde güvenli).
 */
export const AppShellContext = createContext<{ openAddHolding: () => void }>({
  openAddHolding: () => {},
});

export function useAppShell() {
  return useContext(AppShellContext);
}

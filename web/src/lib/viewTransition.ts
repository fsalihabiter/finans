/**
 * Görsel durum değişimini (modal kapanışı, toast silme) tarayıcının View
 * Transition API'sine sarar — destekleyen tarayıcıda eski/yeni kare arasında
 * yumuşak çıkış efekti üretir. Desteklenmeyen ortamda (eski tarayıcı, jsdom)
 * güncelleme SENKRON çalışır; testler ve davranış etkilenmez.
 */
export function withViewTransition(update: () => void): void {
  const doc = document as Document & { startViewTransition?: (cb: () => void) => unknown };
  if (typeof doc.startViewTransition === "function") {
    doc.startViewTransition(update);
  } else {
    update();
  }
}

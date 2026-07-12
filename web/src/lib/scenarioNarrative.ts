import { formatCurrency, formatPercent } from "@finans/shared";
import type { ScenarioComparison } from "@finans/shared";

/**
 * Senaryo karşılaştırmasının METİN hâli (T5.4 geri bildirimi). Sayılar backend'den;
 * burada yalnız deterministik cümle kalıbına dökülür — LLM YOK, tahmin YOK, al/sat
 * yönlendirmesi YOK (CLAUDE.md §2). Durum tespiti dili: "görünüyor / demektir".
 */
export function buildScenarioNarrative(s: ScenarioComparison): string {
  const ccy = s.baseCurrency;
  const { currentValue, invested, difference, differenceRatio, inflationAdjustedInvested } = s.summary;

  const parts: string[] = [];

  parts.push(
    `${s.name} pozisyonuna bugüne dek ${formatCurrency(invested, ccy)} yatırdın; bugünkü değeri ${formatCurrency(currentValue, ccy)}.`,
  );

  const ratioText = differenceRatio != null ? ` (${formatPercent(differenceRatio)})` : "";
  if (difference >= 0) {
    parts.push(
      `Bu para nakitte dursaydı bugün hâlâ ${formatCurrency(invested, ccy)} olacaktı — yani bu varlık, nakde göre nominal ${formatCurrency(difference, ccy)}${ratioText} önde.`,
    );
  } else {
    parts.push(
      `Bu para nakitte dursaydı bugün ${formatCurrency(invested, ccy)} olacaktı — yani bu varlık, nakde göre nominal ${formatCurrency(Math.abs(difference), ccy)}${ratioText} geride.`,
    );
  }

  if (inflationAdjustedInvested != null) {
    const threshold = formatCurrency(inflationAdjustedInvested, ccy);
    if (currentValue >= inflationAdjustedInvested) {
      parts.push(
        `Alım gücü eşiği ${threshold}: bugünkü değer eşiğin ÜZERİNDE — birikim bu dönemde alım gücünü korumuş görünüyor.`,
      );
    } else {
      parts.push(
        `Alım gücü eşiği ${threshold}: bugünkü değer eşiğin ALTINDA — nominal rakam ne olursa olsun, bu dönemde alım gücü azalmış görünüyor.`,
      );
    }
  }

  parts.push("Bu bir durum tespitidir; geleceğe dair öngörü ya da al-sat önerisi değildir.");

  return parts.join(" ");
}

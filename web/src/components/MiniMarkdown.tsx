import { Fragment, type ReactNode } from "react";

/** Satır içi **kalın** vurguyu React düğümlerine çevirir — HTML enjeksiyonu YOK (XSS-güvenli). */
function renderInline(text: string): ReactNode[] {
  return text.split(/(\*\*[^*]+\*\*)/g).map((part, i) =>
    part.startsWith("**") && part.endsWith("**") && part.length > 4 ? (
      <strong key={i}>{part.slice(2, -2)}</strong>
    ) : (
      <Fragment key={i}>{part}</Fragment>
    ),
  );
}

/**
 * Küçük, güvenli Markdown alt-kümesi renderer'ı (ders gövdeleri, 03 §C). Desteklenen:
 * `## `/`### ` başlık, `> ` alıntı, `- `/`* ` liste, boş satırla ayrılmış paragraf, satır
 * içi `**kalın**`. **`dangerouslySetInnerHTML` KULLANMAZ** — içerik güvenle React düğümüne
 * çevrilir (topluluk katkısına açık ders içeriği, `14` §4-D2). Tam markdown gerekirse
 * ileride bir kütüphaneye geçilir; MVP için bu alt-küme yeter.
 */
export function MiniMarkdown({ markdown, className }: { markdown: string; className?: string }) {
  const lines = markdown.replace(/\r\n/g, "\n").split("\n");
  const blocks: ReactNode[] = [];
  let paragraph: string[] = [];
  let list: string[] = [];
  let quote: string[] = [];

  const flushParagraph = () => {
    if (paragraph.length > 0) {
      blocks.push(<p key={`p${blocks.length}`}>{renderInline(paragraph.join(" "))}</p>);
      paragraph = [];
    }
  };
  const flushList = () => {
    if (list.length > 0) {
      const items = list;
      blocks.push(
        <ul key={`ul${blocks.length}`}>
          {items.map((item, i) => (
            <li key={i}>{renderInline(item)}</li>
          ))}
        </ul>,
      );
      list = [];
    }
  };
  /**
   * Ardışık `> ` satırlarını TEK alıntıda birleştirir. Önceden her satır ayrı
   * <blockquote> üretiyordu: çok satırlı bir alıntı ekranda 4 ayrı kutuya
   * bölünüyordu (2026-07-20'de tarayıcıda görüldü). Paragraf/liste ile aynı desen.
   */
  const flushQuote = () => {
    if (quote.length > 0) {
      blocks.push(<blockquote key={`q${blocks.length}`}>{renderInline(quote.join(" "))}</blockquote>);
      quote = [];
    }
  };
  const flush = () => {
    flushParagraph();
    flushList();
    flushQuote();
  };

  for (const raw of lines) {
    const line = raw.trim();
    if (line === "") {
      flush();
    } else if (line.startsWith("### ")) {
      flush();
      blocks.push(<h4 key={`h${blocks.length}`}>{renderInline(line.slice(4))}</h4>);
    } else if (line.startsWith("## ")) {
      flush();
      blocks.push(<h3 key={`h${blocks.length}`}>{renderInline(line.slice(3))}</h3>);
    } else if (line.startsWith("> ")) {
      flushParagraph();
      flushList();
      quote.push(line.slice(2));
    } else if (line.startsWith("- ") || line.startsWith("* ")) {
      flushParagraph();
      flushQuote();
      list.push(line.slice(2));
    } else if (list.length > 0 && paragraph.length === 0) {
      // Liste öğesinin DEVAM satırı: kaynakta sarılmış uzun madde tek öğedir.
      // Önceden ayrı paragraf oluyordu ve madde ortadan ikiye bölünüyordu
      // (2026-07-20 tarayıcı kontrolü) — blockquote ile aynı sınıf hata.
      list[list.length - 1] += ` ${line}`;
    } else {
      flushList();
      flushQuote();
      paragraph.push(line);
    }
  }
  flush();

  return <div className={className}>{blocks}</div>;
}

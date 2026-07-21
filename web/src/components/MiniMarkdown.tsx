import { Fragment, type ReactNode } from "react";

/**
 * Bağlantı için İZİNLİ şemalar. Bunun dışındaki her şey (`javascript:`, `data:`,
 * `vbscript:`, `file:` …) bağlantıya çevrilmez — ham metin olarak kalır.
 */
const SAFE_SCHEME = /^(?:https?:|mailto:)/i;

/**
 * Boşluk ve kontrol karakterlerini atar. Bunlar şema gizlemekte kullanılır:
 * "java<TAB>script:alert(1)" tarayıcıda `javascript:` olarak çözülür. Karakter
 * KODUYLA süzülür — kaynak dosyada kontrol karakteri taşımamak için (regex
 * kaçış dizisi kazası yaşandı, 2026-07-22).
 */
function stripHiddenChars(raw: string): string {
  return Array.from(raw)
    .filter((ch) => {
      const code = ch.charCodeAt(0);
      return code > 0x20 && code !== 0x7f;
    })
    .join("");
}

type SafeLink = { href: string; newTab: boolean };

/**
 * Bağlantı hedefini doğrular; güvenli değilse `null` döner ve çağıran ham metni
 * gösterir. **Tıklanabilir saldırı yüzeyi oluşturmamak** bu fonksiyonun tek işi
 * (`11` §4 — girdi doğrulanır; içerik topluluk katkısına açık, `14` §4-D2).
 */
function safeHref(raw: string): SafeLink | null {
  const url = stripHiddenChars(raw);
  if (url === "") return null;
  // Uygulama içi mutlak yol. "//host" (protokole göreli) DIŞARIDA bırakılır —
  // dış siteye çıkar ama iç bağlantı gibi görünür.
  if (url.startsWith("/") && !url.startsWith("//")) return { href: url, newTab: false };
  if (SAFE_SCHEME.test(url)) return { href: url, newTab: /^https?:/i.test(url) };
  return null;
}

/** Satır içi belirteçler: `**kalın**` ve `[metin](hedef)`. */
const INLINE_TOKEN = /(\*\*[^*\n]+\*\*|\[[^\]\n]+\]\([^()\s]+\))/g;
const LINK_TOKEN = /^\[([^\]\n]+)\]\(([^()\s]+)\)$/;

/**
 * Satır içi vurgu ve bağlantıları React düğümlerine çevirir — **HTML enjeksiyonu YOK**
 * (`dangerouslySetInnerHTML` kullanılmaz, XSS-güvenli).
 *
 * Not: bağlantı metninin içinde `**kalın**` işlenmez (iç içe belirteç yok) —
 * ders içeriğinde ihtiyaç duyulmadı, alt-küme sade kalsın.
 */
function renderInline(text: string): ReactNode[] {
  return text.split(INLINE_TOKEN).map((part, i) => {
    if (part.startsWith("**") && part.endsWith("**") && part.length > 4) {
      return <strong key={i}>{part.slice(2, -2)}</strong>;
    }

    const link = LINK_TOKEN.exec(part);
    if (link) {
      const target = safeHref(link[2]);
      // Güvensiz hedef → bağlantı ÜRETİLMEZ, kaynak metin olduğu gibi görünür.
      if (!target) return <Fragment key={i}>{part}</Fragment>;
      return (
        <a
          key={i}
          href={target.href}
          {...(target.newTab ? { target: "_blank", rel: "noopener noreferrer" } : {})}
        >
          {link[1]}
          {target.newTab ? <span aria-hidden="true"> ↗</span> : null}
        </a>
      );
    }

    return <Fragment key={i}>{part}</Fragment>;
  });
}

/** `| --- | :--: |` biçimli hizalama satırı (tablonun başlıktan sonraki satırı). */
const TABLE_SEPARATOR = /^\|(?:\s*:?-+:?\s*\|)+$/;

/** `| a | b |` satırını hücrelere böler. Kaçışlı `\|` desteklenmez (içerikte gerekmedi). */
function splitRow(line: string): string[] {
  return line
    .replace(/^\|/, "")
    .replace(/\|$/, "")
    .split("|")
    .map((cell) => cell.trim());
}

/** Hizalama satırından sütun hizasını okur (`:--` sol · `:-:` orta · `--:` sağ). */
function alignOf(spec: string): "center" | "right" | undefined {
  const s = spec.trim();
  if (s.startsWith(":") && s.endsWith(":")) return "center";
  if (s.endsWith(":")) return "right";
  return undefined; // varsayılan sola — sayısal sütunda `--:` kullanılır (TR biçim, `CLAUDE.md` §8)
}

/**
 * Küçük, güvenli Markdown alt-kümesi renderer'ı (ders gövdeleri, `03` §C). Desteklenen:
 * `## `/`### ` başlık, `> ` alıntı, `- `/`* ` liste, boş satırla ayrılmış paragraf,
 * satır içi `**kalın**`, **`[metin](hedef)` bağlantı** ve **boru işaretli tablo** (T6.8).
 *
 * **`dangerouslySetInnerHTML` KULLANMAZ** — içerik güvenle React düğümüne çevrilir
 * (topluluk katkısına açık ders içeriği, `14` §4-D2). Bağlantı hedefi şema
 * beyaz-listesinden geçer; geçmezse bağlantı üretilmez (bkz. {@link safeHref}).
 *
 * Desteklenmeyen: kod bloğu, iç içe liste, resim, satır içi HTML. Tam markdown
 * gerekirse ileride bir kütüphaneye geçilir; ders içeriği için bu alt-küme yeter.
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

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i].trim();

    if (line === "") {
      flush();
    } else if (line.startsWith("### ")) {
      flush();
      blocks.push(<h4 key={`h${blocks.length}`}>{renderInline(line.slice(4))}</h4>);
    } else if (line.startsWith("## ")) {
      flush();
      blocks.push(<h3 key={`h${blocks.length}`}>{renderInline(line.slice(3))}</h3>);
    } else if (line.startsWith("|") && TABLE_SEPARATOR.test((lines[i + 1] ?? "").trim())) {
      // Tablo YALNIZCA başlık satırını hizalama satırı izlediğinde başlar; tek başına
      // `|` içeren satır eskisi gibi düz paragraf kalır (geriye dönük davranış).
      flush();
      const header = splitRow(line);
      const aligns = splitRow(lines[i + 1].trim()).map(alignOf);

      const rows: string[][] = [];
      let end = i + 2;
      while (end < lines.length && lines[end].trim().startsWith("|")) {
        rows.push(splitRow(lines[end].trim()));
        end++;
      }

      blocks.push(
        // Dar ekranda tabloyu SAYFA yerine kendi kabı kaydırır (yatay taşma yok).
        <div className="md-table-wrap" key={`tw${blocks.length}`}>
          <table>
            <thead>
              <tr>
                {header.map((cell, c) => (
                  <th key={c} style={aligns[c] ? { textAlign: aligns[c] } : undefined}>
                    {renderInline(cell)}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {rows.map((row, r) => (
                <tr key={r}>
                  {/* Sütun sayısı BAŞLIKTAN gelir: eksik hücre boş kalır, fazlası düşer. */}
                  {header.map((_, c) => (
                    <td key={c} style={aligns[c] ? { textAlign: aligns[c] } : undefined}>
                      {renderInline(row[c] ?? "")}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>,
      );
      i = end - 1;
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

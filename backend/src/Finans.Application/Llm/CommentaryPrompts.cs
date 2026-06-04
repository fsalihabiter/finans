namespace Finans.Application.Llm;

/// <summary>
/// Portföy yorum kartları için sistem promptu + few-shot örnekler + JSON çıktı şeması (T3.2 — 07 §3,§4).
/// <para>
/// <b>Tek cümlelik kural (CLAUDE.md §2, §3.1):</b> LLM hesap YAPMAZ; sana verilen hazır sayıları sade
/// Türkçe ile <b>eğitici/farkındalık yaratıcı</b> dille açıklar — alım-satım yönlendirmesi YASAK.
/// </para>
/// <para>
/// İçerik <b>statik</b> ve <b>cache'lenebilir</b>: T3.6 prompt caching kapsamında sistem promptu + few-shot
/// bloğu Anthropic cache'ine alınır. Bu yüzden burada zamanlama/kullanıcı verisi YOK — yalnız kalıcı
/// talimatlar ve örnekler.
/// </para>
/// </summary>
public static class CommentaryPrompts
{
    /// <summary>
    /// Eğitmen kişiliği + KESİN KURALLAR + iki few-shot örnek (doğru/yanlış davranış). Şema
    /// <c>tool_use</c> ile ayrıca zorlanır; bu metin kuralları/üslubu kalıcı kılar.
    /// </summary>
    public static readonly string SystemPrompt = """
        Sen bir finans EĞİTMENİsin, danışman DEĞİLsin. Görevin: sana verilen HAZIR sayıları sade
        Türkçe ile açıklamak ve kullanıcıda farkındalık yaratmak. Yatırım tavsiyesi vermek SPK
        lisansı gerektirir; bu uygulama tavsiye vermez (CLAUDE.md §2 — Türkiye yasal çerçevesi).

        KESİN KURALLAR:
        1. Sana verilen sayıların DIŞINDA rakam üretme. Hesap yapma. Yeni yüzde/oran/tutar üretme.
        2. "Al / sat / şundan çık / şuna gir / yükselir / düşer / şu hisseyi öner" gibi
           YÖNLENDİRME YAPMA. Geleceği TAHMİN ETME.
        3. Mevcut durumu ve geçmişi açıkla; "Bu metrik şu durumda yüksek/riskli sayılır",
           "Şu yoğunlaşma şu riski taşır" gibi GENEL ÇERÇEVELER ver.
        4. Türkçe yaz; sıfır finans bilgisi olan birine anlatır gibi sade ol. "Yoğunlaşma",
           "reel getiri" gibi terimleri ilk kullanımda kısaca açıkla.
        5. Çıktın YALNIZCA `structured_output` aracı çağrısı (JSON şeması) — düz metin YOK,
           giriş/kapanış cümlesi YOK, başka açıklama YOK.
        6. Kart sayısını 3-5 ile sınırla. Her kart bir TEMA (Genel Sağlık, Dağılım/Yoğunlaşma,
           Getiri, Reel Getiri/Enflasyon, Bağlam). Aynı temayı iki kez tekrar etme.
        7. `body` 60-220 karakter arası — 1-2 cümle. Akademik dil yok, samimi yok.

        ÖRNEK — DOĞRU (kullanılacak üslup):
        Girdi özeti: { "baseCurrency": "TRY", "totalValue": 641403, "returnRatio": 0.516,
                       "realReturnRatio": 0.21, "concentrationTop2": 0.84,
                       "allocation": [{"type":"Gold","weight":0.40},
                                      {"type":"Bes","weight":0.44}, ...] }
        Doğru kart (yoğunlaşma):
          { "emoji": "⚖️", "title": "Dağılımın Yoğun",
            "body": "Portföyünün yaklaşık %84'ü iki kalemde (Altın ve BES). Yoğunlaşma demek
                    bu iki varlık aynı anda değer kaybederse portföyünün büyük kısmının etkilenmesi
                    demek. Bu doğru ya da yanlış değil; bir farkındalık.",
            "tags": ["yoğunlaşma", "dağılım"] }
        Doğru kart (reel getiri):
          { "emoji": "📉", "title": "Enflasyon Sonrası Getiri",
            "body": "Nominal getirin %51,6 olsa da enflasyon etkisi düşüldüğünde 'reel' getirin
                    %21. Yani satın alma gücün açısından kazancın bu — büyük bir fark.",
            "tags": ["reel-getiri"] }

        ÖRNEK — YANLIŞ (asla yapma):
        - "Altından çıkıp hisseye geçmelisin." (YASAK: yönlendirme)
        - "Bu seviyeden BES eklemek mantıklı olur." (YASAK: tavsiye)
        - "Önümüzdeki ay USD/TRY yükselir." (YASAK: tahmin)
        - "Toplam değerin aslında 650.000." (YASAK: yeni rakam üretme)
        """;

    /// <summary>
    /// `structured_output` araç çağrısı için JSON şeması (07 §4). Anthropic <c>input_schema</c>
    /// bunu modele dayatır; gelen JSON üst katmanda (T3.4) yine güvenli parse edilir.
    /// </summary>
    public static readonly string CommentaryJsonSchema = """
        {
          "type": "object",
          "properties": {
            "cards": {
              "type": "array",
              "minItems": 3,
              "maxItems": 5,
              "items": {
                "type": "object",
                "properties": {
                  "emoji": {
                    "type": "string",
                    "description": "Karta görsel kimlik veren tek emoji (ör. 🧭, ⚖️, 📈, 📉, 💡)."
                  },
                  "title": {
                    "type": "string",
                    "minLength": 2,
                    "maxLength": 40,
                    "description": "Kısa, somut başlık (Türkçe)."
                  },
                  "body": {
                    "type": "string",
                    "minLength": 60,
                    "maxLength": 220,
                    "description": "1-2 cümle açıklama (Türkçe, sade, tavsiye YOK)."
                  },
                  "meter": {
                    "type": "object",
                    "description": "Opsiyonel: kart için sayısal gösterge çubuğu (0..1).",
                    "properties": {
                      "value": { "type": "number", "minimum": 0, "maximum": 1 },
                      "lowLabel": { "type": "string", "maxLength": 24 },
                      "highLabel": { "type": "string", "maxLength": 24 }
                    },
                    "required": ["value", "lowLabel", "highLabel"]
                  },
                  "tags": {
                    "type": "array",
                    "description": "Opsiyonel kısa etiketler (Türkçe, küçük harf).",
                    "items": { "type": "string", "maxLength": 24 },
                    "maxItems": 4
                  }
                },
                "required": ["emoji", "title", "body"]
              }
            }
          },
          "required": ["cards"]
        }
        """;
}

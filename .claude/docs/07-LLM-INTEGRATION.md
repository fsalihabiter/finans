# 07 — LLM Entegrasyonu (LLM Integration)

> `CLAUDE.md` § 3.1'in mühendislik karşılığı. **Tek cümlelik kural:** LLM'e ham
> sayı verip "hesapla" denmez; LLM **kodun hesapladığı hazır sayıyı eğitici dille
> yorumlar**, yapılandırılmış JSON döner, güvenli parse edilir. Faz 3-4.

---

## 1. Sorumluluk Sınırı

```
[Kod]  Tüm sayı: değer, kâr, getiri%, dağılım%, reel getiri, yoğunlaşma
   │   (decimal, deterministik, testli)
   ▼
[LLM]  Bu HAZIR sayıları al → sade Türkçe ile AÇIKLA / yorumla / çerçevele
       (yeni rakam ÜRETME, tavsiye VERME)
   │   structured JSON
   ▼
[Mobil] JSON kartları render et + disclaimer
```

**LLM'in ASLA yapmayacağı:**
- Yeni sayı uydurmak (kendisine verilmeyen rakam üretmek).
- "Al / sat / şundan çık / bu yükselir" demek (NFR-2, `CLAUDE.md` § 2).
- Geleceği tahmin etmek.

**LLM'in yapacağı:** "Bu metrik şu durumda yüksek/riskli sayılır", "Portföyünün
%84'ü iki varlıkta — bu yoğunlaşma şu riski taşır", "Reel getiri şu demektir".

---

## 2. Sağlayıcı Seçim Kriterleri (✅ KARAR: **Anthropic Claude** — 2026-06-04)

| Kriter | Ağırlık | Not |
|--------|---------|-----|
| Türkçe kalitesi | Yüksek | Çıktı doğrudan kullanıcıya gider |
| Talimat takibi | Yüksek | "Tavsiye verme" sınırına uymalı |
| Yapılandırılmış çıktı (JSON / tool/function calling) | Yüksek | Güvenli parse için şart |
| Maliyet | Orta | Geliştirmede düşük (cache + tetikleme disiplini) |
| Gecikme | Orta | Cache'lendiği için kritik değil |

### Karar gerekçesi
- **Türkçe kalitesi** yüksek; talimat takibi ("yeni rakam üretme, tavsiye verme") en
  güvenilir sağlayıcılardan biri.
- **Yapılandırılmış çıktı:** `tool_use` ile JSON şema **zorlanabilir** (Anthropic'in input_schema
  alanı şemayı modele dayatır → T3.3 parse güvenli).
- **Maliyet:** Faz 3 başlangıç modeli `claude-sonnet-4-6` (yorum kalitesi); cache + günde bir
  üretim (NFR-9) ile aylık maliyet düşük. Sıkışırsa `claude-haiku-4-5` ile değiştirilebilir
  (model env değişikliği yeter — `Llm:Model`).
- **Prompt caching:** sistem promptu + few-shot tekrar tekrar gönderilir → cache token maliyetini
  düşürür (T3.6 ile birlikte).
- **Soyutlama:** `ILlmClient` (`Finans.Application.Llm`) provider-neutral; sağlayıcı değişirse
  yalnızca `Infrastructure.Llm.AnthropicLlmClient` değişir. SDK YOK — küçük REST yüzeyi (tek
  endpoint, az alan) doğrudan typed HttpClient'la kullanılıyor → bağımlılık minimum.

### KVKK çerçevesi (**zorunlu — her LLM çağrısında**)
LLM'e gönderilen her içerikte **kişisel veri yasak**: `UserId`, isim, e-posta, hesap numarası,
açık metin pozisyon kimliği vb. **GİTMEZ**. Yalnız **anonim portföy özeti** (oranlar, kategoriler,
toplam değer/getiri yüzdesi) gider. Anonimleştirme **servis katmanının** (T3.3 `LlmCommentaryService`)
sorumluluğu; `ILlmClient` bunu varsayar (sözleşme yorumda yazılı).

### Yapılandırma
- `Llm:Provider` (varsayılan `Anthropic`)
- `Llm:ApiKey` — env (`Llm__ApiKey`) veya User Secrets; **repoda asla yok** (11 §6)
- `Llm:Model` (varsayılan `claude-sonnet-4-6`)
- `Llm:TimeoutSeconds` (20), `Llm:BaseUrl` (`https://api.anthropic.com/`)

API anahtarı boşsa DI `NoopLlmClient`'ı bağlar → her çağrı `Fail("llm_not_configured")` döner;
üst katman (07 §5) cache veya düz metin fallback ile devam eder → **uygulama çökmez (NFR-5).**

---

## 3. Prompt Tasarımı

### Sistem promptu (iskelet)
```
Sen bir finans EĞİTMENİsin, danışman DEĞİLsin. Görevin: sana verilen HAZIR
sayıları sade Türkçe ile açıklamak ve farkındalık yaratmak.

KESİN KURALLAR:
- Sana verilen sayıların DIŞINDA rakam üretme. Hesap yapma.
- "Al / sat / şundan çık / şuna gir / yükselir / düşer" gibi yönlendirme YAPMA.
- Geleceği tahmin etme; mevcut durumu açıkla.
- Türkçe, sade, sıfır finans bilgisi olan birine anlatır gibi yaz.
- Çıktıyı YALNIZCA aşağıdaki JSON şemasında ver, başka metin ekleme.
```

### Few-shot (1-2 örnek)
Doğru örnek (yoğunlaşma): "Portföyünün %84'ü Altın ve BES'te. İkisi de uzun
vade için makul kabul edilir, ancak ağırlığın iki kalemde olması, bu varlıklar
değer kaybettiğinde portföyünün tümünü birlikte etkiler."
Yanlış örnek (verme!): "Altından çıkıp hisseye geçmelisin." ← yasak.

### Kullanıcı promptu (veri)
Kodun hesapladığı özet **JSON olarak** verilir:
```json
{
  "baseCurrency": "TRY", "totalValue": 641403, "returnRatio": 0.516,
  "realReturnRatio": 0.21,
  "allocation": [
    {"type":"Gold","weight":0.405}, {"type":"Bes","weight":0.436},
    {"type":"Fx","weight":0.150}, {"type":"Cash","weight":0.009}
  ],
  "concentrationTop2": 0.841
}
```

---

## 4. İstenen Çıktı Şeması (structured)

`04` § 6 ile aynı `commentary` kart şeması:
```json
{
  "cards": [
    {
      "emoji": "🧭", "title": "Genel Sağlık",
      "body": "string (Türkçe, tavsiye yok)",
      "meter": { "value": 0.72, "lowLabel": "Yoğunlaşmış", "highLabel": "Çok dağınık" },
      "tags": ["opsiyonel", "etiketler"]
    }
  ]
}
```
- `meter` ve `tags` opsiyonel.
- Mümkünse sağlayıcının **JSON mode / tool calling** özelliğiyle şemayı zorla.

---

## 5. Güvenli Parse & Fallback (FR-3.2 — ZORUNLU)

```
1. LLM yanıtını al.
2. JSON parse dene.
   - Başarılı + şema geçerli → kartları döndür.
   - Başarısız → 
       a) son başarılı cache'lenmiş yorumu döndür, yoksa
       b) düz metin fallback kartı ("Yorum şu an üretilemedi.") döndür.
3. HER DURUMDA HTTP 200 + uygulama çökmez.
```
> Bu mantığın **birim testi var** (`06` § 4): bozuk JSON, eksik alan, boş yanıt.

---

## 6. Cache & Maliyet Kontrolü (NFR-9)

- Yorum **portföy değiştiğinde** veya **günde bir kez** üretilir; her ekran
  açılışında yeni istek **atılmaz**.
- Cache anahtarı: portföy özetinin hash'i (`totalValue`, dağılım, tarih).
- Hisse açıklaması (`/explain`) sembol + metrik snapshot bazında cache'lenir.
- Anthropic SDK kullanılıyorsa sistem promptu + few-shot **prompt cache**'e
  alınır (tekrarlayan token maliyetini düşürür).

---

## 7. Çıktı Güvenlik Filtresi (kuşak-2 koruma)

Prompt korkuluğuna ek, **çıktıda** yasaklı kalıp taraması (savunma derinliği):
- "al", "sat", "alın", "satın", "yükselir", "düşer" gibi yönlendirici kalıpları
  içeren kartı işaretle → gerekirse fallback'e düş veya kartı gizle.
- Bu filtre **basit kural tabanlı** başlar; amaç prompt'un kaçırdığını yakalamak.

> Not: Aşırı agresif filtre meşru eğitim metnini de kesebilir ("enflasyon
> yükselirse" gibi). Filtreyi **yönlendirme bağlamına** odakla, kelime avına değil.

---

## 8. Hisse Açıklama (Faz 4) — fark

Girdi: `StockDataService`'in çektiği metrikler (F/K, PD/DD, temettü, büyüme) +
sektör bağlamı (`above/high/low/positive`). LLM **"bu rakamlar ne anlatıyor"**
diye açıklar (taslaktaki Apple örneği gibi), "iyi/kötü/al" demez. Aynı JSON
şeması, aynı güvenli parse, aynı disclaimer.

# 04 — API Sözleşmesi (API Contract)

> REST + JSON. Tüm parasal değerler backend'de `decimal` hesaplanır, JSON'da
> **sayı** olarak döner (string değil); **TR formatlama mobilde** yapılır
> (NFR-7). Tüm tarihler ISO 8601 UTC. Sürüm öneki: `/api` (gerekirse `/api/v1`).

---

## 1. Genel Kurallar

- **Base path:** `/api`
- **Content-Type:** `application/json; charset=utf-8`
- **Kimlik:** Faz 1'de yok (tekil kullanıcı). Faz 5'te `Authorization: Bearer`.
- **Para alanları:** ham `decimal` sayı (örn. `641403.00`). Formatlama istemcide.
- **Yüzdeler:** oran olarak (`0.516` = %51,6) **veya** açıkça `Percent` sonekli
  alanda yüzde puanı (`51.6`). **Karar:** oran (`0.516`) döndür, mobil `%` ile
  formatlar. Tutarlı kal.
- **Null politikası:** Hesaplanamayan alan `null` döner (örn. nakit getiri).

---

## 2. Hata Formatı (tüm endpoint'ler ortak)

İstemciye **asla stack trace gitmez** (NFR-4). Standart gövde:

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Miktar 0'dan büyük olmalı.",
    "details": [{ "field": "quantity", "issue": "must_be_positive" }]
  }
}
```

| HTTP | code | Ne zaman |
|------|------|----------|
| 400 | `VALIDATION_ERROR` | Geçersiz girdi |
| 404 | `NOT_FOUND` | Kayıt yok |
| 409 | `CONFLICT` | Çakışma (örn. aynı varlıkta ikinci pozisyon) |
| 502 | `UPSTREAM_ERROR` | Dış API (fiyat/LLM) hatası — fallback denendi |
| 500 | `INTERNAL_ERROR` | Beklenmeyen (mesaj jenerik) |

> `message` **Türkçe ve kullanıcıya gösterilebilir** olmalı.

---

## 3. Endpoint'ler — Faz 0

### `GET /api/health`
```json
200 → { "status": "ok" }
```

---

## 4. Endpoint'ler — Faz 1 (Portföy MVP)

### `GET /api/portfolio/summary`
Query: `baseCurrency` (ops., yoksa kullanıcı tercihi).

```json
200 →
{
  "baseCurrency": "TRY",
  "totalValue": 641403.00,
  "totalCost": 422970.00,
  "netProfit": 218433.00,
  "returnRatio": 0.516,
  "realReturnRatio": 0.21,
  "allocation": [
    { "assetType": "Gold", "name": "Altın", "value": 260000.00, "weight": 0.405 },
    { "assetType": "Bes",  "name": "BES",   "value": 279378.00, "weight": 0.436 },
    { "assetType": "Fx",   "name": "Dolar", "value": 96000.00,  "weight": 0.150 },
    { "assetType": "Cash", "name": "Nakit", "value": 6025.00,   "weight": 0.009 }
  ],
  "asOf": "2026-05-29T18:00:00Z"
}
```
> Tüm sayılar backend hesabı (`PortfolioCalculationService`). Mobil yalnızca
> formatlar. `realReturnRatio` enflasyon verisi yoksa `null`.

### `GET /api/holdings`
```json
200 →
[
  {
    "id": "…", "assetType": "Gold", "name": "Altın", "symbol": "XAU",
    "currency": "TRY", "unit": "gram",
    "quantity": 40.0, "avgCost": 4546.275, "currentPrice": 6500.00,
    "totalCost": 181851.00, "currentValue": 260000.00,
    "profit": 78149.00, "returnRatio": 0.43, "weight": 0.405,
    "bes": null
  }
]
```
BES kalemi için `bes` alanı dolu döner:
```json
"bes": { "ownContribution": 148500.00, "stateContribution": 44550.00, "vestingState": "PartiallyVested" }
```

### `GET /api/holdings/{id}`
Tekil holding detayı (yukarıdaki şema + işlem listesi opsiyonel).

### `POST /api/holdings`
```json
İstek →
{
  "assetType": "Gold", "name": "Altın", "symbol": "XAU",
  "currency": "TRY", "unit": "gram",
  "transaction": { "type": "Buy", "quantity": 40, "unitPrice": 4546.275, "date": "2025-01-10T00:00:00Z" }
}
201 → (oluşan holding, GET /holdings/{id} şemasıyla)
```
> Holding ilk işlemiyle birlikte oluşur; ort. maliyet işlemden türetilir (`03` §5).

### `POST /api/holdings/{id}/transactions`
Mevcut pozisyona alış/satış ekler (ort. maliyet & miktar yeniden hesaplanır).
```json
İstek → { "type": "Buy", "quantity": 10, "unitPrice": 5000, "date": "…" }
200 → (güncellenmiş holding)
```

### `PUT /api/holdings/{id}`
Düzenle (örn. güncel fiyatı elle güncelle — Faz 1, FR-1.8):
```json
İstek → { "currentPrice": 6700.00 }
200 → (güncellenmiş holding)
```

### `DELETE /api/holdings/{id}`
```json
204 (gövde yok)
```

### `GET /api/settings` · `PUT /api/settings`
Baz para birimi tercihi (FR-1.4):
```json
GET 200 → { "baseCurrency": "TRY" }
PUT İstek → { "baseCurrency": "USD" } → 200
```

---

## 5. Endpoint'ler — Faz 2 (Canlı Fiyat & Nudge)

### `GET /api/prices?symbols=XAU,USD,AAPL`
```json
200 →
{
  "prices": [
    { "symbol": "XAU", "price": 6500.00, "currency": "TRY", "asOf": "…", "stale": false },
    { "symbol": "USD", "price": 48.00,   "currency": "TRY", "asOf": "…", "stale": true }
  ]
}
```
> `stale: true` → dış API çökmüş, son bilinen fiyat ("yaklaşık" etiketi, FR-2.5).

### `GET /api/portfolio/nudges`
Kural tabanlı eğitici notlar (FR-2.4):
```json
200 →
{
  "nudges": [
    { "id": "low-cash", "icon": "💡", "title": "Biliyor muydun?",
      "body": "Nakitin portföyünün yalnızca %0,9'u. ...", "severity": "info" }
  ]
}
```

---

## 6. Endpoint'ler — Faz 3 (LLM Yorum)

### `GET /api/portfolio/commentary`
Backend, **hazır sayıları** LLM'e yorumlatır (yeni sayı üretmez), JSON kart döner.
```json
200 →
{
  "generatedAt": "2026-05-29T18:00:00Z",
  "cached": true,
  "disclaimer": "Bu yorumlar eğitim amaçlıdır, yatırım tavsiyesi değildir.",
  "cards": [
    { "emoji": "🧭", "title": "Genel Sağlık", "body": "Portföyün dört ...",
      "meter": { "value": 0.72, "lowLabel": "Yoğunlaşmış", "highLabel": "Çok dağınık" } },
    { "emoji": "⚖️", "title": "Yoğunlaşma", "body": "...",
      "tags": ["Altın %40,5", "BES %43,6", "→ birlikte %84"] }
  ]
}
```
> JSON şeması ve "tavsiye değil" korkulukları [`07-LLM-INTEGRATION.md`](07-LLM-INTEGRATION.md)'te.
> Parse hatasında 200 + `cards: []` + fallback metin (uygulama çökmez, FR-3.2).

---

## 7. Endpoint'ler — Faz 4 (Hisse Analiz)

### `GET /api/stocks/{symbol}/metrics`
```json
200 →
{
  "symbol": "AAPL", "name": "Apple Inc.", "exchange": "NASDAQ", "currency": "USD",
  "price": 201.40, "changeRatio": 0.012,
  "metrics": {
    "peRatio": 28.4, "pbRatio": 44.1, "dividendYield": 0.0052, "earningsGrowth": 0.091
  },
  "sectorContext": { "peRatio": "above", "pbRatio": "high", "dividendYield": "low", "earningsGrowth": "positive" }
}
404 → { "error": { "code": "NOT_FOUND", "message": "Bu sembol için veri bulunamadı." } }
```

### `GET /api/stocks/{symbol}/explain`
Metriklerin **ne anlama geldiğini** açıklatır (tavsiye yok), `commentary` ile aynı
kart şeması.

---

## 7.5 Endpoint'ler — Eğitim Modülü (Faz 5)

> Veri modeli `03` §C. İçerik herkese açık (okuma); ilerleme kullanıcıya özel
> (`UserId` kapsamı, `11` §3).

### `GET /api/education/tracks`
```json
200 → [ { "id":"…","slug":"temeller","title":"Temeller","level":"Beginner","lessonCount":5 } ]
```

### `GET /api/education/tracks/{slug}/lessons`
```json
200 →
[
  { "id":"…","slug":"enflasyon-ve-reel-getiri","order":1,"title":"Enflasyon ve Reel Getiri",
    "summary":"Param büyüdü mü, yoksa sadece rakam mı?","estimatedMinutes":4,
    "status":"Completed","progressPercent":100,"locked":false },
  { "id":"…","order":4,"title":"Risk ve Getiri İlişkisi","status":"InProgress",
    "progressPercent":0,"locked":false },
  { "id":"…","order":5,"title":"Bileşik Getirinin Gücü","status":"NotStarted",
    "progressPercent":0,"locked":true }
]
```
> `status`/`progressPercent` = kullanıcının `UserLessonProgress`'i; `locked` =
> ön-koşul derslerden **türetilir** (`03` §C).

### `GET /api/education/lessons/{slug}`
Tek ders: `bodyMarkdown`, `sections[]`, bağlı `quiz` (varsa), `conceptTags[]`.

### `PUT /api/education/lessons/{id}/progress`
```json
İstek → { "status":"Completed", "progressPercent":100 }
200 → (güncel progress)   // UserLessonProgress upsert (UserId kapsamlı)
```

### `POST /api/education/quizzes/{id}/attempts`
```json
İstek → { "answers":[ { "questionId":"…","selectedOptionIds":["…"] } ] }
200 → { "score":67, "passed":true, "results":[ { "questionId":"…","correct":true,"explanation":"…" } ] }
```

### `GET /api/education/lessons/by-concept/{conceptKey}`
Analiz/Hisse kartından derin bağlantı (örn. `diversification` → "Çeşitlendirme").

---

## 8. Sürümleme & Geriye Uyum

- Kıran değişiklikte `/api/v2`. Faz 1-4 boyunca `/api` (v1 implicit) yeterli.
- Yeni alan eklemek kıran değişiklik değil; mobil bilmediği alanı yok sayar.
- DTO'lar backend'de `Finans.Application` altında; mobilde TypeScript tip
  karşılıkları `mobile/src/api/types.ts` (elle veya OpenAPI'den üret).

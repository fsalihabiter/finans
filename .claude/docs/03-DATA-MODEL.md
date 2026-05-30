# 03 — Veri Modeli (Data Model)

> `CLAUDE.md` § 5 + `ROADMAP.md` Faz 0 taslağını **uygulanabilir, kolon-düzeyinde**
> bir şemaya dönüştürür. Üç alan: **(A)** portföy çekirdeği, **(B)** kimlik &
> güvenlik & audit, **(C)** eğitim modülü. Sonda **kapsamlı, tutarlı seed verisi**
> (§ 12). İlgili: `02` (mimari), `04` (API), `11` (güvenlik), `12` (audit).

---

## 1. Genel Konvansiyonlar (tüm tablolar)

| Konu | Karar | Gerekçe |
|------|-------|---------|
| **PK** | `uuid` (tercihen **UUIDv7** — zaman-sıralı, indeks lokalitesi iyi) | Dağıtık üretim, tahmin edilemez id (IDOR yüzeyini küçültür, `11` §3) |
| **Para / miktar** | `numeric(18,6)` (.NET `decimal`) | NFR-1; 6 ondalık gram altın/kripto'ya yeter. **Asla float/double.** |
| **Oran / yüzde** | **Saklanmaz** — runtime'da hesaplanır | Tek doğruluk kaynağı hesap servisi (`02` §1) |
| **Tarih/saat** | `timestamptz`, **UTC** (`*Utc` sonek) | Çoklu bölge/tutarlılık |
| **Zaman damgaları** | `CreatedAtUtc` (zorunlu) + `UpdatedAtUtc` (değişen tablolar) | Denetlenebilirlik |
| **Eş zamanlılık** | Optimistic concurrency: PostgreSQL `xmin` (EF `IsRowVersion`) | Kayıp güncelleme önlenir |
| **Soft delete** | Kullanıcı-içeriği tablolarında `IsDeleted` + `DeletedAtUtc` (varsayılan filtre dışı). **KVKK "verimi sil"** ise **hard delete** (cascade) | Yanlışlıkla silmeye karşı + yasal silme hakkı (`11` §7) |
| **Enum** | DB'de `varchar` + uygulama enum'u (CHECK ile allow-list) | Taşınabilir, okunur; § 2 |
| **İsimlendirme** | Tablo PascalCase çoğul, kolon PascalCase; FK `<Entity>Id` | EF Core konvansiyonu |
| **String** | `text` (PostgreSQL'de performans farkı yok); gerekirse CHECK uzunluk | Esneklik |

---

## 2. Enumerasyonlar (allow-list)

```
AssetType        : Gold | Fx | Stock | Fund | Bes | Cash        (gelecek: RealEstate, Crypto)
TransactionType  : Buy | Sell
VestingState     : NotVested | PartiallyVested | Vested
CurrencyCode     : TRY | USD | EUR                               (ISO 4217; allow-list)
PriceSource      : Manual | <providerKey>                        (örn. "frankfurter", "yfinance")
UserRole         : User | Admin
AuditAction      : Login | Logout | Create | Update | Delete | AccessDenied | Export | PasswordChange
LessonLevel      : Beginner | Intermediate | Advanced
LessonStatus     : NotStarted | InProgress | Completed           (Locked = ön-koşuldan TÜRETİLİR, saklanmaz)
QuizQuestionType : SingleChoice | MultipleChoice | TrueFalse
```

> `Currency` allow-list güvenlik için de önemli (girdi doğrulama, `11` §4).

---

## 3. ER Genel Bakış

```
(B) Kimlik/Güvenlik          (A) Portföy Çekirdeği                 (C) Eğitim
─────────────────            ──────────────────────                ───────────────
Users 1─* RefreshTokens      Users 1─* Holdings *─1 Assets         LearningTracks 1─* Lessons
Users *─* Roles (UserRoles)  Holdings 1─* Transactions             Lessons 1─* LessonSections
Users 1─* AuditLogs          Holdings 1─0..1 BesDetails            Lessons *─* Lessons (Prerequisites)
                             Assets 1─* PriceSnapshots             Lessons *─* ConceptTags
                             FxRates (kur çiftleri)                Lessons 1─0..1 Quizzes
                             InflationRates (dönemsel)             Quizzes 1─* QuizQuestions 1─* QuizOptions
                                                                   Users 1─* UserLessonProgress *─1 Lessons
                                                                   Users 1─* UserQuizAttempts *─1 Quizzes
```

---

## A. PORTFÖY ÇEKİRDEĞİ

### `Assets` — varlık *tanımı* (kullanıcıdan bağımsız katalog)
| Kolon | Tip | Null | Not |
|-------|-----|------|-----|
| Id | uuid PK | — | |
| Type | varchar(20) | hayır | `AssetType` (CHECK) |
| Name | text | hayır | "Altın (gram)", "Apple Inc." |
| Symbol | varchar(20) | evet | `XAU`, `USD`, `AAPL`; nakit'te null |
| Unit | varchar(20) | hayır | `gram`, `adet`, `birim`, `TRY` |
| **PricingCurrency** | varchar(3) | hayır | Fiyat/maliyetin **ifade edildiği** pb. Altın→TRY, USD varlığı→TRY, AAPL→USD. (`CurrencyCode`) |
| Exchange | varchar(20) | evet | `NASDAQ`, `BIST` (hisse) |
| IsActive | boolean | hayır | varsayılan `true` |
| CreatedAtUtc | timestamptz | hayır | |

> **Önemli ayrım:** `Unit` = miktarın birimi (gram/adet/USD); `PricingCurrency`
> = birim fiyatın yazıldığı para birimi. Dolar varlığında Unit=`USD`,
> PricingCurrency=`TRY` (2.000 $ × 48 ₺). Bu, kur dönüşümünü netleştirir (`04`).

### `Holdings` — kullanıcının bir varlıktaki *pozisyonu*
| Kolon | Tip | Null | Not |
|-------|-----|------|-----|
| Id | uuid PK | — | |
| UserId | uuid FK→Users | hayır | **Her erişim bununla kapsanır** (`11` §3) |
| AssetId | uuid FK→Assets | hayır | |
| Quantity | numeric(18,6) | hayır | **Transactions'tan türetilir** (cache) |
| AvgCost | numeric(18,6) | hayır | **Türetilir**; PricingCurrency cinsinden birim maliyet |
| CurrentPrice | numeric(18,6) | evet | Faz 1 elle; Faz 2 PriceSnapshots'tan (cache) |
| Notes | text | evet | |
| IsDeleted | boolean | hayır | soft delete |
| CreatedAtUtc / UpdatedAtUtc | timestamptz | hayır/evet | |
| xmin | (sistem) | — | concurrency token |

Kısıt: **`UNIQUE(UserId, AssetId)` (IsDeleted=false)**, `Quantity >= 0`,
`AvgCost >= 0`. İndeks: `(UserId)`, `(UserId, AssetId)`.

### `Transactions` — alış/satış (DOĞRULUK KAYNAĞI)
| Kolon | Tip | Null | Not |
|-------|-----|------|-----|
| Id | uuid PK | — | |
| HoldingId | uuid FK→Holdings | hayır | |
| Type | varchar(10) | hayır | `TransactionType` (CHECK) |
| Quantity | numeric(18,6) | hayır | `> 0` |
| UnitPrice | numeric(18,6) | hayır | PricingCurrency cinsinden; `>= 0` |
| Fee | numeric(18,6) | hayır | varsayılan `0` (komisyon/masraf) |
| TransactedAtUtc | timestamptz | hayır | işlem tarihi |
| Note | text | evet | |
| CreatedAtUtc | timestamptz | hayır | |

İndeks: `(HoldingId, TransactedAtUtc)`.

### `BesDetails` — BES'e özel (Holdings 1—0..1)
| Kolon | Tip | Null | Not |
|-------|-----|------|-----|
| Id | uuid PK | — | |
| HoldingId | uuid FK→Holdings | hayır | **UNIQUE** |
| OwnContribution | numeric(18,6) | hayır | Kendi katkı payı (maliyet bileşeni) |
| StateContribution | numeric(18,6) | hayır | **Devlet katkısı — AYRI** (FR-1.5) |
| VestingState | varchar(20) | hayır | `VestingState` (CHECK) |
| ProviderName | text | evet | BES şirketi |
| JoinedAtUtc | timestamptz | evet | |

> **Modelleme kararı (BES getirisi):** Getiri maliyet tabanı = **OwnContribution
> + StateContribution** (taslaktaki +%88 buradan gelir). Devlet katkısı UI'da
> **ayrı satır** gösterilir ama getiri tabanına dahildir. `Holdings.AvgCost`
> BES için bu toplamı (notional 1 "pay" üzerinden) yansıtır.

### `PriceSnapshots` — fiyat geçmişi (reel getiri & senaryo)
| Kolon | Tip | Null | Not |
|-------|-----|------|-----|
| Id | uuid PK | — | |
| AssetId | uuid FK→Assets | hayır | |
| Price | numeric(18,6) | hayır | PricingCurrency cinsinden |
| Source | varchar(40) | hayır | `PriceSource` |
| AsOfUtc | timestamptz | hayır | |
| CreatedAtUtc | timestamptz | hayır | |

İndeks: `(AssetId, AsOfUtc DESC)` (son fiyat hızlı). Kısıt: `Price >= 0`.

### `FxRates` — kur çevrimi (deterministik, NFR-1)
| Kolon | Tip | Null | Not |
|-------|-----|------|-----|
| Id | uuid PK | — | |
| FromCurrency / ToCurrency | varchar(3) | hayır | `CurrencyCode` |
| Rate | numeric(18,6) | hayır | `1 From = Rate × To`; `> 0` |
| Source | varchar(40) | hayır | `PriceSource` |
| AsOfUtc | timestamptz | hayır | |

İndeks: `(FromCurrency, ToCurrency, AsOfUtc DESC)`.

### `InflationRates` — reel getiri için (dönemsel)
| Kolon | Tip | Null | Not |
|-------|-----|------|-----|
| Id | uuid PK | — | |
| PeriodStartUtc / PeriodEndUtc | timestamptz | hayır | dönem aralığı |
| AnnualRate | numeric(9,6) | hayır | örn. `0.380000` = %38 (ondalık oran) |
| Source | varchar(40) | hayır | **TÜİK** (resmi) — seed'de örnek, prod'da gerçek veri |
| CreatedAtUtc | timestamptz | hayır | |

> Reel getiri = `(1 + nominal) / (1 + enflasyon) − 1` (`CLAUDE.md` §6). Bu tablo
> nominal getiriyi reele çevirmek için kullanılır.

---

## B. KİMLİK, GÜVENLİK & AUDIT

> Tam kullanım Faz 5 (gerçek kimlik), **ama şema şimdiden modellenir** ki sonra
> sancısız. Faz 1-4'te tekil seed kullanıcı. İlgili: `11` (güvenlik), `12` (audit).

### `Users`
| Kolon | Tip | Null | Not |
|-------|-----|------|-----|
| Id | uuid PK | — | |
| Email | citext | evet | **UNIQUE**; Faz 5'te zorunlu (PII — minimum tut, `11` §7) |
| PasswordHash | text | evet | **Argon2id** (Faz 5); asla düz parola |
| DisplayName | text | evet | "Yatırımcı" |
| BaseCurrency | varchar(3) | hayır | `CurrencyCode`; varsayılan `TRY` |
| IsActive | boolean | hayır | |
| CreatedAtUtc / UpdatedAtUtc | timestamptz | hayır/evet | |

### `Roles` / `UserRoles`
- `Roles(Id, Name)` — `UserRole` değerleri (`User`, `Admin`).
- `UserRoles(UserId, RoleId)` — çoklu (PK çift). Admin yetkisi (`11` §3).

### `RefreshTokens`
| Kolon | Tip | Null | Not |
|-------|-----|------|-----|
| Id | uuid PK | — | |
| UserId | uuid FK→Users | hayır | |
| TokenHash | text | hayır | token'ın **hash'i** saklanır, düz değil |
| ExpiresAtUtc | timestamptz | hayır | |
| RevokedAtUtc | timestamptz | evet | iptal/rotasyon (`11` §2) |
| CreatedByIp | inet | evet | |
| CreatedAtUtc | timestamptz | hayır | |

İndeks: `(UserId)`, `(TokenHash)`.

### `AuditLogs` — güvenlik & inkâr-edilemezlik (`12` §7)
| Kolon | Tip | Null | Not |
|-------|-----|------|-----|
| Id | uuid PK | — | |
| UserId | uuid FK→Users | evet | başarısız girişte null olabilir |
| Action | varchar(30) | hayır | `AuditAction` |
| EntityType / EntityId | text/uuid | evet | hangi kayıt |
| Result | varchar(20) | hayır | `Success` / `Denied` / `Failure` |
| IpAddress | inet | evet | |
| AtUtc | timestamptz | hayır | |

> **Salt-ekleme** (append-only), uzun retention. **PII/sır yazılmaz** (`12` §3).

---

## C. EĞİTİM MODÜLÜ

> Taslaktaki "Eğitim" sekmesi (dersler + ilerleme çubuğu + sıralı/kilitli durum)
> için tam model. İçerik **DB'de** tutulur (ders gövdesi Markdown). Kullanım
> Faz 5 (`ROADMAP`), ama model + seed şimdi hazır.

### `LearningTracks` — ders kümesi (örn. "Temeller")
| Kolon | Tip | Null | Not |
|-------|-----|------|-----|
| Id | uuid PK | — | |
| Slug | varchar(80) | hayır | **UNIQUE**, `temeller` |
| Title | text | hayır | "Temeller" |
| Description | text | evet | |
| Level | varchar(20) | hayır | `LessonLevel` |
| OrderIndex | int | hayır | sıralama |
| IsPublished | boolean | hayır | |
| CreatedAtUtc / UpdatedAtUtc | timestamptz | hayır/evet | |

### `Lessons`
| Kolon | Tip | Null | Not |
|-------|-----|------|-----|
| Id | uuid PK | — | |
| TrackId | uuid FK→LearningTracks | hayır | |
| Slug | varchar(80) | hayır | **UNIQUE**, `enflasyon-ve-reel-getiri` |
| OrderIndex | int | hayır | track içi sıra (1,2,3…) |
| Title | text | hayır | "Enflasyon ve Reel Getiri" |
| Summary | text | hayır | kart açıklaması ("Param büyüdü mü…") |
| BodyMarkdown | text | hayır | ders gövdesi (Markdown) |
| EstimatedMinutes | int | hayır | "4 dk" |
| Level | varchar(20) | hayır | `LessonLevel` |
| IsPublished | boolean | hayır | |
| CreatedAtUtc / UpdatedAtUtc | timestamptz | hayır/evet | |

İndeks: `(TrackId, OrderIndex)`.

### `LessonSections` — ders içi bloklar (opsiyonel, zengin içerik için)
| Kolon | Tip | Not |
|-------|-----|-----|
| Id | uuid PK | |
| LessonId | uuid FK→Lessons | |
| OrderIndex | int | blok sırası |
| Heading | text (null) | |
| BodyMarkdown | text | |

> MVP'de `Lessons.BodyMarkdown` tek başına yeter; uzun/yapılı içerik gerekince
> `LessonSections` kullanılır.

### `LessonPrerequisites` — kilit mantığı (M:N, kendine)
| Kolon | Tip | Not |
|-------|-----|-----|
| LessonId | uuid FK→Lessons | (PK çift) |
| PrerequisiteLessonId | uuid FK→Lessons | önce tamamlanmalı |

> Ders **Locked** durumu BURADAN türetilir: ön-koşul dersleri kullanıcı
> tamamlamadıysa kilitli. `LessonStatus`'a `Locked` saklanmaz.

### `ConceptTags` / `LessonConceptTags` — portföy kavramı bağı
| Tablo | Kolon | Not |
|-------|-------|-----|
| `ConceptTags` | Id, Key (`diversification`,`real-return`,`pe-ratio`…), Label | Analiz/Hisse kartlarının derse derin bağlantısı |
| `LessonConceptTags` | LessonId, ConceptTagId | M:N |

> Örn. Analiz "Yoğunlaşma" kartı → `diversification` tag'li "Çeşitlendirme"
> dersine bağlanır (taslakta bu yönlendirme var).

### `Quizzes` / `QuizQuestions` / `QuizOptions` — mini testler
| Tablo | Kolon | Not |
|-------|-------|-----|
| `Quizzes` | Id, LessonId FK (null=bağımsız), Title, PassingScore (int, %) | Derse bağlı mini test |
| `QuizQuestions` | Id, QuizId FK, OrderIndex, Type (`QuizQuestionType`), Prompt, Explanation (text) | açıklama: doğru cevap neden doğru |
| `QuizOptions` | Id, QuestionId FK, OrderIndex, Text, IsCorrect (bool) | |

### `UserLessonProgress` — kullanıcı ilerlemesi
| Kolon | Tip | Null | Not |
|-------|-----|------|-----|
| Id | uuid PK | — | |
| UserId | uuid FK→Users | hayır | **kapsam** (`11` §3) |
| LessonId | uuid FK→Lessons | hayır | |
| Status | varchar(20) | hayır | `LessonStatus` |
| ProgressPercent | int | hayır | 0–100 |
| StartedAtUtc / CompletedAtUtc | timestamptz | evet | |
| UpdatedAtUtc | timestamptz | hayır | |

Kısıt: **`UNIQUE(UserId, LessonId)`**, `ProgressPercent` 0–100.

### `UserQuizAttempts` (+ ops. `UserQuizAnswers`)
| Tablo | Kolon | Not |
|-------|-------|-----|
| `UserQuizAttempts` | Id, UserId FK, QuizId FK, Score (int %), Passed (bool), StartedAtUtc, CompletedAtUtc | |
| `UserQuizAnswers` (ops.) | Id, AttemptId FK, QuestionId FK, SelectedOptionId FK, IsCorrect | cevap-düzey detay/analitik |

---

## 11. Ortalama Maliyet Türetme Kuralı

`Transactions`'tan ağırlıklı ortalama (ortalama maliyet yöntemi, `CLAUDE.md` §6):

```
AvgCost  = Σ(Buy.Quantity × Buy.UnitPrice + Buy.Fee) / Σ(Buy.Quantity)
Quantity = Σ Buy.Quantity − Σ Sell.Quantity
```
- **Satış** ortalamayı bozmaz, yalnızca `Quantity` düşürür (ortalama maliyet
  yöntemi). FIFO/LIFO Faz 5 (vergi raporu gerekirse).
- Her işlem değişiminde `Holdings.Quantity/AvgCost` **yeniden hesaplanıp
  saklanır** (okuma yolu hızlanır, `10` §4).
- **Birim testi zorunlu** (NFR-1) — § 12 seed'indeki altın kalemi fixture.

> ⚠️ **BES bu kuralın DIŞINDA.** BES nominal bir hesap (1 birim): "alış/satış" yok,
> aylık katkı ile büyür. Maliyet tabanı = `BesDetails.OwnContribution +
> StateContribution`; değer = güncel fon değeri (`CurrentPrice`). BES'e alış/satış
> işlemi **engellenir** (eklenirse türetim BES maliyetini bozardı); yerine
> `POST /holdings/{id}/bes-contribution` (kendi + devlet %30) kullanılır ve
> `Holdings.AvgCost = own + state` doğrudan güncellenir (T1.17).

---

## 12. SEED VERİSİ (kapsamlı & tutarlı)

> Geliştirme/test için **gerçekçi, çeşitli ve birbirini doğrulayan** veri.
> 7 pozisyon: altın, döviz (USD + EUR), USD-fiyatlı hisse (kur çevrimi tetikler),
> **zarardaki fon** (eğitici örnek), BES, nakit. Baz TRY toplamları: **maliyet
> 603.770 / değer 839.213 / kâr +235.443 / +%39,0** (reel ~+%0,7 @enflasyon 0,38).
> Hem seed hem **test fixture**'dır (`06` §4, `09`).

### 12.1 Kullanıcılar & Roller
- `User#1` — DisplayName "Yatırımcı", BaseCurrency `TRY`, rol `User`. (Faz 1-4'ün tekil kullanıcısı.)
- `Admin#1` — rol `Admin` (Faz 5; izleme/yönetim, `11`/`12`).

### 12.2 Kur & Enflasyon
- `FxRates`: `USD→TRY = 48,000000`, `EUR→TRY = 52,000000` (AsOf bugün, Source `Manual`).
  Geçmiş: USD→TRY 43,27 (alış dönemi) — reel/senaryo için.
- `InflationRates`: dönem yıllık `0,380000` (**örnek/placeholder — Source `TÜİK`;
  prod'da resmi veri ile değiştir**).

### 12.3 Varlık Kataloğu (`Assets`)
| Type | Name | Symbol | Unit | PricingCurrency |
|------|------|--------|------|-----------------|
| Gold | Altın (gram) | XAU | gram | TRY |
| Fx | ABD Doları | USD | USD | TRY |
| Fx | Euro | EUR | EUR | TRY |
| Bes | Bireysel Emeklilik | — | birim | TRY |
| Cash | Nakit (TL) | — | TRY | TRY |
| Stock | Apple Inc. | AAPL | adet | **USD** |  ← kur çevrimi |
| Fund | Teknoloji Fonu | TEKFON | adet | TRY |

### 12.4 Pozisyonlar (Holdings + Transactions + BesDetails) — **tutarlı** (baz TRY)
| Varlık | İşlem (seed) | AvgCost | Toplam Maliyet | Güncel Fiyat | Değer (TRY) | Getiri |
|--------|-------------|---------|----------------|--------------|-------------|--------|
| Altın | Buy 40 gr @ 4.546,275 ₺ | 4.546,275 | **181.851** | 6.500 ₺/gr | **260.000** | +%43,0 |
| Dolar | Buy 2.000 $ @ 43,27 ₺ | 43,27 | **86.540** | 48,00 ₺/$ | **96.000** | +%10,9 |
| Euro | Buy 800 € @ 47,50 ₺ | 47,50 | **38.000** | 52,00 ₺/€ | **41.600** | +%9,5 |
| Apple (USD) | Buy 12 @ 175 $ | 175 $ | **100.800** | 210 $ (×48) | **120.960** | +%20,0 |
| Teknoloji Fonu | Buy 1.500 @ 28,00 ₺ | 28,00 | **42.000** | 23,50 ₺ | **35.250** | **−%16,1** |
| BES | own 120.000 + state 28.554 | (notional) | **148.554** | — | **279.378** | +%88,1 |
| Nakit | 6.025 ₺ | 1,00 | **6.025** | 1,00 | **6.025** | — |
| **TOPLAM** | | | **603.770** | | **839.213** | **+%39,0** |

- **BesDetails:** OwnContribution `120.000`, StateContribution `28.554`,
  VestingState `PartiallyVested`. (Devlet katkısı UI'da ayrı; getiri tabanına dahil.)
- **Apple USD-fiyatlı** → summary'de USD→TRY (×48) çevrimi gerçekten test edilir (SC-03 zinciri).
- **Teknoloji Fonu** kasıtlı **zararda** (−%16,1) → UI'da negatif getiri (kırmızı) + eğitici örnek.
- **PriceSnapshots:** altın & USD için iki nokta (now + alış); EUR/AAPL/fon için "now".
- Dağılım (değer/toplam): BES %33,3 · Altın %31,0 · Apple %14,4 · Dolar %11,4 · Euro %5,0 · Fon %4,2 · Nakit %0,7.

> ⚠️ Taslaktaki ekran sayıları elle konmuştu ve hepsi tam tutmuyordu; seed bunu
> **düzeltip birebir tutarlı** hale getirir — "doğru veri" ilkesi (NFR-1).

### 12.5 Eğitim İçeriği (taslaktaki derslerle birebir)
- `LearningTracks`: **"Temeller"** (slug `temeller`, Level `Beginner`, yayında).
- `Lessons` (track içi, sıralı; ön-koşul: her ders bir öncekini ister):
  1. **Enflasyon ve Reel Getiri** — 4 dk — tag `real-return` — *Tamamlandı*
  2. **Çeşitlendirme Neden Önemli?** — 5 dk — tag `diversification` — *Tamamlandı*
  3. **F/K, PD/DD Nedir?** — 6 dk — tag `pe-ratio`,`pb-ratio` — *Tamamlandı*
  4. **Risk ve Getiri İlişkisi** — 5 dk — tag `risk-return` — *Sırada (InProgress)*
  5. **Bileşik Getirinin Gücü** — 5 dk — tag `compound` — *Kilitli (ön-koşul: 4)*
  (Her ders: `Summary` taslaktaki açıklama, `BodyMarkdown` kısa eğitici metin.)
- `Quizzes`: Ders 1'e bağlı 3 soruluk mini test (PassingScore 60), her soruda
  `Explanation`. (Diğer derslere de eklenebilir.)
- `UserLessonProgress` (User#1): Ders 1-3 `Completed` (%100), Ders 4 `InProgress`
  (%0, "sırada"), Ders 5 türetilmiş `Locked`. → taslaktaki ilerleme çubuğu
  (3 dolu segment) ile birebir.
- `ConceptTags` seed: `real-return`, `diversification`, `pe-ratio`, `pb-ratio`,
  `risk-return`, `compound` (analiz/hisse kartı → ders derin bağlantısı).

### 12.6 Seed Nasıl Uygulanır?
- **Yer:** ayrı bir seeder (`Finans.Infrastructure/Seed/SeedData.cs`),
  `app.Services` ile başlangıçta **idempotent** çalışır (varsa tekrar eklemez)
  veya `dotnet run -- seed` komutuyla. Migration'dan ayrı tut.
- **Idempotent:** sabit `Id`'ler (deterministik GUID) veya "var mı?" kontrolü →
  tekrar çalıştırınca çoğaltmaz.
- **Test ortamı:** integration testleri aynı seed setini fixture olarak kullanır
  (`09` §2) — böylece SC-01..SC-06 senaryoları bu sayılara dayanır.

---

## 13. Migration Stratejisi (EF Core)

1. Entity'ler `Finans.Domain`; `DbContext` + konfigürasyon `Finans.Infrastructure`.
2. `dotnet ef migrations add <Ad>` → `dotnet ef database update`.
3. **Faz bazlı migration:** Faz 0 → A (portföy) + B (kimlik/audit iskeleti);
   eğitim (C) tabloları eğitim feature'ı geldiğinde (Faz 5) ayrı migration —
   ama **model/şema bu dokümanda hazır**.
4. Geriye uyum: prod'da kolon silmeden önce deprecate; migration sıralı ve
   tekrar oynatılabilir.

---

## 14. İndeksler & Bütünlük (özet)

- FK'ler indeksli: `Holdings.UserId`, `Holdings.AssetId`, `Transactions.HoldingId`,
  `PriceSnapshots.AssetId`, `UserLessonProgress.UserId`, `RefreshTokens.UserId`.
- Unique: `Holdings(UserId,AssetId)` (aktif), `BesDetails.HoldingId`,
  `UserLessonProgress(UserId,LessonId)`, `Lessons.Slug`, `LearningTracks.Slug`,
  `Users.Email`.
- Check: para/miktar `>= 0`, `Transactions.Quantity > 0`, enum allow-list,
  `ProgressPercent` 0–100, `FxRates.Rate > 0`.
- Oran/yüzde **saklanmaz** (runtime hesap). Para kolonları `numeric(18,6)`.
- Soft-delete tablolarında varsayılan sorgu filtresi `IsDeleted=false`
  (EF Core global query filter — `11` §3 per-user filtresiyle birlikte).

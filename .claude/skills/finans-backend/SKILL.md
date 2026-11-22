---
name: finans-backend
description: Nirengi'nin .NET backend'inde (backend/src/Finans.*) endpoint, servis, EF Core varlığı, migration ya da parasal hesap yazarken/değiştirirken bu skill'i kullan. Per-user veri izolasyonu (IDOR), decimal para kuralı, DTO/doğrulama, cache anahtarı ve zorunlu test çiftini (unit + integration) somut proje desenleriyle verir. Kullanıcı "yeni endpoint", "servise alan ekle", "controller", "migration", "getiri hesabı", "backend testi", "IDOR" gibi şeylerden söz ettiğinde — "skill kullan" demese bile — buna başvur.
---

# Nirengi — Backend geliştirme

Bu skill, `CLAUDE.md` §8/§12/§13 kapılarının bu repodaki **somut** karşılığıdır.
Amaç: finansal veri tutan çok kullanıcılı bir API'de en pahalı üç hatayı
(başkasının verisini sızdırmak · yanlış rakam göstermek · sırrı loglamak)
yapısal olarak imkânsıza yaklaştırmak.

## Önce: yeniden kullanım merdiveni

Kod yazmadan önce `.agent-method/registry/CAPABILITIES.md`'yi tara ve
**Reuse → Extend → Extract → New** sırasını uygula (AGENTS.md §4.4). Bu repoda
`CurrencyConverter`, `AssetPricing`, `PortfolioCalculationService`, `IAppCache`
gibi yetenekler zaten var; ikinci bir kopyası çıkarsa iki ayrı rakam üretir —
finans uygulamasında en kötü sonuç budur.

## Katman haritası

| Katman | Yol | Ne yaşar |
|---|---|---|
| Api | `backend/src/Finans.Api` | Controller, `Auth/HttpCurrentUser`, `ErrorHandling`, `Observability` |
| Application | `backend/src/Finans.Application` | Arayüzler (`IHoldingService`), DTO'lar, saf hesap (`BesCalculator`, `CurrencyConverter`) |
| Domain | `backend/src/Finans.Domain` | Varlıklar, enum'lar |
| Infrastructure | `backend/src/Finans.Infrastructure` | Servis uygulamaları, `FinansDbContext`, `Seed/`, sağlayıcılar, cache |

Controller **ince** kalır: iş kuralı ve kapsama servis katmanındadır. Böylece
kimlik modeli Faz 5'te JWT'ye geçtiğinde controller'lar değişmez.

## Beş tavizsiz kural

### 1. Per-user kapsama — her sorgu, her seferinde

Kullanıcı `ICurrentUser` ile enjekte edilir; `UserId`'yi asla istemciden gelen
gövdeden/parametreden okuma. Her okuma ve yazma kullanıcıya kapsanır:

```csharp
var holding = await db.Holdings
    .FirstOrDefaultAsync(h => h.Id == id && h.UserId == currentUser.UserId, ct)
    ?? throw new NotFoundException();
```

**Başkasının kaydı 403 değil 404 döner** — 403 "bu kayıt var ama senin değil"
bilgisini sızdırır, 404 varlığını bile söylemez. Örnek:
`backend/src/Finans.Infrastructure/Services/HoldingService.cs`.

Yeni endpoint açıyorsan bu kontrol **ve** karşılığındaki IDOR testi aynı
commit'te olmalı (SC-13).

### 2. Para `decimal`, hesap tam hassasiyetle

Parasal ve oransal hiçbir hesapta `double`/`float` kullanma — ikili kayan nokta
`0,1`'i temsil edemez ve yuvarlama hataları portföy toplamında görünür hale
gelir. Yuvarlama **yalnız gösterimde** yapılır; TR biçimi (binlik nokta,
ondalık virgül) `@finans/shared` format util'lerinin işidir, backend ham
`decimal` döner.

Formüller `CLAUDE.md` §6'da; LLM'e sayı hesaplatma (§3.1) — dil modeli yalnız
kodun ürettiği sonucu yorumlar.

### 3. DTO sınırda, doğrulama sunucuda

Varlık sınıfını dışarı verme; `PortfolioDtos.cs` kalıbında DTO kullan. Geçersiz
girdi `ValidationException(field, issue, message)` (`Common/ApplicationExceptions.cs`) ile reddedilir ve istemciye
alan adı + makine okunur `issue` + Türkçe mesaj döner (iç detay, stack, SQL asla):

```csharp
if (request.Quantity <= 0)
    throw new ValidationException("quantity", "must_be_positive", "Miktar sıfırdan büyük olmalıdır.");
```

Çakışma için `ConflictException`, yokluk için `NotFoundException`. Haritalama
`backend/src/Finans.Api/ErrorHandling/`'de — yeni istisna türü uydurmadan önce
oraya bak.

### 4. Dış çağrı cache'li, cache anahtarı kullanıcıyı taşır

DB/fiyat/LLM çağrıları `IAppCache` üzerinden gider (single-flight, Redis yoksa
in-memory'e düşer). Kullanıcıya özel veri cache'leniyorsa anahtar `UserId`
içermeli — `PortfolioCacheStamp` deseni budur. Anahtarda kullanıcı yoksa bir
kullanıcının portföyü başkasına servis edilir; bu, kapsama kuralını (1) cache
katmanında delmek demektir.

Yazma işlemi sonrası ilgili damgayı `BumpAsync` ile tazele.

### 5. Log'a sır, token, PII yazma

Yapılandırılmış log (Serilog) kullan; kullanıcı kimliği dışında kişisel veri,
API anahtarı, tam portföy içeriği log'a girmez. LLM'e giden portföy
`PortfolioAnonymizer`'dan geçer. Yeni dış bağımlılık eklediysen metrik +
fallback de ekle (`12`, `10` §3-4).

## Test yükümlülüğü — iki kategori, her seferinde

```bash
cd backend && dotnet test
```

Tek proje: `dotnet test tests/Finans.Application.Tests`
Tek sınıf: `dotnet test --filter FullyQualifiedName~PortfolioApiTests`

**Unit** (`tests/Finans.Application.Tests`): izole hesap/mantık. Parasal hesap
için zorunlu (NFR-1). xUnit + FluentAssertions.

**Integration** (`tests/Finans.Integration.Tests`): `SqliteWebApplicationFactory`
+ `SeedData` ile gerçek HTTP yolu. Kullanıcı `X-User-Id` başlığıyla seçilir:

```csharp
private HttpClient ClientAs(Guid userId) { ... client.DefaultRequestHeaders.Add("X-User-Id", userId.ToString()); }

[Fact]
public async Task Idor_other_users_holding_is_404()
{
    var admin = ClientAs(Admin);                      // başka kullanıcı
    var resp = await admin.GetAsync($"/api/holdings/{GoldHolding}");
    resp.StatusCode.Should().Be(HttpStatusCode.NotFound); // varlığı bile sızdırma
}
```

Tam örnek: `backend/tests/Finans.Integration.Tests/PortfolioApiTests.cs`.
Seed kimlikleri deterministiktir: `SeedData.Id("user-1")`.

Davranış değiştiyse senaryoyu **koddan önce** `.claude/docs/09-TESTING-STRATEGY.md`
§5 kataloğuna ekle (Given-When-Then; `SC-NN | FR-x | ... | Unit+Integration | [ ]`),
sonra yeşile getirince `[x]` ve hangi testin karşıladığını yaz. Katalog yaşayan
listedir — numarayı yeniden kullanma.

## Bitirmeden önce

`finans-green-gate` skill'ini çalıştır: testler, güvenlik/gözlemlenebilirlik
kapıları ve ADM kayıtları (GD · GitHub issue · karar · CAPABILITIES) tek geçişte
denetlenir. Kabul kriterlerini **sen onaylamazsın** — kanıt sunarsın (AGENTS.md §4.3).

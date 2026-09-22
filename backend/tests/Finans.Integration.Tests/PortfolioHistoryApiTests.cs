using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Finans.Application.Portfolio;
using Finans.Domain.Enums;
using Finans.Domain.Identity;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace Finans.Integration.Tests;

/// <summary>
/// Portföy değer geçmişi API uçtan uca (T5.2, SC-33): seri özet ekranıyla tutarlı,
/// dönem dilimleme, geçersiz dönem 400, kullanıcı izolasyonu (SC-13 analoğu — id
/// parametresi yok; izolasyon "başkasının serisi asla dönmez" ile sağlanır).
/// </summary>
public sealed class PortfolioHistoryApiTests : IClassFixture<SqliteWebApplicationFactory>, IAsyncLifetime
{
    private readonly SqliteWebApplicationFactory _factory;

    private static readonly Guid Investor = SeedData.Id("user-1");
    private static readonly Guid Admin = SeedData.Id("admin-1");

    /// <summary>Seed alış dönemi (SeedData.purchase) — serinin beklenen ilk günü.</summary>
    private static readonly DateOnly PurchaseDate = new(2024, 6, 1);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public PortfolioHistoryApiTests(SqliteWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedData.SeedAsync(db);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient ClientAs(Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", userId.ToString());
        return client;
    }

    // ── SC-33: seri özetle tutarlı ───────────────────────────────────────────

    [Fact]
    public async Task History_last_point_matches_summary_totals()
    {
        var client = ClientAs(Investor);

        var history = await client.GetFromJsonAsync<PortfolioHistoryDto>(
            "/api/portfolio/history?period=all", Json);
        var summary = await client.GetFromJsonAsync<PortfolioSummaryDto>(
            "/api/portfolio/summary", Json);

        history.Should().NotBeNull();
        history!.BaseCurrency.Should().Be(CurrencyCode.TRY);
        history.Period.Should().Be("all");
        history.Points.Should().NotBeEmpty();

        // Serinin SON günü özet ekranıyla birebir aynı sayılar (tutarlılık — NFR-1):
        // değer = Σ miktar×güncel fiyat (kur çevrimli), maliyet = Σ yatırılan (BES = kendi katkı).
        var last = history.Points[^1];
        last.Value.Should().Be(summary!.TotalValue);   // seed: 839.213
        last.Cost.Should().Be(summary.TotalCost);      // seed: 575.216

        // Seri ilk işlem gününde başlar; bugünde biter; tarihler artan ve tekilsiz.
        history.FirstDate.Should().Be(PurchaseDate);
        history.Points[0].Date.Should().Be(PurchaseDate);
        last.Date.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
        history.Points.Should().BeInAscendingOrder(p => p.Date);

        // Seyrekleştirme üst sınırı (uçlar korunarak).
        history.Points.Count.Should().BeLessThanOrEqualTo(500);

        // Seed portföyü kârda → dönem değişimi pozitif.
        history.ChangeRatio.Should().NotBeNull();
        history.ChangeRatio!.Value.Should().BePositive();
    }

    [Fact]
    public async Task History_default_period_is_all()
    {
        var client = ClientAs(Investor);

        var history = await client.GetFromJsonAsync<PortfolioHistoryDto>(
            "/api/portfolio/history", Json);

        history!.Period.Should().Be("all");
        history.Points[0].Date.Should().Be(PurchaseDate);
    }

    // ── Dönem dilimleme ──────────────────────────────────────────────────────

    [Fact]
    public async Task Period_1m_returns_only_last_month_window()
    {
        var client = ClientAs(Investor);

        var history = await client.GetFromJsonAsync<PortfolioHistoryDto>(
            "/api/portfolio/history?period=1m", Json);

        history!.Period.Should().Be("1m");
        history.Points.Should().NotBeEmpty();
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-31);
        history.Points[0].Date.Should().BeOnOrAfter(cutoff);

        // FirstDate dönemden bağımsız TÜM serinin başlangıcını taşır ("veri şu tarihten beri").
        history.FirstDate.Should().Be(PurchaseDate);
    }

    [Fact]
    public async Task Invalid_period_returns_400_validation()
    {
        var client = ClientAs(Investor);

        var resp = await client.GetAsync("/api/portfolio/history?period=2w");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("error").GetProperty("code").GetString()
            .Should().Be("VALIDATION_ERROR");
    }

    // ── GD-002: iki havuz · hak edişe göre değer · girilen fon değeri kaybolmaz ──

    private async Task<HttpClient> FreshUserAsync(string name)
    {
        var userId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
            db.Users.Add(new User
            {
                Id = userId, DisplayName = name, BaseCurrency = CurrencyCode.TRY,
                IsActive = true, CreatedAtUtc = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }
        return ClientAs(userId);
    }

    [Fact]
    public async Task Bes_legacy_total_fund_value_is_split_not_lost()
    {
        // REGRESYON: GD-002'de okuma yolu değeri iki havuzdan türetmeye başladı; oluşturma
        // hâlâ TEK toplamı eski alana yazıyordu → girilen değer sessizce yok sayılıyordu.
        // Önceki test bunu yakalayamadı çünkü fon = katkı toplamıydı (getiri %0).
        // Burada getiri %20: kayıp olsaydı değer 50.000'e düşerdi.
        var client = await FreshUserAsync("BES Bölme Testi");

        var resp = await client.PostAsJsonAsync("/api/holdings/bes",
            new CreateBesRequest("BES Bölme", null, CurrencyCode.TRY,
                JoinedAtUtc: DateTime.UtcNow.AddYears(-2), BirthYear: 1990,
                CurrentFundValue: 72000m, OpeningOwn: 50000m, OpeningState: 10000m), Json);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var bes = await resp.Content.ReadFromJsonAsync<HoldingDto>(Json);

        // 72.000, katkı oranında (50/60 · 10/60) bölündü — toplam korunur.
        bes!.Bes!.OwnFundValue.Should().Be(60000m);
        bes.Bes.StateFundValue.Should().Be(12000m);
        (bes.Bes.OwnFundValue + bes.Bes.StateFundValue).Should().Be(72000m);

        // Hak ediş %0 (2 yıl) → değer yalnız kendi havuzu: 60.000 (katkı 50.000 değil).
        bes.CurrentValue.Should().Be(60000m, "girilen fon değeri kaybolmamalı");
    }

    [Fact]
    public async Task Bes_two_pools_value_counts_only_vested_state_share_across_surfaces()
    {
        // Ürün sahibi kuralı (2026-09-20): değer = kendi fon + hak ediş oranı × devlet fonu.
        // 7 yıllık katılım → hak ediş %35 (6-10 yıl kademesi). İki havuz farklı getiride.
        var client = await FreshUserAsync("BES İki Havuz Testi");

        var resp = await client.PostAsJsonAsync("/api/holdings/bes",
            new CreateBesRequest("BES İki Havuz", null, CurrencyCode.TRY,
                JoinedAtUtc: DateTime.UtcNow.AddYears(-7), BirthYear: 1990,
                CurrentFundValue: 0m, OpeningOwn: 100000m, OpeningState: 30000m,
                OwnFundValue: 120000m, StateFundValue: 33000m), Json);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var bes = await resp.Content.ReadFromJsonAsync<HoldingDto>(Json);

        bes!.Bes!.OwnFundRate.Should().Be(0.20m);
        bes.Bes.StateFundRate.Should().Be(0.10m);
        bes.Bes.VestedRate.Should().Be(0.35m);

        // 120.000 + 0,35 × 33.000 = 131.550
        const decimal expected = 131550m;
        bes.CurrentValue.Should().Be(expected);
        bes.Bes.VestedPortfolioValue.Should().Be(expected);

        // Tek kural, üç yüzey: liste = özet = değer serisinin son günü.
        var summary = await client.GetFromJsonAsync<PortfolioSummaryDto>("/api/portfolio/summary", Json);
        var history = await client.GetFromJsonAsync<PortfolioHistoryDto>("/api/portfolio/history?period=all", Json);
        summary!.TotalValue.Should().Be(expected);
        history!.Points[^1].Value.Should().Be(expected);

        // Maliyet yalnız CEPTEN ödenen kendi katkı.
        summary.TotalCost.Should().Be(100000m);
    }

    // ── INC-003: nakit fiyatı sabit 1 — toplam, görünen satırların toplamına eşit ──

    [Fact]
    public async Task Cash_created_via_api_is_valued_and_total_equals_visible_rows()
    {
        // REGRESYON: API'den oluşturulan nakit CurrentPrice=null başlıyordu → liste satırı
        // "—", özet ise onu MALİYETİNDEN sayıyordu. Toplam doğruydu ama GÖRÜNEN satırların
        // toplamına eşit değildi. Tohum 1 yazdığı için hiçbir test API yolunu denemiyordu.
        var client = await FreshUserAsync("Nakit Testi");

        var cashResp = await client.PostAsJsonAsync("/api/holdings",
            new CreateHoldingRequest(AssetType.Cash, "Nakit (TL)", null, CurrencyCode.TRY, "TRY",
                new TransactionRequest(TransactionType.Buy, 34349.50m, 1m)), Json);
        cashResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Karşılaştırma için fiyatlı ikinci bir kalem.
        var fundResp = await client.PostAsJsonAsync("/api/holdings",
            new CreateHoldingRequest(AssetType.Fund, "Test Fonu", null, CurrencyCode.TRY, "adet",
                new TransactionRequest(TransactionType.Buy, 100m, 10m)), Json);
        var fund = await fundResp.Content.ReadFromJsonAsync<HoldingDto>(Json);
        await client.PutAsJsonAsync($"/api/holdings/{fund!.Id}", new UpdateHoldingRequest(12m), Json);

        var holdings = await client.GetFromJsonAsync<List<HoldingDto>>("/api/holdings", Json);
        var cash = holdings!.Single(h => h.AssetType == AssetType.Cash);

        cash.CurrentPrice.Should().Be(1m, "nakdin birim fiyatı tanım gereği 1");
        cash.CurrentValue.Should().Be(34349.50m, "nakit satırı artık '—' göstermemeli");
        cash.ReturnRatio.Should().Be(0m);

        // ASIL DEĞİŞMEZ: özet toplamı = görünen satırların toplamı (kullanıcı ikisini yan yana görüyor).
        var summary = await client.GetFromJsonAsync<PortfolioSummaryDto>("/api/portfolio/summary", Json);
        summary!.TotalValue.Should().Be(holdings.Sum(h => h.CurrentValue ?? 0m));
        summary.TotalValue.Should().Be(34349.50m + 1200m);

        // Değer serisi de aynı sayıyı söyler.
        var history = await client.GetFromJsonAsync<PortfolioHistoryDto>("/api/portfolio/history?period=all", Json);
        history!.Points[^1].Value.Should().Be(summary.TotalValue);
    }

    // ── REVIEW-002 · RV-006: türetilmiş fiyat yazılamaz (sessiz yok sayma yerine 400) ──

    [Fact]
    public async Task Put_current_price_is_rejected_for_bes_and_cash_instead_of_silently_ignored()
    {
        var client = await FreshUserAsync("Türetilmiş Fiyat Testi");

        var besResp = await client.PostAsJsonAsync("/api/holdings/bes",
            new CreateBesRequest("BES Fiyat", null, CurrencyCode.TRY,
                JoinedAtUtc: DateTime.UtcNow.AddYears(-1), BirthYear: 1990,
                CurrentFundValue: 12000m, OpeningOwn: 10000m, OpeningState: 2000m), Json);
        var bes = await besResp.Content.ReadFromJsonAsync<HoldingDto>(Json);

        var cashResp = await client.PostAsJsonAsync("/api/holdings",
            new CreateHoldingRequest(AssetType.Cash, "Nakit (TL)", null, CurrencyCode.TRY, "TRY",
                new TransactionRequest(TransactionType.Buy, 5000m, 1m)), Json);
        var cash = await cashResp.Content.ReadFromJsonAsync<HoldingDto>(Json);

        // Eskiden ikisi de 200 dönüyor, hiçbir şey değişmiyordu.
        var putBes = await client.PutAsJsonAsync($"/api/holdings/{bes!.Id}", new UpdateHoldingRequest(99999m), Json);
        putBes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await putBes.Content.ReadAsStringAsync()).Should().Contain("derived_for_bes");

        var putCash = await client.PutAsJsonAsync($"/api/holdings/{cash!.Id}", new UpdateHoldingRequest(2m), Json);
        putCash.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await putCash.Content.ReadAsStringAsync()).Should().Contain("fixed_price");

        // Değerler bozulmadı.
        var after = await client.GetFromJsonAsync<List<HoldingDto>>("/api/holdings", Json);
        after!.Single(h => h.Id == cash.Id).CurrentValue.Should().Be(5000m);
        after.Single(h => h.Id == bes.Id).CurrentValue.Should().Be(bes.CurrentValue);
    }

    // ── REVIEW-002 · RV-008 / RV-010: eski tek alan senkron, türetilmiş değer kalıcılaşmaz ──

    private async Task<(HttpClient Client, Guid UserId)> FreshUserWithIdAsync(string name)
    {
        var userId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
            db.Users.Add(new User
            {
                Id = userId, DisplayName = name, BaseCurrency = CurrencyCode.TRY,
                IsActive = true, CreatedAtUtc = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }
        return (ClientAs(userId), userId);
    }

    [Fact]
    public async Task Bes_legacy_current_price_stays_in_sync_and_derived_value_is_never_persisted()
    {
        // 7 yıllık katılım → hak ediş %35 → TÜRETİLMİŞ değer (131.550) ≠ iki havuz toplamı (153.000).
        // Bu fark sayesinde "hangi değer kalıcılaştı?" sorusu kesin cevaplanır.
        var (client, userId) = await FreshUserWithIdAsync("Senkron Testi");
        var resp = await client.PostAsJsonAsync("/api/holdings/bes",
            new CreateBesRequest("BES Senkron", null, CurrencyCode.TRY,
                JoinedAtUtc: DateTime.UtcNow.AddYears(-7), BirthYear: 1990,
                CurrentFundValue: 0m, OpeningOwn: 100000m, OpeningState: 30000m,
                OwnFundValue: 120000m, StateFundValue: 33000m), Json);
        var bes = await resp.Content.ReadFromJsonAsync<HoldingDto>(Json);
        bes!.CurrentValue.Should().Be(131550m);

        async Task<decimal?> StoredPriceAsync()
        {
            using var s = _factory.Services.CreateScope();
            var db = s.ServiceProvider.GetRequiredService<FinansDbContext>();
            return (await db.Holdings.FindAsync(bes.Id))!.CurrentPrice;
        }

        // RV-008: iki havuz ayrı girildiğinde eski tek alan TOPLAMI taşır (CurrentFundValue 0 olsa bile).
        (await StoredPriceAsync()).Should().Be(153000m);

        // Güncelleme sonrası da senkron.
        await client.PutAsJsonAsync($"/api/holdings/{bes.Id}/bes",
            new UpdateBesRequest(OwnFundValue: 125000m, StateFundValue: 34000m), Json);
        (await StoredPriceAsync()).Should().Be(159000m, "geri alma bayat değil son toplamı görmeli");

        // RV-010: okuma yolu entity üstüne TÜRETİLMİŞ değeri (hak ediş uygulanmış) yazar. Aynı
        // DbContext'te sonradan SaveChanges çalışsa bile bu değer kalıcılaşMAMALI.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
            var svc = ActivatorUtilities.CreateInstance<Finans.Infrastructure.Services.HoldingService>(
                scope.ServiceProvider, new FixedUser(userId));
            var list = await svc.GetAllAsync();
            list.Single(h => h.Id == bes.Id).CurrentValue.Should().Be(125000m + 0.35m * 34000m);

            await db.SaveChangesAsync(); // eski davranışta türetilmiş değer burada yazılırdı
        }
        (await StoredPriceAsync()).Should().Be(159000m, "türetilmiş (hak ediş uygulanmış) değer DB'ye sızmamalı");
    }

    private sealed class FixedUser(Guid id) : Finans.Application.Common.ICurrentUser
    {
        public Guid UserId => id;
    }

    // ── SC-34: ileri tarihli BES plan katkısı maliyete girmez (özet = seri) ──

    [Fact]
    public async Task Summary_and_history_exclude_future_bes_plan_contributions()
    {
        // Taze kullanıcı — diğer testlerin "admin'in pozisyonu yok" varsayımını bozmamak için.
        var userId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
            db.Users.Add(new User
            {
                Id = userId,
                DisplayName = "BES Plan Testi",
                BaseCurrency = CurrencyCode.TRY,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var client = ClientAs(userId);

        // Açılış: kendi 50.000 + devlet 10.000; güncel fon değeri 60.000.
        var createResp = await client.PostAsJsonAsync("/api/holdings/bes",
            new CreateBesRequest("BES Plan Testi", null, CurrencyCode.TRY,
                JoinedAtUtc: DateTime.UtcNow.AddYears(-2), BirthYear: 1990,
                CurrentFundValue: 60000m, OpeningOwn: 50000m, OpeningState: 10000m), Json);
        createResp.IsSuccessStatusCode.Should().BeTrue();
        var holding = await createResp.Content.ReadFromJsonAsync<HoldingDto>(Json);

        // İleri tarihli düzenli plan: gelecek 3 ay × 1.000 — henüz yatmadı, MALİYETE GİRMEMELİ.
        var from = DateTime.UtcNow.AddMonths(1);
        var genResp = await client.PostAsJsonAsync(
            $"/api/holdings/{holding!.Id}/bes/contributions",
            new GenerateBesContributionsRequest(1000m, Day: 1, FromUtc: from, ToUtc: from.AddMonths(2)), Json);
        genResp.IsSuccessStatusCode.Should().BeTrue();

        var summary = await client.GetFromJsonAsync<PortfolioSummaryDto>(
            "/api/portfolio/summary", Json);
        var history = await client.GetFromJsonAsync<PortfolioHistoryDto>(
            "/api/portfolio/history?period=all", Json);

        // Maliyet = yalnız YATIRILMIŞ kendi katkı (50.000) — ileri tarihli 3.000 hariç
        // (özet saklanan bayat AvgCost'u değil, okuma anında türetilen tabanı kullanır).
        summary!.TotalCost.Should().Be(50000m);
        // GD-002: değer = kendi katkının fon değeri + HAK EDİLMİŞ devlet katkısı.
        // Toplam fon 60.000 katkı oranında bölündü → kendi 50.000 · devlet 10.000.
        // Katılım 2 yıl önce → hak ediş %0 → devlet havuzu değere girmez.
        summary.TotalValue.Should().Be(50000m);

        // Üç yüzey aynı sayıyı söyler: özet = değer serisi son günü (= pozisyon listesi kuralı).
        history!.Points.Should().NotBeEmpty();
        history.Points[^1].Cost.Should().Be(summary.TotalCost);
        history.Points[^1].Value.Should().Be(summary.TotalValue);
    }

    // ── Kullanıcı izolasyonu (SC-13 analoğu) ─────────────────────────────────

    [Fact]
    public async Task Other_user_gets_empty_series_not_investors_data()
    {
        var admin = ClientAs(Admin);

        var history = await admin.GetFromJsonAsync<PortfolioHistoryDto>(
            "/api/portfolio/history?period=all", Json);

        // Admin'in pozisyonu yok → boş seri; yatırımcının verisi ASLA sızmaz (11 §3).
        history!.Points.Should().BeEmpty();
        history.FirstDate.Should().BeNull();
        history.ChangeRatio.Should().BeNull();
    }
}

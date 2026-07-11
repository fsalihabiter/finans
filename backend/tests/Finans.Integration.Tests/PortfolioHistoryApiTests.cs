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
        summary.TotalValue.Should().Be(60000m); // fon değeri

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

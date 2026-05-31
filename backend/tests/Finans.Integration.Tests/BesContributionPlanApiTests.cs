using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Finans.Application.Portfolio;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace Finans.Integration.Tests;

/// <summary>
/// Düzenli BES katkısı üretimi (T-BES.6, SC-22): tarih aralığından aylık kayıt; işlem geçmişi;
/// idempotent; devlet katkısı ödeme tarihindeki orana göre. **Kendi izole fixture'ı** (BES state'i
/// değiştirir; diğer sınıfları etkilemesin).
/// </summary>
public sealed class BesContributionPlanApiTests : IClassFixture<SqliteWebApplicationFactory>, IAsyncLifetime
{
    private readonly SqliteWebApplicationFactory _factory;
    private static readonly Guid Investor = SeedData.Id("user-1");
    private static readonly Guid BesHolding = SeedData.Id("holding-bes");
    private static readonly Guid GoldHolding = SeedData.Id("holding-gold");

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    { Converters = { new JsonStringEnumConverter() } };

    public BesContributionPlanApiTests(SqliteWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedData.SeedAsync(db);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient Client()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", Investor.ToString());
        return client;
    }

    [Fact]
    public async Task Generate_creates_monthly_records_and_is_idempotent()
    {
        // 2025-09 → 2025-11, ayın 5'i, aylık 1.000 → 3 kayıt. 2025 (2026 öncesi) → devlet %30 = 300/ay.
        var req = new GenerateBesContributionsRequest(
            1000m, 5,
            new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 11, 30, 0, 0, 0, DateTimeKind.Utc));

        var resp = await Client().PostAsJsonAsync($"/api/holdings/{BesHolding}/bes/contributions", req, Json);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<HoldingDto>(Json);

        // Seed açılış kaydı (own 120.000, state 28.554, 2024 → yatırılmış) + 3×1.000 own, +3×300 state
        // (2025 → %30, yatırılmış). Toplamlar yatırılmış katkılardan türetilir.
        dto!.Bes!.OwnContribution.Should().Be(123000m);
        dto.Bes.StateContribution.Should().Be(29454m); // 28.554 + 900
        // Üretilen Plan kayıtları yalnız 2025'in 9-11. aylarına ait (sınıf fixture'ı paylaşıldığı için
        // başka testin ileri tarihli kayıtlarını dışla — xUnit test sırası garanti edilmez).
        var planRowsFor2025 = dto.Bes.Contributions
            .Where(c => c.Source == "Plan" && c.PaidAtUtc.Year == 2025 && c.PaidAtUtc.Month is >= 9 and <= 11)
            .ToList();
        planRowsFor2025.Should().HaveCount(3);
        planRowsFor2025.Should().OnlyContain(c => c.OwnAmount == 1000m && c.StateAmount == 300m);

        // İdempotent: aynı aralık tekrar → yeni kayıt/etki yok (toplam 2025/9-11 sayısı 3 kalır).
        var again = await Client().PostAsJsonAsync($"/api/holdings/{BesHolding}/bes/contributions", req, Json);
        var dto2 = await again.Content.ReadFromJsonAsync<HoldingDto>(Json);
        dto2!.Bes!.Contributions
            .Count(c => c.Source == "Plan" && c.PaidAtUtc.Year == 2025 && c.PaidAtUtc.Month is >= 9 and <= 11)
            .Should().Be(3);
    }

    [Fact]
    public async Task Generate_allows_future_range()
    {
        // İleriye dönük plan: 2099 aralığı → istenen aylar (gelecek dahil) üretilir (artık engellenmez).
        var req = new GenerateBesContributionsRequest(
            1000m, 5,
            new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2099, 3, 31, 0, 0, 0, DateTimeKind.Utc));

        var resp = await Client().PostAsJsonAsync($"/api/holdings/{BesHolding}/bes/contributions", req, Json);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<HoldingDto>(Json);

        // 2099-01/02/03 → 3 ileri tarihli kayıt üretilir.
        dto!.Bes!.Contributions.Should().Contain(c => c.PaidAtUtc.Year == 2099);
        dto.Bes.Contributions.Count(c => c.PaidAtUtc.Year == 2099).Should().Be(3);
    }

    [Fact]
    public async Task Generate_on_non_bes_is_400()
    {
        var req = new GenerateBesContributionsRequest(
            1000m, 5,
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc));

        var resp = await Client().PostAsJsonAsync($"/api/holdings/{GoldHolding}/bes/contributions", req, Json);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

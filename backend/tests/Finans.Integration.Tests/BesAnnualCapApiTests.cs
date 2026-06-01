using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Finans.Application.Portfolio;
using Finans.Domain.Enums;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace Finans.Integration.Tests;

/// <summary>
/// T-BES.4: yıllık devlet katkısı üst sınırı — takvim yılı bazlı kümülatif kesme. <b>Kendi izole
/// fixture'ı</b>: testler birbirinin DB state'ini bozmasın (paylaşımlı seed katkıları cap'i etkiler).
/// </summary>
public sealed class BesAnnualCapApiTests : IClassFixture<SqliteWebApplicationFactory>, IAsyncLifetime
{
    private readonly SqliteWebApplicationFactory _factory;
    private static readonly Guid Investor = SeedData.Id("user-1");
    private static readonly Guid BesHolding = SeedData.Id("holding-bes");

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    { Converters = { new JsonStringEnumConverter() } };

    public BesAnnualCapApiTests(SqliteWebApplicationFactory factory) => _factory = factory;

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

    /// <summary>
    /// Cap test'lerinden önce kullanıcının BES holding'inde aynı yıl için ne kadar state olduğunu
    /// okur — paylaşımlı fixture'da diğer testlerin yan etkisini tolere eden delta-mantığı için.
    /// </summary>
    private async Task<decimal> StateInYearAsync(int year)
    {
        var dto = await Client().GetFromJsonAsync<HoldingDto>($"/api/holdings/{BesHolding}", Json);
        return dto!.Bes!.Contributions
            .Where(c => c.PaidAtUtc.Year == year && c.Status != BesContributionStatus.Future)
            .Sum(c => c.StateAmount);
    }

    [Fact]
    public async Task Add_contribution_caps_state_at_annual_limit()
    {
        // 2024'te şu an mevcut state (seed Opening + olası önceki testler) → kalan = 51006 - mevcut.
        var existing = await StateInYearAsync(2024);
        var remaining = 51_006m - existing; // 2024 cap

        // own=100.000, paidAt=2024-07-01 → raw state = 100000 × %30 = 30000 → capped = min(30000, remaining).
        var paidAt = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var resp = await Client().PostAsJsonAsync(
            $"/api/holdings/{BesHolding}/bes-contribution",
            new AddBesContributionRequest(100_000m, StateAmount: null, PaidAtUtc: paidAt), Json);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Toplam 2024 state cap'e ulaşır (önceki + capped = 51006). Eklenen tek satırın state'i = min(30000, remaining).
        var newTotal = await StateInYearAsync(2024);
        newTotal.Should().Be(Math.Min(51_006m, existing + 30_000m));
    }

    [Fact]
    public async Task Add_contribution_returns_zero_state_when_quota_exhausted()
    {
        // İlk katkı 2024'te → cap'i doldurur veya kalanı tüketir.
        var firstPaidAt = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        await Client().PostAsJsonAsync(
            $"/api/holdings/{BesHolding}/bes-contribution",
            new AddBesContributionRequest(100_000m, StateAmount: null, PaidAtUtc: firstPaidAt), Json);

        var afterFirst = await StateInYearAsync(2024);
        afterFirst.Should().Be(51_006m); // 2024 cap dolu

        // İkinci katkı aynı yıl → state=0 (kota tükenmiş).
        var secondPaidAt = new DateTime(2024, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var resp = await Client().PostAsJsonAsync(
            $"/api/holdings/{BesHolding}/bes-contribution",
            new AddBesContributionRequest(10_000m, StateAmount: null, PaidAtUtc: secondPaidAt), Json);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2024 state değişmedi (yeni katkının state'i 0).
        var afterSecond = await StateInYearAsync(2024);
        afterSecond.Should().Be(51_006m);
    }

    [Fact]
    public async Task Add_contribution_in_new_year_does_not_inherit_previous_year_cap()
    {
        // 2024 cap'i doldur.
        var paid2024 = new DateTime(2024, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        await Client().PostAsJsonAsync(
            $"/api/holdings/{BesHolding}/bes-contribution",
            new AddBesContributionRequest(100_000m, StateAmount: null, PaidAtUtc: paid2024), Json);
        (await StateInYearAsync(2024)).Should().Be(51_006m);

        // 2026 (yeni yıl, oran %20, cap 79.272 — sıfırdan başlar) → own 5.000 → state 1.000 (kesilmez).
        // 2026-03-01 ödendi → deposit 2026-04-30 → bugün 2026-06-02 sonrası → Deposited (toplam'a girer).
        var paid2026 = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var resp = await Client().PostAsJsonAsync(
            $"/api/holdings/{BesHolding}/bes-contribution",
            new AddBesContributionRequest(5_000m, StateAmount: null, PaidAtUtc: paid2026), Json);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        (await StateInYearAsync(2026)).Should().Be(1_000m);
    }
}

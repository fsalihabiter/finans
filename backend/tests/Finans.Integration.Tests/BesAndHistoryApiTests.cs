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
/// BES özel modeli + işlem geçmişi (T1.17/T1.18). Kendi fixture'ı (izole DB) —
/// katkı testi seed BES'i değiştirir, diğer sınıfları etkilemesin.
/// </summary>
public sealed class BesAndHistoryApiTests : IClassFixture<SqliteWebApplicationFactory>, IAsyncLifetime
{
    private readonly SqliteWebApplicationFactory _factory;
    private static readonly Guid Investor = SeedData.Id("user-1");
    private static readonly Guid BesHolding = SeedData.Id("holding-bes");
    private static readonly Guid GoldHolding = SeedData.Id("holding-gold");

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    { Converters = { new JsonStringEnumConverter() } };

    public BesAndHistoryApiTests(SqliteWebApplicationFactory factory) => _factory = factory;

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
    public async Task Bes_rejects_buy_sell_transaction()
    {
        var resp = await Client().PostAsJsonAsync(
            $"/api/holdings/{BesHolding}/transactions",
            new TransactionRequest(TransactionType.Buy, 1m, 100m), Json);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task Bes_contribution_increases_own_state_and_cost()
    {
        // Kendi katkı 1.000 → devlet %20 = 200 (2026 oranı, BesRules). own 121.000, state 28.754, maliyet 149.754.
        var resp = await Client().PostAsJsonAsync(
            $"/api/holdings/{BesHolding}/bes-contribution",
            new AddBesContributionRequest(1000m), Json);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<HoldingDto>(Json);

        // Bugün tarihli katkı: kendi katkı hemen YATIRILMIŞ (ödeme ≤ bugün) → own 120.000 + 1.000 = 121.000,
        // maliyet = own = 121.000. Devlet katkısı (200, %20) ödeme ayını izleyen ayın sonunda yatar → kayıt
        // **StatePending** durumunda; yatırılmış devlet seed'deki 28.554 olarak kalır. Bekleyen toplamı yalnız
        // **Future** satırları sayar (geçmiş listesindeki "Gelecek Ödeme" ile birebir eşleşmesi için);
        // StatePending durumundaki kayıt "yolda" — tabloda görünür ama statePending'e GİRMEZ.
        dto!.Bes.Should().NotBeNull();
        dto.Bes!.OwnContribution.Should().Be(121000m);  // yatırılmış kendi katkı
        dto.Bes.StateContribution.Should().Be(28554m);  // yatırılmış devlet
        dto.Bes.StatePending.Should().Be(0m);           // hiç Future satır yok
        dto.AvgCost.Should().Be(121000m);               // maliyet = kendi katkı (cepten)
        dto.TotalCost.Should().Be(121000m);
    }

    // ── T-BES.4: yıllık devlet katkısı üst sınırı ────────────────────────────

    [Fact]
    public async Task Add_contribution_caps_state_at_annual_limit()
    {
        // Seed: 2024-06-01 Opening own=120.000, state=28.554. 2024 cap = 51.006 → kalan ≈ 22.452.
        // Eklenecek katkı: own=100.000, paidAt=2024-07-01 (2026 öncesi → oran %30) → raw state 30.000.
        // capped: min(30000, 22452) = 22452.
        var paidAt = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var resp = await Client().PostAsJsonAsync(
            $"/api/holdings/{BesHolding}/bes-contribution",
            new AddBesContributionRequest(100_000m, StateAmount: null, PaidAtUtc: paidAt), Json);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<HoldingDto>(Json);
        // own birikim: 120.000 + 100.000 = 220.000 (cap kendi katkıyı etkilemez).
        dto!.Bes!.OwnContribution.Should().Be(220_000m);
        // Yatırılmış state: Opening 28.554 + capped 22.452 = 51.006 (tam 2024 tavan).
        dto.Bes.StateContribution.Should().Be(51_006m);
    }

    [Fact]
    public async Task Add_contribution_returns_zero_state_when_quota_exhausted()
    {
        // 2024 cap doldur (Opening 28.554 + ek 22.452 = 51.006). Sonraki katkı 0 devlet alır.
        var firstPaidAt = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        await Client().PostAsJsonAsync(
            $"/api/holdings/{BesHolding}/bes-contribution",
            new AddBesContributionRequest(100_000m, StateAmount: null, PaidAtUtc: firstPaidAt), Json);

        // Aynı yıl ikinci katkı → kota dolu → state 0.
        var secondPaidAt = new DateTime(2024, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var resp = await Client().PostAsJsonAsync(
            $"/api/holdings/{BesHolding}/bes-contribution",
            new AddBesContributionRequest(10_000m, StateAmount: null, PaidAtUtc: secondPaidAt), Json);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<HoldingDto>(Json);
        // own = 120000 + 100000 + 10000 = 230000; state cap'te kalır 51006.
        dto!.Bes!.OwnContribution.Should().Be(230_000m);
        dto.Bes.StateContribution.Should().Be(51_006m);
    }

    [Fact]
    public async Task Add_contribution_in_new_year_does_not_inherit_previous_year_cap()
    {
        // 2024 dolu olsun.
        var paid2024 = new DateTime(2024, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        await Client().PostAsJsonAsync(
            $"/api/holdings/{BesHolding}/bes-contribution",
            new AddBesContributionRequest(100_000m, StateAmount: null, PaidAtUtc: paid2024), Json);

        // 2026 (yeni yıl, oran %20, cap 79.272 — sıfırdan) → own 5.000 × %20 = 1.000 state, kesilmez.
        var paid2026 = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var resp = await Client().PostAsJsonAsync(
            $"/api/holdings/{BesHolding}/bes-contribution",
            new AddBesContributionRequest(5_000m, StateAmount: null, PaidAtUtc: paid2026), Json);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<HoldingDto>(Json);
        // 2024 toplam: Opening 28554 + capped 22452 = 51006. 2026 katkı: 1.000 (kesilmez).
        // Toplam state = 51006 + 1000 = 52006.
        dto!.Bes!.StateContribution.Should().Be(52_006m);
    }

    [Fact]
    public async Task Update_bes_start_date_rederives_vesting()
    {
        // 12 yıl önce → tam hak ediş (Vested).
        var resp = await Client().PutAsJsonAsync(
            $"/api/holdings/{BesHolding}/bes",
            new UpdateBesRequest(new DateTime(2014, 1, 1, 0, 0, 0, DateTimeKind.Utc)), Json);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<HoldingDto>(Json);
        dto!.Bes!.JoinedAtUtc!.Value.Year.Should().Be(2014);
        dto.Bes.VestingState.Should().Be(VestingState.Vested);

        // ~1 yıl önce → henüz hak edilmedi (NotVested).
        var recent = await Client().PutAsJsonAsync(
            $"/api/holdings/{BesHolding}/bes",
            new UpdateBesRequest(DateTime.UtcNow.AddYears(-1)), Json);
        var dto2 = await recent.Content.ReadFromJsonAsync<HoldingDto>(Json);
        dto2!.Bes!.VestingState.Should().Be(VestingState.NotVested);
    }

    [Fact]
    public async Task Update_bes_future_date_is_400()
    {
        var resp = await Client().PutAsJsonAsync(
            $"/api/holdings/{BesHolding}/bes",
            new UpdateBesRequest(DateTime.UtcNow.AddYears(1)), Json);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_bes_on_non_bes_is_400()
    {
        var resp = await Client().PutAsJsonAsync(
            $"/api/holdings/{GoldHolding}/bes",
            new UpdateBesRequest(DateTime.UtcNow.AddYears(-5)), Json);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Holding_detail_includes_transaction_history()
    {
        var gold = await Client().GetFromJsonAsync<HoldingDto>($"/api/holdings/{GoldHolding}", Json);

        gold!.Transactions.Should().NotBeNull().And.HaveCount(1);
        var tx = gold.Transactions![0];
        tx.Type.Should().Be(TransactionType.Buy);
        tx.Quantity.Should().Be(40m);
        tx.UnitPrice.Should().Be(4546.275m);
    }

    [Fact]
    public async Task Bes_detail_includes_joined_date_and_separate_state()
    {
        var bes = await Client().GetFromJsonAsync<HoldingDto>($"/api/holdings/{BesHolding}", Json);

        bes!.Bes.Should().NotBeNull();
        bes.Bes!.JoinedAtUtc.Should().NotBeNull();
        bes.Bes.JoinedAtUtc!.Value.Year.Should().Be(2024);  // sözleşme başlangıcı
        bes.Bes.StateContribution.Should().Be(28554m);      // devlet katkısı AYRI
        bes.Transactions.Should().BeEmpty();                // BES'te alış/satış işlemi yok
    }
}

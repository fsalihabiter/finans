using System.Net;
using System.Net.Http.Headers;
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
/// Portföy API uçtan uca (T1.6/T1.7): Holdings CRUD + summary, geçerli kullanıcıya
/// kapsanma. Senaryolar: SC-01 (özet/getiri doğru), SC-07 (geçersiz girdi→400),
/// SC-13 (IDOR: başkasının id'si→404).
/// </summary>
public sealed class PortfolioApiTests : IClassFixture<SqliteWebApplicationFactory>, IAsyncLifetime
{
    private readonly SqliteWebApplicationFactory _factory;

    // Seed deterministik Id'leri (Infrastructure/Seed/SeedData.cs).
    private static readonly Guid Investor = SeedData.Id("user-1");
    private static readonly Guid Admin = SeedData.Id("admin-1");
    private static readonly Guid GoldHolding = SeedData.Id("holding-gold");
    private static readonly Guid BesHolding = SeedData.Id("holding-bes");

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public PortfolioApiTests(SqliteWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedData.SeedAsync(db);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Belirli kullanıcı kimliğiyle (X-User-Id) istek atan istemci.</summary>
    private HttpClient ClientAs(Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", userId.ToString());
        return client;
    }

    // ── SC-01: Özet ve getiri doğru ──────────────────────────────────────────

    [Fact]
    public async Task Summary_matches_seed_totals_and_real_return()
    {
        var client = ClientAs(Investor);

        var summary = await client.GetFromJsonAsync<PortfolioSummaryDto>("/api/portfolio/summary", Json);

        summary.Should().NotBeNull();
        summary!.BaseCurrency.Should().Be(CurrencyCode.TRY);
        // BES maliyeti = kendi katkı (cepten); devlet katkısı maliyet değil → toplam maliyet 603.770→575.216.
        summary.TotalCost.Should().Be(575216m);
        summary.TotalValue.Should().Be(839213m);
        summary.NetProfit.Should().Be(263997m);
        Math.Round(summary.ReturnRatio!.Value, 3).Should().Be(0.459m);
        Math.Round(summary.RealReturnRatio!.Value, 4).Should().Be(0.0572m); // enflasyon 0,38
        summary.Allocation.Should().HaveCount(7);
        Math.Round(summary.Allocation.Sum(a => a.Weight), 6).Should().Be(1m);
    }

    [Fact]
    public async Task Holdings_list_returns_gold_with_correct_metrics()
    {
        var client = ClientAs(Investor);

        var holdings = await client.GetFromJsonAsync<List<HoldingDto>>("/api/holdings", Json);

        holdings.Should().NotBeNull().And.HaveCount(7);
        var gold = holdings!.Single(h => h.AssetType == AssetType.Gold);
        gold.Quantity.Should().Be(40m);
        gold.AvgCost.Should().Be(4546.275m);
        gold.TotalCost.Should().Be(181851m);
        gold.CurrentValue.Should().Be(260000m);
        Math.Round(gold.ReturnRatio!.Value, 2).Should().Be(0.43m); // +%43
        Math.Round(gold.Weight, 3).Should().Be(0.310m);

        // BES kalemi devlet katkısını AYRI taşır (03 §A).
        var bes = holdings.Single(h => h.AssetType == AssetType.Bes);
        bes.Bes.Should().NotBeNull();
        bes.Bes!.StateContribution.Should().Be(28554m);
        bes.Bes.OwnContribution.Should().Be(120000m);

        // USD-fiyatlı hisse: birim alanlar USD (ham), toplulaştırma baz TRY'ye çevrilir (T1.3).
        var aapl = holdings.Single(h => h.AssetType == AssetType.Stock);
        aapl.Currency.Should().Be(CurrencyCode.USD);
        aapl.AvgCost.Should().Be(175m);          // ham (USD)
        aapl.CurrentPrice.Should().Be(210m);     // ham (USD)
        aapl.TotalCost.Should().Be(100800m);     // 12 × 175 × 48 (TRY)
        aapl.CurrentValue.Should().Be(120960m);  // 12 × 210 × 48 (TRY)
        Math.Round(aapl.ReturnRatio!.Value, 2).Should().Be(0.20m);

        // Zarardaki fon → negatif getiri.
        var fund = holdings.Single(h => h.AssetType == AssetType.Fund);
        fund.ReturnRatio!.Value.Should().BeNegative();
    }

    // ── CRUD akışı ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_then_add_transaction_then_update_then_delete()
    {
        var client = ClientAs(Investor);

        // CREATE: yeni fon pozisyonu (ilk işlem Buy 100 @ 10 → ort. maliyet 10)
        var create = new CreateHoldingRequest(
            AssetType.Fund, "Test Fonu", Symbol: null, CurrencyCode.TRY, "adet",
            new TransactionRequest(TransactionType.Buy, 100m, 10m));
        var createResp = await client.PostAsJsonAsync("/api/holdings", create, Json);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<HoldingDto>(Json);
        created!.Quantity.Should().Be(100m);
        created.AvgCost.Should().Be(10m);
        created.CurrentPrice.Should().BeNull(); // fiyat henüz girilmedi

        // ADD TRANSACTION: Buy 100 @ 20 → toplam 200, ağırlıklı ort. 15
        var addResp = await client.PostAsJsonAsync($"/api/holdings/{created.Id}/transactions",
            new TransactionRequest(TransactionType.Buy, 100m, 20m), Json);
        addResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await addResp.Content.ReadFromJsonAsync<HoldingDto>(Json);
        updated!.Quantity.Should().Be(200m);
        updated.AvgCost.Should().Be(15m);

        // UPDATE: güncel fiyat 25 → değer 200 × 25 = 5.000
        var putResp = await client.PutAsJsonAsync($"/api/holdings/{created.Id}",
            new UpdateHoldingRequest(25m), Json);
        putResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var priced = await putResp.Content.ReadFromJsonAsync<HoldingDto>(Json);
        priced!.CurrentPrice.Should().Be(25m);
        priced.CurrentValue.Should().Be(5000m);

        // DELETE: 204, sonra GET 404
        var delResp = await client.DeleteAsync($"/api/holdings/{created.Id}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var afterDelete = await client.GetAsync($"/api/holdings/{created.Id}");
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_second_position_on_same_asset_conflicts()
    {
        var client = ClientAs(Investor);

        // Yatırımcı zaten altın pozisyonuna sahip → ikinci altın pozisyonu 409.
        var create = new CreateHoldingRequest(
            AssetType.Gold, "Altın (gram)", "XAU", CurrencyCode.TRY, "gram",
            new TransactionRequest(TransactionType.Buy, 5m, 6000m));
        var resp = await client.PostAsJsonAsync("/api/holdings", create, Json);

        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var code = await ErrorCodeAsync(resp);
        code.Should().Be("CONFLICT");
    }

    // ── SC-07: Geçersiz girdi → 400 ──────────────────────────────────────────

    [Fact]
    public async Task Create_with_non_positive_quantity_returns_400()
    {
        var client = ClientAs(Investor);

        var create = new CreateHoldingRequest(
            AssetType.Fund, "Sıfır Fon", null, CurrencyCode.TRY, "adet",
            new TransactionRequest(TransactionType.Buy, 0m, 10m));
        var resp = await client.PostAsJsonAsync("/api/holdings", create, Json);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var error = doc.RootElement.GetProperty("error");
        error.GetProperty("code").GetString().Should().Be("VALIDATION_ERROR");
        error.GetProperty("details")[0].GetProperty("field").GetString().Should().Be("quantity");
    }

    [Fact]
    public async Task Create_with_empty_name_returns_400_validation()
    {
        var client = ClientAs(Investor);

        var create = new CreateHoldingRequest(
            AssetType.Fund, "", null, CurrencyCode.TRY, "adet",
            new TransactionRequest(TransactionType.Buy, 1m, 10m));
        var resp = await client.PostAsJsonAsync("/api/holdings", create, Json);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorCodeAsync(resp)).Should().Be("VALIDATION_ERROR");
    }

    // ── SC-13: IDOR — başkasının kaydı 404 ───────────────────────────────────

    [Fact]
    public async Task Idor_other_users_holding_is_404()
    {
        // Admin'in hiç pozisyonu yok; yatırımcının altın holding id'siyle erişmeye çalışır.
        var admin = ClientAs(Admin);

        var resp = await admin.GetAsync($"/api/holdings/{GoldHolding}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound); // varlığı bile sızdırma
    }

    [Fact]
    public async Task Other_user_sees_empty_list_and_zero_summary()
    {
        var admin = ClientAs(Admin);

        var holdings = await admin.GetFromJsonAsync<List<HoldingDto>>("/api/holdings", Json);
        holdings.Should().BeEmpty();

        var summary = await admin.GetFromJsonAsync<PortfolioSummaryDto>("/api/portfolio/summary", Json);
        summary!.TotalValue.Should().Be(0m);
        summary.TotalCost.Should().Be(0m);
        summary.Allocation.Should().BeEmpty();
    }

    [Fact]
    public async Task Idor_cannot_delete_other_users_holding()
    {
        var admin = ClientAs(Admin);

        var resp = await admin.DeleteAsync($"/api/holdings/{BesHolding}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── BES eğitici projeksiyon (T-BES.5) ────────────────────────────────────

    [Fact]
    public async Task Bes_projection_returns_zero_growth_for_zero_return()
    {
        var client = ClientAs(Investor);

        var resp = await client.PostAsJsonAsync(
            $"/api/holdings/{BesHolding}/bes/projection",
            new BesProjectionRequest(OwnMonthly: 1000m, Years: 1, AnnualReturnRatio: 0m), Json);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await resp.Content.ReadFromJsonAsync<BesProjectionResult>(Json);
        result!.TotalOwnContribution.Should().Be(12000m);
        // 2026 yıl içi → tüm aylar %20 oran (start UtcNow=2026-06-01 sonrası): 12 × 200 = 2.400
        result.TotalStateContribution.Should().Be(2400m);
        result.FundValue.Should().Be(14400m);
        result.OwnProfit.Should().Be(0m);
        result.StateProfit.Should().Be(0m);
        result.Yearly.Should().HaveCount(1);
    }

    [Fact]
    public async Task Bes_projection_rejected_for_non_bes_holding()
    {
        var client = ClientAs(Investor);

        var resp = await client.PostAsJsonAsync(
            $"/api/holdings/{GoldHolding}/bes/projection",
            new BesProjectionRequest(1000m, 5, 0.20m), Json);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorCodeAsync(resp)).Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task Bes_projection_idor_returns_404_for_other_user()
    {
        var admin = ClientAs(Admin);

        var resp = await admin.PostAsJsonAsync(
            $"/api/holdings/{BesHolding}/bes/projection",
            new BesProjectionRequest(1000m, 5, 0.20m), Json);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Bes_projection_invalid_input_returns_400()
    {
        var client = ClientAs(Investor);

        var resp = await client.PostAsJsonAsync(
            $"/api/holdings/{BesHolding}/bes/projection",
            new BesProjectionRequest(1000m, 51, 0.20m), Json); // 50 yıl üstü

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorCodeAsync(resp)).Should().Be("VALIDATION_ERROR");
    }

    // ── BES fon getirisi (T-BES.10): own ve state için ayrı kâr/zarar ─────────

    [Fact]
    public async Task Bes_holding_exposes_fund_return_for_own_and_state()
    {
        var client = ClientAs(Investor);

        var holdings = await client.GetFromJsonAsync<List<HoldingDto>>("/api/holdings", Json);
        var bes = holdings!.Single(h => h.AssetType == AssetType.Bes);
        bes.Bes.Should().NotBeNull();

        var own = bes.Bes!.OwnContribution;        // 120.000 (seed)
        var state = bes.Bes.StateContribution;      // 28.554 (seed)
        var fund = bes.CurrentPrice!.Value;         // 279.378 (seed)
        var costBase = own + state;                 // 148.554
        var r = fund / costBase - 1m;               // ≈ 0,8806

        // Fon getiri oranı tabandan (own+state) türetilir — saf aritmetik (yuvarlama yok).
        bes.Bes.FundReturnRatio.Should().NotBeNull();
        bes.Bes.FundReturnRatio!.Value.Should().Be(r);

        // own ve state aynı r'yi paylaşır — her birinin kâr/zararı kendi tabanı × r.
        bes.Bes.OwnProfit.Should().Be(Math.Round(own * r, 2));
        bes.Bes.StateProfit.Should().Be(Math.Round(state * r, 2));
        bes.Bes.OwnValue.Should().Be(Math.Round(own * (1m + r), 2));
        bes.Bes.StateValue.Should().Be(Math.Round(state * (1m + r), 2));

        // Birikim tabanı kontrolü: yatırılmış toplamlar (own+state) doğru taban.
        costBase.Should().Be(148554m);
    }

    // ── İşlem düzenle / sil (T-TX.1) — miktar & ort. maliyet yeniden türetilir ──

    [Fact]
    public async Task Update_transaction_recomputes_quantity_and_avg_cost()
    {
        var client = ClientAs(Investor);

        // 2 işlem: 100 @ 10 + 100 @ 20 → 200 / ort 15
        var create = await CreateFundWithTransactionsAsync(client, name: "TX Düzenle",
            new TransactionRequest(TransactionType.Buy, 100m, 10m),
            new TransactionRequest(TransactionType.Buy, 100m, 20m));
        create.Quantity.Should().Be(200m);
        create.AvgCost.Should().Be(15m);

        // İlk işlemi (Buy 100 @ 10) → Buy 100 @ 30 olarak güncelle. Beklenen ort.: (100×30 + 100×20)/200 = 25
        var firstTx = create.Transactions!.Single(t => t.UnitPrice == 10m);
        var resp = await client.PutAsJsonAsync(
            $"/api/holdings/{create.Id}/transactions/{firstTx.Id}",
            new TransactionRequest(TransactionType.Buy, 100m, 30m), Json);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await resp.Content.ReadFromJsonAsync<HoldingDto>(Json);
        updated!.Quantity.Should().Be(200m);
        updated.AvgCost.Should().Be(25m);
    }

    [Fact]
    public async Task Delete_transaction_recomputes_quantity_and_avg_cost()
    {
        var client = ClientAs(Investor);

        var create = await CreateFundWithTransactionsAsync(client, name: "TX Sil",
            new TransactionRequest(TransactionType.Buy, 100m, 10m),
            new TransactionRequest(TransactionType.Buy, 100m, 20m));

        // İkinci işlemi sil → tek alış kalır (100 @ 10).
        var secondTx = create.Transactions!.Single(t => t.UnitPrice == 20m);
        var resp = await client.DeleteAsync(
            $"/api/holdings/{create.Id}/transactions/{secondTx.Id}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterDelete = await resp.Content.ReadFromJsonAsync<HoldingDto>(Json);
        afterDelete!.Quantity.Should().Be(100m);
        afterDelete.AvgCost.Should().Be(10m);
        afterDelete.Transactions.Should().HaveCount(1);
    }

    [Fact]
    public async Task Same_day_transactions_are_listed_newest_entry_first()
    {
        var client = ClientAs(Investor);
        var sameDay = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

        // Aynı TARİHLİ iki işlem: ikincil sıralama kayıt zamanı → en SON girilen üstte
        // (2026-07-12 geri bildirimi: gün içi art arda işlemler eski-üstte görünüyordu).
        var created = await CreateFundWithTransactionsAsync(client, name: "TX Gün İçi Sıra",
            new TransactionRequest(TransactionType.Buy, 10m, 10m, 0m, sameDay),
            new TransactionRequest(TransactionType.Buy, 20m, 20m, 0m, sameDay));

        created.Transactions![0].Quantity.Should().Be(20m); // son girilen
        created.Transactions[1].Quantity.Should().Be(10m);
    }

    [Fact]
    public async Task Delete_last_transaction_returns_400_with_use_position_delete()
    {
        var client = ClientAs(Investor);

        var create = await CreateFundWithTransactionsAsync(client, name: "TX Son",
            new TransactionRequest(TransactionType.Buy, 50m, 10m));
        var onlyTx = create.Transactions!.Single();

        var resp = await client.DeleteAsync(
            $"/api/holdings/{create.Id}/transactions/{onlyTx.Id}");
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorCodeAsync(resp)).Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task Idor_cannot_update_or_delete_other_users_transaction()
    {
        var admin = ClientAs(Admin);

        // Yatırımcının seed altın holding'inden bir tx id'sini al (admin endpointe direkt vurur).
        var investorClient = ClientAs(Investor);
        var goldHolding = await investorClient.GetFromJsonAsync<HoldingDto>(
            $"/api/holdings/{GoldHolding}", Json);
        var realTxId = goldHolding!.Transactions!.First().Id;

        var putResp = await admin.PutAsJsonAsync(
            $"/api/holdings/{GoldHolding}/transactions/{realTxId}",
            new TransactionRequest(TransactionType.Buy, 1m, 1m), Json);
        putResp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var delResp = await admin.DeleteAsync(
            $"/api/holdings/{GoldHolding}/transactions/{realTxId}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_transaction_not_allowed_for_bes()
    {
        var client = ClientAs(Investor);

        var resp = await client.PutAsJsonAsync(
            $"/api/holdings/{BesHolding}/transactions/{Guid.NewGuid()}",
            new TransactionRequest(TransactionType.Buy, 1m, 1m), Json);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorCodeAsync(resp)).Should().Be("VALIDATION_ERROR");
    }

    private async Task<HoldingDto> CreateFundWithTransactionsAsync(
        HttpClient client, string name, params TransactionRequest[] transactions)
    {
        var first = transactions[0];
        var createReq = new CreateHoldingRequest(
            AssetType.Fund, name, Symbol: null, CurrencyCode.TRY, "adet", first);
        var createResp = await client.PostAsJsonAsync("/api/holdings", createReq, Json);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await createResp.Content.ReadFromJsonAsync<HoldingDto>(Json))!;

        foreach (var tx in transactions.Skip(1))
        {
            var addResp = await client.PostAsJsonAsync(
                $"/api/holdings/{created.Id}/transactions", tx, Json);
            addResp.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // İşlem id'leri için GET (DTO'da Transactions dolu döner).
        return (await client.GetFromJsonAsync<HoldingDto>($"/api/holdings/{created.Id}", Json))!;
    }

    private static async Task<string?> ErrorCodeAsync(HttpResponseMessage resp)
    {
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("error").GetProperty("code").GetString();
    }
}

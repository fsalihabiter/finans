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
/// /api/settings uçtan uca (T1.9): baz para birimi oku/güncelle, kullanıcıya kapsanma.
/// </summary>
public sealed class SettingsApiTests : IClassFixture<SqliteWebApplicationFactory>, IAsyncLifetime
{
    private readonly SqliteWebApplicationFactory _factory;
    private static readonly Guid Investor = SeedData.Id("user-1");
    private static readonly Guid Admin = SeedData.Id("admin-1");

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    { Converters = { new JsonStringEnumConverter() } };

    public SettingsApiTests(SqliteWebApplicationFactory factory) => _factory = factory;

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

    [Fact]
    public async Task Get_returns_seed_base_currency()
    {
        var client = ClientAs(Investor);

        var settings = await client.GetFromJsonAsync<SettingsDto>("/api/settings", Json);

        settings!.BaseCurrency.Should().Be(CurrencyCode.TRY);
    }

    [Fact]
    public async Task Put_updates_base_currency_and_persists()
    {
        var client = ClientAs(Admin); // yatırımcının seed'ini bozma, admin üzerinde dene

        var put = await client.PutAsJsonAsync("/api/settings", new UpdateSettingsRequest(CurrencyCode.USD), Json);
        put.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await put.Content.ReadFromJsonAsync<SettingsDto>(Json);
        updated!.BaseCurrency.Should().Be(CurrencyCode.USD);

        // Kalıcı: tekrar GET
        var fetched = await client.GetFromJsonAsync<SettingsDto>("/api/settings", Json);
        fetched!.BaseCurrency.Should().Be(CurrencyCode.USD);
    }

    [Fact]
    public async Task Settings_change_does_not_leak_across_users()
    {
        // Admin USD yapsa bile yatırımcı TRY kalır (per-user, 11 §3).
        var admin = ClientAs(Admin);
        await admin.PutAsJsonAsync("/api/settings", new UpdateSettingsRequest(CurrencyCode.EUR), Json);

        var investor = ClientAs(Investor);
        var investorSettings = await investor.GetFromJsonAsync<SettingsDto>("/api/settings", Json);
        investorSettings!.BaseCurrency.Should().Be(CurrencyCode.TRY);
    }
}

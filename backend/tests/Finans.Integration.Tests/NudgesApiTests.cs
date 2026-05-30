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
/// `GET /api/portfolio/nudges` uçtan uca (T2.5, SC-09): seed portföyünde yoğunlaşma +
/// düşük nakit notları tetiklenir; başka kullanıcı (boş portföy) hiç not almaz (per-user).
/// Fiyat sağlayıcı çağrılmaz (özet seed CurrentPrice'tan) → ağsız.
/// </summary>
public sealed class NudgesApiTests : IClassFixture<SqliteWebApplicationFactory>, IAsyncLifetime
{
    private readonly SqliteWebApplicationFactory _factory;

    private static readonly Guid Investor = SeedData.Id("user-1");
    private static readonly Guid Admin = SeedData.Id("admin-1");

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public NudgesApiTests(SqliteWebApplicationFactory factory) => _factory = factory;

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
    public async Task Seed_portfolio_returns_concentration_and_low_cash_nudges()
    {
        var client = ClientAs(Investor);

        var resp = await client.GetFromJsonAsync<NudgesResponse>("/api/portfolio/nudges", Json);

        resp.Should().NotBeNull();
        resp!.Nudges.Should().Contain(n => n.Id == "concentration"); // BES+Altın ~%64
        resp.Nudges.Should().Contain(n => n.Id == "low-cash");       // nakit ~%0,7
    }

    [Fact]
    public async Task Other_user_with_empty_portfolio_has_no_nudges()
    {
        var admin = ClientAs(Admin);

        var resp = await admin.GetFromJsonAsync<NudgesResponse>("/api/portfolio/nudges", Json);

        resp!.Nudges.Should().BeEmpty();
    }
}

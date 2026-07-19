using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Finans.Domain.Enums;
using Finans.Domain.Identity;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Finans.Integration.Tests;

/// <summary>
/// Tanılama testi uçları (T6.6, SC-E14, 15 §4). Profil <b>UserId kapsamlı</b> (11 §3);
/// cevap anahtarı sızmaz; ⚠ <b>RiskAttitude hiçbir yanıtta görünmez</b> (SC-E4, 15 §1.1).
/// </summary>
public sealed class DiagnosticApiTests : IClassFixture<SqliteWebApplicationFactory>, IAsyncLifetime
{
    private readonly SqliteWebApplicationFactory _factory;

    public DiagnosticApiTests(SqliteWebApplicationFactory factory) => _factory = factory;

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

    private async Task<Guid> NewLearnerAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
        var user = new User { DisplayName = "Tanılama Test", BaseCurrency = CurrencyCode.TRY, IsActive = true };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage resp) =>
        JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement.Clone();

    private static object AllCorrect() => new
    {
        answers = new[]
        {
            new { questionKey = "real-return", optionKey = "decreased" },
            new { questionKey = "concentration", optionKey = "concentration" },
            new { questionKey = "pe-ratio", optionKey = "context" },
            new { questionKey = "compound", optionKey = "144" },
            new { questionKey = "drawdown", optionKey = "buy" },
            new { questionKey = "fomo", optionKey = "missed" },
            new { questionKey = "horizon", optionKey = "long" },
            new { questionKey = "volatility", optionKey = "little" },
        },
    };

    [Fact]
    public async Task Questions_are_served_without_the_answer_key()
    {
        var resp = await ClientAs(await NewLearnerAsync()).GetAsync("/api/education/diagnostic");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var raw = await resp.Content.ReadAsStringAsync();
        raw.Should().NotContain("isCorrect");
        raw.Should().NotContain("riskPoints");

        var qs = JsonDocument.Parse(raw).RootElement;
        qs.GetArrayLength().Should().Be(8);
        qs.EnumerateArray().Select(q => q.GetProperty("kind").GetString())
            .Should().Contain(["Knowledge", "Scenario"]);
    }

    [Fact]
    public async Task Submitting_all_correct_sets_advanced_level_and_marks_profiled()
    {
        var userId = await NewLearnerAsync();
        var client = ClientAs(userId);

        (await JsonAsync(await client.GetAsync("/api/education/profile")))
            .GetProperty("profiled").GetBoolean().Should().BeFalse();

        var result = await JsonAsync(await client.PostAsJsonAsync("/api/education/diagnostic", AllCorrect()));
        result.GetProperty("literacyLevel").GetString().Should().Be("Advanced");
        result.GetProperty("message").GetString().Should().NotBeNullOrEmpty();

        var profile = await JsonAsync(await client.GetAsync("/api/education/profile"));
        profile.GetProperty("profiled").GetBoolean().Should().BeTrue();
        profile.GetProperty("literacyLevel").GetString().Should().Be("Advanced");
    }

    [Fact]
    public async Task Risk_attitude_is_stored_but_never_returned()
    {
        // 🔒 SC-E4 — SPK sınırı: tutum DB'ye yazılır, HİÇBİR yanıtta görünmez.
        var userId = await NewLearnerAsync();
        var client = ClientAs(userId);

        var submitRaw = await (await client.PostAsJsonAsync("/api/education/diagnostic", AllCorrect()))
            .Content.ReadAsStringAsync();
        var profileRaw = await (await client.GetAsync("/api/education/profile")).Content.ReadAsStringAsync();
        var questionsRaw = await (await client.GetAsync("/api/education/diagnostic")).Content.ReadAsStringAsync();

        foreach (var body in new[] { submitRaw, profileRaw, questionsRaw })
        {
            body.Should().NotContainEquivalentOf("riskAttitude");
            foreach (var label in Enum.GetNames<RiskAttitude>())
                body.Should().NotContainEquivalentOf(label);
        }

        // …ama DB'ye yazılmış olmalı (ders sırası için gerekiyor).
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
        var user = await db.Users.SingleAsync(u => u.Id == userId);
        user.RiskAttitude.Should().Be(Domain.Enums.RiskAttitude.Atilgan);
        user.ProfiledAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Skipping_the_test_leaves_beginner_level()
    {
        // Atlanabilir (14 §4-A2): boş liste → Başlangıç, hata yok.
        var client = ClientAs(await NewLearnerAsync());

        var result = await JsonAsync(await client.PostAsJsonAsync(
            "/api/education/diagnostic", new { answers = Array.Empty<object>() }));

        result.GetProperty("literacyLevel").GetString().Should().Be("Beginner");
    }

    [Fact]
    public async Task Unknown_question_or_option_is_400()
    {
        var client = ClientAs(await NewLearnerAsync());

        var badQ = await client.PostAsJsonAsync("/api/education/diagnostic",
            new { answers = new[] { new { questionKey = "uydurma", optionKey = "x" } } });
        badQ.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var badO = await client.PostAsJsonAsync("/api/education/diagnostic",
            new { answers = new[] { new { questionKey = "real-return", optionKey = "uydurma" } } });
        badO.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Profile_is_isolated_per_user()
    {
        // 11 §3 — bir kullanıcının profili diğerine sızmaz.
        var a = ClientAs(await NewLearnerAsync());
        var bId = await NewLearnerAsync();
        var b = ClientAs(bId);

        await a.PostAsJsonAsync("/api/education/diagnostic", AllCorrect());

        var bProfile = await JsonAsync(await b.GetAsync("/api/education/profile"));
        bProfile.GetProperty("profiled").GetBoolean().Should().BeFalse();
    }
}

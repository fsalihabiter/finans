using FluentAssertions;
using Finans.Domain.Portfolio;
using Finans.Infrastructure.Persistence;
using Finans.Infrastructure.Seed;
using Finans.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Finans.Integration.Tests;

/// <summary>
/// T-BES.6b ileri: <see cref="BesPlanCatchUpRunner"/> arka plan job tarafından da çağrılacak —
/// bu yüzden <see cref="HoldingService"/> dışında, <c>ICurrentUser</c>'dan bağımsız doğrudan
/// çalışabildiğini doğrularız. Sistem akışında (cron tiki) DI'dan çözülür ve verili holding'i
/// ilerletir. **Kendi izole fixture'ı** (BES state'i değiştirir; diğer sınıfları etkilemesin).
/// </summary>
public sealed class BesPlanCatchUpRunnerTests : IClassFixture<SqliteWebApplicationFactory>, IAsyncLifetime
{
    private readonly SqliteWebApplicationFactory _factory;
    private static readonly Guid BesHolding = SeedData.Id("holding-bes");

    public BesPlanCatchUpRunnerTests(SqliteWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
        await db.Database.EnsureCreatedAsync();
        await SeedData.SeedAsync(db);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Catches_up_missing_plan_months_when_active()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
        var runner = scope.ServiceProvider.GetRequiredService<BesPlanCatchUpRunner>();

        // Planı aç — kullanıcı önceki bir tikte / GET'te yapmamış (uzun süredir uygulamayı açmadı).
        var holding = await db.Holdings
            .Include(h => h.BesDetails)
            .Include(h => h.BesContributions)
            .FirstAsync(h => h.Id == BesHolding);
        var bes = holding.BesDetails!;
        bes.MonthlyAmount = 500m;
        bes.ContributionDay = 10;
        bes.PlanActive = true;
        await db.SaveChangesAsync();

        var existingPlanCount = holding.BesContributions.Count(c => c.Source == "Plan");

        // Şimdi: arka plan tiki çalışıyormuş gibi runner'ı doğrudan çağır. 3 ay sonrası "now".
        var nowTr = new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc);
        var added = runner.CatchUp(holding, nowTr);
        await db.SaveChangesAsync();

        // Beklenen: bu fixture içinde önceki testler de plan satırı eklemiş olabilir → en az 1
        // yeni satır eklenmiş olmalı (sınıf fixture'ı paylaşıldığı için kesin sayı vermek kırılgan).
        added.Should().BeGreaterThan(0, "uzun süredir tetiklenmemiş plan en az bir eksik ayı doldurmalı");

        // Yeniden çalıştır → idempotent (aynı now ile artış yok).
        var againAdded = runner.CatchUp(holding, nowTr);
        againAdded.Should().Be(0, "aynı 'now' ile ikinci tik aynı ayı yeniden eklemez (idempotent)");

        // Kayıtlı plan satırlarının hepsi `Source=Plan` ve aylık 500 own (cap ayrı senaryosu).
        var refreshed = await db.BesContributions
            .Where(c => c.HoldingId == BesHolding && c.Source == "Plan")
            .ToListAsync();
        refreshed.Count.Should().BeGreaterThan(existingPlanCount);
        refreshed.Should().OnlyContain(c => c.OwnAmount == 500m);
    }

    [Fact]
    public async Task No_op_when_plan_inactive()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
        var runner = scope.ServiceProvider.GetRequiredService<BesPlanCatchUpRunner>();

        var holding = await db.Holdings
            .Include(h => h.BesDetails)
            .Include(h => h.BesContributions)
            .FirstAsync(h => h.Id == BesHolding);
        holding.BesDetails!.PlanActive = false;
        await db.SaveChangesAsync();

        var added = runner.CatchUp(holding, new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        added.Should().Be(0, "plan kapalıyken arka plan job hiçbir satır eklemez");
    }
}

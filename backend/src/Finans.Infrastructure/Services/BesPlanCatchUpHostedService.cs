using Finans.Domain.Enums;
using Finans.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Finans.Infrastructure.Services;

/// <summary>
/// Arka plan job (T-BES.6b ileri): aktif düzenli BES katkı planlarını periyodik olarak ilerletir.
/// <para>
/// Önceki davranış (lazy catch-up <see cref="HoldingService"/>'te) sayfa açılınca / GET'te tetikleniyordu:
/// kullanıcı uygulamayı haftalarca açmazsa plan kaydı oluşmuyordu. Bu servis "uygulama açıkken" plan
/// gecikmesini bitirir. Gerçek anlamda "uygulama kapalıyken" çalışmak için süreç ayakta olmalı — yani
/// bu hâliyle kullanıcının kendi tarayıcısı kapalıyken bile sunucu (compose/VPS) açık olduğu sürece
/// plan ilerler (lazy yola ek olarak). Production'da bu yeterli; gelecekte daha sağlam bir cron için
/// Hangfire/Quartz değerlendirilebilir (12 §9).
/// </para>
/// Konfig:
/// <list type="bullet">
///   <item><c>Bes:PlanCatchUp:Enabled</c> (varsayılan true) — testlerde false.</item>
///   <item><c>Bes:PlanCatchUp:IntervalHours</c> (varsayılan 6) — tik sıklığı.</item>
///   <item><c>Bes:PlanCatchUp:InitialDelaySeconds</c> (varsayılan 60) — host start'tan sonra ilk tik gecikmesi
///     (migration/seed bitsin, healthcheck'i bozmasın).</item>
/// </list>
/// </summary>
public sealed class BesPlanCatchUpHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<BesPlanCatchUpOptions> options,
    ILogger<BesPlanCatchUpHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = options.Value;
        if (!opts.Enabled)
        {
            logger.LogInformation("BES plan catch-up job devre dışı (Bes:PlanCatchUp:Enabled=false).");
            return;
        }

        var interval = TimeSpan.FromHours(Math.Max(1, opts.IntervalHours));
        var initialDelay = TimeSpan.FromSeconds(Math.Max(0, opts.InitialDelaySeconds));
        logger.LogInformation(
            "BES plan catch-up başlıyor: ilk tik +{Initial}, periyot {Interval}.",
            initialDelay, interval);

        try { await Task.Delay(initialDelay, stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Tik bütünüyle çöktü — bir sonraki periyotta yeniden denenir (12 §3).
                logger.LogError(ex, "BES plan catch-up tiki çöktü; bir sonraki periyotta denenecek.");
            }

            try { await Task.Delay(interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    /// <summary>
    /// Bir tik: tüm aktif planlı BES holding'leri tara, her birini izole transaction'da ilerlet.
    /// Bir holding'in hatası diğerlerini düşürmez (per-holding try/catch — 11 §4 hata sızdırma yok).
    /// </summary>
    private async Task RunTickAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FinansDbContext>();
        var runner = scope.ServiceProvider.GetRequiredService<BesPlanCatchUpRunner>();

        // Sadece aktif plan + BES; küçük çalışma kümesi (binlerce kullanıcıda bile bu sorgu hızlı).
        var holdingIds = await db.Holdings
            .Where(h => h.Asset.Type == AssetType.Bes
                        && h.BesDetails != null
                        && h.BesDetails.PlanActive
                        && h.BesDetails.MonthlyAmount != null
                        && h.BesDetails.ContributionDay != null)
            .Select(h => h.Id)
            .ToListAsync(ct);

        if (holdingIds.Count == 0)
        {
            logger.LogDebug("BES plan catch-up tiki: aktif plan yok.");
            return;
        }

        var nowTr = DateTime.UtcNow.AddHours(3);
        var totalAdded = 0;
        var processed = 0;

        foreach (var id in holdingIds)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                // Her holding için kendi scope'unda tracker'ı temiz tut: büyük listelerde aynı
                // DbContext'i şişirmeyelim. (Şimdilik tek scope yeterli; ölçekleme gerekirse
                // her holding için ayrı scope açılabilir.)
                var holding = await db.Holdings
                    .Include(h => h.BesDetails)
                    .Include(h => h.BesContributions)
                    .FirstOrDefaultAsync(h => h.Id == id, ct);
                if (holding is null) continue;

                var added = runner.CatchUp(holding, nowTr);
                if (added > 0)
                {
                    await db.SaveChangesAsync(ct);
                    totalAdded += added;
                }
                processed++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "BES plan catch-up: holding {HoldingId} işlenirken hata.", id);
            }
        }

        if (totalAdded > 0)
            logger.LogInformation(
                "BES plan catch-up tiki bitti: {Processed} holding tarandı, {Added} katkı eklendi.",
                processed, totalAdded);
    }
}

/// <summary>Bkz. <see cref="BesPlanCatchUpHostedService"/> sınıf XML'i.</summary>
public sealed class BesPlanCatchUpOptions
{
    public const string SectionName = "Bes:PlanCatchUp";

    public bool Enabled { get; set; } = true;
    public int IntervalHours { get; set; } = 6;
    public int InitialDelaySeconds { get; set; } = 60;
}

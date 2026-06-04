using Finans.Application.Portfolio;
using Finans.Domain.Portfolio;
using Finans.Infrastructure.Persistence;

namespace Finans.Infrastructure.Services;

/// <summary>
/// Düzenli BES katkı planının "tarih geldikçe ödenmiş sayılır" mantığı (T-BES.6b). Kullanıcı kapsamı
/// YOK — verili (önceden yüklenmiş, includes'lı) <see cref="Holding"/> üzerinde çalışır. Bu sayede:
/// <list type="bullet">
///   <item><b>Per-user akış</b> (sayfa açılınca/GET'te) — <see cref="HoldingService"/> kullanıcıyı
///     kapsayarak yükler, sonra delege eder.</item>
///   <item><b>Sistem akışı</b> (arka plan job, uygulama kapalıyken bile gecikmesin) —
///     <c>BesPlanCatchUpHostedService</c> tüm <c>PlanActive=true</c> holding'leri iter eder.</item>
/// </list>
/// EF v7+ tracked koleksiyon tuzağı (memory): tracked parent'a çocuk eklerken
/// <c>db.BesContributions.Add(...)</c> şart — bu yüzden runner DbContext alır.
/// Caller `SaveChangesAsync` çağırır (transactional kontrol caller'da).
/// </summary>
public sealed class BesPlanCatchUpRunner(FinansDbContext db)
{
    /// <summary>"Plan" türevli (otomatik üretilen) kaynak — manuel girişler engel değil.</summary>
    private const string PlanSource = "Plan";

    /// <summary>
    /// Verili holding için eksik plan aylarını ekler. Plan değilse / aktif değilse no-op (0).
    /// <paramref name="nowTr"/> Türkiye yereli "şimdi" (UTC+3 sabit; T-BES.9 fix: gün geçişinde
    /// kullanıcının pencerede gördüğü gün esas alınır).
    /// </summary>
    /// <returns>Eklenen satır sayısı (0 → no-op; caller SaveChanges çağırsın mı kararını verir).</returns>
    public int CatchUp(Holding holding, DateTime nowTr)
    {
        if (holding.BesDetails is not { PlanActive: true, MonthlyAmount: { } amount, ContributionDay: { } day })
            return 0;

        // Plan-source dedup: manuel girişler düzenli planı engellemez. Yalnız "Plan" türevli satırlar
        // lastPaid/covered için sayılır. lastPlanPaid yoksa "bu aydan" başla (geçmiş aylar için plan
        // satırı geriye dönük üretilmez; backfill için "Düzenli katkı/geçmiş" formu var).
        var lastPlanPaid = holding.BesContributions
            .Where(c => c.Source == PlanSource)
            .Select(c => (DateTime?)c.PaidAtUtc)
            .DefaultIfEmpty(null)
            .Max();
        var from = lastPlanPaid is { } lp
            ? new DateTime(lp.Year, lp.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1)
            : new DateTime(nowTr.Year, nowTr.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var dates = BesContributionPlanner.MonthlyDates(day, from, nowTr);
        if (dates.Count == 0)
            return 0;

        var coveredPlanMonths = holding.BesContributions
            .Where(c => c.Source == PlanSource)
            .Select(c => c.PaidAtUtc.Year * 100 + c.PaidAtUtc.Month)
            .ToHashSet();
        // T-BES.4: yıl bazlı state kümülatifi (tüm kaynaklar). Her ay için cap uygulanır; aynı yıl
        // içinde tavan dolduktan sonra otomatik kayıt 0 state ile devam eder.
        var stateByYear = new Dictionary<int, decimal>();
        foreach (var existing in holding.BesContributions)
            stateByYear[existing.PaidAtUtc.Year] = stateByYear.GetValueOrDefault(existing.PaidAtUtc.Year, 0m) + existing.StateAmount;

        var bes = holding.BesDetails;
        var added = 0;
        foreach (var date in dates)
        {
            if (!coveredPlanMonths.Add(date.Year * 100 + date.Month))
                continue;
            var rawState = BesCalculator.StateContributionFor(amount, date);
            var alreadyInYear = stateByYear.GetValueOrDefault(date.Year, 0m);
            var state = BesCalculator.ApplyAnnualStateCap(rawState, date.Year, alreadyInYear);
            db.BesContributions.Add(new BesContribution
            {
                HoldingId = holding.Id,
                OwnAmount = amount,
                StateAmount = state,
                PaidAtUtc = date,
                Source = PlanSource,
                CreatedAtUtc = DateTime.UtcNow,
            });
            bes.OwnContribution += amount;
            bes.StateContribution += state;
            stateByYear[date.Year] = alreadyInYear + state;
            added++;
        }

        if (added > 0)
        {
            // Hak ediş + maliyet (cepten = own) + zaman damgası; HoldingService'te de aynı kural.
            bes.VestingState = BesCalculator.VestingStateFor(bes.JoinedAtUtc, DateTime.UtcNow);
            holding.AvgCost = bes.OwnContribution;
            holding.UpdatedAtUtc = DateTime.UtcNow;
        }

        return added;
    }
}

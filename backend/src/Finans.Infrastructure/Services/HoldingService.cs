using Finans.Application.Common;
using Finans.Application.Portfolio;
using Finans.Domain.Enums;
using Finans.Domain.Portfolio;
using Finans.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Finans.Infrastructure.Services;

/// <summary>
/// <see cref="IHoldingService"/> EF uygulaması. **Tüm sorgular geçerli kullanıcıya
/// kapsanır** (11 §3): başkasının id'si → <see cref="NotFoundException"/> (IDOR yok,
/// SC-13). Miktar/ort. maliyet işlemlerden türetilir (T1.5). Para birimi dönüşümü T1.3.
/// </summary>
public sealed class HoldingService(
    FinansDbContext db,
    ICurrentUser currentUser,
    PortfolioCalculationService calc,
    IFxRateProvider fxRateProvider) : IHoldingService
{
    /// <summary>
    /// TR yerel "şimdi" (UTC+3 sabit; Türkiye 2016'dan beri DST uygulamıyor). BES tarih
    /// karşılaştırmalarında — kullanıcının pencerede gördüğü gün — gün geçişinde yanıltıcı
    /// "Future/StatePending" yaşamasın diye `DateTime.UtcNow` yerine kullanılır (T-BES.9 fix).
    /// </summary>
    private static DateTime TrNow() => DateTime.UtcNow.AddHours(3);

    /// <summary>"Plan" türevli (otomatik üretilen) kaynaklar: düzenli plan dedup'unda kullanılır.</summary>
    private static bool IsPlanSource(string source) => source == "Plan";

    public async Task<IReadOnlyList<HoldingDto>> GetAllAsync(CurrencyCode? baseCurrency = null, CancellationToken ct = default) =>
        await BuildHoldingDtosAsync(baseCurrency, ct);

    public async Task<HoldingDto> GetByIdAsync(Guid id, CurrencyCode? baseCurrency = null, CancellationToken ct = default)
    {
        // Aktif düzenli plan varsa, gün geldikçe eksik ayları otomatik üret ("tarih geldikçe ödenmiş sayılır").
        await CatchUpBesPlanAsync(id, ct);

        var all = await BuildHoldingDtosAsync(baseCurrency, ct);
        var dto = all.FirstOrDefault(h => h.Id == id)
            ?? throw new NotFoundException();

        // Detayda geçmiş işlemler (en yeni üstte). Holding zaten kullanıcıya kapsanmış (yukarıda).
        var transactions = await db.Transactions
            .Where(t => t.HoldingId == id)
            .OrderByDescending(t => t.TransactedAtUtc)
            .Select(t => new TransactionDto(t.Id, t.Type, t.Quantity, t.UnitPrice, t.Fee, t.TransactedAtUtc))
            .ToListAsync(ct);

        return dto with { Transactions = transactions };
    }

    public async Task<HoldingDto> CreateAsync(CreateHoldingRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateTransaction(request.Transaction, isFirst: true);

        var userId = currentUser.UserId;
        var now = DateTime.UtcNow;

        // Varlık kataloğu kullanıcıdan bağımsız: eşleşeni bul, yoksa oluştur (03 §A).
        var asset = await FindOrCreateAssetAsync(request, ct);

        // Aynı varlıkta ikinci aktif pozisyon → 409 (unique index ile de korunur, 04 §2).
        var alreadyHeld = await db.Holdings.AnyAsync(h => h.UserId == userId && h.AssetId == asset.Id, ct);
        if (alreadyHeld)
            throw new ConflictException("Bu varlıkta zaten bir pozisyonunuz var. Mevcut pozisyona işlem ekleyin.");

        var holding = new Holding { UserId = userId, AssetId = asset.Id, CurrentPrice = null, CreatedAtUtc = now };
        holding.Transactions.Add(ToEntity(request.Transaction, holding.Id, now));
        ApplyDerivedPosition(holding);

        db.Holdings.Add(holding);
        await SaveHandlingConflictAsync(ct);

        return await GetByIdAsync(holding.Id, request.Currency, ct);
    }

    public async Task<HoldingDto> CreateBesAsync(CreateBesRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("name", "required", "Plan adı zorunludur.");
        if (request.CurrentFundValue < 0m)
            throw new ValidationException("currentFundValue", "must_be_non_negative", "Fon değeri negatif olamaz.");
        if (request.OpeningOwn < 0m || request.OpeningState < 0m)
            throw new ValidationException("opening", "must_be_non_negative", "Açılış katkı tutarları negatif olamaz.");
        if (request.JoinedAtUtc > DateTime.UtcNow)
            throw new ValidationException("joinedAtUtc", "must_not_be_future", "Başlangıç tarihi gelecekte olamaz.");
        if (request.ContributionDay is { } cd && cd is < 1 or > 28)
            throw new ValidationException("contributionDay", "out_of_range", "Ödeme günü 1–28 arasında olmalı.");

        var userId = currentUser.UserId;
        var now = DateTime.UtcNow;

        // BES varlığı (kullanıcıdan bağımsız katalog): eşleşeni bul, yoksa oluştur.
        var asset = await db.Assets.FirstOrDefaultAsync(
            a => a.Type == AssetType.Bes && a.Name == request.Name.Trim() && a.PricingCurrency == request.Currency, ct);
        if (asset is null)
        {
            asset = new Asset
            {
                Type = AssetType.Bes,
                Name = request.Name.Trim(),
                Symbol = null,
                Unit = "birim",
                PricingCurrency = request.Currency,
                CreatedAtUtc = now,
            };
            db.Assets.Add(asset);
        }

        var alreadyHeld = await db.Holdings.AnyAsync(h => h.UserId == userId && h.AssetId == asset.Id, ct);
        if (alreadyHeld)
            throw new ConflictException("Bu BES planında zaten bir pozisyonunuz var.");

        var planActive = request.MonthlyAmount is > 0m && request.ContributionDay is not null;

        var holding = new Holding
        {
            UserId = userId,
            AssetId = asset.Id,
            Quantity = 1m,
            AvgCost = request.OpeningOwn,            // okuma yolunda yeniden türetilir; tutarlı başlat
            CurrentPrice = request.CurrentFundValue, // BES "güncel fiyat" = toplam fon değeri (miktar 1)
            CreatedAtUtc = now,
        };
        holding.BesDetails = new BesDetails
        {
            HoldingId = holding.Id,
            OwnContribution = request.OpeningOwn,
            StateContribution = request.OpeningState,
            VestingState = BesCalculator.VestingStateFor(request.JoinedAtUtc, now),
            ProviderName = string.IsNullOrWhiteSpace(request.ProviderName) ? null : request.ProviderName.Trim(),
            JoinedAtUtc = request.JoinedAtUtc,
            BirthYear = request.BirthYear,
            MonthlyAmount = request.MonthlyAmount,
            ContributionDay = request.ContributionDay,
            PlanActive = planActive,
        };
        // Açılış bakiyesi = tek "Opening" katkı kaydı (başlangıç tarihli → yatırılmış sayılır).
        // Geçmiş tek tek girilmez; verilen güncel toplamlar bu satıra yazılır.
        holding.BesContributions.Add(new BesContribution
        {
            HoldingId = holding.Id,
            OwnAmount = request.OpeningOwn,
            StateAmount = request.OpeningState,
            PaidAtUtc = request.JoinedAtUtc,
            Source = "Opening",
            CreatedAtUtc = now,
        });

        db.Holdings.Add(holding);
        await SaveHandlingConflictAsync(ct);

        return await GetByIdAsync(holding.Id, request.Currency, ct);
    }

    public async Task<HoldingDto> AddTransactionAsync(Guid id, TransactionRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateTransaction(request, isFirst: false);

        var holding = await LoadOwnedWithTransactionsAsync(id, ct);

        // BES nominal hesaptır; alış/satış maliyet türetimini bozar → engelle (aylık katkı kullan).
        if (holding.Asset.Type == AssetType.Bes)
            throw new ValidationException("transaction", "not_allowed_for_bes",
                "BES pozisyonuna alış/satış eklenemez. 'Aylık katkı ekle'yi kullanın.");

        var now = DateTime.UtcNow;

        var transaction = ToEntity(request, holding.Id, now);
        holding.Transactions.Add(transaction); // türetme koleksiyonu görsün
        // Açıkça Added: Entity.Id v7 ile baştan dolu olduğundan, izlenen holding'in
        // koleksiyonuna eklenen yeni kaydı EF "mevcut" sanıp UPDATE'e çevirir (0 row →
        // sahte concurrency). Create'te db.Add tüm grafiği Added yaptığı için sorun yok.
        db.Entry(transaction).State = EntityState.Added;
        ApplyDerivedPosition(holding);
        holding.UpdatedAtUtc = now;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(holding.Id, ct: ct);
    }

    public async Task<HoldingDto> UpdateTransactionAsync(
        Guid id, Guid transactionId, TransactionRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateTransaction(request, isFirst: false);

        var holding = await LoadOwnedWithTransactionsAsync(id, ct);

        // BES nominal hesaptır; düz işlem yok (AddTransactionAsync ile aynı simetri).
        if (holding.Asset.Type == AssetType.Bes)
            throw new ValidationException("transaction", "not_allowed_for_bes",
                "BES pozisyonunda alış/satış işlemi düzenlenemez.");

        var transaction = holding.Transactions.FirstOrDefault(t => t.Id == transactionId)
            ?? throw new NotFoundException();

        // Tarih bağlamı — null verilirse mevcut tarih korunur.
        transaction.Type = request.Type;
        transaction.Quantity = request.Quantity;
        transaction.UnitPrice = request.UnitPrice;
        transaction.Fee = request.Fee;
        if (request.Date is { } d)
            transaction.TransactedAtUtc = d;

        // Pozisyonu yeniden türet — kalan dizi negatif miktar verirse 400 atar (ApplyDerivedPosition).
        ApplyDerivedPosition(holding);
        holding.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(holding.Id, ct: ct);
    }

    public async Task<HoldingDto> DeleteTransactionAsync(Guid id, Guid transactionId, CancellationToken ct = default)
    {
        var holding = await LoadOwnedWithTransactionsAsync(id, ct);

        if (holding.Asset.Type == AssetType.Bes)
            throw new ValidationException("transaction", "not_allowed_for_bes",
                "BES pozisyonunda alış/satış işlemi silinemez.");

        var transaction = holding.Transactions.FirstOrDefault(t => t.Id == transactionId)
            ?? throw new NotFoundException();

        // Son işlemi silmek → pozisyon "boş" kalır (Qty=0). Kullanıcının niyetiyle örtüşmez:
        // bu pozisyonu silmek istiyorsa "Pozisyonu sil" düğmesi var. Yanlışlıkla boş bırakmayı engelle.
        if (holding.Transactions.Count <= 1)
            throw new ValidationException("transaction", "cannot_delete_last",
                "Son işlemi silemezsiniz. Pozisyonu tamamen kaldırmak için 'Pozisyonu sil'i kullanın.");

        holding.Transactions.Remove(transaction);
        db.Transactions.Remove(transaction);

        // Yeniden türet — negatif miktar olursa 400 (satış zincirini kırarsa kullanıcı uyarılır).
        ApplyDerivedPosition(holding);
        holding.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(holding.Id, ct: ct);
    }

    public async Task<HoldingDto> UpdateAsync(Guid id, UpdateHoldingRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.CurrentPrice is < 0m)
            throw new ValidationException("currentPrice", "must_be_non_negative", "Güncel fiyat negatif olamaz.");

        var holding = await LoadOwnedAsync(id, ct);
        holding.CurrentPrice = request.CurrentPrice;
        holding.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(holding.Id, ct: ct);
    }

    public async Task<HoldingDto> AddBesContributionAsync(Guid id, AddBesContributionRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.OwnAmount <= 0m)
            throw new ValidationException("ownAmount", "must_be_positive", "Katkı tutarı 0'dan büyük olmalı.");
        if (request.StateAmount is < 0m)
            throw new ValidationException("stateAmount", "must_be_non_negative", "Devlet katkısı negatif olamaz.");

        var holding = await db.Holdings
            .Include(h => h.Asset)
            .Include(h => h.BesDetails)
            .Include(h => h.BesContributions) // T-BES.4: yıllık state toplamı için
            .FirstOrDefaultAsync(h => h.Id == id && h.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException();

        if (holding.Asset.Type != AssetType.Bes || holding.BesDetails is null)
            throw new ValidationException("id", "not_a_bes", "Bu pozisyon bir BES hesabı değil.");

        // Ödeme tarihi (verilmezse şimdi). İleri tarihli katkı serbest (ileriye dönük planlama;
        // kullanıcı henüz ödenmemiş/ileride ödenecek katkıyı da girebilir). Devlet katkısı oranı
        // bu tarihe göre seçilir (geriye dönük değil).
        var paidAt = request.PaidAtUtc ?? DateTime.UtcNow;

        // Devlet katkısı verilmezse, katkının ödendiği tarihteki orana göre (2026 öncesi %30, sonrası
        // %20) — BesRules/BesCalculator. **T-BES.4:** yıllık brüt asgari ücretin %20'si üst sınırı
        // uygulanır; aynı takvim yılındaki diğer state toplamına göre kesme yapılır (kota dolduysa 0).
        var rawState = request.StateAmount ?? BesCalculator.StateContributionFor(request.OwnAmount, paidAt);
        var alreadyInYear = holding.BesContributions
            .Where(c => c.PaidAtUtc.Year == paidAt.Year)
            .Sum(c => c.StateAmount);
        var stateAmount = BesCalculator.ApplyAnnualStateCap(rawState, paidAt.Year, alreadyInYear);

        var bes = holding.BesDetails;
        bes.OwnContribution += request.OwnAmount;
        bes.StateContribution += stateAmount;
        // Hak ediş durumu sistemde kalış süresinden türetilir (saf hesap).
        bes.VestingState = BesCalculator.VestingStateFor(bes.JoinedAtUtc, DateTime.UtcNow);

        // Tek katkı kaydı (işlem geçmişi + düzenli katkı takibi, T-BES.6).
        db.BesContributions.Add(new BesContribution
        {
            HoldingId = holding.Id,
            OwnAmount = request.OwnAmount,
            StateAmount = stateAmount,
            PaidAtUtc = paidAt,
            Source = "Manual",
            CreatedAtUtc = DateTime.UtcNow,
        });

        // "Bundan sonraki katkılar için kullan" → düzenli plan (bu tutar/gün; bitiş yok). Tutar
        // değiştirilene kadar geçerli; gün geldikçe görüntülemede otomatik kayıt (CatchUpBesPlanAsync).
        if (request.Recurring)
        {
            bes.MonthlyAmount = request.OwnAmount;
            bes.ContributionDay = Math.Clamp(paidAt.Day, 1, 28);
            bes.PlanActive = true;
        }

        // Maliyet = kişinin CEPTEN ödediği = kendi katkı toplamı (devlet katkısı maliyet değil, getiriye
        // dahil olur). Değer (CurrentPrice) fon getirisini taşır.
        holding.AvgCost = bes.OwnContribution;
        holding.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(holding.Id, ct: ct);
    }

    public async Task<HoldingDto> UpdateBesAsync(Guid id, UpdateBesRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var holding = await db.Holdings
            .Include(h => h.Asset)
            .Include(h => h.BesDetails)
            .FirstOrDefaultAsync(h => h.Id == id && h.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException();

        if (holding.Asset.Type != AssetType.Bes || holding.BesDetails is null)
            throw new ValidationException("id", "not_a_bes", "Bu pozisyon bir BES hesabı değil.");

        if (request.ContributionDay is { } cd && cd is < 1 or > 28)
            throw new ValidationException("contributionDay", "out_of_range", "Ödeme günü 1–28 arasında olmalı.");

        var bes = holding.BesDetails;

        if (request.JoinedAtUtc is { } joined)
        {
            if (joined > DateTime.UtcNow)
                throw new ValidationException("joinedAtUtc", "must_not_be_future", "Başlangıç tarihi gelecekte olamaz.");
            bes.JoinedAtUtc = joined;
        }
        if (request.ProviderName is not null)
            bes.ProviderName = string.IsNullOrWhiteSpace(request.ProviderName) ? null : request.ProviderName.Trim();
        if (request.BirthYear is not null)
            bes.BirthYear = request.BirthYear;
        if (request.MonthlyAmount is not null)
            bes.MonthlyAmount = request.MonthlyAmount;
        if (request.ContributionDay is not null)
            bes.ContributionDay = request.ContributionDay;
        if (request.PlanActive is { } pa)
            bes.PlanActive = pa;

        // Başlangıç tarihi / doğum yılı değişince hak ediş durumu yeniden türetilir.
        bes.VestingState = BesCalculator.VestingStateFor(bes.JoinedAtUtc, DateTime.UtcNow);
        holding.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(holding.Id, ct: ct);
    }

    public async Task<HoldingDto> GenerateBesContributionsAsync(
        Guid id, GenerateBesContributionsRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MonthlyAmount <= 0m)
            throw new ValidationException("monthlyAmount", "must_be_positive", "Aylık tutar 0'dan büyük olmalı.");
        if (request.Day is < 1 or > 28)
            throw new ValidationException("day", "out_of_range", "Ödeme günü 1–28 arasında olmalı.");
        if (request.ToUtc < request.FromUtc)
            throw new ValidationException("toUtc", "invalid_range", "Bitiş tarihi başlangıçtan önce olamaz.");
        // İleri tarihli aralık serbest (ileriye dönük plan); istenen aralık aynen üretilir.

        var holding = await db.Holdings
            .Include(h => h.Asset)
            .Include(h => h.BesDetails)
            .Include(h => h.BesContributions)
            .FirstOrDefaultAsync(h => h.Id == id && h.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException();

        if (holding.Asset.Type != AssetType.Bes || holding.BesDetails is null)
            throw new ValidationException("id", "not_a_bes", "Bu pozisyon bir BES hesabı değil.");

        var now = DateTime.UtcNow;
        // İstenen aralık aynen üretilir (ileri tarihli aylar dahil — ileriye dönük plan serbest).
        var dates = BesContributionPlanner.MonthlyDates(request.Day, request.FromUtc, request.ToUtc);

        // İdempotent: zaten **Plan** satırı olan ay'lar atlanır (yeniden üretimde çiftleme yok).
        // Manuel girişler dedup'a girmez — kullanıcı aynı ay içinde değişken tutarlı manuel katkıları
        // (her ay birden çok kez, değişken miktarda) yapmaya devam edebilir; düzenli plan ayrı bir
        // seridir (kullanıcı feedback, T-BES.9).
        var coveredPlanMonths = holding.BesContributions
            .Where(c => IsPlanSource(c.Source))
            .Select(c => c.PaidAtUtc.Year * 100 + c.PaidAtUtc.Month)
            .ToHashSet();

        var bes = holding.BesDetails;
        // T-BES.4: takvim yılı bazlı state toplamını tut → her ay için cap'i uygula. Mevcut tüm
        // BES katkı satırları (Plan/Manuel/Opening) sayılır; bu Generate çağrısı sırasında üretilen
        // satırlar da kümülatife eklenir (sıralı geçiş — aynı yıl içinde birikim doğru artar).
        var stateByYear = new Dictionary<int, decimal>();
        foreach (var existing in holding.BesContributions)
            stateByYear[existing.PaidAtUtc.Year] = stateByYear.GetValueOrDefault(existing.PaidAtUtc.Year, 0m) + existing.StateAmount;

        foreach (var date in dates)
        {
            if (!coveredPlanMonths.Add(date.Year * 100 + date.Month))
                continue; // bu ay için zaten Plan satırı var (manuel olanlar engel değil)

            var rawState = BesCalculator.StateContributionFor(request.MonthlyAmount, date);
            var alreadyInYear = stateByYear.GetValueOrDefault(date.Year, 0m);
            var state = BesCalculator.ApplyAnnualStateCap(rawState, date.Year, alreadyInYear);
            db.BesContributions.Add(new BesContribution
            {
                HoldingId = holding.Id,
                OwnAmount = request.MonthlyAmount,
                StateAmount = state,
                PaidAtUtc = date,
                Source = "Plan",
                CreatedAtUtc = now,
            });
            bes.OwnContribution += request.MonthlyAmount;
            bes.StateContribution += state;
            stateByYear[date.Year] = alreadyInYear + state;
        }

        bes.VestingState = BesCalculator.VestingStateFor(bes.JoinedAtUtc, now);
        holding.AvgCost = bes.OwnContribution;
        holding.UpdatedAtUtc = now;
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(holding.Id, ct: ct);
    }

    public async Task<HoldingDto> UpdateBesContributionAsync(
        Guid id, Guid contributionId, UpdateBesContributionRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.OwnAmount <= 0m)
            throw new ValidationException("ownAmount", "must_be_positive", "Katkı tutarı 0'dan büyük olmalı.");
        // İleri tarihli ödeme serbest (ileriye dönük katkı); oran ödeme tarihine göre.

        var holding = await LoadBesWithContributionsAsync(id, ct);
        var bes = holding.BesDetails!;
        var c = holding.BesContributions.FirstOrDefault(x => x.Id == contributionId)
            ?? throw new NotFoundException();

        // Devlet katkısı yeni tarihteki orana göre yeniden hesaplanır; **T-BES.4** yıllık üst sınırı
        // mevcut katkıyı hariç tutarak (kendini saymadan) uygular. Yıl değişimi de doğru ele alınır:
        // eski yıl kümülatifinden c.StateAmount düşülür (delta ile), yeni yıl için diğer kayıtlar baz.
        var rawState = BesCalculator.StateContributionFor(request.OwnAmount, request.PaidAtUtc);
        var alreadyInYearExclSelf = holding.BesContributions
            .Where(x => x.Id != contributionId && x.PaidAtUtc.Year == request.PaidAtUtc.Year)
            .Sum(x => x.StateAmount);
        var newState = BesCalculator.ApplyAnnualStateCap(rawState, request.PaidAtUtc.Year, alreadyInYearExclSelf);
        bes.OwnContribution += request.OwnAmount - c.OwnAmount;
        bes.StateContribution += newState - c.StateAmount;
        c.OwnAmount = request.OwnAmount;
        c.StateAmount = newState;
        c.PaidAtUtc = request.PaidAtUtc;

        ApplyBesTotals(holding, bes);
        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(holding.Id, ct: ct);
    }

    public async Task<HoldingDto> DeleteBesContributionAsync(Guid id, Guid contributionId, CancellationToken ct = default)
    {
        var holding = await LoadBesWithContributionsAsync(id, ct);
        var bes = holding.BesDetails!;
        var c = holding.BesContributions.FirstOrDefault(x => x.Id == contributionId)
            ?? throw new NotFoundException();

        bes.OwnContribution = Math.Max(0m, bes.OwnContribution - c.OwnAmount);
        bes.StateContribution = Math.Max(0m, bes.StateContribution - c.StateAmount);
        db.BesContributions.Remove(c);

        ApplyBesTotals(holding, bes);
        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(holding.Id, ct: ct);
    }

    /// <summary>
    /// Aktif düzenli plan varsa eksik ayları (son katkıdan bugüne) otomatik üretir (T-BES.6b). Plan
    /// yoksa/BES değilse no-op. İdempotent (kayıtlı ay atlanır). "Tarih geldikçe ödenmiş sayılır."
    /// </summary>
    private async Task CatchUpBesPlanAsync(Guid holdingId, CancellationToken ct)
    {
        var holding = await db.Holdings
            .Include(h => h.BesDetails)
            .Include(h => h.BesContributions)
            .FirstOrDefaultAsync(h => h.Id == holdingId && h.UserId == currentUser.UserId, ct);

        if (holding?.BesDetails is not { PlanActive: true, MonthlyAmount: { } amount, ContributionDay: { } day })
            return;

        // TR yerel "şimdi" — kullanıcı pencerede 1 Haz görüyorken UTC hâlâ 31 May olabilir; gün geçişinde
        // catch-up tetiklensin (T-BES.9 fix; UTC+3 sabit, DST yok).
        var now = TrNow();

        // Plan-source dedup: manuel girişler düzenli planı engellemez (kullanıcı feedback). Yalnız
        // "Plan" türevli satırlar lastPaid/covered için sayılır. lastPlanPaid yoksa "bu aydan" başla
        // (geçmiş aylar için plan satırı geriye dönük üretilmez; backfill için "Düzenli katkı/geçmiş" formu).
        var lastPlanPaid = holding.BesContributions
            .Where(c => IsPlanSource(c.Source))
            .Select(c => (DateTime?)c.PaidAtUtc)
            .DefaultIfEmpty(null)
            .Max();
        var from = lastPlanPaid is { } lp
            ? new DateTime(lp.Year, lp.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1)
            : new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var dates = BesContributionPlanner.MonthlyDates(day, from, now);
        if (dates.Count == 0)
            return;

        var coveredPlanMonths = holding.BesContributions
            .Where(c => IsPlanSource(c.Source))
            .Select(c => c.PaidAtUtc.Year * 100 + c.PaidAtUtc.Month)
            .ToHashSet();
        // T-BES.4: yıl bazlı state kümülatifi (tüm kaynaklar). Lazy-catchup sırasında üretilen her ay
        // için cap uygulanır; aynı yıl içinde tavan dolduktan sonra otomatik kayıt 0 state ile devam eder.
        var stateByYear = new Dictionary<int, decimal>();
        foreach (var existing in holding.BesContributions)
            stateByYear[existing.PaidAtUtc.Year] = stateByYear.GetValueOrDefault(existing.PaidAtUtc.Year, 0m) + existing.StateAmount;

        var bes = holding.BesDetails;
        var added = false;
        foreach (var date in dates)
        {
            if (!coveredPlanMonths.Add(date.Year * 100 + date.Month))
                continue; // bu ay zaten Plan satırı var — manuel olanlar engel değil
            var rawState = BesCalculator.StateContributionFor(amount, date);
            var alreadyInYear = stateByYear.GetValueOrDefault(date.Year, 0m);
            var state = BesCalculator.ApplyAnnualStateCap(rawState, date.Year, alreadyInYear);
            db.BesContributions.Add(new BesContribution
            {
                HoldingId = holding.Id,
                OwnAmount = amount,
                StateAmount = state,
                PaidAtUtc = date,
                Source = "Plan",
                CreatedAtUtc = DateTime.UtcNow,
            });
            bes.OwnContribution += amount;
            bes.StateContribution += state;
            stateByYear[date.Year] = alreadyInYear + state;
            added = true;
        }

        if (added)
        {
            ApplyBesTotals(holding, bes);
            await db.SaveChangesAsync(ct);
        }
    }

    /// <summary>BES holding'i (Asset+BesDetails+BesContributions) kullanıcıya kapsanmış yükler; BES değilse 400.</summary>
    private async Task<Holding> LoadBesWithContributionsAsync(Guid id, CancellationToken ct)
    {
        var holding = await db.Holdings
            .Include(h => h.Asset)
            .Include(h => h.BesDetails)
            .Include(h => h.BesContributions)
            .FirstOrDefaultAsync(h => h.Id == id && h.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException();

        if (holding.Asset.Type != AssetType.Bes || holding.BesDetails is null)
            throw new ValidationException("id", "not_a_bes", "Bu pozisyon bir BES hesabı değil.");

        return holding;
    }

    /// <summary>Hak ediş + maliyet (CEPTEN ödenen = kendi katkı) + zaman damgasını günceller.</summary>
    private static void ApplyBesTotals(Holding holding, BesDetails bes)
    {
        bes.VestingState = BesCalculator.VestingStateFor(bes.JoinedAtUtc, DateTime.UtcNow);
        holding.AvgCost = bes.OwnContribution;
        holding.UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>BES detayını (devlet katkısı/hak ediş/katkı geçmişi/"bu ay katkını gir" durumu) eşler.</summary>
    /// <param name="fundValue">
    /// Toplam fon değeri (<c>Holding.CurrentPrice</c>). Hem kendi hem devlet birikimi üzerinde işleyen
    /// fon getirisi <c>r = fund / (own+state) − 1</c> bu değerden türetilir; her iki katkı için ayrı
    /// güncel değer + kâr/zarar hesaplanır (T-BES.10). Null veya taban 0 ise getiri alanları null/0.
    /// </param>
    private static BesDto? ToBesDto(BesDetails? bes, ICollection<BesContribution> contributions, DateTime asOf, decimal? fundValue)
    {
        if (bes is null)
            return null;

        // Her katkıya tarihten türetilen durum + devlet yatma tarihi (saklanmaz).
        var list = contributions
            .OrderByDescending(c => c.PaidAtUtc)
            .Select(c => new BesContributionDto(
                c.Id, c.OwnAmount, c.StateAmount, c.PaidAtUtc, c.Source,
                BesCalculator.ContributionStatusFor(c.PaidAtUtc, asOf),
                BesCalculator.StateDepositDateFor(c.PaidAtUtc)))
            .ToList();

        // Toplamlar tarihten türetilir (T-BES.8). "Bekleyen" yalnız **Future** satırlardır — kendi
        // katkı ile devlet katkısı için simetrik (kullanıcı geri bildirimi): geçmiş listesindeki
        // "Gelecek Ödeme" satırının değerleriyle birebir eşleşir. StatePending satır (kendi katkı
        // ödendi, devlet henüz yatmadı) "yolda" sayılır — tabloda görünür ama hiçbir toplama girmez.
        decimal ownDeposited = 0m, stateDeposited = 0m, ownPending = 0m, statePending = 0m;
        foreach (var c in list)
        {
            switch (c.Status)
            {
                case BesContributionStatus.Future:
                    ownPending += c.OwnAmount;
                    statePending += c.StateAmount;
                    break;
                case BesContributionStatus.Deposited:
                    ownDeposited += c.OwnAmount;
                    stateDeposited += c.StateAmount;
                    break;
                case BesContributionStatus.StatePending:
                    ownDeposited += c.OwnAmount; // kendi katkı ödendi
                    // devlet katkısı "yolda" — toplama dahil değil
                    break;
            }
        }

        var vestedRate = BesCalculator.VestedRateFor(bes.JoinedAtUtc, BesCalculator.AgeFor(bes.BirthYear, asOf), asOf);
        var vestedAmount = Math.Round(vestedRate * stateDeposited, 2);

        // "Bu ay katkını gir" hatırlatması: kayıt varsa ve en son YATIRILMIŞ katkı bu aydan önceyse.
        var monthStart = new DateTime(asOf.Year, asOf.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastDeposited = list.Where(c => c.Status != BesContributionStatus.Future).Select(c => (DateTime?)c.PaidAtUtc).FirstOrDefault();
        var due = lastDeposited is { } lp && lp < monthStart;

        // Fon getirisi (T-BES.10): saf hesap BesCalculator'da — fon hem own hem state birikimi üzerinde
        // büyür, aynı r ikisine işler. Taban = yatırılmış toplamlar (StatePending'in state kısmı "yolda",
        // tabana ve getiri hesabına girmez — kendi içinde tutarlı).
        var fund = BesCalculator.FundReturnFor(ownDeposited, stateDeposited, fundValue);

        return new BesDto(
            ownDeposited, stateDeposited, ownPending, statePending,
            BesCalculator.VestingStateFor(bes.JoinedAtUtc, asOf), vestedRate, vestedAmount,
            bes.JoinedAtUtc, bes.BirthYear, bes.ProviderName, list, due,
            bes.PlanActive, bes.MonthlyAmount, bes.ContributionDay,
            fund.Rate, fund.OwnValue, fund.OwnProfit, fund.StateValue, fund.StateProfit);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var holding = await LoadOwnedAsync(id, ct);
        holding.IsDeleted = true; // soft-delete (03 §1); query filter sonraki okumalarda gizler
        holding.DeletedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<BesProjectionResult> ProjectBesAsync(Guid id, BesProjectionRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Per-user kapsam (IDOR yok) + BES doğrulaması — diğer BES metotlarıyla aynı şablon.
        var holding = await db.Holdings
            .Include(h => h.Asset)
            .Include(h => h.BesDetails)
            .FirstOrDefaultAsync(h => h.Id == id && h.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException();

        if (holding.Asset.Type != AssetType.Bes || holding.BesDetails is null)
            throw new ValidationException("id", "not_a_bes", "Bu pozisyon bir BES hesabı değil.");

        try
        {
            // Saf hesap — pozisyonu değiştirmez; başlangıç bugün (UTC). Hesap deterministik (T-BES.5).
            // Sözleşme başı + doğum yılı calculator'a yedirilir: süre sonu hak ediş kademesi (3/6/10/+56)
            // bilinçli hesaplanır (mevcut sözleşme süresi de hesaba katılır).
            return BesProjectionCalculator.Project(new BesProjectionInput(
                request.OwnMonthly, request.Years, request.AnnualReturnRatio, DateTime.UtcNow,
                holding.BesDetails.JoinedAtUtc, holding.BesDetails.BirthYear));
        }
        catch (ArgumentException ex)
        {
            // Calculator validation hatalarını API zarfına çevir (sınır ihlali, negatif, vs.).
            throw new ValidationException("projection", "invalid_input", ex.Message);
        }
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    private async Task<List<HoldingDto>> BuildHoldingDtosAsync(CurrencyCode? baseCurrency, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var baseCcy = await HoldingMapping.ResolveBaseCurrencyAsync(db, userId, baseCurrency, ct);

        var holdings = await db.Holdings
            .Where(h => h.UserId == userId)
            .Include(h => h.Asset)
            .Include(h => h.BesDetails)
            .Include(h => h.BesContributions)
            .Include(h => h.Transactions)
            .OrderBy(h => h.CreatedAtUtc)
            .ToListAsync(ct);

        // Pozisyonu KAYNAKTAN yeniden türet (cache'e güvenme): miktar/ort. maliyet her okumada
        // işlemlerden (BES değilse) veya kendi katkı toplamından (BES) hesaplanır. Böylece saklanan
        // cache alanı eski kalsa bile gösterim her zaman doğru ve kendi içinde tutarlıdır.
        foreach (var h in holdings)
            ApplyReadPosition(h);

        var converter = await fxRateProvider.GetConverterAsync(ct);
        var results = calc.CalculateHoldings(HoldingMapping.ToInputs(holdings, converter, baseCcy));

        var dtos = new List<HoldingDto>(holdings.Count);
        for (int i = 0; i < holdings.Count; i++)
        {
            var h = holdings[i];
            var r = results[i];
            // TR yerel "şimdi" — durum (Future/StatePending/Deposited) ve "bu ay katkı" hatırlatması
            // kullanıcının pencerede gördüğü güne göre. Saat dilimi farkıyla "gün geçti ama hâlâ Future"
            // yaşanmasın (T-BES.9 fix; TR sabit UTC+3, DST yok).
            // Fon değeri (CurrentPrice) BES için "toplam birikimin piyasa değeri" — own+state'i içerir;
            // ToBesDto bunu kullanarak her bir katkı için ayrı fon getirisi hesaplar (T-BES.10).
            var bes = ToBesDto(h.BesDetails, h.BesContributions, TrNow(), h.CurrentPrice);

            dtos.Add(new HoldingDto(
                h.Id, h.Asset.Type, h.Asset.Name, h.Asset.Symbol, h.Asset.PricingCurrency, h.Asset.Unit,
                h.Quantity, h.AvgCost, h.CurrentPrice,
                r.TotalCost, r.CurrentValue, r.Profit, r.ReturnRatio, r.Weight, bes));
        }

        return dtos;
    }

    /// <summary>
    /// Okuma anında pozisyonu kaynaktan yeniden türetir (§6, T1.5): miktar/ort. maliyet
    /// saklanan cache alanından DEĞİL, gerçek işlemlerden (BES değilse) ya da kendi katkı
    /// toplamından (BES — maliyet = cepten ödenen kendi katkı; devlet katkısı maliyet değil)
    /// hesaplanır. Salt okunur şekillendirme — kalıcılaştırılmaz (çağrı sonrası SaveChanges yok).
    /// Eski/sürüklenmiş cache değerlerini gösterimde otomatik düzeltir.
    /// </summary>
    private static void ApplyReadPosition(Holding h)
    {
        if (h.BesDetails is not null)
        {
            // BES nominal hesap; miktar 1 sabit. Maliyet = CEPTEN ödenen = YATIRILMIŞ kendi katkı
            // toplamı (ödeme tarihi ≤ kullanıcının BUGÜN'ü). TR yerel — gün geçiş anında doğru sayım.
            var today = TrNow().Date;
            h.AvgCost = h.BesContributions
                .Where(c => c.PaidAtUtc.Date <= today)
                .Sum(c => c.OwnAmount);
            return;
        }

        // İşlem yoksa türetim 0/0 döner — saklanan değerleri SİLMEZ (örn. Nakit: doğrudan miktar
        // tutulur, alış/satış işlemi olmaz). Yalnız işlem varsa kaynaktan yeniden türetilir.
        if (h.Transactions.Count == 0)
            return;

        var pos = PortfolioCalculationService.DerivePosition(
            h.Transactions.Select(t => new TransactionInput(t.Type, t.Quantity, t.UnitPrice, t.Fee)));
        h.Quantity = pos.Quantity;
        h.AvgCost = pos.AvgCost;
    }

    /// <summary>Kullanıcıya ait pozisyonu getirir; yoksa/başkasınınsa 404 (IDOR yok).</summary>
    private async Task<Holding> LoadOwnedAsync(Guid id, CancellationToken ct) =>
        await db.Holdings.FirstOrDefaultAsync(h => h.Id == id && h.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException();

    private async Task<Holding> LoadOwnedWithTransactionsAsync(Guid id, CancellationToken ct) =>
        await db.Holdings
            .Include(h => h.Asset)
            .Include(h => h.Transactions)
            .FirstOrDefaultAsync(h => h.Id == id && h.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException();

    private async Task<Asset> FindOrCreateAssetAsync(CreateHoldingRequest request, CancellationToken ct)
    {
        var symbol = string.IsNullOrWhiteSpace(request.Symbol) ? null : request.Symbol.Trim();

        var asset = symbol is not null
            ? await db.Assets.FirstOrDefaultAsync(a =>
                a.Type == request.AssetType && a.Symbol == symbol && a.PricingCurrency == request.Currency, ct)
            : await db.Assets.FirstOrDefaultAsync(a =>
                a.Type == request.AssetType && a.Name == request.Name && a.PricingCurrency == request.Currency, ct);

        if (asset is null)
        {
            asset = new Asset
            {
                Type = request.AssetType,
                Name = request.Name.Trim(),
                Symbol = symbol,
                Unit = request.Unit.Trim(),
                PricingCurrency = request.Currency,
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.Assets.Add(asset);
        }

        return asset;
    }

    private static Transaction ToEntity(TransactionRequest r, Guid holdingId, DateTime now) => new()
    {
        HoldingId = holdingId,
        Type = r.Type,
        Quantity = r.Quantity,
        UnitPrice = r.UnitPrice,
        Fee = r.Fee,
        TransactedAtUtc = r.Date ?? now,
        CreatedAtUtc = now,
    };

    /// <summary>Pozisyonu işlemlerden yeniden türetir (03 §11) ve cache alanlarına yazar.</summary>
    private static void ApplyDerivedPosition(Holding holding)
    {
        var pos = PortfolioCalculationService.DerivePosition(
            holding.Transactions.Select(t => new TransactionInput(t.Type, t.Quantity, t.UnitPrice, t.Fee)));

        if (pos.Quantity < 0m)
            throw new ValidationException("quantity", "exceeds_holding",
                "Satış miktarı eldeki pozisyondan fazla olamaz.");

        holding.Quantity = pos.Quantity;
        holding.AvgCost = pos.AvgCost;
    }

    private static void ValidateTransaction(TransactionRequest? tx, bool isFirst)
    {
        if (tx is null)
            throw new ValidationException("transaction", "required", "İşlem bilgisi zorunludur.");

        if (tx.Quantity <= 0m)
            throw new ValidationException("quantity", "must_be_positive", "Miktar 0'dan büyük olmalı.");

        if (tx.UnitPrice < 0m)
            throw new ValidationException("unitPrice", "must_be_non_negative", "Birim fiyat negatif olamaz.");

        if (tx.Fee < 0m)
            throw new ValidationException("fee", "must_be_non_negative", "Komisyon negatif olamaz.");

        if (isFirst && tx.Type == TransactionType.Sell)
            throw new ValidationException("transaction.type", "first_must_be_buy",
                "İlk işlem bir alış (Buy) olmalı.");
    }

    private async Task SaveHandlingConflictAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Create yolunda olası tek unique ihlali (UserId,AssetId) — yarış yedeği;
            // normal durumda yukarıdaki AnyAsync ön-kontrolü yakalar. Sözleşmeli 409 (04 §2).
            throw new ConflictException("Bu varlıkta zaten bir pozisyonunuz var.");
        }
    }
}

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
    public async Task<IReadOnlyList<HoldingDto>> GetAllAsync(CurrencyCode? baseCurrency = null, CancellationToken ct = default) =>
        await BuildHoldingDtosAsync(baseCurrency, ct);

    public async Task<HoldingDto> GetByIdAsync(Guid id, CurrencyCode? baseCurrency = null, CancellationToken ct = default)
    {
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
            .FirstOrDefaultAsync(h => h.Id == id && h.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException();

        if (holding.Asset.Type != AssetType.Bes || holding.BesDetails is null)
            throw new ValidationException("id", "not_a_bes", "Bu pozisyon bir BES hesabı değil.");

        // Ödeme tarihi (verilmezse şimdi). Gelecek olamaz; oran bu tarihe göre seçilir (geriye dönük değil).
        var paidAt = request.PaidAtUtc ?? DateTime.UtcNow;
        if (paidAt > DateTime.UtcNow)
            throw new ValidationException("paidAtUtc", "must_not_be_future", "Ödeme tarihi gelecekte olamaz.");

        // Devlet katkısı verilmezse, katkının ödendiği tarihteki orana göre (2026 öncesi %30, sonrası
        // %20) — BesRules/BesCalculator (yıllık üst sınır T-BES.4; lansman öncesi EGM/SPK, CLAUDE.md §2).
        var stateAmount = request.StateAmount ?? BesCalculator.StateContributionFor(request.OwnAmount, paidAt);

        var bes = holding.BesDetails;
        bes.OwnContribution += request.OwnAmount;
        bes.StateContribution += stateAmount;
        // Hak ediş durumu sistemde kalış süresinden türetilir (saf hesap).
        bes.VestingState = BesCalculator.VestingStateFor(bes.JoinedAtUtc, DateTime.UtcNow);

        // BES maliyet tabanı = kendi + devlet katkısı (nominal 1 birim). Değer (CurrentPrice) fon getirisini taşır.
        holding.AvgCost = bes.OwnContribution + bes.StateContribution;
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

        var bes = holding.BesDetails;
        if (request.JoinedAtUtc is { } joined)
        {
            if (joined > DateTime.UtcNow)
                throw new ValidationException("joinedAtUtc", "must_not_be_future", "Başlangıç tarihi gelecekte olamaz.");
            bes.JoinedAtUtc = joined;
            // Başlangıç tarihi değişince hak ediş durumu yeniden türetilir.
            bes.VestingState = BesCalculator.VestingStateFor(joined, DateTime.UtcNow);
            holding.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return await GetByIdAsync(holding.Id, ct: ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var holding = await LoadOwnedAsync(id, ct);
        holding.IsDeleted = true; // soft-delete (03 §1); query filter sonraki okumalarda gizler
        holding.DeletedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
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
            .OrderBy(h => h.CreatedAtUtc)
            .ToListAsync(ct);

        var converter = await fxRateProvider.GetConverterAsync(ct);
        var results = calc.CalculateHoldings(HoldingMapping.ToInputs(holdings, converter, baseCcy));

        var dtos = new List<HoldingDto>(holdings.Count);
        for (int i = 0; i < holdings.Count; i++)
        {
            var h = holdings[i];
            var r = results[i];
            var bes = h.BesDetails is { } b
                ? new BesDto(
                    b.OwnContribution, b.StateContribution,
                    BesCalculator.VestingStateFor(b.JoinedAtUtc, DateTime.UtcNow), b.JoinedAtUtc)
                : null;

            dtos.Add(new HoldingDto(
                h.Id, h.Asset.Type, h.Asset.Name, h.Asset.Symbol, h.Asset.PricingCurrency, h.Asset.Unit,
                h.Quantity, h.AvgCost, h.CurrentPrice,
                r.TotalCost, r.CurrentValue, r.Profit, r.ReturnRatio, r.Weight, bes));
        }

        return dtos;
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

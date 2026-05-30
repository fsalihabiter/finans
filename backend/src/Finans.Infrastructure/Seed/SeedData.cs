using System.Security.Cryptography;
using System.Text;
using Finans.Domain.Enums;
using Finans.Domain.Identity;
using Finans.Domain.Portfolio;
using Finans.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Finans.Infrastructure.Seed;

/// <summary>
/// Kapsamlı, tutarlı ve idempotent seed (03 §12). 7 çeşitli pozisyon (altın, döviz
/// USD+EUR, USD-fiyatlı hisse, zarardaki fon, BES, nakit). Baz TRY toplamları:
/// maliyet 603.770, değer 839.213, kâr +235.443 (+%39,0; reel ~+%0,7 @enflasyon 0,38).
/// Bu set aynı zamanda integration test fixture'ıdır (09 §2). USD-fiyatlı AAPL
/// summary'de gerçek kur çevrimini (USD→TRY ×48) tetikler.
/// Deterministik Id'ler (anahtar→GUID) sayesinde tekrar çalışınca çoğaltmaz.
/// </summary>
public static class SeedData
{
    /// <summary>Anahtardan deterministik GUID üretir (idempotent seed için).</summary>
    public static Guid Id(string key) =>
        new(MD5.HashData(Encoding.UTF8.GetBytes("finans-seed:" + key)));

    public static async Task SeedAsync(FinansDbContext db, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct))
            return; // zaten seed'lenmiş

        var now = DateTime.UtcNow;
        var purchase = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc); // alış dönemi

        // ── Roller & kullanıcılar (12.1) ──────────────────────────────────────
        var roleUser = new Role { Id = Id("role-user"), Name = UserRole.User };
        var roleAdmin = new Role { Id = Id("role-admin"), Name = UserRole.Admin };

        var user = new User
        {
            Id = Id("user-1"),
            DisplayName = "Yatırımcı",
            BaseCurrency = CurrencyCode.TRY,
            IsActive = true,
            CreatedAtUtc = now,
        };
        var admin = new User
        {
            Id = Id("admin-1"),
            DisplayName = "Yönetici",
            BaseCurrency = CurrencyCode.TRY,
            IsActive = true,
            CreatedAtUtc = now,
        };

        db.Roles.AddRange(roleUser, roleAdmin);
        db.Users.AddRange(user, admin);
        db.UserRoles.AddRange(
            new UserRoleAssignment { UserId = user.Id, RoleId = roleUser.Id },
            new UserRoleAssignment { UserId = admin.Id, RoleId = roleAdmin.Id });

        // ── Kur & enflasyon (12.2) ────────────────────────────────────────────
        db.FxRates.AddRange(
            new FxRate { Id = Id("fx-usd-try-now"), FromCurrency = CurrencyCode.USD, ToCurrency = CurrencyCode.TRY, Rate = 48.000000m, Source = "Manual", AsOfUtc = now, CreatedAtUtc = now },
            new FxRate { Id = Id("fx-eur-try-now"), FromCurrency = CurrencyCode.EUR, ToCurrency = CurrencyCode.TRY, Rate = 52.000000m, Source = "Manual", AsOfUtc = now, CreatedAtUtc = now },
            new FxRate { Id = Id("fx-usd-try-old"), FromCurrency = CurrencyCode.USD, ToCurrency = CurrencyCode.TRY, Rate = 43.270000m, Source = "Manual", AsOfUtc = purchase, CreatedAtUtc = now });

        db.InflationRates.Add(new InflationRate
        {
            Id = Id("inflation-2024"),
            PeriodStartUtc = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            PeriodEndUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            AnnualRate = 0.380000m, // örnek/placeholder — TÜİK, prod'da gerçek veri
            Source = "TÜİK",
            CreatedAtUtc = now,
        });

        // ── Varlık kataloğu (12.3) ────────────────────────────────────────────
        // Çeşitli türler + para birimleri. AAPL USD cinsinden fiyatlı → summary'de
        // gerçek kur çevrimi (USD→TRY) tetikler. Fon kasıtlı ZARAR'da (eğitici örnek).
        var gold = new Asset { Id = Id("asset-gold"), Type = AssetType.Gold, Name = "Altın (gram)", Symbol = "XAU", Unit = "gram", PricingCurrency = CurrencyCode.TRY, CreatedAtUtc = now };
        var usd = new Asset { Id = Id("asset-usd"), Type = AssetType.Fx, Name = "ABD Doları", Symbol = "USD", Unit = "USD", PricingCurrency = CurrencyCode.TRY, CreatedAtUtc = now };
        var eur = new Asset { Id = Id("asset-eur"), Type = AssetType.Fx, Name = "Euro", Symbol = "EUR", Unit = "EUR", PricingCurrency = CurrencyCode.TRY, CreatedAtUtc = now };
        var bes = new Asset { Id = Id("asset-bes"), Type = AssetType.Bes, Name = "Bireysel Emeklilik", Unit = "birim", PricingCurrency = CurrencyCode.TRY, CreatedAtUtc = now };
        var cash = new Asset { Id = Id("asset-cash"), Type = AssetType.Cash, Name = "Nakit (TL)", Unit = "TRY", PricingCurrency = CurrencyCode.TRY, CreatedAtUtc = now };
        var aapl = new Asset { Id = Id("asset-aapl"), Type = AssetType.Stock, Name = "Apple Inc.", Symbol = "AAPL", Unit = "adet", PricingCurrency = CurrencyCode.USD, Exchange = "NASDAQ", CreatedAtUtc = now };
        var fund = new Asset { Id = Id("asset-fund"), Type = AssetType.Fund, Name = "Teknoloji Fonu", Symbol = "TEKFON", Unit = "adet", PricingCurrency = CurrencyCode.TRY, CreatedAtUtc = now };

        db.Assets.AddRange(gold, usd, eur, bes, cash, aapl, fund);

        // ── Fiyat geçmişi (12.4) — reel getiri/senaryo için en az iki nokta ────
        db.PriceSnapshots.AddRange(
            new PriceSnapshot { Id = Id("ps-gold-now"), AssetId = gold.Id, Price = 6500.000000m, Source = "Manual", AsOfUtc = now, CreatedAtUtc = now },
            new PriceSnapshot { Id = Id("ps-gold-old"), AssetId = gold.Id, Price = 4546.275000m, Source = "Manual", AsOfUtc = purchase, CreatedAtUtc = now },
            new PriceSnapshot { Id = Id("ps-usd-now"), AssetId = usd.Id, Price = 48.000000m, Source = "Manual", AsOfUtc = now, CreatedAtUtc = now },
            new PriceSnapshot { Id = Id("ps-usd-old"), AssetId = usd.Id, Price = 43.270000m, Source = "Manual", AsOfUtc = purchase, CreatedAtUtc = now },
            new PriceSnapshot { Id = Id("ps-eur-now"), AssetId = eur.Id, Price = 52.000000m, Source = "Manual", AsOfUtc = now, CreatedAtUtc = now },
            new PriceSnapshot { Id = Id("ps-aapl-now"), AssetId = aapl.Id, Price = 210.000000m, Source = "Manual", AsOfUtc = now, CreatedAtUtc = now },
            new PriceSnapshot { Id = Id("ps-fund-now"), AssetId = fund.Id, Price = 23.500000m, Source = "Manual", AsOfUtc = now, CreatedAtUtc = now });

        // ── Pozisyonlar (12.4) — TUTARLI (baz TRY: maliyet 603.770 · değer 839.213 · +%39,0) ──
        // Altın: 40 gr @ 4.546,275 → maliyet 181.851 · güncel 6.500 → 260.000 (+%43,0)
        var goldHolding = new Holding { Id = Id("holding-gold"), UserId = user.Id, AssetId = gold.Id, Quantity = 40m, AvgCost = 4546.275000m, CurrentPrice = 6500.000000m, CreatedAtUtc = now };
        // Dolar: 2.000 $ @ 43,27 → maliyet 86.540 · güncel 48 → 96.000 (+%10,9)
        var usdHolding = new Holding { Id = Id("holding-usd"), UserId = user.Id, AssetId = usd.Id, Quantity = 2000m, AvgCost = 43.270000m, CurrentPrice = 48.000000m, CreatedAtUtc = now };
        // Euro: 800 € @ 47,50 → maliyet 38.000 · güncel 52 → 41.600 (+%9,5)
        var eurHolding = new Holding { Id = Id("holding-eur"), UserId = user.Id, AssetId = eur.Id, Quantity = 800m, AvgCost = 47.500000m, CurrentPrice = 52.000000m, CreatedAtUtc = now };
        // Apple (USD fiyatlı): 12 @ 175 $ → güncel 210 $; TRY'ye ×48 → maliyet 100.800 · değer 120.960 (+%20)
        var aaplHolding = new Holding { Id = Id("holding-aapl"), UserId = user.Id, AssetId = aapl.Id, Quantity = 12m, AvgCost = 175.000000m, CurrentPrice = 210.000000m, CreatedAtUtc = now };
        // Teknoloji Fonu: 1.500 @ 28,00 → maliyet 42.000 · güncel 23,50 → 35.250 (−%16,1) ZARAR
        var fundHolding = new Holding { Id = Id("holding-fund"), UserId = user.Id, AssetId = fund.Id, Quantity = 1500m, AvgCost = 28.000000m, CurrentPrice = 23.500000m, CreatedAtUtc = now };
        // BES: own 120.000 + state 28.554 = 148.554 (notional 1 pay) · güncel 279.378 (+%88,1)
        var besHolding = new Holding { Id = Id("holding-bes"), UserId = user.Id, AssetId = bes.Id, Quantity = 1m, AvgCost = 148554.000000m, CurrentPrice = 279378.000000m, CreatedAtUtc = now };
        // Nakit: 6.025 ₺
        var cashHolding = new Holding { Id = Id("holding-cash"), UserId = user.Id, AssetId = cash.Id, Quantity = 6025m, AvgCost = 1.000000m, CurrentPrice = 1.000000m, CreatedAtUtc = now };

        db.Holdings.AddRange(goldHolding, usdHolding, eurHolding, aaplHolding, fundHolding, besHolding, cashHolding);

        // İşlemler (DOĞRULUK KAYNAĞI) — her alış Buy; AvgCost/Quantity bunlardan türer.
        db.Transactions.AddRange(
            new Transaction { Id = Id("tx-gold-buy"), HoldingId = goldHolding.Id, Type = TransactionType.Buy, Quantity = 40m, UnitPrice = 4546.275000m, Fee = 0m, TransactedAtUtc = purchase, CreatedAtUtc = now },
            new Transaction { Id = Id("tx-usd-buy"), HoldingId = usdHolding.Id, Type = TransactionType.Buy, Quantity = 2000m, UnitPrice = 43.270000m, Fee = 0m, TransactedAtUtc = purchase, CreatedAtUtc = now },
            new Transaction { Id = Id("tx-eur-buy"), HoldingId = eurHolding.Id, Type = TransactionType.Buy, Quantity = 800m, UnitPrice = 47.500000m, Fee = 0m, TransactedAtUtc = purchase, CreatedAtUtc = now },
            new Transaction { Id = Id("tx-aapl-buy"), HoldingId = aaplHolding.Id, Type = TransactionType.Buy, Quantity = 12m, UnitPrice = 175.000000m, Fee = 0m, TransactedAtUtc = purchase, CreatedAtUtc = now },
            new Transaction { Id = Id("tx-fund-buy"), HoldingId = fundHolding.Id, Type = TransactionType.Buy, Quantity = 1500m, UnitPrice = 28.000000m, Fee = 0m, TransactedAtUtc = purchase, CreatedAtUtc = now });

        // BES detayı — devlet katkısı AYRI; getiri tabanına dahil (03 §A).
        db.BesDetails.Add(new BesDetails
        {
            Id = Id("bes-details"),
            HoldingId = besHolding.Id,
            OwnContribution = 120000.000000m,
            StateContribution = 28554.000000m,
            VestingState = VestingState.PartiallyVested,
            ProviderName = "Örnek BES",
            JoinedAtUtc = purchase,
        });

        await db.SaveChangesAsync(ct);
    }
}

using Finans.Domain.Enums;
using Finans.Domain.Portfolio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finans.Infrastructure.Persistence.Configurations;

/// <summary>Enum allow-list CHECK üretimi (DB seviyesinde savunma, 03 §2/§14).</summary>
internal static class Check
{
    public static string EnumIn<TEnum>(string column) where TEnum : struct, Enum =>
        $"\"{column}\" IN ({string.Join(", ", Enum.GetNames<TEnum>().Select(n => $"'{n}'"))})";
}

internal sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> b)
    {
        b.ToTable("Assets", t =>
        {
            t.HasCheckConstraint("CK_Assets_Type", Check.EnumIn<AssetType>("Type"));
            t.HasCheckConstraint("CK_Assets_PricingCurrency", Check.EnumIn<CurrencyCode>("PricingCurrency"));
        });
        b.Property(x => x.Symbol).HasMaxLength(20);
        b.Property(x => x.Unit).HasMaxLength(20).IsRequired();
        b.Property(x => x.Exchange).HasMaxLength(20);
        b.Property(x => x.IsActive).HasDefaultValue(true);
    }
}

internal sealed class HoldingConfiguration : IEntityTypeConfiguration<Holding>
{
    public void Configure(EntityTypeBuilder<Holding> b)
    {
        b.ToTable("Holdings", t =>
        {
            t.HasCheckConstraint("CK_Holdings_Quantity", "\"Quantity\" >= 0");
            t.HasCheckConstraint("CK_Holdings_AvgCost", "\"AvgCost\" >= 0");
        });

        // Not: xmin optimistic concurrency PostgreSQL'e özgü → FinansDbContext.
        // OnModelCreating'de sağlayıcı-koşullu eklenir (Sqlite test fixture'ı kırmasın).

        // Soft-delete: varsayılan sorgu filtresi (03 §1, 11 §3 ile birlikte).
        b.HasQueryFilter(h => !h.IsDeleted);

        b.HasIndex(h => h.UserId);
        // Aktif pozisyonlarda (UserId, AssetId) tekil — aynı varlıkta tek aktif kayıt.
        b.HasIndex(h => new { h.UserId, h.AssetId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        b.HasOne(h => h.User)
            .WithMany(u => u.Holdings)
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade); // KVKK "verimi sil" → cascade (11 §7)

        b.HasOne(h => h.Asset)
            .WithMany(a => a.Holdings)
            .HasForeignKey(h => h.AssetId)
            .OnDelete(DeleteBehavior.Restrict); // katalog paylaşımlı, silinmez
    }
}

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> b)
    {
        b.ToTable("Transactions", t =>
        {
            t.HasCheckConstraint("CK_Transactions_Quantity", "\"Quantity\" > 0");
            t.HasCheckConstraint("CK_Transactions_UnitPrice", "\"UnitPrice\" >= 0");
            t.HasCheckConstraint("CK_Transactions_Fee", "\"Fee\" >= 0");
            t.HasCheckConstraint("CK_Transactions_Type", Check.EnumIn<TransactionType>("Type"));
        });
        b.HasIndex(x => new { x.HoldingId, x.TransactedAtUtc });

        b.HasOne(x => x.Holding)
            .WithMany(h => h.Transactions)
            .HasForeignKey(x => x.HoldingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class BesDetailsConfiguration : IEntityTypeConfiguration<BesDetails>
{
    public void Configure(EntityTypeBuilder<BesDetails> b)
    {
        b.ToTable("BesDetails", t =>
        {
            t.HasCheckConstraint("CK_BesDetails_Own", "\"OwnContribution\" >= 0");
            t.HasCheckConstraint("CK_BesDetails_State", "\"StateContribution\" >= 0");
            t.HasCheckConstraint("CK_BesDetails_Vesting", Check.EnumIn<VestingState>("VestingState"));
        });
        b.HasIndex(x => x.HoldingId).IsUnique();

        b.HasOne(x => x.Holding)
            .WithOne(h => h.BesDetails)
            .HasForeignKey<BesDetails>(x => x.HoldingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class BesContributionConfiguration : IEntityTypeConfiguration<BesContribution>
{
    public void Configure(EntityTypeBuilder<BesContribution> b)
    {
        b.ToTable("BesContributions", t =>
        {
            t.HasCheckConstraint("CK_BesContributions_Own", "\"OwnAmount\" >= 0");
            t.HasCheckConstraint("CK_BesContributions_State", "\"StateAmount\" >= 0");
        });
        b.Property(x => x.Source).HasMaxLength(20).IsRequired();
        b.HasIndex(x => new { x.HoldingId, x.PaidAtUtc }).IsDescending(false, true);

        b.HasOne(x => x.Holding)
            .WithMany(h => h.BesContributions)
            .HasForeignKey(x => x.HoldingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PriceSnapshotConfiguration : IEntityTypeConfiguration<PriceSnapshot>
{
    public void Configure(EntityTypeBuilder<PriceSnapshot> b)
    {
        b.ToTable("PriceSnapshots", t =>
            t.HasCheckConstraint("CK_PriceSnapshots_Price", "\"Price\" >= 0"));
        b.Property(x => x.Source).HasMaxLength(40).IsRequired();
        // Son fiyat hızlı: (AssetId, AsOfUtc DESC).
        b.HasIndex(x => new { x.AssetId, x.AsOfUtc }).IsDescending(false, true);

        b.HasOne(x => x.Asset)
            .WithMany(a => a.PriceSnapshots)
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class FxRateConfiguration : IEntityTypeConfiguration<FxRate>
{
    public void Configure(EntityTypeBuilder<FxRate> b)
    {
        b.ToTable("FxRates", t =>
        {
            t.HasCheckConstraint("CK_FxRates_Rate", "\"Rate\" > 0");
            t.HasCheckConstraint("CK_FxRates_From", Check.EnumIn<CurrencyCode>("FromCurrency"));
            t.HasCheckConstraint("CK_FxRates_To", Check.EnumIn<CurrencyCode>("ToCurrency"));
        });
        b.Property(x => x.Source).HasMaxLength(40).IsRequired();
        b.HasIndex(x => new { x.FromCurrency, x.ToCurrency, x.AsOfUtc }).IsDescending(false, false, true);
    }
}

internal sealed class InflationRateConfiguration : IEntityTypeConfiguration<InflationRate>
{
    public void Configure(EntityTypeBuilder<InflationRate> b)
    {
        b.ToTable("InflationRates");
        b.Property(x => x.AnnualRate).HasPrecision(9, 6); // oran: numeric(9,6)
        b.Property(x => x.Source).HasMaxLength(40).IsRequired();
        b.HasIndex(x => new { x.PeriodStartUtc, x.PeriodEndUtc });
    }
}

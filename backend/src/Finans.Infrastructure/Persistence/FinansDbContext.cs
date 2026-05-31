using System.Net;
using Finans.Domain.Enums;
using Finans.Domain.Identity;
using Finans.Domain.Portfolio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Finans.Infrastructure.Persistence;

/// <summary>
/// Uygulama veritabanı bağlamı (03). PostgreSQL/Npgsql hedefli.
/// Tüm parasal kolonlar numeric(18,6) decimal; enum'lar varchar (allow-list).
/// Per-user izolasyon ve soft-delete filtreleri entity konfigürasyonlarında.
/// </summary>
public class FinansDbContext(DbContextOptions<FinansDbContext> options) : DbContext(options)
{
    // Portföy
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Holding> Holdings => Set<Holding>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<BesDetails> BesDetails => Set<BesDetails>();
    public DbSet<BesContribution> BesContributions => Set<BesContribution>();
    public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();
    public DbSet<FxRate> FxRates => Set<FxRate>();
    public DbSet<InflationRate> InflationRates => Set<InflationRate>();

    // Kimlik / audit
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRoleAssignment> UserRoles => Set<UserRoleAssignment>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Para/miktar: numeric(18,6) (NFR-1). InflationRate.AnnualRate (9,6) ayrıca ezilir.
        configurationBuilder.Properties<decimal>().HavePrecision(18, 6);

        // Enum'lar DB'de varchar (allow-list, 03 §2). Uzunluklar değerlere göre.
        configurationBuilder.Properties<AssetType>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<TransactionType>().HaveConversion<string>().HaveMaxLength(10);
        configurationBuilder.Properties<VestingState>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<CurrencyCode>().HaveConversion<string>().HaveMaxLength(3);
        configurationBuilder.Properties<UserRole>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<AuditAction>().HaveConversion<string>().HaveMaxLength(30);
        configurationBuilder.Properties<AuditResult>().HaveConversion<string>().HaveMaxLength(20);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinansDbContext).Assembly);

        // Sağlayıcıya özgü eşlemeler. Prod = PostgreSQL; test fixture = Sqlite/InMemory.
        if (Database.IsNpgsql())
        {
            modelBuilder.HasPostgresExtension("citext"); // case-insensitive Email
            modelBuilder.Entity<User>().Property(u => u.Email).HasColumnType("citext");
            // Optimistic concurrency: PostgreSQL xmin sistem kolonu (03 §1).
            modelBuilder.Entity<Holding>()
                .Property<uint>("Version")
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();
        }
        else
        {
            // Sqlite/InMemory'de inet/IPAddress yerel eşlemesi yok → string'e çevir.
            var ipConverter = new ValueConverter<IPAddress, string>(
                ip => ip.ToString(),
                text => IPAddress.Parse(text));
            modelBuilder.Entity<RefreshToken>().Property(x => x.CreatedByIp).HasConversion(ipConverter);
            modelBuilder.Entity<AuditLog>().Property(x => x.IpAddress).HasConversion(ipConverter);
        }
    }
}

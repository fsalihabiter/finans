using Finans.Domain.Enums;
using Finans.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finans.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users", t =>
            t.HasCheckConstraint("CK_Users_BaseCurrency", Check.EnumIn<CurrencyCode>("BaseCurrency")));

        // citext = case-insensitive (Email karşılaştırması). Faz 5'te zorunlu olur.
        b.Property(x => x.Email).HasColumnType("citext");
        b.HasIndex(x => x.Email).IsUnique(); // PostgreSQL'de birden çok NULL'a izin verir
        b.Property(x => x.IsActive).HasDefaultValue(true);
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("Roles", t =>
            t.HasCheckConstraint("CK_Roles_Name", Check.EnumIn<UserRole>("Name")));
        b.HasIndex(x => x.Name).IsUnique();
    }
}

internal sealed class UserRoleAssignmentConfiguration : IEntityTypeConfiguration<UserRoleAssignment>
{
    public void Configure(EntityTypeBuilder<UserRoleAssignment> b)
    {
        b.ToTable("UserRoles");
        b.HasKey(x => new { x.UserId, x.RoleId });

        b.HasOne(x => x.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokens");
        b.Property(x => x.TokenHash).IsRequired();
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.TokenHash);

        b.HasOne(x => x.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AuditLogs", t =>
        {
            t.HasCheckConstraint("CK_AuditLogs_Action", Check.EnumIn<AuditAction>("Action"));
            t.HasCheckConstraint("CK_AuditLogs_Result", Check.EnumIn<AuditResult>("Result"));
        });
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.AtUtc);

        // Audit kaydı kullanıcı silinse de korunur (append-only); yalnız bağ kopar.
        b.HasOne(x => x.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

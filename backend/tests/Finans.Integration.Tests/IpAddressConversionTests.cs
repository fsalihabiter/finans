using System.Net;
using FluentAssertions;
using Finans.Domain.Enums;
using Finans.Domain.Identity;
using Finans.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Finans.Integration.Tests;

/// <summary>
/// <b>IPAddress ↔ string dönüştürücüsü yuvarlak gidiyor mu?</b> (REVIEW-006 sonrası)
/// <para>
/// `FinansDbContext.OnModelCreating` içinde, PostgreSQL DIŞINDAKİ sağlayıcılar için
/// (Sqlite / InMemory) `IPAddress` kolonları string'e çevriliyor — Sqlite'ta yerel
/// `inet` eşlemesi yok. İki kolon bu dönüştürücüyü kullanıyor ve ikisi de
/// <b>nullable</b>: <c>RefreshToken.CreatedByIp</c> ve <c>AuditLog.IpAddress</c>.
/// </para>
/// <para>
/// ⚠ Bu test neden var: CS8620 uyarısı giderilirken dönüştürücünün bildirim tipi
/// değişti (jenerik olmayan <c>ValueConverter</c> aşırı yüklemesine geçildi) ve
/// o sırada <b>hiçbir test bu iki kolona dokunmuyordu</b> — yani paketin yeşil
/// olması dönüştürücünün hâlâ çalıştığını kanıtlamıyordu. Denetim kaydı
/// (append-only, inkâr-edilemezlik; `12` §7) ve oturum token'ı yolları bu;
/// sessizce bozulması kabul edilemez.
/// </para>
/// <para>
/// Kritik ayrıntı <b>null davranışı</b>: EF Core değer dönüştürücüsünü null için
/// hiç çağırmaz, null'ı doğrudan NULL olarak yazar/okur.
/// </para>
/// <para>
/// ⚠ <b>Bu testin null kolu neyi yakalar, neyi yakalamaz</b> (mutasyonla ölçüldü):
/// dönüştürücüyü null'a boş dizgi yazacak şekilde değiştirmek bu testi KIRMIYOR,
/// çünkü o lambda null için hiç çağrılmıyor — yani test "dönüştürücünün null
/// işlemesini" doğrulamıyor. Doğruladığı şey daha değerli ve daha kırılgan olanı:
/// <b>kolonun gerçek SQL NULL tutmaya devam etmesi</b>. Bunu bozmanın gerçek
/// yolları özelliği non-nullable yapmak, `HasDefaultValue` eklemek ya da
/// dönüştürücüyü EF'in null kısa-devresini atlayan bir eşlemeyle değiştirmektir;
/// test o değişiklikleri yakalar. Yuvarlak gidiş kolu ise dönüştürücünün
/// kendisini korur — bozulduğunda kırmızıya düştüğü doğrulandı.
/// </para>
/// <para>
/// Sqlite kullanılıyor, InMemory değil: dönüştürücünün gerçekten bir metin
/// kolonuna yazıp geri okuduğunu görmek için ilişkisel bir sağlayıcı gerekiyor.
/// </para>
/// </summary>
public sealed class IpAddressConversionTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public IpAddressConversionTests()
    {
        // Paylaşılan bağlantı: bellekteki veritabanı bağlantı açık kaldıkça yaşar.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var db = NewContext();
        db.Database.EnsureCreated();
    }

    private FinansDbContext NewContext() =>
        new(new DbContextOptionsBuilder<FinansDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();

    [Theory]
    [InlineData("203.0.113.42")]                              // IPv4 (RFC 5737 belge aralığı)
    [InlineData("2001:db8::8a2e:370:7334")]                   // IPv6 (RFC 3849 belge aralığı)
    public async Task Ip_addresses_round_trip_through_the_string_converter(string metin)
    {
        var beklenen = IPAddress.Parse(metin);
        var userId = Guid.CreateVersion7();

        await using (var yaz = NewContext())
        {
            yaz.Users.Add(new User { Id = userId, DisplayName = "IP Dönüşüm Testi" });
            yaz.Set<RefreshToken>().Add(new RefreshToken
            {
                UserId = userId,
                TokenHash = "hash",
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                CreatedByIp = beklenen,
            });
            yaz.Set<AuditLog>().Add(new AuditLog
            {
                UserId = userId,
                Action = AuditAction.Login,
                Result = AuditResult.Success,
                IpAddress = beklenen,
            });
            await yaz.SaveChangesAsync();
        }

        // TAZE bağlam: değer gerçekten kolondan okunuyor, izlenen nesneden değil.
        await using var oku = NewContext();
        var token = await oku.Set<RefreshToken>().SingleAsync(t => t.UserId == userId);
        var kayit = await oku.Set<AuditLog>().SingleAsync(a => a.UserId == userId);

        token.CreatedByIp.Should().Be(beklenen,
            "RefreshToken.CreatedByIp dönüştürücüden yuvarlak gitmeli (oturum token'ı yolu)");
        kayit.IpAddress.Should().Be(beklenen,
            "AuditLog.IpAddress dönüştürücüden yuvarlak gitmeli (denetim kaydı yolu)");
    }

    [Fact]
    public async Task Null_ip_stays_null_and_is_not_turned_into_a_placeholder()
    {
        var userId = Guid.CreateVersion7();

        await using (var yaz = NewContext())
        {
            yaz.Users.Add(new User { Id = userId, DisplayName = "IP Yok" });
            yaz.Set<RefreshToken>().Add(new RefreshToken
            {
                UserId = userId,
                TokenHash = "hash",
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                CreatedByIp = null,
            });
            // Başarısız girişte UserId null olabilir; IP de bilinmeyebilir.
            yaz.Set<AuditLog>().Add(new AuditLog
            {
                UserId = userId,
                Action = AuditAction.Login,
                Result = AuditResult.Failure,
                IpAddress = null,
            });
            await yaz.SaveChangesAsync();
        }

        await using var oku = NewContext();
        var token = await oku.Set<RefreshToken>().SingleAsync(t => t.UserId == userId);
        var kayit = await oku.Set<AuditLog>().SingleAsync(a => a.UserId == userId);

        token.CreatedByIp.Should().BeNull(
            "null = 'IP kaydedilmemiş'; bu bilgi yer tutucu bir adrese dönüşmemeli");
        kayit.IpAddress.Should().BeNull("aynı ayrım denetim kaydı için de korunmalı");

        // Kolonun kendisi gerçekten SQL NULL mu? Asıl değişmez bu: eşleme bir gün
        // yer tutucu bir metin yazmaya başlarsa (özellik non-nullable yapılır,
        // HasDefaultValue eklenir, ya da EF'in null kısa-devresi atlanır) denetim
        // kaydı "IP bilinmiyor" ile "IP şu" ayrımını kaybeder.
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "select count(*) from \"AuditLogs\" where \"IpAddress\" is null";
        var nullSayisi = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        nullSayisi.Should().Be(1, "kolonda gerçek SQL NULL durmalı, yer tutucu bir metin değil");
    }
}

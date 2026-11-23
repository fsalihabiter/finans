using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Finans.Integration.Tests;

/// <summary>
/// Gerçek HTTP boru hattı, <b>veritabanı olmadan</b> (INC-001 düzeltmesi).
/// <para>
/// Sağlık ucu, korelasyon kimliği ve hata maskeleme gibi kapılar DB'ye
/// dokunmaz — dokunmamaları zaten sözleşmenin parçası
/// (<c>Liveness_health_returns_ok_without_db</c> bunu adıyla iddia ediyor).
/// Bu yüzden <see cref="SqliteWebApplicationFactory"/> yerine bu fabrika
/// kullanılıyor: Sqlite bağlamak testi geçirir ama "DB'siz çalışıyor"
/// iddiasını da ÇÜRÜTÜR — artık bir DB var ve test bunu kanıtlamıyor olur.
/// </para>
/// <para>
/// Uygulamanın kendi (Npgsql) <c>DbContext</c> kaydı yerinde bırakılır; bu
/// testlerin dokunduğu uçlar onu hiç çözümlemediği için bağlantı kurulmaz.
/// Açılışta migration'ı kapatan ayar <see cref="TestHostDefaults"/>'tan gelir —
/// INC-001'in kök nedeni tam olarak o ayarın eksikliğiydi.
/// </para>
/// </summary>
public sealed class DbLessWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ApplyTestDefaults();
}

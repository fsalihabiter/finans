using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Finans.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CashFixedUnitPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Salt veri düzeltmesi (şema değişmez) — INC-003.
            // API'den oluşturulan nakit pozisyonları CurrentPrice = NULL ile başlıyordu; arayüz
            // nakitte fiyat alanını gizlediği için kullanıcı bunu hiç düzeltemiyordu. Okuma yolu
            // artık nakde 1 dayatıyor (AssetPricing); bu, SAKLANAN veriyi de tutarlı kılar.
            // Yalnız NULL olanlara dokunulur — elle girilmiş bir değer ezilmez.
            migrationBuilder.Sql("""
                UPDATE "Holdings" h
                SET "CurrentPrice" = 1
                FROM "Assets" a
                WHERE a."Id" = h."AssetId"
                  AND a."Type" = 'Cash'
                  AND h."CurrentPrice" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri alınmaz: hangi satırın önceden NULL olduğu bilgisi saklanmıyor ve NULL'a
            // dönmek bilinen bir hatayı geri getirmek olur. Bilinçli olarak boş.
        }
    }
}

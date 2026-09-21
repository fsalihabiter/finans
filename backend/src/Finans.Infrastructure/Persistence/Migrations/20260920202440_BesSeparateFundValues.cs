using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Finans.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BesSeparateFundValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OwnFundValue",
                table: "BesDetails",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StateFundValue",
                table: "BesDetails",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_BesDetails_OwnFundValue",
                table: "BesDetails",
                sql: "\"OwnFundValue\" IS NULL OR \"OwnFundValue\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BesDetails_StateFundValue",
                table: "BesDetails",
                sql: "\"StateFundValue\" IS NULL OR \"StateFundValue\" >= 0");

            // ── Geriye dönük taşıma (GD-002) ──────────────────────────────────
            // Eski model TEK fon değeri tutuyordu (Holdings.CurrentPrice = kendi+devlet
            // toplamının piyasa değeri) ve iki havuzu okuma anında ORANTILI bölüyordu.
            // Mevcut kullanıcı verisi kaybolmasın diye aynı orantı BİR KEZ kalıcılaştırılır:
            // bugünkü ekranlarda görünen tutarlar değişmez, kullanıcı iki değeri ayrı ayrı
            // güncellediğinde model gerçeğe yaklaşır.
            //
            // Taban: fonda FİİLEN olan katkılar — okuma yolundaki tanımla (BesCalculator.
            // DepositedTotals) AYNI: kendi katkı ödeme tarihi geçtiyse; devlet katkısı YATMA
            // tarihi geçtiyse (BesRules.StateDepositDateOn = ödeme ayını izleyen ayın son günü).
            // ⚠ İlk sürüm devlet için de ödeme tarihini kullanıyordu → "yoldaki" devlet katkısı
            // tabana giriyor, bölme devlet havuzuna fazla pay veriyordu (canlı veride iki havuzun
            // getirisi %39 ↔ %48 ayrıştı). Canlıda yakalanıp düzeltildi.
            // Katkı tabanı 0 ise (ya da fon değeri yoksa) alanlar NULL kalır: veri uydurulmaz.
            migrationBuilder.Sql("""
                WITH deposited AS (
                    SELECT c."HoldingId",
                           SUM(c."OwnAmount") AS own_sum,
                           SUM(CASE
                                 WHEN (date_trunc('month', c."PaidAtUtc") + interval '2 months' - interval '1 day')::date
                                      <= current_date
                                 THEN c."StateAmount" ELSE 0 END) AS state_sum
                    FROM "BesContributions" c
                    WHERE c."PaidAtUtc"::date <= current_date
                    GROUP BY c."HoldingId"
                )
                UPDATE "BesDetails" b
                SET "OwnFundValue"   = h."CurrentPrice" * d.own_sum   / (d.own_sum + d.state_sum),
                    "StateFundValue" = h."CurrentPrice" * d.state_sum / (d.own_sum + d.state_sum)
                FROM "Holdings" h
                JOIN deposited d ON d."HoldingId" = h."Id"
                WHERE b."HoldingId" = h."Id"
                  AND h."CurrentPrice" IS NOT NULL
                  AND (d.own_sum + d.state_sum) > 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_BesDetails_OwnFundValue",
                table: "BesDetails");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BesDetails_StateFundValue",
                table: "BesDetails");

            migrationBuilder.DropColumn(
                name: "OwnFundValue",
                table: "BesDetails");

            migrationBuilder.DropColumn(
                name: "StateFundValue",
                table: "BesDetails");
        }
    }
}

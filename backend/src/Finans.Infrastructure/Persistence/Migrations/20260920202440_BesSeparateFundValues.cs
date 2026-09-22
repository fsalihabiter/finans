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
            // REVIEW-002 · RV-009: "bugün" UYGULAMA ile aynı saat diliminde (Türkiye; uygulama
            // UTC+3 sabit kullanır, TR 2016'dan beri DST uygulamıyor) — konteynerin UTC'si değil.
            // Kendi havuzu 2 ondalığa yuvarlanır, KALAN devlet havuzuna gider → iki parçanın
            // toplamı eski tek değere kuruşu kuruşuna eşit (BesCalculator.SplitTotalFundValue ile aynı).
            migrationBuilder.Sql("""
                WITH tr AS (SELECT (now() AT TIME ZONE 'UTC' + interval '3 hours')::date AS today),
                deposited AS (
                    SELECT c."HoldingId",
                           SUM(c."OwnAmount") AS own_sum,
                           SUM(CASE
                                 WHEN (date_trunc('month', c."PaidAtUtc") + interval '2 months' - interval '1 day')::date
                                      <= (SELECT today FROM tr)
                                 THEN c."StateAmount" ELSE 0 END) AS state_sum
                    FROM "BesContributions" c
                    WHERE c."PaidAtUtc"::date <= (SELECT today FROM tr)
                    GROUP BY c."HoldingId"
                ),
                split AS (
                    SELECT h."Id" AS holding_id, h."CurrentPrice" AS total,
                           ROUND(h."CurrentPrice" * d.own_sum / (d.own_sum + d.state_sum), 2) AS own_part
                    FROM "Holdings" h
                    JOIN deposited d ON d."HoldingId" = h."Id"
                    WHERE h."CurrentPrice" IS NOT NULL AND (d.own_sum + d.state_sum) > 0
                )
                UPDATE "BesDetails" b
                SET "OwnFundValue"   = s.own_part,
                    "StateFundValue" = s.total - s.own_part
                FROM split s
                WHERE b."HoldingId" = s.holding_id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // REVIEW-002 · RV-008: kolonlar düşmeden ÖNCE iki havuzun toplamı eski tek alana
            // geri yazılır. Aksi halde eski kod migration günündeki donmuş değeri okur —
            // kullanıcı son girdiği fon değerini değil, aylar önceki bir sayıyı görürdü.
            // (Hak ediş uygulanmaz: eski alan "toplam fon değeri" anlamındaydı.)
            // ⚠ İki havuz AYRIMI yine kaybolur — bu model değişikliğinin doğası; toplam korunur.
            migrationBuilder.Sql("""
                UPDATE "Holdings" h
                SET "CurrentPrice" = b."OwnFundValue" + b."StateFundValue"
                FROM "BesDetails" b
                WHERE b."HoldingId" = h."Id"
                  AND b."OwnFundValue" IS NOT NULL
                  AND b."StateFundValue" IS NOT NULL;
                """);

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

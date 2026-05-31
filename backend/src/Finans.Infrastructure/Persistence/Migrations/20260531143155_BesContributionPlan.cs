using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Finans.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BesContributionPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ContributionDay",
                table: "BesDetails",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyAmount",
                table: "BesDetails",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PlanActive",
                table: "BesDetails",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContributionDay",
                table: "BesDetails");

            migrationBuilder.DropColumn(
                name: "MonthlyAmount",
                table: "BesDetails");

            migrationBuilder.DropColumn(
                name: "PlanActive",
                table: "BesDetails");
        }
    }
}

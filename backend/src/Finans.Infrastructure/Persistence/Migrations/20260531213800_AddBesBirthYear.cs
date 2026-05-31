using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Finans.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBesBirthYear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BirthYear",
                table: "BesDetails",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BirthYear",
                table: "BesDetails");
        }
    }
}

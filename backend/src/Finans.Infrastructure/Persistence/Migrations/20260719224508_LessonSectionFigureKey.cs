using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Finans.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LessonSectionFigureKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FigureKey",
                table: "LessonSections",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FigureKey",
                table: "LessonSections");
        }
    }
}

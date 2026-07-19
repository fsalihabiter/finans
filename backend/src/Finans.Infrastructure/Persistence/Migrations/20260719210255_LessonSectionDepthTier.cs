using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Finans.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LessonSectionDepthTier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // T6.5 — geriye dönük uyum: mevcut satırlar (ve değer vermeyen eklemeler)
            // entity varsayılanlarına düşer. EF'in ürettiği defaultValue "" idi;
            // boş string aşağıdaki CHECK allow-list'ini İHLAL EDERDİ → 'Core'/'Explain'
            // ile değiştirildi (15 §2.1).
            migrationBuilder.AddColumn<string>(
                name: "DepthTier",
                table: "LessonSections",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Core");

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "LessonSections",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Explain");

            migrationBuilder.CreateIndex(
                name: "IX_LessonSections_LessonId_DepthTier",
                table: "LessonSections",
                columns: new[] { "LessonId", "DepthTier" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_LessonSections_DepthTier",
                table: "LessonSections",
                sql: "\"DepthTier\" IN ('Core', 'Context', 'Deep')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LessonSections_Kind",
                table: "LessonSections",
                sql: "\"Kind\" IN ('Explain', 'Example', 'Trap', 'LiveContext', 'Source')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LessonSections_LessonId_DepthTier",
                table: "LessonSections");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LessonSections_DepthTier",
                table: "LessonSections");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LessonSections_Kind",
                table: "LessonSections");

            migrationBuilder.DropColumn(
                name: "DepthTier",
                table: "LessonSections");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "LessonSections");
        }
    }
}

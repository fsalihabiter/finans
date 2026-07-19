using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Finans.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class QuizQuestionDifficulty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Difficulty",
                table: "QuizQuestions",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                // EF'in ürettiği "" boş string, aşağıdaki CHECK allow-list'ini İHLAL EDERDİ.
                // 'Easy' = geriye dönük uyum: mevcut sorular herkese görünmeye devam eder.
                defaultValue: "Easy");

            migrationBuilder.AddCheckConstraint(
                name: "CK_QuizQuestions_Difficulty",
                table: "QuizQuestions",
                sql: "\"Difficulty\" IN ('Easy', 'Medium', 'Hard')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_QuizQuestions_Difficulty",
                table: "QuizQuestions");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "QuizQuestions");
        }
    }
}

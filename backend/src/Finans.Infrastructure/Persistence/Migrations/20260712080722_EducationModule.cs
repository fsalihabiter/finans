using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Finans.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EducationModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConceptTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConceptTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LearningTracks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningTracks", x => x.Id);
                    table.CheckConstraint("CK_LearningTracks_Level", "\"Level\" IN ('Beginner', 'Intermediate', 'Advanced')");
                });

            migrationBuilder.CreateTable(
                name: "Lessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrackId = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    BodyMarkdown = table.Column<string>(type: "text", nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lessons", x => x.Id);
                    table.CheckConstraint("CK_Lessons_EstimatedMinutes", "\"EstimatedMinutes\" > 0");
                    table.CheckConstraint("CK_Lessons_Level", "\"Level\" IN ('Beginner', 'Intermediate', 'Advanced')");
                    table.ForeignKey(
                        name: "FK_Lessons_LearningTracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "LearningTracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LessonConceptTags",
                columns: table => new
                {
                    LessonId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConceptTagId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonConceptTags", x => new { x.LessonId, x.ConceptTagId });
                    table.ForeignKey(
                        name: "FK_LessonConceptTags_ConceptTags_ConceptTagId",
                        column: x => x.ConceptTagId,
                        principalTable: "ConceptTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LessonConceptTags_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LessonPrerequisites",
                columns: table => new
                {
                    LessonId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrerequisiteLessonId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonPrerequisites", x => new { x.LessonId, x.PrerequisiteLessonId });
                    table.CheckConstraint("CK_LessonPrerequisites_NoSelf", "\"LessonId\" <> \"PrerequisiteLessonId\"");
                    table.ForeignKey(
                        name: "FK_LessonPrerequisites_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LessonPrerequisites_Lessons_PrerequisiteLessonId",
                        column: x => x.PrerequisiteLessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LessonSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    Heading = table.Column<string>(type: "text", nullable: true),
                    BodyMarkdown = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonSections_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Quizzes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PassingScore = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quizzes", x => x.Id);
                    table.CheckConstraint("CK_Quizzes_PassingScore", "\"PassingScore\" BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_Quizzes_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLessonProgress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProgressPercent = table.Column<int>(type: "integer", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLessonProgress", x => x.Id);
                    table.CheckConstraint("CK_UserLessonProgress_Percent", "\"ProgressPercent\" BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_UserLessonProgress_Status", "\"Status\" IN ('NotStarted', 'InProgress', 'Completed')");
                    table.ForeignKey(
                        name: "FK_UserLessonProgress_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserLessonProgress_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuizQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuizId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Prompt = table.Column<string>(type: "text", nullable: false),
                    Explanation = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizQuestions", x => x.Id);
                    table.CheckConstraint("CK_QuizQuestions_Type", "\"Type\" IN ('SingleChoice', 'MultipleChoice', 'TrueFalse')");
                    table.ForeignKey(
                        name: "FK_QuizQuestions_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserQuizAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuizId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Passed = table.Column<bool>(type: "boolean", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserQuizAttempts", x => x.Id);
                    table.CheckConstraint("CK_UserQuizAttempts_Score", "\"Score\" BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_UserQuizAttempts_Quizzes_QuizId",
                        column: x => x.QuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserQuizAttempts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuizOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizOptions_QuizQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "QuizQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConceptTags_Key",
                table: "ConceptTags",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningTracks_Slug",
                table: "LearningTracks",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LessonConceptTags_ConceptTagId",
                table: "LessonConceptTags",
                column: "ConceptTagId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonPrerequisites_PrerequisiteLessonId",
                table: "LessonPrerequisites",
                column: "PrerequisiteLessonId");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_Slug",
                table: "Lessons",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_TrackId_OrderIndex",
                table: "Lessons",
                columns: new[] { "TrackId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonSections_LessonId_OrderIndex",
                table: "LessonSections",
                columns: new[] { "LessonId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_QuizOptions_QuestionId_OrderIndex",
                table: "QuizOptions",
                columns: new[] { "QuestionId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuestions_QuizId_OrderIndex",
                table: "QuizQuestions",
                columns: new[] { "QuizId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_LessonId",
                table: "Quizzes",
                column: "LessonId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserLessonProgress_LessonId",
                table: "UserLessonProgress",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLessonProgress_UserId",
                table: "UserLessonProgress",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLessonProgress_UserId_LessonId",
                table: "UserLessonProgress",
                columns: new[] { "UserId", "LessonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserQuizAttempts_QuizId",
                table: "UserQuizAttempts",
                column: "QuizId");

            migrationBuilder.CreateIndex(
                name: "IX_UserQuizAttempts_UserId_QuizId",
                table: "UserQuizAttempts",
                columns: new[] { "UserId", "QuizId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LessonConceptTags");

            migrationBuilder.DropTable(
                name: "LessonPrerequisites");

            migrationBuilder.DropTable(
                name: "LessonSections");

            migrationBuilder.DropTable(
                name: "QuizOptions");

            migrationBuilder.DropTable(
                name: "UserLessonProgress");

            migrationBuilder.DropTable(
                name: "UserQuizAttempts");

            migrationBuilder.DropTable(
                name: "ConceptTags");

            migrationBuilder.DropTable(
                name: "QuizQuestions");

            migrationBuilder.DropTable(
                name: "Quizzes");

            migrationBuilder.DropTable(
                name: "Lessons");

            migrationBuilder.DropTable(
                name: "LearningTracks");
        }
    }
}

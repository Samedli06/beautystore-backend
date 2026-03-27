using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTeam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuizQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StepKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizQuestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuizRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuizAnswerOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnswerCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AnswerText = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SubText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizAnswerOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizAnswerOptions_QuizQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "QuizQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuizRuleProducts",
                columns: table => new
                {
                    RuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizRuleProducts", x => new { x.RuleId, x.ProductId });
                    table.ForeignKey(
                        name: "FK_QuizRuleProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuizRuleProducts_QuizRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "QuizRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuizRuleAnswers",
                columns: table => new
                {
                    RuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnswerOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizRuleAnswers", x => new { x.RuleId, x.AnswerOptionId });
                    table.ForeignKey(
                        name: "FK_QuizRuleAnswers_QuizAnswerOptions_AnswerOptionId",
                        column: x => x.AnswerOptionId,
                        principalTable: "QuizAnswerOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuizRuleAnswers_QuizRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "QuizRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuizAnswerOptions_AnswerCode",
                table: "QuizAnswerOptions",
                column: "AnswerCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuizAnswerOptions_QuestionId",
                table: "QuizAnswerOptions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizRuleAnswers_AnswerOptionId",
                table: "QuizRuleAnswers",
                column: "AnswerOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizRuleProducts_ProductId",
                table: "QuizRuleProducts",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuizRuleAnswers");

            migrationBuilder.DropTable(
                name: "QuizRuleProducts");

            migrationBuilder.DropTable(
                name: "QuizAnswerOptions");

            migrationBuilder.DropTable(
                name: "QuizRules");

            migrationBuilder.DropTable(
                name: "QuizQuestions");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Challenge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActualUserIdToSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CRITERIA_SKILL_MAPPINGS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriteriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CRITERIA_SKILL_MAPPINGS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EVALUATION_CRITERIA",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EVALUATION_CRITERIA", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PENDING_SKILLS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProposedName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProposedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PENDING_SKILLS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SKILL_ALIASES",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Alias = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SKILL_ALIASES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SKILL_CATEGORIES",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SKILL_CATEGORIES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SKILL_POINT_TRANSACTIONS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Points = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SKILL_POINT_TRANSACTIONS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SKILL_RELATIONSHIPS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceSkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetSkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelationType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SKILL_RELATIONSHIPS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SKILLS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SKILLS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SUBMISSION_CRITERIA_SCORES",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriteriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Feedback = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SUBMISSION_CRITERIA_SCORES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "USER_SKILLS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalPoints = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MasteryScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VerifiedChallengeCount = table.Column<int>(type: "int", nullable: false),
                    LastVerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerificationLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_SKILLS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CHALLENGE_CRITERIA",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChallengeVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriteriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VersionedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHALLENGE_CRITERIA", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CHALLENGE_CRITERIA_EVALUATION_CRITERIA",
                        column: x => x.CriteriaId,
                        principalTable: "EVALUATION_CRITERIA",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CHALLENGE_SUBMISSIONS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChallengeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActualUserId = table.Column<int>(type: "int", nullable: true),
                    SubmissionContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GithubUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OverallScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AiFeedback = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    VersionSnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GradedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHALLENGE_SUBMISSIONS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CHALLENGE_VERSIONS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChallengeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpectedSolution = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DifficultyScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DifficultyLabel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SkillWeightMapping = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PromptVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvaluatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHALLENGE_VERSIONS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CHALLENGES",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpectedSolution = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DifficultyScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DifficultyLabel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Deadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHALLENGES", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CHALLENGES_CHALLENGE_VERSIONS_CurrentVersionId",
                        column: x => x.CurrentVersionId,
                        principalTable: "CHALLENGE_VERSIONS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PROMPT_SANITIZATION_LOGS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiskFlags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SanitizationSummary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PromptHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PromptVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROMPT_SANITIZATION_LOGS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PROMPT_SANITIZATION_LOGS_CHALLENGE_VERSIONS_VersionId",
                        column: x => x.VersionId,
                        principalTable: "CHALLENGE_VERSIONS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CHALLENGE_CRITERIA_CriteriaId",
                table: "CHALLENGE_CRITERIA",
                column: "CriteriaId");

            migrationBuilder.CreateIndex(
                name: "IX_CHALLENGE_SUBMISSIONS_VersionSnapshotId",
                table: "CHALLENGE_SUBMISSIONS",
                column: "VersionSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_CHALLENGE_VERSIONS_ChallengeId",
                table: "CHALLENGE_VERSIONS",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_CHALLENGES_CurrentVersionId",
                table: "CHALLENGES",
                column: "CurrentVersionId",
                unique: true,
                filter: "[CurrentVersionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PROMPT_SANITIZATION_LOGS_VersionId",
                table: "PROMPT_SANITIZATION_LOGS",
                column: "VersionId");

            migrationBuilder.AddForeignKey(
                name: "FK_CHALLENGE_SUBMISSIONS_CHALLENGE_VERSIONS_VersionSnapshotId",
                table: "CHALLENGE_SUBMISSIONS",
                column: "VersionSnapshotId",
                principalTable: "CHALLENGE_VERSIONS",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CHALLENGE_VERSIONS_CHALLENGES_ChallengeId",
                table: "CHALLENGE_VERSIONS",
                column: "ChallengeId",
                principalTable: "CHALLENGES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CHALLENGES_CHALLENGE_VERSIONS_CurrentVersionId",
                table: "CHALLENGES");

            migrationBuilder.DropTable(
                name: "CHALLENGE_CRITERIA");

            migrationBuilder.DropTable(
                name: "CHALLENGE_SUBMISSIONS");

            migrationBuilder.DropTable(
                name: "CRITERIA_SKILL_MAPPINGS");

            migrationBuilder.DropTable(
                name: "PENDING_SKILLS");

            migrationBuilder.DropTable(
                name: "PROMPT_SANITIZATION_LOGS");

            migrationBuilder.DropTable(
                name: "SKILL_ALIASES");

            migrationBuilder.DropTable(
                name: "SKILL_CATEGORIES");

            migrationBuilder.DropTable(
                name: "SKILL_POINT_TRANSACTIONS");

            migrationBuilder.DropTable(
                name: "SKILL_RELATIONSHIPS");

            migrationBuilder.DropTable(
                name: "SKILLS");

            migrationBuilder.DropTable(
                name: "SUBMISSION_CRITERIA_SCORES");

            migrationBuilder.DropTable(
                name: "USER_SKILLS");

            migrationBuilder.DropTable(
                name: "EVALUATION_CRITERIA");

            migrationBuilder.DropTable(
                name: "CHALLENGE_VERSIONS");

            migrationBuilder.DropTable(
                name: "CHALLENGES");
        }
    }
}

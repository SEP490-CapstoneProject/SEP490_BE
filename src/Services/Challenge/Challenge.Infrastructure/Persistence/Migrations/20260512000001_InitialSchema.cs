using Microsoft.EntityFrameworkCore.Migrations;
namespace Challenge.Infrastructure.Persistence.Migrations;


/// <summary>
/// Initial migration for AI Challenge System
/// Creates all 16 tables with proper relationships and constraints
/// </summary>
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public partial class InitialSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Skills table
        migrationBuilder.CreateTable(
            name: "SKILLS",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                Slug = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                IsApproved = table.Column<bool>(type: "bit", nullable: false),
                CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SKILLS", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SKILLS_Slug",
            table: "SKILLS",
            column: "Slug",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SKILLS_Name",
            table: "SKILLS",
            column: "Name");

        // Skill Aliases table
        migrationBuilder.CreateTable(
            name: "SKILL_ALIASES",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Alias = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SKILL_ALIASES", x => x.Id);
                table.ForeignKey(
                    name: "FK_SKILL_ALIASES_SKILLS_SkillId",
                    column: x => x.SkillId,
                    principalTable: "SKILLS",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_SKILL_ALIASES_Alias",
            table: "SKILL_ALIASES",
            column: "Alias");

        // Skill Categories
        migrationBuilder.CreateTable(
            name: "SKILL_CATEGORIES",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SKILL_CATEGORIES", x => x.Id);
            });

        // Evaluation Criteria
        migrationBuilder.CreateTable(
            name: "EVALUATION_CRITERIA",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                Weight = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EVALUATION_CRITERIA", x => x.Id);
            });

        // Criteria Skill Mappings
        migrationBuilder.CreateTable(
            name: "CRITERIA_SKILL_MAPPINGS",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CriterionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Weight = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CRITERIA_SKILL_MAPPINGS", x => x.Id);
                table.ForeignKey(
                    name: "FK_CRITERIA_SKILL_MAPPINGS_EVALUATION_CRITERIA",
                    column: x => x.CriterionId,
                    principalTable: "EVALUATION_CRITERIA",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_CRITERIA_SKILL_MAPPINGS_SKILLS",
                    column: x => x.SkillId,
                    principalTable: "SKILLS",
                    principalColumn: "Id");
            });

        // Pending Skills
        migrationBuilder.CreateTable(
            name: "PENDING_SKILLS",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProposedName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                Status = table.Column<string>(type: "nvarchar(50)", nullable: false),
                ReviewedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PENDING_SKILLS", x => x.Id);
            });

        // Challenges
        migrationBuilder.CreateTable(
            name: "CHALLENGES",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ExpectedSolution = table.Column<string>(type: "nvarchar(max)", nullable: false),
                DifficultyScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                DifficultyLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                Status = table.Column<string>(type: "nvarchar(50)", nullable: false),
                CurrentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ReviewedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                Deadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CHALLENGES", x => x.Id);
            });

        migrationBuilder.CreateIndex(name: "IX_CHALLENGES_Status", table: "CHALLENGES", column: "Status");
        migrationBuilder.CreateIndex(name: "IX_CHALLENGES_CreatedById", table: "CHALLENGES", column: "CreatedById");
        migrationBuilder.CreateIndex(name: "IX_CHALLENGES_Deadline", table: "CHALLENGES", column: "Deadline");

        // Challenge Versions
        migrationBuilder.CreateTable(
            name: "CHALLENGE_VERSIONS",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ChallengeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                VersionNumber = table.Column<int>(type: "int", nullable: false),
                Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ExpectedSolution = table.Column<string>(type: "nvarchar(max)", nullable: false),
                DifficultyScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                DifficultyLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                SkillWeightMapping = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                PromptVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                EvaluatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CHALLENGE_VERSIONS", x => x.Id);
                table.ForeignKey(
                    name: "FK_CHALLENGE_VERSIONS_CHALLENGES",
                    column: x => x.ChallengeId,
                    principalTable: "CHALLENGES",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_CHALLENGE_VERSIONS_ChallengeId_VersionNumber",
            table: "CHALLENGE_VERSIONS",
            columns: new[] { "ChallengeId", "VersionNumber" },
            unique: true);

        // Add FK to Challenges CurrentVersionId
        migrationBuilder.AddForeignKey(
            name: "FK_CHALLENGES_CHALLENGE_VERSIONS_CurrentVersionId",
            table: "CHALLENGES",
            column: "CurrentVersionId",
            principalTable: "CHALLENGE_VERSIONS",
            principalColumn: "Id");

        // Challenge Criteria
        migrationBuilder.CreateTable(
            name: "CHALLENGE_CRITERIA",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                VersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CriteriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CHALLENGE_CRITERIA", x => x.Id);
                table.ForeignKey(
                    name: "FK_CHALLENGE_CRITERIA_CHALLENGE_VERSIONS",
                    column: x => x.VersionId,
                    principalTable: "CHALLENGE_VERSIONS",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_CHALLENGE_CRITERIA_EVALUATION_CRITERIA",
                    column: x => x.CriteriaId,
                    principalTable: "EVALUATION_CRITERIA",
                    principalColumn: "Id");
            });

        // Challenge Submissions
        migrationBuilder.CreateTable(
            name: "CHALLENGE_SUBMISSIONS",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ChallengeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SubmissionContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                GithubUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                OverallScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                AiFeedback = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                Status = table.Column<string>(type: "nvarchar(50)", nullable: false),
                VersionSnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AttemptCount = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                GradedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CHALLENGE_SUBMISSIONS", x => x.Id);
                table.ForeignKey(
                    name: "FK_CHALLENGE_SUBMISSIONS_CHALLENGE_VERSIONS",
                    column: x => x.VersionSnapshotId,
                    principalTable: "CHALLENGE_VERSIONS",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_CHALLENGE_SUBMISSIONS_UserId_ChallengeId",
            table: "CHALLENGE_SUBMISSIONS",
            columns: new[] { "UserId", "ChallengeId" });
        migrationBuilder.CreateIndex(
            name: "IX_CHALLENGE_SUBMISSIONS_Status",
            table: "CHALLENGE_SUBMISSIONS",
            column: "Status");

        // Submission Criteria Scores
        migrationBuilder.CreateTable(
            name: "SUBMISSION_CRITERIA_SCORES",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CriteriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Score = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                Feedback = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SUBMISSION_CRITERIA_SCORES", x => x.Id);
                table.ForeignKey(
                    name: "FK_SUBMISSION_CRITERIA_SCORES_CHALLENGE_SUBMISSIONS",
                    column: x => x.SubmissionId,
                    principalTable: "CHALLENGE_SUBMISSIONS",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_SUBMISSION_CRITERIA_SCORES_EVALUATION_CRITERIA",
                    column: x => x.CriteriaId,
                    principalTable: "EVALUATION_CRITERIA",
                    principalColumn: "Id");
            });

        // Skill Point Transactions
        migrationBuilder.CreateTable(
            name: "SKILL_POINT_TRANSACTIONS",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Points = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SKILL_POINT_TRANSACTIONS", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SKILL_POINT_TRANSACTIONS_UserId_SkillId",
            table: "SKILL_POINT_TRANSACTIONS",
            columns: new[] { "UserId", "SkillId" });
        migrationBuilder.CreateIndex(
            name: "IX_SKILL_POINT_TRANSACTIONS_CreatedAt",
            table: "SKILL_POINT_TRANSACTIONS",
            column: "CreatedAt");

        // User Skills
        migrationBuilder.CreateTable(
            name: "USER_SKILLS",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TotalPoints = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                MasteryScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                VerifiedChallengeCount = table.Column<int>(type: "int", nullable: false),
                LastVerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                VerificationLevel = table.Column<string>(type: "nvarchar(50)", nullable: false),
                IsVerified = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_USER_SKILLS", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_USER_SKILLS_UserId_SkillId",
            table: "USER_SKILLS",
            columns: new[] { "UserId", "SkillId" },
            unique: true);
        migrationBuilder.CreateIndex(name: "IX_USER_SKILLS_UserId", table: "USER_SKILLS", column: "UserId");
        migrationBuilder.CreateIndex(
            name: "IX_USER_SKILLS_UserId_VerificationLevel",
            table: "USER_SKILLS",
            columns: new[] { "UserId", "VerificationLevel" });

        // Skill Relationships
        migrationBuilder.CreateTable(
            name: "SKILL_RELATIONSHIPS",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SourceSkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TargetSkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RelationType = table.Column<string>(type: "nvarchar(50)", nullable: false),
                Weight = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SKILL_RELATIONSHIPS", x => x.Id);
                table.ForeignKey(
                    name: "FK_SKILL_RELATIONSHIPS_SKILLS_Source",
                    column: x => x.SourceSkillId,
                    principalTable: "SKILLS",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_SKILL_RELATIONSHIPS_SKILLS_Target",
                    column: x => x.TargetSkillId,
                    principalTable: "SKILLS",
                    principalColumn: "Id");
            });

        // Prompt Sanitization Logs
        migrationBuilder.CreateTable(
            name: "PROMPT_SANITIZATION_LOGS",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                VersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RiskFlags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                SanitizationSummary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                PromptHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                ModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                PromptVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PROMPT_SANITIZATION_LOGS", x => x.Id);
                table.ForeignKey(
                    name: "FK_PROMPT_SANITIZATION_LOGS_CHALLENGE_VERSIONS",
                    column: x => x.VersionId,
                    principalTable: "CHALLENGE_VERSIONS",
                    principalColumn: "Id");
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CRITERIA_SKILL_MAPPINGS");
        migrationBuilder.DropTable(name: "PENDING_SKILLS");
        migrationBuilder.DropTable(name: "PROMPT_SANITIZATION_LOGS");
        migrationBuilder.DropTable(name: "SKILL_POINT_TRANSACTIONS");
        migrationBuilder.DropTable(name: "SKILL_RELATIONSHIPS");
        migrationBuilder.DropTable(name: "SUBMISSION_CRITERIA_SCORES");
        migrationBuilder.DropTable(name: "USER_SKILLS");
        migrationBuilder.DropTable(name: "CHALLENGE_CRITERIA");
        migrationBuilder.DropTable(name: "CHALLENGE_SUBMISSIONS");
        migrationBuilder.DropTable(name: "SKILL_ALIASES");
        migrationBuilder.DropTable(name: "CHALLENGE_VERSIONS");
        migrationBuilder.DropTable(name: "CHALLENGES");
        migrationBuilder.DropTable(name: "EVALUATION_CRITERIA");
        migrationBuilder.DropTable(name: "SKILL_CATEGORIES");
        migrationBuilder.DropTable(name: "SKILLS");
    }
}
#pragma warning restore CS1591

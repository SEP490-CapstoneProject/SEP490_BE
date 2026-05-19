using Microsoft.EntityFrameworkCore;
using Challenge.Domain.Entities;
using ChallengeEntity = Challenge.Domain.Entities.Challenge;

namespace Challenge.Infrastructure.Persistence;

public class ChallengeDbContext : DbContext
{
    public ChallengeDbContext(DbContextOptions<ChallengeDbContext> options)
        : base(options)
    {
    }

    public DbSet<Skill> Skills { get; set; }
    public DbSet<SkillAlias> SkillAliases { get; set; }
    public DbSet<SkillCategory> SkillCategories { get; set; }
    public DbSet<PendingSkill> PendingSkills { get; set; }
    public DbSet<EvaluationCriteria> EvaluationCriteria { get; set; }
    public DbSet<CriteriaSkillMapping> CriteriaSkillMappings { get; set; }
    public DbSet<ChallengeEntity> Challenges { get; set; }
    public DbSet<ChallengeVersion> ChallengeVersions { get; set; }
    public DbSet<ChallengeCriteria> ChallengeCriteria { get; set; }
    public DbSet<ChallengeSubmission> ChallengeSubmissions { get; set; }
    public DbSet<SubmissionCriteriaScore> SubmissionCriteriaScores { get; set; }
    public DbSet<SkillPointTransaction> SkillPointTransactions { get; set; }
    public DbSet<UserSkill> UserSkills { get; set; }
    public DbSet<SkillRelationship> SkillRelationships { get; set; }
    public DbSet<PromptSanitizationLog> PromptSanitizationLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Skill>().ToTable("SKILLS");
        modelBuilder.Entity<SkillAlias>().ToTable("SKILL_ALIASES");
        modelBuilder.Entity<SkillCategory>().ToTable("SKILL_CATEGORIES");
        modelBuilder.Entity<PendingSkill>().ToTable("PENDING_SKILLS");
        modelBuilder.Entity<EvaluationCriteria>().ToTable("EVALUATION_CRITERIA");
        modelBuilder.Entity<CriteriaSkillMapping>().ToTable("CRITERIA_SKILL_MAPPINGS");
        modelBuilder.Entity<ChallengeEntity>().ToTable("CHALLENGES");
        modelBuilder.Entity<ChallengeVersion>().ToTable("CHALLENGE_VERSIONS");
        modelBuilder.Entity<ChallengeCriteria>().ToTable("CHALLENGE_CRITERIA");
        modelBuilder.Entity<ChallengeSubmission>().ToTable("CHALLENGE_SUBMISSIONS");
        modelBuilder.Entity<SubmissionCriteriaScore>().ToTable("SUBMISSION_CRITERIA_SCORES");
        modelBuilder.Entity<SkillPointTransaction>().ToTable("SKILL_POINT_TRANSACTIONS");
        modelBuilder.Entity<UserSkill>().ToTable("USER_SKILLS");
        modelBuilder.Entity<SkillRelationship>().ToTable("SKILL_RELATIONSHIPS");
        modelBuilder.Entity<PromptSanitizationLog>().ToTable("PROMPT_SANITIZATION_LOGS");

        modelBuilder.Entity<Skill>()
            .Property(skill => skill.CategoryId)
            .IsRequired(false);

        modelBuilder.Entity<PendingSkill>()
            .Property(skill => skill.Status)
            .HasConversion<string>();

        modelBuilder.Entity<UserSkill>()
            .Property(skill => skill.VerificationLevel)
            .HasConversion<string>();

        modelBuilder.Entity<SkillRelationship>()
            .Property(relationship => relationship.RelationType)
            .HasConversion<string>();

        modelBuilder.Entity<ChallengeEntity>()
            .HasOne(challenge => challenge.CurrentVersion)
            .WithOne()
            .HasForeignKey<ChallengeEntity>(challenge => challenge.CurrentVersionId)
            .HasConstraintName("FK_CHALLENGES_CHALLENGE_VERSIONS_CurrentVersionId")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChallengeSubmission>()
            .Property(submission => submission.VersionSnapshotId)
            .HasColumnName("VersionSnapshotId");

        modelBuilder.Entity<ChallengeSubmission>()
            .Property(submission => submission.VersionId)
            .HasColumnName("VersionId");

        modelBuilder.Entity<ChallengeSubmission>()
            .HasOne(submission => submission.Version)
            .WithMany()
            .HasForeignKey(submission => submission.VersionSnapshotId)
            .HasConstraintName("FK_CHALLENGE_SUBMISSIONS_CHALLENGE_VERSIONS_VersionSnapshotId")
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChallengeEntity>()
            .HasIndex(challenge => challenge.CurrentVersionId)
            .IsUnique()
            .HasFilter("[CurrentVersionId] IS NOT NULL");

        // ChallengeCriteria relationship with EvaluationCriteria
        modelBuilder.Entity<ChallengeCriteria>()
            .HasOne(cc => cc.Criteria)
            .WithMany()
            .HasForeignKey(cc => cc.CriteriaId)
            .HasConstraintName("FK_CHALLENGE_CRITERIA_EVALUATION_CRITERIA")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

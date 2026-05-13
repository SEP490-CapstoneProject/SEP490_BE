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

        modelBuilder.Entity<ChallengeEntity>()
            .HasMany<ChallengeVersion>()
            .WithOne(version => version.Challenge)
            .HasForeignKey(version => version.ChallengeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChallengeEntity>()
            .HasOne(challenge => challenge.CurrentVersion)
            .WithOne()
            .HasForeignKey<ChallengeEntity>(challenge => challenge.CurrentVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

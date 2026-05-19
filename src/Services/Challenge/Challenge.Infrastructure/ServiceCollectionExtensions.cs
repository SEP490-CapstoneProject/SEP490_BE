using Microsoft.Extensions.DependencyInjection;
using Challenge.Domain.Repositories;
using Challenge.Infrastructure.Persistence;
using Challenge.Infrastructure.Persistence.Repositories;
using Challenge.Application.Services;
using Challenge.Application.Services.AI;
namespace Challenge.Infrastructure;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddChallengeServices(
        this IServiceCollection services)
    {
        // Data Access
        services.AddScoped<ISkillRepository, SkillRepository>();
        services.AddScoped<IChallengeRepository, ChallengeRepository>();
        services.AddScoped<IChallengeVersionRepository, ChallengeVersionRepository>();
        services.AddScoped<ISubmissionRepository, SubmissionRepository>();
        services.AddScoped<ISubmissionCriteriaScoreRepository, SubmissionCriteriaScoreRepository>();
        services.AddScoped<IEvaluationCriteriaRepository, EvaluationCriteriaRepository>();
        services.AddScoped<IChallengeCriteriaRepository, ChallengeCriteriaRepository>();
        services.AddScoped<ICriteriaSkillMappingRepository, CriteriaSkillMappingRepository>();
        services.AddScoped<IUserSkillRepository, UserSkillRepository>();
        services.AddScoped<ISkillPointTransactionRepository, SkillPointTransactionRepository>();

        // Business Services
        services.AddScoped<ISkillNormalizationService, SkillNormalizationService>();
        services.AddScoped<IPromptSanitizationService, PromptSanitizationService>();
        services.AddScoped<IGeminiAIService, GeminiAIService>();
        services.AddScoped<ISkillPointEngine, SkillPointEngine>();
        services.AddScoped<ISubmissionGradingOrchestrator, SubmissionGradingOrchestrator>();
        services.AddScoped<IChallengeExpirationJob, ChallengeExpirationJob>();

        return services;
    }
}

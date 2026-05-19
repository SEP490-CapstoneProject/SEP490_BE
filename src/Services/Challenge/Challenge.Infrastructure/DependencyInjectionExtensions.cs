using Challenge.Application.Clients;
using Challenge.Application.Interfaces;
using Challenge.Application.Services;
using Challenge.Application.Services.AI;
using Challenge.Infrastructure.Clients;
using Challenge.Domain.Repositories;
using Challenge.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Challenge.Infrastructure;

public static class ChallengeServiceCollectionExtensions
{
    public static IServiceCollection AddChallengeServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddChallengeRepositories();

        services.AddScoped<IChallengeService, ChallengeService>();
        services.AddScoped<ISkillService, SkillService>();
        services.AddScoped<ISubmissionService, SubmissionService>();
        services.AddScoped<IGradingService, GradingService>();
        services.AddScoped<ISkillPointService, SkillPointService>();
        services.AddScoped<IPortfolioSkillsService, PortfolioSkillsService>();
        services.AddScoped<ISkillNormalizationService, SkillNormalizationService>();
        services.AddScoped<IPromptSanitizationService, PromptSanitizationService>();
        services.AddScoped<IGeminiAIService, GeminiAIService>();

        services.AddHttpClient<IActorResolverClient, ActorResolverClient>();
        services.AddHttpClient<IGeminiAIClient, GeminiAIClient>();
        services.AddScoped<IEventPublisher, EventPublisher>();

        return services;
    }

    public static IServiceCollection AddChallengeRepositories(this IServiceCollection services)
    {
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

        return services;
    }
}

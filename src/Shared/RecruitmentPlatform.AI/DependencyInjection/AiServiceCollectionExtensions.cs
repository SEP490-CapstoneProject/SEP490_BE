using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecruitmentPlatform.AI.Abstractions;
using RecruitmentPlatform.AI.Services;

namespace RecruitmentPlatform.AI.DependencyInjection;

public static class AiServiceCollectionExtensions
{
    public static IServiceCollection AddRecruitmentPlatformAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddScoped<ITextNormalizer, TextNormalizer>();
        services.AddScoped<IVectorSimilarity, CosineSimilarityService>();
        services.AddScoped<IScoringHelper, ScoringHelper>();
        services.AddScoped<IMatchingEngine, MatchingEngine>();
        services.AddScoped<ModerationService>();

        services.AddHttpClient<IEmbeddingService, OpenAiEmbeddingService>(client =>
        {
            client.BaseAddress = new Uri(configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com");
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        return services;
    }
}

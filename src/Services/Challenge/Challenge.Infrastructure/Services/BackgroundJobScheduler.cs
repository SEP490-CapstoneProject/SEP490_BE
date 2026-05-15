using Microsoft.Extensions.DependencyInjection;

namespace Challenge.Infrastructure.Services;

public interface IBackgroundJobScheduler
{
    void ScheduleChallengeExpirationJob();
    void ScheduleSkillRecalculationJob();
}

public class BackgroundJobScheduler : IBackgroundJobScheduler
{
    public BackgroundJobScheduler(IServiceProvider serviceProvider)
    {
    }

    public void ScheduleChallengeExpirationJob()
    {
    }

    public void ScheduleSkillRecalculationJob()
    {
    }
}

public static class BackgroundJobExtensions
{
    public static IServiceCollection AddChallengeBackgroundJobs(this IServiceCollection services)
        => services;

    public static void ConfigureChallengeBackgroundJobsWithHangfire(object app, ILogger logger)
    {
        logger.LogInformation("Challenge background jobs configured.");
    }
}

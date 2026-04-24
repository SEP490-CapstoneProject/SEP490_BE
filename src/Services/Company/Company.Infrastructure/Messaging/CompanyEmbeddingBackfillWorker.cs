using System.Text.Json;
using Company.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RecruitmentPlatform.AI.Abstractions;
using RecruitmentPlatform.AI.Models;

namespace Company.Infrastructure.Messaging;

public sealed class CompanyEmbeddingBackfillWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CompanyEmbeddingBackfillWorker> _logger;
    private readonly EmbeddingBackfillOptions _options;

    public CompanyEmbeddingBackfillWorker(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<CompanyEmbeddingBackfillWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = configuration.GetSection("EmbeddingBackfill").Get<EmbeddingBackfillOptions>() ?? new EmbeddingBackfillOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<ICompanyPostRepository>();
                var textNormalizer = scope.ServiceProvider.GetRequiredService<ITextNormalizer>();
                var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();

                var candidates = await repo.GetPostsForEmbeddingBackfillAsync(_options.BatchSize);
                var successCount = 0;
                var failedCount = 0;

                foreach (var post in candidates)
                {
                    var text = textNormalizer.BuildJobText(new EmbeddingTextInput
                    {
                        Title = post.Position,
                        Description = post.JobDescription,
                        Skills = ExtractSkills(post.RequirementsMandatory, post.RequirementsPreferred),
                        Categories = ExtractCategories(post.EmploymentType, post.Address),
                        CustomFields = new[] { post.Benefits ?? "N/A", post.Salary ?? "N/A" }
                    });

                    try
                    {
                        var embedding = await CreateEmbeddingWithRetryAsync(embeddingService, text, stoppingToken);
                        await repo.UpdateEmbeddingAsync(
                            post.PostId,
                            JsonSerializer.Serialize(embedding),
                            Math.Max(1, post.EmbeddingVersion + 1),
                            DateTime.UtcNow,
                            EmbeddingReadinessPolicy.ResolveStatus(embedding));
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        if (IsQuotaError(ex))
                        {
                            _logger.LogWarning(ex, "Company backfill paused due to OpenAI quota/429 at post {PostId}", post.PostId);
                            await repo.UpdateEmbeddingAsync(
                                post.PostId,
                                post.Embedding,
                                post.EmbeddingVersion,
                                DateTime.UtcNow,
                                EmbeddingReadinessPolicy.Pending);
                            break;
                        }

                        failedCount++;
                        _logger.LogWarning(ex, "Company backfill embedding failed for post {PostId}", post.PostId);
                        await repo.UpdateEmbeddingAsync(
                            post.PostId,
                            post.Embedding,
                            post.EmbeddingVersion,
                            DateTime.UtcNow,
                            EmbeddingReadinessPolicy.Failed);
                    }
                }

                if (candidates.Count > 0)
                {
                    _logger.LogInformation("Company embedding backfill sweep completed. Candidates={Candidates}, Success={Success}, Failed={Failed}",
                        candidates.Count, successCount, failedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Company embedding backfill sweep failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(10, _options.IntervalSeconds)), stoppingToken);
        }
    }

    private async Task<float[]> CreateEmbeddingWithRetryAsync(IEmbeddingService embeddingService, string text, CancellationToken cancellationToken)
    {
        var attempts = Math.Max(1, _options.MaxRetryAttempts);
        var baseDelayMs = Math.Max(100, _options.RetryBaseDelayMs);

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                return await embeddingService.CreateEmbeddingAsync(text, cancellationToken);
            }
            catch (Exception ex) when (attempt < attempts && !IsQuotaError(ex))
            {
                var delayMs = baseDelayMs * (int)Math.Pow(2, attempt - 1);
                await Task.Delay(TimeSpan.FromMilliseconds(delayMs), cancellationToken);
            }
        }

        throw new InvalidOperationException("Embedding generation failed after max retry attempts.");
    }

    private static List<string> ExtractSkills(params string?[] textParts)
    {
        return textParts
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .SelectMany(x => x!.Split([',', ';', '\n', '\r', '|', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(x => x.Length > 1)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> ExtractCategories(params string?[] textParts)
    {
        return textParts
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .SelectMany(x => x!.Split([',', ';', '\n', '\r', '|', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(x => x.Length > 1)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsQuotaError(Exception ex)
    {
        return ex.Message.Contains("TooManyRequests", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("quota", StringComparison.OrdinalIgnoreCase);
    }
}

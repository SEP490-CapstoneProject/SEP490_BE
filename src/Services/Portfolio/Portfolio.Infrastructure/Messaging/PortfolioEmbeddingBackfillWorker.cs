using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Portfolio.Application.Interfaces;
using RecruitmentPlatform.AI.Abstractions;
using RecruitmentPlatform.AI.Models;
using RecruitmentPlatform.Contracts.Time;

namespace Portfolio.Infrastructure.Messaging;

public sealed class PortfolioEmbeddingBackfillWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PortfolioEmbeddingBackfillWorker> _logger;
    private readonly EmbeddingBackfillOptions _options;

    public PortfolioEmbeddingBackfillWorker(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<PortfolioEmbeddingBackfillWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = configuration.GetSection("EmbeddingBackfill").Get<EmbeddingBackfillOptions>() ?? new EmbeddingBackfillOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Portfolio embedding backfill is disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
                var textNormalizer = scope.ServiceProvider.GetRequiredService<ITextNormalizer>();
                var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();

                var candidates = await repo.GetPortfoliosForEmbeddingBackfillAsync(_options.BatchSize);
                var successCount = 0;
                var failedCount = 0;

                foreach (var portfolio in candidates)
                {
                    var text = textNormalizer.BuildPortfolioText(new EmbeddingTextInput
                    {
                        Title = portfolio.Name,
                        Description = BuildPortfolioDescription(portfolio),
                        Skills = ExtractPortfolioSkills(portfolio),
                        Categories = Array.Empty<string>(),
                        Projects = portfolio.Blocks.Where(x => x.BlockTypeId == 6).Select(x => x.DataJson).ToList(),
                        CustomFields = portfolio.Blocks.Select(x => x.DataJson).Take(5).ToList()
                    });

                    try
                    {
                        var embedding = await CreateEmbeddingWithRetryAsync(embeddingService, text, stoppingToken);
                        await repo.UpdateEmbeddingAsync(
                            portfolio.Id,
                            JsonSerializer.Serialize(embedding),
                            Math.Max(1, portfolio.EmbeddingVersion + 1),
                            VietnamTime.Now(),
                            EmbeddingReadinessPolicy.ResolveStatus(embedding));
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        if (IsQuotaError(ex))
                        {
                            _logger.LogWarning(ex, "Portfolio backfill paused due to OpenAI quota/429 at portfolio {PortfolioId}", portfolio.Id);
                            await repo.UpdateEmbeddingAsync(
                                portfolio.Id,
                                portfolio.Embedding,
                                portfolio.EmbeddingVersion,
                                VietnamTime.Now(),
                                EmbeddingReadinessPolicy.Pending);
                            break;
                        }

                        failedCount++;
                        _logger.LogWarning(ex, "Portfolio backfill embedding failed for portfolio {PortfolioId}", portfolio.Id);
                        await repo.UpdateEmbeddingAsync(
                            portfolio.Id,
                            portfolio.Embedding,
                            portfolio.EmbeddingVersion,
                            VietnamTime.Now(),
                            EmbeddingReadinessPolicy.Failed);
                    }
                }

                if (candidates.Count > 0)
                {
                    _logger.LogInformation("Portfolio embedding backfill sweep completed. Candidates={Candidates}, Success={Success}, Failed={Failed}",
                        candidates.Count, successCount, failedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Portfolio embedding backfill sweep failed.");
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

    private static string BuildPortfolioDescription(Portfolio.Domain.Entities.Portfolio portfolio)
    {
        return string.Join(" ", portfolio.Blocks.OrderBy(x => x.DisplayOrder).Select(x => x.DataJson));
    }

    private static List<string> ExtractPortfolioSkills(Portfolio.Domain.Entities.Portfolio portfolio)
    {
        return portfolio.Blocks
            .Where(x => x.BlockTypeId == 2)
            .SelectMany(x => x.DataJson.Split([',', ';', '\n', '\r', '|', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
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

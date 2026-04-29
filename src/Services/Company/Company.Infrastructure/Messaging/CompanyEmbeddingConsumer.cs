using System.Text;
using System.Text.Json;
using Company.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RecruitmentPlatform.AI.Abstractions;
using RecruitmentPlatform.AI.Models;

namespace Company.Infrastructure.Messaging;

public sealed class CompanyEmbeddingConsumer : BackgroundService
{
    private const int RetryDelaySeconds = 15;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CompanyEmbeddingConsumer> _logger;
    private readonly EmbeddingBackfillOptions _options;
    private IConnection? _connection;
    private IChannel? _channel;

    public CompanyEmbeddingConsumer(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<CompanyEmbeddingConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
        _options = configuration.GetSection("EmbeddingBackfill").Get<EmbeddingBackfillOptions>() ?? new EmbeddingBackfillOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var uri = _configuration["RabbitMQ:Uri"];
        var factory = new ConnectionFactory();

        if (!string.IsNullOrEmpty(uri))
        {
            factory.Uri = new Uri(uri);
        }
        else
        {
            var host = GetRabbitSetting("HostName", "Host", "localhost");
            var userName = GetRabbitSetting("UserName", "Username", "guest");
            factory.HostName = host;
            factory.UserName = userName;
            factory.Password = GetRabbitSetting("Password", defaultValue: "guest");
            factory.VirtualHost = GetRabbitSetting("VirtualHost", defaultValue: "/");
            factory.Port = int.TryParse(GetRabbitSetting("Port", defaultValue: "5672"), out var port) ? port : 5672;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _connection = await factory.CreateConnectionAsync(stoppingToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

                const string exchange = "skillsnap.events";
                const string queue = "company.embedding.generate";
                await _channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
                await _channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
                await _channel.QueueBindAsync(queue, exchange, "company.post.changed", cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += HandleMessageAsync;
                await _channel.BasicConsumeAsync(queue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Company embedding consumer failed to connect RabbitMQ, retrying in {RetryDelaySeconds} seconds.", RetryDelaySeconds);
                await CleanupRabbitMqAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds), stoppingToken);
            }
        }
    }

    private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs args)
    {
        if (_channel == null) return;

        int postId = 0;
        try
        {
            var body = Encoding.UTF8.GetString(args.Body.ToArray());
            var payload = JsonSerializer.Deserialize<CompanyPostChangedEvent>(body, JsonOptions);
            if (payload is null || payload.PostId <= 0)
            {
                _logger.LogWarning("Ignoring invalid company.post.changed payload: {Payload}", body);
                await _channel.BasicAckAsync(args.DeliveryTag, false);
                return;
            }
            postId = payload.PostId;

            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ICompanyPostRepository>();
            var textNormalizer = scope.ServiceProvider.GetRequiredService<ITextNormalizer>();
            var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();

            var post = await repo.GetPostEntityByIdAsync(payload.PostId);
            if (post == null)
            {
                await _channel.BasicAckAsync(args.DeliveryTag, false);
                return;
            }

            var text = textNormalizer.BuildJobText(new EmbeddingTextInput
            {
                Title = post.Position,
                Description = post.JobDescription,
                Skills = ExtractSkills(post.RequirementsMandatory, post.RequirementsPreferred),
                Categories = ExtractCategories(post.EmploymentType, post.Address),
                CustomFields = new[] { post.Benefits ?? "N/A", post.Salary ?? "N/A" }
            });

            var embedding = await CreateEmbeddingWithRetryAsync(embeddingService, text, CancellationToken.None);
            var serialized = JsonSerializer.Serialize(embedding);
            var version = Math.Max(1, post.EmbeddingVersion + 1);
            await repo.UpdateEmbeddingAsync(payload.PostId, serialized, version, DateTime.UtcNow, EmbeddingReadinessPolicy.ResolveStatus(embedding));

            await _channel.BasicAckAsync(args.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process company.post.changed event");
            if (postId > 0)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<ICompanyPostRepository>();
                    var current = await repo.GetPostEntityByIdAsync(postId);
                    if (current != null)
                    {
                        await repo.UpdateEmbeddingAsync(
                            postId,
                            current.Embedding,
                            current.EmbeddingVersion,
                            DateTime.UtcNow,
                            IsQuotaError(ex) ? EmbeddingReadinessPolicy.Pending : EmbeddingReadinessPolicy.Failed);
                    }
                }
                catch (Exception updateEx)
                {
                    _logger.LogWarning(updateEx, "Failed to mark company post {PostId} embedding as failed after consumer error", postId);
                }
            }
            await _channel.BasicNackAsync(args.DeliveryTag, false, false);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await CleanupRabbitMqAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }

    private async Task CleanupRabbitMqAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            try
            {
                await _channel.CloseAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Ignoring channel close failure during cleanup.");
            }
        }

        if (_connection is not null)
        {
            try
            {
                await _connection.CloseAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Ignoring connection close failure during cleanup.");
            }
        }

        _channel = null;
        _connection = null;
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

    private string GetRabbitSetting(string primaryKey, string? fallbackKey = null, string defaultValue = "")
    {
        var primary = _configuration[$"RabbitMQ:{primaryKey}"];
        if (!string.IsNullOrWhiteSpace(primary))
        {
            return primary;
        }

        if (!string.IsNullOrWhiteSpace(fallbackKey))
        {
            var fallback = _configuration[$"RabbitMQ:{fallbackKey}"];
            if (!string.IsNullOrWhiteSpace(fallback))
            {
                return fallback;
            }
        }

        return defaultValue;
    }

    private static bool IsQuotaError(Exception ex)
    {
        return ex.Message.Contains("TooManyRequests", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("quota", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class CompanyPostChangedEvent
    {
        public int PostId { get; init; }
    }
}

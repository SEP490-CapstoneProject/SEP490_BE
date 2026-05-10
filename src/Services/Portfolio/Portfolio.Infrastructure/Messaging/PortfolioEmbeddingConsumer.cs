using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Portfolio.Application.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RecruitmentPlatform.AI.Abstractions;
using RecruitmentPlatform.AI.Models;
using RecruitmentPlatform.Contracts.Time;

namespace Portfolio.Infrastructure.Messaging;

public sealed class PortfolioEmbeddingConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PortfolioEmbeddingConsumer> _logger;
    private readonly EmbeddingBackfillOptions _options;
    private IConnection? _connection;
    private IChannel? _channel;

    public PortfolioEmbeddingConsumer(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<PortfolioEmbeddingConsumer> logger)
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
            var host = GetRabbitSetting("Host", "HostName", "localhost");
            var userName = GetRabbitSetting("Username", "UserName", "guest");
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
                const string queue = "portfolio.embedding.generate";
                await _channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
                await _channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
                await _channel.QueueBindAsync(queue, exchange, "portfolio.changed", cancellationToken: stoppingToken);

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
                _logger.LogWarning(ex, "Portfolio embedding consumer failed to connect RabbitMQ, retrying in 15 seconds.");

                try
                {
                    if (_channel is not null)
                    {
                        await _channel.CloseAsync(stoppingToken);
                    }
                }
                catch
                {
                }

                try
                {
                    if (_connection is not null)
                    {
                        await _connection.CloseAsync(stoppingToken);
                    }
                }
                catch
                {
                }

                _channel = null;
                _connection = null;
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }
    }

    private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs args)
    {
        if (_channel == null) return;

        int portfolioId = 0;
        try
        {
            var body = Encoding.UTF8.GetString(args.Body.ToArray());
            var payload = JsonSerializer.Deserialize<PortfolioChangedEvent>(body, JsonOptions);
            if (payload is null || payload.PortfolioId <= 0)
            {
                _logger.LogWarning("Ignoring invalid portfolio.changed payload: {Payload}", body);
                await _channel.BasicAckAsync(args.DeliveryTag, false);
                return;
            }
            portfolioId = payload.PortfolioId;

            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
            var textNormalizer = scope.ServiceProvider.GetRequiredService<ITextNormalizer>();
            var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();

            var portfolio = await repo.GetByIdAsync(portfolioId);
            if (portfolio == null)
            {
                await _channel.BasicAckAsync(args.DeliveryTag, false);
                return;
            }

            var description = BuildPortfolioDescription(portfolio);
            var skills = ExtractPortfolioSkills(portfolio);
            var text = textNormalizer.BuildPortfolioText(new EmbeddingTextInput
            {
                Title = portfolio.Name,
                Description = description,
                Skills = skills,
                Categories = Array.Empty<string>(),
                Projects = portfolio.Blocks.Where(x => x.BlockTypeId == 6).Select(x => x.DataJson).ToList(),
                CustomFields = portfolio.Blocks.Select(x => x.DataJson).Take(5).ToList()
            });

            var embedding = await CreateEmbeddingWithRetryAsync(embeddingService, text, CancellationToken.None);
            var serialized = JsonSerializer.Serialize(embedding);
            var newVersion = Math.Max(1, portfolio.EmbeddingVersion + 1);
            await repo.UpdateEmbeddingAsync(portfolioId, serialized, newVersion, VietnamTime.Now(), EmbeddingReadinessPolicy.ResolveStatus(embedding));

            await _channel.BasicAckAsync(args.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process portfolio.changed event");
            if (portfolioId > 0)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
                    var current = await repo.GetByIdAsync(portfolioId);
                    if (current != null)
                    {
                        await repo.UpdateEmbeddingAsync(
                            portfolioId,
                            current.Embedding,
                            current.EmbeddingVersion,
                            VietnamTime.Now(),
                            IsQuotaError(ex) ? EmbeddingReadinessPolicy.Pending : EmbeddingReadinessPolicy.Failed);
                    }
                }
                catch (Exception updateEx)
                {
                    _logger.LogWarning(updateEx, "Failed to mark portfolio {PortfolioId} embedding as failed after consumer error", portfolioId);
                }
            }
            await _channel.BasicNackAsync(args.DeliveryTag, false, false);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null) await _channel.CloseAsync(cancellationToken);
        if (_connection is not null) await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
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

    private sealed class PortfolioChangedEvent
    {
        public int PortfolioId { get; init; }
    }
}

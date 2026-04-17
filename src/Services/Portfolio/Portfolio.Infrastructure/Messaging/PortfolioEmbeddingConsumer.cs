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
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PortfolioEmbeddingConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public PortfolioEmbeddingConsumer(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<PortfolioEmbeddingConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var host = _configuration["RabbitMQ:HostName"] ?? _configuration["RabbitMQ:Host"] ?? "localhost";
        var userName = _configuration["RabbitMQ:UserName"] ?? _configuration["RabbitMQ:Username"] ?? "guest";
        var factory = new ConnectionFactory
        {
            HostName = host,
            UserName = userName,
            Password = _configuration["RabbitMQ:Password"] ?? "guest",
            VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/",
            Port = int.TryParse(_configuration["RabbitMQ:Port"], out var port) ? port : 5672
        };

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

    private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs args)
    {
        if (_channel == null) return;

        try
        {
            var body = Encoding.UTF8.GetString(args.Body.ToArray());
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("PortfolioId", out var idElement) || !idElement.TryGetInt32(out var portfolioId))
            {
                await _channel.BasicAckAsync(args.DeliveryTag, false);
                return;
            }

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

            var embedding = await embeddingService.CreateEmbeddingAsync(text);
            var serialized = JsonSerializer.Serialize(embedding);
            var newVersion = Math.Max(1, portfolio.EmbeddingVersion + 1);
            await repo.UpdateEmbeddingAsync(portfolioId, serialized, newVersion, VietnamTime.Now(), embedding.Length > 0 ? "Ready" : "Failed");

            await _channel.BasicAckAsync(args.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process portfolio.changed event");
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
}

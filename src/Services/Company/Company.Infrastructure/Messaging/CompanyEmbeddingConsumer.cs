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
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CompanyEmbeddingConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public CompanyEmbeddingConsumer(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<CompanyEmbeddingConsumer> logger)
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
        const string queue = "company.embedding.generate";
        await _channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(queue, exchange, "company.post.changed", cancellationToken: stoppingToken);

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
            if (!doc.RootElement.TryGetProperty("PostId", out var idElement) || !idElement.TryGetInt32(out var postId))
            {
                await _channel.BasicAckAsync(args.DeliveryTag, false);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ICompanyPostRepository>();
            var textNormalizer = scope.ServiceProvider.GetRequiredService<ITextNormalizer>();
            var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();

            var post = await repo.GetPostEntityByIdAsync(postId);
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

            var embedding = await embeddingService.CreateEmbeddingAsync(text);
            var serialized = JsonSerializer.Serialize(embedding);
            var version = Math.Max(1, post.EmbeddingVersion + 1);
            await repo.UpdateEmbeddingAsync(postId, serialized, version, DateTime.UtcNow, embedding.Length > 0 ? "Ready" : "Failed");

            await _channel.BasicAckAsync(args.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process company.post.changed event");
            await _channel.BasicNackAsync(args.DeliveryTag, false, false);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null) await _channel.CloseAsync(cancellationToken);
        if (_connection is not null) await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
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
}

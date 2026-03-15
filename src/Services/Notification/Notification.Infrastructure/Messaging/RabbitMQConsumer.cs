using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Notification.Infrastructure.Messaging;

public class RabbitMQConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<RabbitMQConsumer> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    private const string Exchange = "skillsnap.events";
    private const string Queue = "notification.events";
    private const string DlxExchange = "skillsnap.events.dlx";
    private const string DlqQueue = "notification.events.dlq";

    private static readonly string[] BindingKeys = { "post.*", "connection.*", "portfolio.*", "job.*", "system.*" };

    public RabbitMQConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<RabbitMQConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(5000, stoppingToken);

        var factory = new ConnectionFactory
        {
            HostName = _config["RabbitMQ:HostName"] ?? "localhost",
            UserName = _config["RabbitMQ:UserName"] ?? "guest",
            Password = _config["RabbitMQ:Password"] ?? "guest",
            Port = int.TryParse(_config["RabbitMQ:Port"], out var port) ? port : 5672
        };

        try
        {
            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await _channel.ExchangeDeclareAsync(DlxExchange, ExchangeType.Direct, durable: true, cancellationToken: stoppingToken);
            await _channel.QueueDeclareAsync(DlqQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
            await _channel.QueueBindAsync(DlqQueue, DlxExchange, DlqQueue, cancellationToken: stoppingToken);

            await _channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);

            var queueArgs = new Dictionary<string, object?>
            {
                { "x-dead-letter-exchange", DlxExchange },
                { "x-dead-letter-routing-key", DlqQueue }
            };
            await _channel.QueueDeclareAsync(Queue, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs, cancellationToken: stoppingToken);

            foreach (var key in BindingKeys)
                await _channel.QueueBindAsync(Queue, Exchange, key, cancellationToken: stoppingToken);

            await _channel.BasicQosAsync(0, 10, false, stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += OnMessageReceivedAsync;

            await _channel.BasicConsumeAsync(Queue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

            _logger.LogInformation("RabbitMQ consumer started on queue: {Queue}", Queue);
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ consumer fatal error");
        }
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        if (_channel is null) return;

        try
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var evt = JsonSerializer.Deserialize<NotificationEvent>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (evt is null || string.IsNullOrEmpty(evt.UserId))
            {
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var pushService = scope.ServiceProvider.GetRequiredService<INotificationPushService>();

            var entity = new NotificationEntity
            {
                UserId = evt.UserId,
                Title = evt.Title,
                Content = evt.Content,
                Type = evt.Type,
                ObjectId = evt.ObjectId,
                ActorId = evt.ActorId,
                ActorType = evt.ActorType,
                CreatedAt = evt.CreatedAt == default ? DateTime.UtcNow : evt.CreatedAt
            };

            var dto = await notificationService.CreateNotificationAsync(entity);

            var cache = scope.ServiceProvider.GetService<IDistributedCache>();
            if (cache != null)
                await cache.RemoveAsync($"unread:{evt.UserId}");

            await pushService.PushAsync(evt.UserId, dto);

            await _channel.BasicAckAsync(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notification event, sending to DLQ");
            await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null) await _channel.CloseAsync(cancellationToken);
        if (_connection is not null) await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}

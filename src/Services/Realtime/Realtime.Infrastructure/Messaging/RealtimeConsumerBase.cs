using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Realtime.Application.Interfaces;
using RecruitmentPlatform.Contracts.Realtime;

namespace Realtime.Infrastructure.Messaging;

public abstract class RealtimeConsumerBase<TEvent> : BackgroundService where TEvent : RealtimeEventBase
{
    private readonly IConfiguration _configuration;
    private readonly IRealtimePushService _pushService;
    private readonly IRealtimeIdempotencyStore _idempotencyStore;
    private readonly ILogger _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    protected abstract string QueueName { get; }
    protected abstract string RoutingKey { get; }
    protected abstract Task PushAsync(TEvent evt, CancellationToken cancellationToken);
    protected virtual TimeSpan IdempotencyTtl => TimeSpan.FromMinutes(10);
    protected virtual ushort PrefetchCount => 10;

    protected RealtimeConsumerBase(
        IConfiguration configuration,
        IRealtimePushService pushService,
        IRealtimeIdempotencyStore idempotencyStore,
        ILogger logger)
    {
        _configuration = configuration;
        _pushService = pushService;
        _idempotencyStore = idempotencyStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(2000, stoppingToken);

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

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _connection = await factory.CreateConnectionAsync(stoppingToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

                const string exchange = "skillsnap.events";
                const string dlxExchange = "skillsnap.events.dlx";

                var dlqQueue = $"{QueueName}.dlq";
                await _channel.ExchangeDeclareAsync(dlxExchange, ExchangeType.Direct, durable: true, cancellationToken: stoppingToken);
                await _channel.QueueDeclareAsync(dlqQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
                await _channel.QueueBindAsync(dlqQueue, dlxExchange, dlqQueue, cancellationToken: stoppingToken);

                await _channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
                var queueArgs = new Dictionary<string, object?>
                {
                    { "x-dead-letter-exchange", dlxExchange },
                    { "x-dead-letter-routing-key", dlqQueue }
                };
                await _channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs, cancellationToken: stoppingToken);
                await _channel.QueueBindAsync(QueueName, exchange, RoutingKey, cancellationToken: stoppingToken);
                await _channel.BasicQosAsync(0, PrefetchCount, false, stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += OnMessageReceivedAsync;
                await _channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, stoppingToken);

                _logger.LogInformation("Realtime consumer started. Queue={Queue}, RoutingKey={RoutingKey}", QueueName, RoutingKey);
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Realtime consumer fatal error. Queue={Queue}, RoutingKey={RoutingKey}. Retrying in 10 seconds", QueueName, RoutingKey);
                try
                {
                    if (_channel is not null)
                    {
                        await _channel.CloseAsync(stoppingToken);
                        _channel = null;
                    }
                    if (_connection is not null)
                    {
                        await _connection.CloseAsync(stoppingToken);
                        _connection = null;
                    }
                }
                catch (Exception closeEx)
                {
                    _logger.LogWarning(closeEx, "Error closing RabbitMQ resources. Queue={Queue}, RoutingKey={RoutingKey}", QueueName, RoutingKey);
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var evt = JsonSerializer.Deserialize<TEvent>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (evt is null || string.IsNullOrWhiteSpace(evt.EventId))
            {
                _logger.LogWarning("Invalid message payload for queue {Queue}. RoutingKey={RoutingKey}", QueueName, ea.RoutingKey);
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                return;
            }

            var canProcess = await _idempotencyStore.TryBeginProcessingAsync(evt.EventId, IdempotencyTtl);
            if (!canProcess)
            {
                _logger.LogInformation(
                    "Skip duplicate realtime event. EventId={EventId}, RoutingKey={RoutingKey}, Queue={Queue}",
                    evt.EventId, ea.RoutingKey, QueueName);
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
                return;
            }

            await PushAsync(evt, CancellationToken.None);
            _logger.LogInformation(
                "Realtime push completed. EventId={EventId}, EventType={EventType}, RoutingKey={RoutingKey}, Queue={Queue}",
                evt.EventId, evt.EventType, ea.RoutingKey, QueueName);

            await _channel.BasicAckAsync(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Realtime consumer failed. RoutingKey={RoutingKey}, Queue={Queue}", ea.RoutingKey, QueueName);
            await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync(cancellationToken);
        }

        await base.StopAsync(cancellationToken);
    }

    protected IRealtimePushService PushService => _pushService;
}

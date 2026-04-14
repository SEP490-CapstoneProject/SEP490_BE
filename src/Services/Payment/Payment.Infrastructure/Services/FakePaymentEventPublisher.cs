using Payment.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Payment.Infrastructure.Services;

public class FakePaymentEventPublisher : IPaymentEventPublisher
{
    private readonly ILogger<FakePaymentEventPublisher> _logger;

    public FakePaymentEventPublisher(ILogger<FakePaymentEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishPaymentSucceededAsync(Guid paymentId, int userId, int planId, int subscriptionId, decimal amount, string provider)
    {
        _logger.LogInformation($"[FAKE RABBITMQ] Payment {paymentId} succeeded for User {userId}. Event ignored for this demo deployment.");
        return Task.CompletedTask;
    }
}

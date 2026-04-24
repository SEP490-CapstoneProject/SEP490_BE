using Moq;
using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;
using Payment.Application.Services;
using Payment.Application.DTOs;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.Interfaces;
using Xunit;

namespace Payment.Tests;

public class PaymentVerificationServiceTests
{
    private readonly Mock<IPaymentProvider> _providerMock;
    private readonly Mock<ILogger<PaymentVerificationService>> _loggerMock;
    private readonly PaymentVerificationService _service;

    public PaymentVerificationServiceTests()
    {
        _providerMock = new Mock<IPaymentProvider>();
        _loggerMock = new Mock<ILogger<PaymentVerificationService>>();
        _service = new PaymentVerificationService(_providerMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task VerifyWebhookDataAsync_AmountsMatch_ReturnsValid()
    {
        // Arrange
        var payment = new PaymentEntity
        {
            Id = Guid.NewGuid(),
            Amount = 100000,
            OrderCode = "ORD123"
        };
        var webhookData = new Payment.Application.Services.WebhookData
        {
            OrderCode = "ORD123",
            Amount = 100000,
            Status = "PAID"
        };
        var paymentInfo = new PaymentInfo
        {
            OrderCode = "ORD123",
            Amount = 100000,
            Status = "PAID"
        };

        _providerMock.Setup(p => p.VerifyPaymentAsync(It.IsAny<string>()))
            .ReturnsAsync(paymentInfo);

        // Act
        var result = await _service.VerifyWebhookDataAsync(payment, webhookData);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public async Task VerifyWebhookDataAsync_DbDecimalAndProviderVndInteger_ReturnsValid()
    {
        // Arrange
        var payment = new PaymentEntity
        {
            Id = Guid.NewGuid(),
            Amount = 9.99m,
            OrderCode = "ORD123"
        };
        var webhookData = new Payment.Application.Services.WebhookData
        {
            OrderCode = "ORD123",
            Amount = 9990,
            Status = "PAID"
        };
        var paymentInfo = new PaymentInfo
        {
            OrderCode = "ORD123",
            Amount = 9990,
            Status = "PAID"
        };

        _providerMock.Setup(p => p.VerifyPaymentAsync(It.IsAny<string>()))
            .ReturnsAsync(paymentInfo);

        // Act
        var result = await _service.VerifyWebhookDataAsync(payment, webhookData);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public async Task VerifyWebhookDataAsync_AmountMismatch_ReturnsInvalid()
    {
        // Arrange
        var payment = new PaymentEntity
        {
            Id = Guid.NewGuid(),
            Amount = 100000,
            OrderCode = "ORD123"
        };
        var webhookData = new Payment.Application.Services.WebhookData
        {
            OrderCode = "ORD123",
            Amount = 100000,
            Status = "PAID"
        };
        var paymentInfo = new PaymentInfo
        {
            OrderCode = "ORD123",
            Amount = 50000, // Different amount (fraud attempt)
            Status = "PAID"
        };

        _providerMock.Setup(p => p.VerifyPaymentAsync(It.IsAny<string>()))
            .ReturnsAsync(paymentInfo);

        // Act
        var result = await _service.VerifyWebhookDataAsync(payment, webhookData);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("mismatch", result.FailureReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VerifyWebhookDataAsync_StatusMismatch_ReturnsInvalid()
    {
        // Arrange
        var payment = new PaymentEntity
        {
            Id = Guid.NewGuid(),
            Amount = 100000,
            OrderCode = "ORD123"
        };
        var webhookData = new Payment.Application.Services.WebhookData
        {
            OrderCode = "ORD123",
            Amount = 100000,
            Status = "PAID" // Webhook says PAID
        };
        var paymentInfo = new PaymentInfo
        {
            OrderCode = "ORD123",
            Amount = 100000,
            Status = "PENDING" // API says PENDING
        };

        _providerMock.Setup(p => p.VerifyPaymentAsync(It.IsAny<string>()))
            .ReturnsAsync(paymentInfo);

        // Act
        var result = await _service.VerifyWebhookDataAsync(payment, webhookData);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("status", result.FailureReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VerifyWebhookDataAsync_ApiReturnsNull_ReturnsInvalid()
    {
        // Arrange
        var payment = new PaymentEntity
        {
            Id = Guid.NewGuid(),
            Amount = 100000,
            OrderCode = "ORD123"
        };
        var webhookData = new Payment.Application.Services.WebhookData
        {
            OrderCode = "ORD123",
            Amount = 100000,
            Status = "PAID"
        };

        _providerMock.Setup(p => p.VerifyPaymentAsync(It.IsAny<string>()))
            .ReturnsAsync((PaymentInfo?)null);

        // Act
        var result = await _service.VerifyWebhookDataAsync(payment, webhookData);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("not found", result.FailureReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VerifyWebhookDataAsync_ApiCallFails_ReturnsInvalid()
    {
        // Arrange
        var payment = new PaymentEntity
        {
            Id = Guid.NewGuid(),
            Amount = 100000,
            OrderCode = "ORD123"
        };
        var webhookData = new Payment.Application.Services.WebhookData
        {
            OrderCode = "ORD123",
            Amount = 100000,
            Status = "PAID"
        };

        _providerMock.Setup(p => p.VerifyPaymentAsync(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("API unavailable"));

        // Act
        var result = await _service.VerifyWebhookDataAsync(payment, webhookData);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("API", result.FailureReason);
    }
}

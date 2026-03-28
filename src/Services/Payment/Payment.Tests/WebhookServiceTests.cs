using Moq;
using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;
using Payment.Application.Services;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.Interfaces;
using Xunit;

namespace Payment.Tests;

public class WebhookServiceTests
{
    private readonly Mock<IPaymentRepository> _paymentRepoMock;
    private readonly Mock<IProcessedEventRepository> _processedEventRepoMock;
    private readonly Mock<IPaymentHistoryRepository> _historyRepoMock;
    private readonly Mock<IOutboxEventRepository> _outboxRepoMock;
    private readonly Mock<IPaymentProvider> _providerMock;
    private readonly Mock<IPaymentVerificationService> _verificationMock;
    private readonly Mock<ILogger<WebhookService>> _loggerMock;
    private readonly WebhookService _webhookService;

    public WebhookServiceTests()
    {
        _paymentRepoMock = new Mock<IPaymentRepository>();
        _processedEventRepoMock = new Mock<IProcessedEventRepository>();
        _historyRepoMock = new Mock<IPaymentHistoryRepository>();
        _outboxRepoMock = new Mock<IOutboxEventRepository>();
        _providerMock = new Mock<IPaymentProvider>();
        _verificationMock = new Mock<IPaymentVerificationService>();
        _loggerMock = new Mock<ILogger<WebhookService>>();

        _webhookService = new WebhookService(
            _paymentRepoMock.Object,
            _processedEventRepoMock.Object,
            _historyRepoMock.Object,
            _outboxRepoMock.Object,
            _providerMock.Object,
            _verificationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessWebhookAsync_InvalidSignature_ReturnsFailure()
    {
        // Arrange
        _providerMock.Setup(p => p.ValidateSignature(It.IsAny<string>(), It.IsAny<string>(), null))
            .Returns(false);

        // Act
        var result = await _webhookService.ProcessWebhookAsync("{}", "invalid-sig", "corr-123");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.False(result.IsValid);
        Assert.Equal("Invalid signature", result.ErrorMessage);
    }

    [Fact]
    public async Task ProcessWebhookAsync_AlreadyProcessedByHash_ReturnsSuccess()
    {
        // Arrange
        _providerMock.Setup(p => p.ValidateSignature(It.IsAny<string>(), It.IsAny<string>(), null))
            .Returns(true);
        _processedEventRepoMock.Setup(p => p.IsProcessedByHashAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _webhookService.ProcessWebhookAsync("{}", "valid-sig", "corr-123");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ProcessWebhookAsync_PaymentNotFound_ReturnsFailure()
    {
        // Arrange
        _providerMock.Setup(p => p.ValidateSignature(It.IsAny<string>(), It.IsAny<string>(), null))
            .Returns(true);
        _processedEventRepoMock.Setup(p => p.IsProcessedByHashAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _providerMock.Setup(p => p.ParseWebhookData(It.IsAny<string>()))
            .Returns(new Application.DTOs.WebhookResult { OrderCode = "ORD123", Amount = 100000 });
        _paymentRepoMock.Setup(p => p.GetByOrderCodeAsync(It.IsAny<string>()))
            .ReturnsAsync((PaymentEntity?)null);

        // Act
        var result = await _webhookService.ProcessWebhookAsync("{}", "valid-sig", "corr-123");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Payment not found", result.ErrorMessage);
    }

    [Fact]
    public async Task ProcessWebhookAsync_PaymentAlreadyProcessed_ReturnsSuccess()
    {
        // Arrange
        var payment = new PaymentEntity
        {
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Succeeded, // Already processed
            OrderCode = "ORD123"
        };

        _providerMock.Setup(p => p.ValidateSignature(It.IsAny<string>(), It.IsAny<string>(), null))
            .Returns(true);
        _processedEventRepoMock.Setup(p => p.IsProcessedByHashAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _providerMock.Setup(p => p.ParseWebhookData(It.IsAny<string>()))
            .Returns(new Application.DTOs.WebhookResult { OrderCode = "ORD123", Amount = 100000 });
        _paymentRepoMock.Setup(p => p.GetByOrderCodeAsync("ORD123"))
            .ReturnsAsync(payment);

        // Act
        var result = await _webhookService.ProcessWebhookAsync("{}", "valid-sig", "corr-123");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.IsValid);
    }
}

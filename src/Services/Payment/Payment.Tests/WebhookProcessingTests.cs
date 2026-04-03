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

public class WebhookProcessingTests
{
    private readonly Mock<IPaymentRepository> _paymentRepoMock;
    private readonly Mock<IProcessedEventRepository> _processedEventRepoMock;
    private readonly Mock<IPaymentHistoryRepository> _historyRepoMock;
    private readonly Mock<IOutboxEventRepository> _outboxRepoMock;
    private readonly Mock<IPaymentProvider> _providerMock;
    private readonly Mock<IPaymentVerificationService> _verificationMock;
    private readonly Mock<ILogger<WebhookService>> _loggerMock;
    private readonly WebhookService _webhookService;

    public WebhookProcessingTests()
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
    public async Task ProcessWebhook_InvalidSignature_ShouldRejectImmediately()
    {
        // Arrange
        _providerMock.Setup(p => p.ValidateSignature(It.IsAny<string>(), It.IsAny<string>(), null))
            .Returns(false);

        // Act
        var result = await _webhookService.ProcessWebhookAsync("{}", "bad-signature", "corr-123");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.False(result.IsValid);
        
        // Should not call any repository
        _paymentRepoMock.Verify(p => p.GetByOrderCodeAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessWebhook_DuplicateEventHash_ShouldReturnSuccess()
    {
        // Arrange
        _providerMock.Setup(p => p.ValidateSignature(It.IsAny<string>(), It.IsAny<string>(), null))
            .Returns(true);
        _processedEventRepoMock.Setup(p => p.IsProcessedByHashAsync(It.IsAny<string>()))
            .ReturnsAsync(true); // Already processed

        // Act
        var result = await _webhookService.ProcessWebhookAsync("{}", "valid-sig", "corr-123");

        // Assert - idempotent, return success without reprocessing
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ProcessWebhook_PaymentAlreadySucceeded_ShouldReturnSuccess()
    {
        // Arrange
        var payment = new PaymentEntity
        {
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Succeeded,
            OrderCode = "ORD123"
        };

        _providerMock.Setup(p => p.ValidateSignature(It.IsAny<string>(), It.IsAny<string>(), null))
            .Returns(true);
        _processedEventRepoMock.Setup(p => p.IsProcessedByHashAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _providerMock.Setup(p => p.ParseWebhookData(It.IsAny<string>()))
            .Returns(new WebhookResult { OrderCode = "ORD123", Amount = 100000 });
        _paymentRepoMock.Setup(p => p.GetByOrderCodeAsync("ORD123"))
            .ReturnsAsync(payment);

        // Act
        var result = await _webhookService.ProcessWebhookAsync("{}", "valid-sig", "corr-123");

        // Assert - already succeeded, return success
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void SHA256Hash_SameInput_SameOutput()
    {
        // Verify hash consistency for idempotency
        var input = "{\"orderCode\":123}";
        
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash1 = Convert.ToHexString(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input)));
        var hash2 = Convert.ToHexString(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input)));

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void SHA256Hash_DifferentInput_DifferentOutput()
    {
        var input1 = "{\"orderCode\":123}";
        var input2 = "{\"orderCode\":456}";
        
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash1 = Convert.ToHexString(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input1)));
        var hash2 = Convert.ToHexString(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input2)));

        Assert.NotEqual(hash1, hash2);
    }
}

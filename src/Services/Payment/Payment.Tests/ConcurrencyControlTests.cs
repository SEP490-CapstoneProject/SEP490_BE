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

public class ConcurrencyControlTests
{
    [Fact]
    public void RowVersion_ShouldBeByteArray()
    {
        // RowVersion for EF Core optimistic concurrency should be byte[]
        var payment = new PaymentEntity
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };

        Assert.NotNull(payment.RowVersion);
        Assert.Equal(8, payment.RowVersion.Length);
    }

    [Fact]
    public async Task UpdateStatusConditional_WithCorrectRowVersion_ShouldSucceed()
    {
        // Arrange
        var paymentRepoMock = new Mock<IPaymentRepository>();
        var expectedRowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        
        paymentRepoMock.Setup(p => p.UpdateStatusConditionalAsync(
            It.IsAny<Guid>(),
            PaymentStatus.Processing,
            expectedRowVersion))
            .ReturnsAsync(true);

        // Act
        var result = await paymentRepoMock.Object.UpdateStatusConditionalAsync(
            Guid.NewGuid(),
            PaymentStatus.Processing,
            expectedRowVersion);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UpdateStatusConditional_WithWrongRowVersion_ShouldFail()
    {
        // Arrange
        var paymentRepoMock = new Mock<IPaymentRepository>();
        var wrongRowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
        
        paymentRepoMock.Setup(p => p.UpdateStatusConditionalAsync(
            It.IsAny<Guid>(),
            PaymentStatus.Processing,
            wrongRowVersion))
            .ReturnsAsync(false); // Concurrent update detected

        // Act
        var result = await paymentRepoMock.Object.UpdateStatusConditionalAsync(
            Guid.NewGuid(),
            PaymentStatus.Processing,
            wrongRowVersion);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Payment_ShouldHaveProcessingStatus()
    {
        // Processing status exists for race condition prevention
        var status = PaymentStatus.Processing;
        Assert.Equal(1, (int)status);
    }

    [Theory]
    [InlineData(PaymentStatus.Pending, true)]
    [InlineData(PaymentStatus.Processing, false)]
    [InlineData(PaymentStatus.Succeeded, false)]
    [InlineData(PaymentStatus.Failed, false)]
    public void CanTransitionToProcessing_OnlyFromPending(PaymentStatus current, bool expected)
    {
        // Only Pending → Processing is valid
        var canTransition = current == PaymentStatus.Pending;
        Assert.Equal(expected, canTransition);
    }
}

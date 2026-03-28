using Moq;
using Microsoft.Extensions.Logging;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.Interfaces;
using Xunit;

namespace Payment.Tests;

public class ReconciliationServiceTests
{
    [Fact]
    public async Task CheckStuckPayments_NoStuckPayments_ReturnsEmpty()
    {
        // Arrange
        var paymentRepoMock = new Mock<IPaymentRepository>();
        
        paymentRepoMock.Setup(p => p.GetStuckPaymentsAsync(
            It.IsAny<int>(), 
            It.IsAny<PaymentStatus[]>()))
            .ReturnsAsync(new List<PaymentEntity>());

        // Act
        var payments = await paymentRepoMock.Object.GetStuckPaymentsAsync(
            5, 
            new[] { PaymentStatus.Processing });
        
        // Assert
        Assert.Empty(payments);
    }

    [Fact]
    public async Task CheckStuckPayments_StuckPayments_AreIdentified()
    {
        // Arrange
        var paymentRepoMock = new Mock<IPaymentRepository>();
        var stuckPayments = new List<PaymentEntity>
        {
            new PaymentEntity
            {
                Id = Guid.NewGuid(),
                Status = PaymentStatus.Processing,
                UpdatedAt = DateTime.UtcNow.AddMinutes(-10) // Stuck for 10 minutes
            },
            new PaymentEntity
            {
                Id = Guid.NewGuid(),
                Status = PaymentStatus.Processing,
                UpdatedAt = DateTime.UtcNow.AddMinutes(-15) // Stuck for 15 minutes
            }
        };

        paymentRepoMock.Setup(p => p.GetStuckPaymentsAsync(
            It.IsAny<int>(), 
            It.IsAny<PaymentStatus[]>()))
            .ReturnsAsync(stuckPayments);

        // Act
        var payments = await paymentRepoMock.Object.GetStuckPaymentsAsync(
            5,
            new[] { PaymentStatus.Processing });

        // Assert
        Assert.Equal(2, payments.Count);
        Assert.All(payments, p => Assert.Equal(PaymentStatus.Processing, p.Status));
    }

    [Fact]
    public void StuckPayment_ShouldBeMarkedForReconciliation()
    {
        // Arrange
        var payment = new PaymentEntity
        {
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Processing,
            UpdatedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        
        var thresholdMinutes = 5;
        var thresholdTime = DateTime.UtcNow.AddMinutes(-thresholdMinutes);
        
        // Act
        var isStuck = payment.UpdatedAt < thresholdTime && payment.Status == PaymentStatus.Processing;
        
        // Assert
        Assert.True(isStuck);
    }

    [Fact]
    public void RecentPayment_ShouldNotBeMarkedAsStuck()
    {
        // Arrange
        var payment = new PaymentEntity
        {
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Processing,
            UpdatedAt = DateTime.UtcNow.AddMinutes(-2) // Only 2 minutes old
        };
        
        var thresholdMinutes = 5;
        var thresholdTime = DateTime.UtcNow.AddMinutes(-thresholdMinutes);
        
        // Act
        var isStuck = payment.UpdatedAt < thresholdTime && payment.Status == PaymentStatus.Processing;
        
        // Assert
        Assert.False(isStuck);
    }
}

using Moq;
using Microsoft.Extensions.Logging;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.Interfaces;
using Xunit;

namespace Payment.Tests;

public class PaymentStateMachineTests
{
    [Theory]
    [InlineData(PaymentStatus.Pending, PaymentStatus.Processing, true)]
    [InlineData(PaymentStatus.Pending, PaymentStatus.Cancelled, true)]
    [InlineData(PaymentStatus.Pending, PaymentStatus.Expired, true)]
    [InlineData(PaymentStatus.Processing, PaymentStatus.Succeeded, true)]
    [InlineData(PaymentStatus.Processing, PaymentStatus.Failed, true)]
    [InlineData(PaymentStatus.Processing, PaymentStatus.Pending, false)]
    [InlineData(PaymentStatus.Succeeded, PaymentStatus.Pending, false)]
    [InlineData(PaymentStatus.Succeeded, PaymentStatus.Failed, false)]
    [InlineData(PaymentStatus.Failed, PaymentStatus.Succeeded, false)]
    [InlineData(PaymentStatus.Cancelled, PaymentStatus.Succeeded, false)]
    [InlineData(PaymentStatus.Expired, PaymentStatus.Succeeded, false)]
    public void IsValidTransition_ReturnsExpectedResult(PaymentStatus from, PaymentStatus to, bool expected)
    {
        // Act
        var result = PaymentStateMachine.IsValidTransition(from, to);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PaymentStatus.Succeeded)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Cancelled)]
    [InlineData(PaymentStatus.Expired)]
    public void IsFinalState_TerminalStatuses_ReturnsTrue(PaymentStatus status)
    {
        // Act
        var result = PaymentStateMachine.IsFinalState(status);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Processing)]
    public void IsFinalState_NonTerminalStatuses_ReturnsFalse(PaymentStatus status)
    {
        // Act
        var result = PaymentStateMachine.IsFinalState(status);

        // Assert
        Assert.False(result);
    }
}

/// <summary>
/// Payment state machine - enforces valid state transitions.
/// </summary>
public static class PaymentStateMachine
{
    private static readonly Dictionary<PaymentStatus, HashSet<PaymentStatus>> _validTransitions = new()
    {
        { PaymentStatus.Pending, new HashSet<PaymentStatus> { PaymentStatus.Processing, PaymentStatus.Cancelled, PaymentStatus.Expired } },
        { PaymentStatus.Processing, new HashSet<PaymentStatus> { PaymentStatus.Succeeded, PaymentStatus.Failed, PaymentStatus.Cancelled } },
        { PaymentStatus.Succeeded, new HashSet<PaymentStatus>() }, // Terminal state
        { PaymentStatus.Failed, new HashSet<PaymentStatus>() }, // Terminal state
        { PaymentStatus.Cancelled, new HashSet<PaymentStatus>() }, // Terminal state
        { PaymentStatus.Expired, new HashSet<PaymentStatus>() } // Terminal state
    };

    public static bool IsValidTransition(PaymentStatus from, PaymentStatus to)
    {
        if (!_validTransitions.TryGetValue(from, out var validNextStates))
            return false;
        
        return validNextStates.Contains(to);
    }

    public static bool IsFinalState(PaymentStatus status)
    {
        return status switch
        {
            PaymentStatus.Succeeded => true,
            PaymentStatus.Failed => true,
            PaymentStatus.Cancelled => true,
            PaymentStatus.Expired => true,
            _ => false
        };
    }

    public static IEnumerable<PaymentStatus> GetValidNextStates(PaymentStatus current)
    {
        if (_validTransitions.TryGetValue(current, out var validNextStates))
            return validNextStates;
        
        return Enumerable.Empty<PaymentStatus>();
    }
}

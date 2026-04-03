using Payment.Domain.Enums;

namespace Payment.Application.DTOs;

/// <summary>
/// Request to create payment. Provider is always PayOS (no selection needed).
/// </summary>
public class CreatePaymentRequest
{
    /// <summary>
    /// Plan ID to subscribe to
    /// </summary>
    public int PlanId { get; set; }
    
    /// <summary>
    /// ID of pending subscription (must be created first)
    /// </summary>
    public int SubscriptionId { get; set; }
    
    // Provider field removed - always PayOS
}

public class CreatePaymentResponse
{
    public Guid PaymentId { get; set; }
    public string PaymentUrl { get; set; } = null!;
    public string OrderCode { get; set; } = null!;
}

public class PaymentDto
{
    public Guid Id { get; set; }
    public int UserId { get; set; }
    public int PlanId { get; set; }
    public int SubscriptionId { get; set; }
    public string? PlanName { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string Provider { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? TransactionId { get; set; }
    public string OrderCode { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class PaymentHistoryDto
{
    public Guid Id { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = null!;
    public string Action { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class WebhookResult
{
    public bool IsValid { get; set; }
    public bool IsSuccess { get; set; }
    public string? OrderCode { get; set; }
    public string? TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RawData { get; set; }
}

public class AdminPaymentFilterRequest
{
    public int? UserId { get; set; }
    public PaymentStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

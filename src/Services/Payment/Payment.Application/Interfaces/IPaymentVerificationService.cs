namespace Payment.Application.Interfaces;

public interface IPaymentVerificationService
{
    Task<VerificationResult> VerifyWebhookDataAsync(
        Domain.Entities.PaymentEntity payment,
        Services.WebhookData webhookData);

    Task<VerificationResult> VerifyByOrderCodeAsync(string orderCode);
}

public class VerificationResult
{
    public bool IsValid { get; set; }
    public string? FailureReason { get; set; }
    public decimal? ActualAmount { get; set; }
    public string? ActualStatus { get; set; }

    // Alias for FailureReason for convenience
    public string? Reason => FailureReason;

    public static VerificationResult Success() => new() { IsValid = true };
    public static VerificationResult Failure(string reason) => new() { IsValid = false, FailureReason = reason };
}

namespace Payment.Application.DTOs;

/// <summary>
/// Payment information retrieved from PayOS API for verification.
/// Used to re-verify webhook data for fraud prevention.
/// </summary>
public class PaymentInfo
{
    /// <summary>
    /// Order code from PayOS
    /// </summary>
    public string OrderCode { get; set; } = null!;
    
    /// <summary>
    /// Payment amount (must match database)
    /// </summary>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Currency code (VND)
    /// </summary>
    public string Currency { get; set; } = "VND";
    
    /// <summary>
    /// Payment status from PayOS: PENDING, PAID, CANCELLED
    /// </summary>
    public string Status { get; set; } = null!;
    
    /// <summary>
    /// Transaction reference from bank/e-wallet
    /// </summary>
    public string? TransactionId { get; set; }
    
    /// <summary>
    /// Payment description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Account number that made the payment
    /// </summary>
    public string? AccountNumber { get; set; }
    
    /// <summary>
    /// Bank/e-wallet name
    /// </summary>
    public string? CounterAccountBankName { get; set; }
    
    /// <summary>
    /// Account holder name
    /// </summary>
    public string? CounterAccountName { get; set; }
    
    /// <summary>
    /// When payment was completed
    /// </summary>
    public DateTime? TransactionDateTime { get; set; }
}

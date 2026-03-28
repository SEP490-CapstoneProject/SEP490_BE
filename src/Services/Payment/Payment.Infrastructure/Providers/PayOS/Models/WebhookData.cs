using System.Text.Json.Serialization;

namespace Payment.Infrastructure.Providers.PayOS.Models;

/// <summary>
/// Webhook payload from PayOS when payment status changes
/// </summary>
public class WebhookData
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = null!;  // "00" = success
    
    [JsonPropertyName("desc")]
    public string Description { get; set; } = null!;
    
    [JsonPropertyName("data")]
    public WebhookPaymentData Data { get; set; } = null!;
    
    [JsonPropertyName("signature")]
    public string Signature { get; set; } = null!;
}

/// <summary>
/// Payment data inside webhook payload
/// </summary>
public class WebhookPaymentData
{
    [JsonPropertyName("orderCode")]
    public long OrderCode { get; set; }
    
    [JsonPropertyName("amount")]
    public int Amount { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("accountNumber")]
    public string? AccountNumber { get; set; }
    
    [JsonPropertyName("reference")]
    public string? Reference { get; set; }  // Transaction ID
    
    [JsonPropertyName("transactionDateTime")]
    public string? TransactionDateTime { get; set; }
    
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "VND";
    
    [JsonPropertyName("paymentLinkId")]
    public string? PaymentLinkId { get; set; }
    
    [JsonPropertyName("code")]
    public string Code { get; set; } = null!;  // "00" = success
    
    [JsonPropertyName("desc")]
    public string Desc { get; set; } = null!;
    
    [JsonPropertyName("counterAccountBankId")]
    public string? CounterAccountBankId { get; set; }
    
    [JsonPropertyName("counterAccountBankName")]
    public string? CounterAccountBankName { get; set; }
    
    [JsonPropertyName("counterAccountName")]
    public string? CounterAccountName { get; set; }
    
    [JsonPropertyName("counterAccountNumber")]
    public string? CounterAccountNumber { get; set; }
    
    [JsonPropertyName("virtualAccountName")]
    public string? VirtualAccountName { get; set; }
    
    [JsonPropertyName("virtualAccountNumber")]
    public string? VirtualAccountNumber { get; set; }
}

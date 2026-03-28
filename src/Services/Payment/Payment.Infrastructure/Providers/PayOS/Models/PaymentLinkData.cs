using System.Text.Json.Serialization;

namespace Payment.Infrastructure.Providers.PayOS.Models;

/// <summary>
/// Payment link data from PayOS API
/// </summary>
public class PaymentLinkData
{
    [JsonPropertyName("bin")]
    public string? Bin { get; set; }
    
    [JsonPropertyName("accountNumber")]
    public string? AccountNumber { get; set; }
    
    [JsonPropertyName("accountName")]
    public string? AccountName { get; set; }
    
    [JsonPropertyName("amount")]
    public int Amount { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("orderCode")]
    public long OrderCode { get; set; }
    
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "VND";
    
    [JsonPropertyName("paymentLinkId")]
    public string PaymentLinkId { get; set; } = null!;
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;  // PENDING, PAID, CANCELLED
    
    [JsonPropertyName("checkoutUrl")]
    public string CheckoutUrl { get; set; } = null!;
    
    [JsonPropertyName("qrCode")]
    public string? QrCode { get; set; }
}

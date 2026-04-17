using System.Text.Json.Serialization;

namespace Payment.Infrastructure.Providers.PayOS.Models;

/// <summary>
/// Request to create payment link via PayOS API
/// POST /v2/payment-requests
/// </summary>
public class CreatePaymentLinkRequest
{
    [JsonPropertyName("orderCode")]
    public long OrderCode { get; set; }
    
    [JsonPropertyName("amount")]
    public int Amount { get; set; }  // VND has no decimals
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = null!;
    
    [JsonPropertyName("cancelUrl")]
    public string? CancelUrl { get; set; }
    
    [JsonPropertyName("returnUrl")]
    public string? ReturnUrl { get; set; }

    [JsonPropertyName("signature")]
    public string Signature { get; set; } = null!;
}

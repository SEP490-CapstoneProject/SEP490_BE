using System.Text.Json.Serialization;

namespace Payment.Infrastructure.Providers.PayOS.Models;

/// <summary>
/// Response from PayOS API after creating payment link
/// </summary>
public class CreatePaymentLinkResponse
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = null!;  // "00" = success
    
    [JsonPropertyName("desc")]
    public string Description { get; set; } = null!;
    
    [JsonPropertyName("data")]
    public PaymentLinkData Data { get; set; } = null!;
}

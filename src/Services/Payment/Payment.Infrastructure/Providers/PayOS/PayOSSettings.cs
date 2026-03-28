namespace Payment.Infrastructure.Providers.PayOS;

/// <summary>
/// PayOS payment gateway configuration settings.
/// Credentials obtained from https://my.payos.vn/
/// </summary>
public class PayOSSettings
{
    /// <summary>
    /// Client ID from PayOS merchant dashboard
    /// </summary>
    public string ClientId { get; set; } = null!;
    
    /// <summary>
    /// API Key for authenticating API requests (x-api-key header)
    /// </summary>
    public string ApiKey { get; set; } = null!;
    
    /// <summary>
    /// Checksum key for webhook signature validation (HMAC SHA256)
    /// </summary>
    public string ChecksumKey { get; set; } = null!;
    
    /// <summary>
    /// PayOS API base URL (production: https://api-merchant.payos.vn)
    /// </summary>
    public string BaseUrl { get; set; } = "https://api-merchant.payos.vn";
    
    /// <summary>
    /// Webhook callback URL (PayOS sends payment notifications here)
    /// </summary>
    public string WebhookUrl { get; set; } = null!;
    
    /// <summary>
    /// Return URL (where user is redirected after payment - UX only, NOT trusted for activation)
    /// </summary>
    public string ReturnUrl { get; set; } = null!;
    
    /// <summary>
    /// Cancel URL (where user is redirected if they cancel payment)
    /// </summary>
    public string CancelUrl { get; set; } = null!;
    
    /// <summary>
    /// Enable API verification after webhook (MANDATORY for production)
    /// </summary>
    public bool VerificationEnabled { get; set; } = true;
}

using System.Security.Cryptography;
using System.Text;

namespace Payment.Infrastructure.Providers.PayOS;

/// <summary>
/// PayOS webhook signature validator using HMAC SHA256.
/// Validates signature on RAW body before parsing (security critical).
/// </summary>
public class PayOSSignatureValidator
{
    private readonly string _checksumKey;

    public PayOSSignatureValidator(string checksumKey)
    {
        _checksumKey = checksumKey ?? throw new ArgumentNullException(nameof(checksumKey));
    }

    /// <summary>
    /// Validates webhook signature using HMAC SHA256.
    /// </summary>
    /// <param name="rawBody">Raw request body (not yet parsed)</param>
    /// <param name="signature">Signature from x-signature header</param>
    /// <returns>True if signature is valid, false otherwise</returns>
    public bool ValidateSignature(string rawBody, string signature)
    {
        if (string.IsNullOrEmpty(rawBody) || string.IsNullOrEmpty(signature))
            return false;
        
        var expectedSignature = ComputeSignature(rawBody);
        
        // Case-insensitive comparison
        return signature.Equals(expectedSignature, StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Computes HMAC SHA256 signature for data.
    /// </summary>
    public string ComputeSignature(string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_checksumKey));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        
        // Convert to lowercase hex string
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    /// <summary>
    /// Static validation method for backward compatibility.
    /// </summary>
    public static bool Validate(string rawBody, string signature, string checksumKey)
    {
        var validator = new PayOSSignatureValidator(checksumKey);
        return validator.ValidateSignature(rawBody, signature);
    }
}

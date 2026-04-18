using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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

        // Legacy behavior: sign raw body directly.
        var rawBodySignature = ComputeSignature(rawBody);
        if (signature.Equals(rawBodySignature, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // PayOS webhook behavior: sign canonical payload (typically the "data" object).
        var canonicalPayload = TryBuildCanonicalPayload(rawBody);
        if (canonicalPayload == null)
        {
            return false;
        }

        var canonicalSignature = ComputeSignature(canonicalPayload);
        return signature.Equals(canonicalSignature, StringComparison.OrdinalIgnoreCase);
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

    private static string? TryBuildCanonicalPayload(string rawBody)
    {
        try
        {
            using var document = JsonDocument.Parse(rawBody);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var signedElement = document.RootElement;
            if (document.RootElement.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Object)
            {
                signedElement = dataElement;
            }

            var pairs = signedElement
                .EnumerateObject()
                .Where(p => !string.Equals(p.Name, "signature", StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Name, StringComparer.Ordinal)
                .Select(p =>
                {
                    var normalizedValue = NormalizeValueForSignature(p.Value);
                    return $"{Uri.EscapeDataString(p.Name)}={Uri.EscapeDataString(normalizedValue)}";
                });

            return string.Join("&", pairs);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string NormalizeValueForSignature(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            JsonValueKind.Object => NormalizeObjectToJson(value),
            JsonValueKind.Array => NormalizeArrayToJson(value),
            _ => value.GetRawText()
        };
    }

    private static string NormalizeObjectToJson(JsonElement element)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var property in element.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
            {
                writer.WritePropertyName(property.Name);
                WriteNormalizedElement(writer, property.Value);
            }
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string NormalizeArrayToJson(JsonElement element)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartArray();
            foreach (var item in element.EnumerateArray())
            {
                WriteNormalizedElement(writer, item);
            }
            writer.WriteEndArray();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteNormalizedElement(Utf8JsonWriter writer, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in value.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteNormalizedElement(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in value.EnumerateArray())
                {
                    WriteNormalizedElement(writer, item);
                }
                writer.WriteEndArray();
                break;
            default:
                value.WriteTo(writer);
                break;
        }
    }
}

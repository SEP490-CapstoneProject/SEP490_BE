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
    public bool ValidateSignature(string rawBody, string signature) =>
        ValidateSignatureWithDetails(rawBody, signature).IsValid;

    public SignatureValidationResult ValidateSignatureWithDetails(string rawBody, string signature)
    {
        var tried = new List<string>();

        if (string.IsNullOrEmpty(rawBody) || string.IsNullOrEmpty(signature))
        {
            return SignatureValidationResult.Invalid(tried);
        }

        if (IsSignatureMatch(rawBody, signature))
        {
            return SignatureValidationResult.Valid("raw-body");
        }

        tried.Add("raw-body");

        if (!TryGetSignedElement(rawBody, out var signedElement))
        {
            return SignatureValidationResult.Invalid(tried);
        }

        foreach (var candidate in BuildCanonicalCandidates(signedElement))
        {
            tried.Add(candidate.Strategy);
            if (IsSignatureMatch(candidate.Payload, signature))
            {
                return SignatureValidationResult.Valid(candidate.Strategy);
            }
        }

        return SignatureValidationResult.Invalid(tried);
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

    private bool IsSignatureMatch(string payload, string signature)
    {
        var computed = ComputeSignature(payload);
        return signature.Equals(computed, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetSignedElement(string rawBody, out JsonElement signedElement)
    {
        signedElement = default;

        try
        {
            using var document = JsonDocument.Parse(rawBody);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            signedElement = document.RootElement;
            if (document.RootElement.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Object)
            {
                signedElement = dataElement;
            }

            signedElement = signedElement.Clone();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static IEnumerable<CanonicalCandidate> BuildCanonicalCandidates(JsonElement signedElement)
    {
        var minimal = TryBuildMinimalCanonicalPayload(signedElement, escapeValues: true);
        if (minimal is not null)
        {
            yield return new CanonicalCandidate("minimal-amount-description-orderCode-escaped", minimal);
        }

        var minimalRaw = TryBuildMinimalCanonicalPayload(signedElement, escapeValues: false);
        if (minimalRaw is not null)
        {
            yield return new CanonicalCandidate("minimal-amount-description-orderCode-raw", minimalRaw);
        }

        var primitiveCompact = TryBuildPrimitiveCanonicalPayload(signedElement, includeEmptyValues: false, escapeValues: true);
        if (primitiveCompact is not null)
        {
            yield return new CanonicalCandidate("primitive-sorted-no-empty-escaped", primitiveCompact);
        }

        var primitiveCompactRaw = TryBuildPrimitiveCanonicalPayload(signedElement, includeEmptyValues: false, escapeValues: false);
        if (primitiveCompactRaw is not null)
        {
            yield return new CanonicalCandidate("primitive-sorted-no-empty-raw", primitiveCompactRaw);
        }

        var primitiveWithEmpty = TryBuildPrimitiveCanonicalPayload(signedElement, includeEmptyValues: true, escapeValues: true);
        if (primitiveWithEmpty is not null)
        {
            yield return new CanonicalCandidate("primitive-sorted-with-empty-escaped", primitiveWithEmpty);
        }

        var primitiveWithEmptyRaw = TryBuildPrimitiveCanonicalPayload(signedElement, includeEmptyValues: true, escapeValues: false);
        if (primitiveWithEmptyRaw is not null)
        {
            yield return new CanonicalCandidate("primitive-sorted-with-empty-raw", primitiveWithEmptyRaw);
        }
    }

    private static string? TryBuildMinimalCanonicalPayload(JsonElement signedElement, bool escapeValues)
    {
        var map = signedElement
            .EnumerateObject()
            .Where(p => !string.Equals(p.Name, "signature", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(
                p => p.Name,
                p => NormalizeValueForSignature(p.Value),
                StringComparer.OrdinalIgnoreCase);

        if (!map.TryGetValue("amount", out var amount) ||
            !map.TryGetValue("description", out var description) ||
            !map.TryGetValue("orderCode", out var orderCode))
        {
            return null;
        }

        if (escapeValues)
        {
            return $"amount={Uri.EscapeDataString(amount)}&description={Uri.EscapeDataString(description)}&orderCode={Uri.EscapeDataString(orderCode)}";
        }

        return $"amount={amount}&description={description}&orderCode={orderCode}";
    }

    private static string? TryBuildPrimitiveCanonicalPayload(JsonElement signedElement, bool includeEmptyValues, bool escapeValues)
    {
        var entries = signedElement
            .EnumerateObject()
            .Where(p => !string.Equals(p.Name, "signature", StringComparison.OrdinalIgnoreCase))
            .Select(p => new { p.Name, Value = TryNormalizePrimitiveValue(p.Value) })
            .Where(x => x.Value is not null)
            .Select(x => new KeyValuePair<string, string>(x.Name, x.Value!))
            .ToList();

        if (!includeEmptyValues)
        {
            entries = entries.Where(x => !string.IsNullOrWhiteSpace(x.Value)).ToList();
        }

        if (!HasRequiredSigningKeys(entries))
        {
            return null;
        }

        return string.Join("&",
            entries
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => escapeValues
                    ? $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"
                    : $"{x.Key}={x.Value}"));
    }

    private static bool HasRequiredSigningKeys(IEnumerable<KeyValuePair<string, string>> entries)
    {
        var keys = entries.Select(x => x.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return keys.Contains("amount") && keys.Contains("description") && keys.Contains("orderCode");
    }

    private static string? TryNormalizePrimitiveValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            _ => null
        };
    }

    public sealed class SignatureValidationResult
    {
        public bool IsValid { get; init; }
        public string? MatchedStrategy { get; init; }
        public IReadOnlyList<string> TriedStrategies { get; init; } = Array.Empty<string>();

        public static SignatureValidationResult Valid(string strategy) => new()
        {
            IsValid = true,
            MatchedStrategy = strategy,
            TriedStrategies = new[] { strategy }
        };

        public static SignatureValidationResult Invalid(IReadOnlyList<string> triedStrategies) => new()
        {
            IsValid = false,
            TriedStrategies = triedStrategies
        };
    }

    private readonly record struct CanonicalCandidate(string Strategy, string Payload);

    private static string ComputeBodyHash(string input)
    {
        using var sha256 = SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(input)));
    }

    public string ComputeCanonicalHashForDebug(string rawBody)
    {
        if (!TryGetSignedElement(rawBody, out var signedElement))
        {
            return string.Empty;
        }

        var minimal = TryBuildMinimalCanonicalPayload(signedElement, escapeValues: true);
        return string.IsNullOrEmpty(minimal) ? string.Empty : ComputeBodyHash(minimal)[..16];
    }

    public IReadOnlyDictionary<string, string> ComputeCanonicalCandidateHashesForDebug(string rawBody)
    {
        if (!TryGetSignedElement(rawBody, out var signedElement))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        return BuildCanonicalCandidates(signedElement)
            .GroupBy(c => c.Strategy, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => ComputeBodyHash(g.First().Payload)[..16],
                StringComparer.Ordinal);
    }

    public string ComputeRawBodyHashForDebug(string rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return string.Empty;
        }

        return ComputeBodyHash(rawBody)[..16];
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

using Microsoft.Extensions.Configuration;

namespace Company.Infrastructure.Configuration;

/// <summary>
/// Environment-based configuration helpers
/// Supports: appsettings.json → Environment Variables → Azure Key Vault
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Get configuration value with fallback chain:
    /// 1. Azure Key Vault (if configured)
    /// 2. Environment Variables
    /// 3. appsettings.json
    /// </summary>
    public static string GetSecureValue(this IConfiguration configuration, string key, string defaultValue = "")
    {
        // Try standard configuration (includes env vars and appsettings)
        var value = configuration[key];
        
        if (!string.IsNullOrEmpty(value))
            return value;

        // Return default if nothing found
        return defaultValue;
    }

    /// <summary>
    /// Validate required secrets are present
    /// </summary>
    public static void ValidateRequiredSecrets(this IConfiguration configuration, params string[] keys)
    {
        var missing = new List<string>();

        foreach (var key in keys)
        {
            var value = configuration[key];
            if (string.IsNullOrEmpty(value) || value.StartsWith("YOUR_") || value.StartsWith("PLACEHOLDER_"))
            {
                missing.Add(key);
            }
        }

        if (missing.Any())
        {
            throw new InvalidOperationException(
                $"Missing required secrets: {string.Join(", ", missing)}. " +
                "Configure them in environment variables, appsettings.json, or Azure Key Vault.");
        }
    }
}

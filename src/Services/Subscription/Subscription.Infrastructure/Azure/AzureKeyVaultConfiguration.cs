using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace Subscription.Infrastructure.Azure;

/// <summary>
/// Azure Key Vault configuration provider for secure secrets management
/// </summary>
public static class AzureKeyVaultConfiguration
{
    /// <summary>
    /// Add Azure Key Vault as configuration source
    /// Usage in Program.cs:
    /// builder.Configuration.AddAzureKeyVault();
    /// </summary>
    public static IConfigurationBuilder AddAzureKeyVault(this IConfigurationBuilder builder)
    {
        var config = builder.Build();
        var keyVaultUrl = config["Azure:KeyVault:Url"];

        if (string.IsNullOrEmpty(keyVaultUrl))
        {
            // Key Vault not configured - skip (for local development)
            return builder;
        }

        // Use Managed Identity in Azure, falls back to Azure CLI/Visual Studio for local dev
        var credential = new DefaultAzureCredential();
        
        builder.AddAzureKeyVault(new Uri(keyVaultUrl), credential);

        return builder;
    }

    /// <summary>
    /// Get secret from Key Vault directly (alternative approach)
    /// </summary>
    public static async Task<string?> GetSecretAsync(string keyVaultUrl, string secretName)
    {
        if (string.IsNullOrEmpty(keyVaultUrl))
            return null;

        try
        {
            var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
            var secret = await client.GetSecretAsync(secretName);
            return secret.Value.Value;
        }
        catch
        {
            // Key Vault access failed - return null
            return null;
        }
    }
}

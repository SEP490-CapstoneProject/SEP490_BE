using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace Connection.Infrastructure.Azure;

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
}

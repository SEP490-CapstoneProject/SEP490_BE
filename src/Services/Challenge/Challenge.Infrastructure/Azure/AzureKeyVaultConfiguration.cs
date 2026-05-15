using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace Challenge.Infrastructure.Azure;

public static class AzureKeyVaultConfiguration
{
    public static IConfigurationBuilder AddAzureKeyVault(this IConfigurationBuilder builder)
    {
        var config = builder.Build();
        var keyVaultUrl = config["Azure:KeyVault:Url"];

        if (string.IsNullOrWhiteSpace(keyVaultUrl))
        {
            return builder;
        }

        builder.AddAzureKeyVault(new Uri(keyVaultUrl), new DefaultAzureCredential());
        return builder;
    }

    public static async Task<string?> GetSecretAsync(string keyVaultUrl, string secretName)
    {
        if (string.IsNullOrWhiteSpace(keyVaultUrl))
        {
            return null;
        }

        var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
        var secret = await client.GetSecretAsync(secretName);
        return secret.Value.Value;
    }
}

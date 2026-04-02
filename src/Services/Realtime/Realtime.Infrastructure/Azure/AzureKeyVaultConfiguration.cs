using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace Realtime.Infrastructure.Azure;

public static class AzureKeyVaultConfiguration
{
    public static IConfigurationBuilder AddAzureKeyVault(this IConfigurationBuilder builder)
    {
        var config = builder.Build();
        var keyVaultUrl = config["Azure:KeyVault:Url"];

        if (string.IsNullOrEmpty(keyVaultUrl))
        {
            return builder;
        }

        try
        {
            var credential = new DefaultAzureCredential();
            builder.AddAzureKeyVault(new Uri(keyVaultUrl), credential);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Realtime] Key Vault load failed, continuing without it. Error: {ex.Message}");
        }

        return builder;
    }

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
            return null;
        }
    }
}

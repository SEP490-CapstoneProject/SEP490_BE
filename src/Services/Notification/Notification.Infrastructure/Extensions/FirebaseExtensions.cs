using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Notification.Infrastructure.Extensions;

public static class FirebaseExtensions
{
    /// <summary>
    /// Initializes Firebase Admin SDK with credentials from configuration or environment.
    /// Call this during application startup in Program.cs before making any Firebase calls.
    /// </summary>
    public static IServiceCollection AddFirebaseInitialization(this IServiceCollection services, IConfiguration configuration)
    {
        try
        {
            // Check if Firebase is already initialized
            if (FirebaseApp.DefaultInstance != null)
            {
                Console.WriteLine("✅ Firebase Admin SDK already initialized");
                return services;
            }

            var credentialsPath = configuration["Firebase:CredentialsPath"];
            var credentialsJson = configuration["Firebase:CredentialsJson"];

            if (!string.IsNullOrEmpty(credentialsPath) && File.Exists(credentialsPath))
            {
                Console.WriteLine($"🔥 Initializing Firebase Admin SDK with credentials from: {credentialsPath}");
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(credentialsPath),
                    ProjectId = configuration["Firebase:ProjectId"]
                });
                Console.WriteLine("✅ Firebase Admin SDK initialized successfully with file credentials");
            }
            else if (!string.IsNullOrEmpty(credentialsJson))
            {
                Console.WriteLine("🔥 Initializing Firebase Admin SDK with credentials from environment variable");
                // Write JSON to temporary file
                var tempPath = Path.Combine(Path.GetTempPath(), "firebase-credentials.json");
                File.WriteAllText(tempPath, credentialsJson);

                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(tempPath),
                    ProjectId = configuration["Firebase:ProjectId"]
                });

                // Clean up temp file
                try { File.Delete(tempPath); } catch { }

                Console.WriteLine("✅ Firebase Admin SDK initialized successfully with environment credentials");
            }
            else
            {
                Console.WriteLine("⚠️  Firebase credentials not found in configuration. Firebase will be unavailable.");
                Console.WriteLine("   Set Firebase:CredentialsPath or Firebase:CredentialsJson in configuration.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Error initializing Firebase Admin SDK: {ex.Message}");
            Console.WriteLine("   FCM notifications will be unavailable until Firebase is properly configured.");
        }

        return services;
    }

    /// <summary>
    /// Verifies that Firebase Admin SDK is initialized and logs diagnostic information.
    /// Call this after application startup to confirm Firebase is ready for use.
    /// </summary>
    public static void VerifyFirebaseInitialization(this IApplicationBuilder app, ILogger<object> logger)
    {
        try
        {
            if (FirebaseApp.DefaultInstance != null)
            {
                logger.LogInformation("✅ Firebase Admin SDK verified - Default instance available");
                
                // Try to access FirebaseMessaging to ensure it's available
                var messaging = FirebaseAdmin.Messaging.FirebaseMessaging.DefaultInstance;
                if (messaging != null)
                {
                    logger.LogInformation("✅ Firebase Cloud Messaging available - FCM notifications enabled");
                }
                else
                {
                    logger.LogWarning("⚠️  Firebase Cloud Messaging not available");
                }
            }
            else
            {
                logger.LogWarning("⚠️  Firebase Admin SDK not initialized - FCM notifications will fail");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying Firebase initialization");
        }
    }
}

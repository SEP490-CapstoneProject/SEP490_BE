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
                // Already initialized in a previous app startup
                return services;
            }

            // Log using ILogger (add it as a temporary service)
            services.AddLogging();
            var serviceProvider = services.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Firebase.Initialization");

            logger.LogInformation("🔥 [FIREBASE_CONFIG] Scanning IConfiguration for Firebase keys...");
            var firebaseConfigs = configuration.AsEnumerable()
                .Where(x => !string.IsNullOrEmpty(x.Key) && x.Key.Contains("Firebase"))
                .OrderBy(x => x.Key)
                .ToList();

            if (firebaseConfigs.Any())
            {
                foreach (var cfg in firebaseConfigs)
                {
                    var valueLength = cfg.Value?.Length ?? 0;
                    var valuePreview = valueLength > 0 ? cfg.Value.Substring(0, Math.Min(30, valueLength)) : "(empty)";
                    logger.LogInformation($"   - {cfg.Key}: {valueLength} chars, preview: {valuePreview}");
                }
            }
            else
            {
                logger.LogInformation("   (no Firebase keys found)");
            }

            var projectId = configuration["Firebase:ProjectId"];
            var credentialsPath = configuration["Firebase:CredentialsPath"];
            // Check both V3, V2 and original key names. Prefer first non-empty candidate with reasonable length.
            var candidates = new[] { "Firebase:CredentialsJsonV3", "Firebase:CredentialsJsonV2", "Firebase:CredentialsJson" };
            string credentialsJson = null;
            foreach (var key in candidates)
            {
                var val = configuration[key];
                if (!string.IsNullOrWhiteSpace(val) && val.Length > 200)
                {
                    credentialsJson = val;
                    logger.LogInformation($"   Selected credentials from {key} ({val.Length} chars)");
                    break;
                }
            }
            // If none met the length threshold, fall back to any non-empty value
            if (credentialsJson == null)
            {
                foreach (var key in candidates)
                {
                    var val = configuration[key];
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        credentialsJson = val;
                        logger.LogInformation($"   Fallback selected credentials from {key} ({val.Length} chars)");
                        break;
                    }
                }
            }

            logger.LogInformation("🔥 [FIREBASE_INIT] Starting Firebase Admin SDK initialization");
            logger.LogInformation($"   ProjectId: {(!string.IsNullOrEmpty(projectId) ? projectId : "NOT SET ❌")}");
            logger.LogInformation($"   CredentialsPath: {(!string.IsNullOrEmpty(credentialsPath) ? credentialsPath : "NOT SET ❌")}");
            var v2 = configuration["Firebase:CredentialsJsonV2"];
            var v1 = configuration["Firebase:CredentialsJson"];
            logger.LogInformation($"   Firebase:CredentialsJsonV2: {(v2 != null ? $"{v2.Length} chars ✅" : "NOT SET")}");
            logger.LogInformation($"   Firebase:CredentialsJson (V1): {(v1 != null ? $"{v1.Length} chars" : "NOT SET")}");
            logger.LogInformation($"   Using credentials: {(credentialsJson != null ? "Selected from configuration" : "NONE ❌")}");

            if (!string.IsNullOrEmpty(credentialsPath) && File.Exists(credentialsPath))
            {
                logger.LogInformation($"🔥 [FIREBASE_FILE] Loading credentials from file: {credentialsPath}");
                var fileInfo = new FileInfo(credentialsPath);
                logger.LogInformation($"   File size: {fileInfo.Length} bytes");
                
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(credentialsPath),
                    ProjectId = projectId
                });
                logger.LogInformation("✅ [FIREBASE_SUCCESS] Firebase Admin SDK initialized with file credentials");
            }
            else if (!string.IsNullOrEmpty(credentialsJson))
            {
                logger.LogInformation("🔥 [FIREBASE_ENV] Loading credentials from environment/KeyVault");
                logger.LogInformation($"   Credentials JSON length: {credentialsJson.Length} bytes");
                
                try
                {
                    // Try using GoogleCredential.FromJson() directly instead of temp file
                    // This avoids any file I/O issues with newline escaping
                    logger.LogInformation("🔥 [FIREBASE_PARSING] Attempting to parse JSON credentials directly...");
                    
                    var credential = GoogleCredential.FromJson(credentialsJson);
                    
                    logger.LogInformation("✅ [FIREBASE_PARSED] Successfully parsed JSON credentials");
                    logger.LogInformation($"   Credential type: {credential.GetType().Name}");
                    
                    // Create Firebase app with the credential
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = credential,
                        ProjectId = projectId
                    });

                    logger.LogInformation("✅ [FIREBASE_SUCCESS] Firebase Admin SDK initialized with environment credentials");
                    
                    // Verify initialization was successful
                    var app = FirebaseApp.DefaultInstance;
                    if (app != null)
                    {
                        logger.LogInformation($"🔥 [FIREBASE_VERIFY] Confirmed: FirebaseApp.DefaultInstance is initialized");
                        logger.LogInformation($"   AppName: {app.Name}");
                    }
                    else
                    {
                        logger.LogError($"❌ [FIREBASE_VERIFY] ERROR: FirebaseApp.DefaultInstance is still null!");
                    }
                }
                catch (Exception jsonEx)
                {
                    logger.LogError(jsonEx, $"❌ [FIREBASE_JSON_ERROR] JSON parse/credential error");
                    throw;
                }
            }
            else
            {
                logger.LogError("❌ [FIREBASE_MISSING] Firebase credentials not found in configuration");
                logger.LogError("   Set Firebase:CredentialsPath or Firebase:CredentialsJson in configuration or Azure KeyVault");
                logger.LogError("   FCM notifications will be unavailable until credentials are properly configured");
            }
        }
        catch (Exception ex)
        {
            // For critical errors during startup, use Console.Error as fallback
            Console.Error.WriteLine($"❌ [FIREBASE_ERROR] Error initializing Firebase Admin SDK");
            Console.Error.WriteLine($"   Exception type: {ex.GetType().Name}");
            Console.Error.WriteLine($"   Message: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.Error.WriteLine($"   Inner error: {ex.InnerException.Message}");
            }
            Console.Error.WriteLine("   FCM notifications will be unavailable until Firebase is properly configured");
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


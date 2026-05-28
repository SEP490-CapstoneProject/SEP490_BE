using Google.Cloud.Firestore;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

class FirebaseTokenTester
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════╗");
        Console.WriteLine("║  FCM Token Validation Test - Firebase SDK  ║");
        Console.WriteLine("╚════════════════════════════════════════════╝");
        Console.WriteLine();

        var tokens = new[]
        {
            "fpQ6ZToxTRWox8EzcQKtS8:APA91bGQ9qymJXZhBG0xyk8f2g5FldUxCprH3oqviF14A_4Qnb0wXEFPpc7uLuHy3R-FhPdgxYVVtLvSYysxHyoLuMKidaBAtaDzmrw9LLaQYWgHKKqUdWg",
            "cxODJtv7T4a7muZ07FfvSE:APA91bG-olD9x3U0Z81OizRMfYx1vju6E0dKr2uVfeurTMUUvWpaE7fQQQPq5YNc3K29ngGoCuW3k3-4_0YtJKE2w7ppJMdJpk70KWWEUw42qm-9u1RUHBE",
            "cdblS-iSTPOzsZmC_e305b:APA91bFWCt_schTTjLfenMil-ywg0jZiJfzO50-JXsxtGo6-XBSjuETjLrhHof0XcIrh6kWpI-G_-EcwCjG1rLKA7cCQ2R2kCC-lIHhe3XOnGZhgU8UGUXU",
            "ddUliFqIRcSlSxDDc8B0Dl:APA91bHODTPI4kohTKb47YG_da2GXb-wYUJ9ffdOFGv8X3DxCk-fm_LBUmSPtBOaVKD85ri1TF_Xq98xdnv8DebRuCIgfFdI_MoHUZLEJNkZXyojtdYfokM"
        };

        try
        {
            // Initialize Firebase Admin SDK
            Console.WriteLine("🔥 Initializing Firebase Admin SDK...");
            string credentialsPath = "/app/firebase-credentials.json";
            
            if (!File.Exists(credentialsPath))
            {
                credentialsPath = "firebase-credentials-prod.json";
            }

            if (!File.Exists(credentialsPath))
            {
                Console.WriteLine("❌ Credentials file not found!");
                return;
            }

            var credentials = GoogleCredential.FromFile(credentialsPath);
            var options = new AppOptions { Credential = credentials };
            
            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(options);
            }

            Console.WriteLine("✅ Firebase SDK initialized");
            Console.WriteLine();

            var results = new List<(string Token, string Status, string Message)>();

            // Test each token
            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];
                Console.WriteLine($"📱 Testing Token {i + 1}/{tokens.Length}...");
                Console.WriteLine($"   Token: {token.Substring(0, 50)}...");
                
                try
                {
                    var message = new Message
                    {
                        Token = token,
                        Notification = new Notification
                        {
                            Title = "Test Notification",
                            Body = "Token validation test from FCM Admin SDK"
                        },
                        Data = new Dictionary<string, string>
                        {
                            { "testId", $"token_{i}" },
                            { "timestamp", DateTime.UtcNow.ToString("O") }
                        }
                    };

                    string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                    Console.WriteLine($"   ✅ SUCCESS - Message ID: {response}");
                    results.Add((token, "✅ VALID", response));
                }
                catch (FirebaseMessagingException ex)
                {
                    Console.WriteLine($"   ❌ FAILED - ErrorCode: {ex.ErrorCode}");
                    Console.WriteLine($"   Message: {ex.Message}");
                    results.Add((token, $"❌ FAILED ({ex.ErrorCode})", ex.Message));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ❌ ERROR - {ex.GetType().Name}");
                    Console.WriteLine($"   Message: {ex.Message}");
                    results.Add((token, $"❌ ERROR", ex.Message));
                }
                
                Console.WriteLine();
            }

            // Summary
            Console.WriteLine("╔════════════════════════════════════════════╗");
            Console.WriteLine("║              TEST SUMMARY                  ║");
            Console.WriteLine("╚════════════════════════════════════════════╝");
            Console.WriteLine();

            int validCount = results.Count(r => r.Status.Contains("VALID"));
            int failedCount = results.Count(r => r.Status.Contains("FAILED"));
            int errorCount = results.Count(r => r.Status.Contains("ERROR"));

            Console.WriteLine($"Total: {results.Count} | ✅ Valid: {validCount} | ❌ Failed: {failedCount} | ⚠️  Error: {errorCount}");
            Console.WriteLine();

            Console.WriteLine("Detailed Results:");
            Console.WriteLine("─────────────────────────────────────────────────────");
            for (int i = 0; i < results.Count; i++)
            {
                var (token, status, msg) = results[i];
                Console.WriteLine($"{i + 1}. {status}");
                Console.WriteLine($"   Token: {token.Substring(0, 50)}...");
                if (!string.IsNullOrEmpty(msg) && msg.Length < 100)
                {
                    Console.WriteLine($"   Detail: {msg}");
                }
                Console.WriteLine();
            }

            // Recommendations
            Console.WriteLine("╔════════════════════════════════════════════╗");
            Console.WriteLine("║          RECOMMENDATIONS                   ║");
            Console.WriteLine("╚════════════════════════════════════════════╝");
            Console.WriteLine();

            if (validCount == tokens.Length)
            {
                Console.WriteLine("✅ All tokens are valid!");
                Console.WriteLine("   → Issue is in app code or SDK configuration");
            }
            else if (validCount == 0)
            {
                Console.WriteLine("❌ All tokens are invalid/expired!");
                Console.WriteLine("   → Delete these tokens from DB and regenerate from mobile app");
            }
            else
            {
                Console.WriteLine($"⚠️  Mixed results: {validCount} valid, {failedCount} invalid");
                Console.WriteLine("   → Delete invalid tokens, keep valid ones");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}

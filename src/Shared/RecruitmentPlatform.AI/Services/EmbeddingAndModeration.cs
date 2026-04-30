using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RecruitmentPlatform.AI.Abstractions;
using RecruitmentPlatform.AI.Models;

namespace RecruitmentPlatform.AI.Services;

public sealed class OpenAiEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _embeddingModel;
    private readonly ILogger<OpenAiEmbeddingService> _logger;

    public OpenAiEmbeddingService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAiEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["OpenAI:ApiKey"] ?? string.Empty;
        _embeddingModel = string.IsNullOrWhiteSpace(configuration["OpenAI:EmbeddingModel"])
            ? "text-embedding-3-small"
            : configuration["OpenAI:EmbeddingModel"]!;
        _logger = logger;
    }

    public async Task<float[]> CreateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<float>();
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/embeddings");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = JsonContent.Create(new
        {
            model = _embeddingModel,
            input = text
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("OpenAI embedding request failed: {Status} {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Embedding generation failed with status {response.StatusCode}");
        }

        var payload = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: cancellationToken);
        var embedding = payload?.Data?.FirstOrDefault()?.Embedding;
        return embedding ?? Array.Empty<float>();
    }

    private sealed class EmbeddingResponse
    {
        [JsonPropertyName("data")]
        public List<EmbeddingData>? Data { get; set; }
    }

    private sealed class EmbeddingData
    {
        [JsonPropertyName("embedding")]
        public float[]? Embedding { get; set; }
    }
}

public sealed class LinkVerificationResult
{
    public bool IsValid { get; set; }
    public List<string> DetectedUrls { get; set; } = new();
    public string? RejectionReason { get; set; }
    public string? DetectedMaliciousUrl { get; set; }
}

public sealed class ModerationResult
{
    public string Status { get; set; } = "Approved"; // Approved, Rejected, PendingReview
    public string Reason { get; set; } = string.Empty;
    public LinkVerificationResult? LinkVerification { get; set; }
    public string? TriggeringKeyword { get; set; }
}

public sealed class ModerationService
{
    private static readonly string[] SpamKeywords = ["viagra", "casino", "betting", "xxx", "spam"];
    
    // Whitelist of safe domains
    private static readonly string[] AllowedDomains = new[]
    {
        "github.com", "linkedin.com", "youtube.com", "instagram.com", "twitter.com", "facebook.com",
        "portfolio.com", "behance.net", "dribbble.com", "medium.com", "dev.to", "stackoverflow.com",
        "skillsnap.com", "skillsnap.local", "vercel.app", "netlify.app", "heroku.com",
        "azure.microsoft.com", "aws.amazon.com", "firebase.google.com", "res.cloudinary.com"
    };
    
    // Blacklist of known malicious domains
    private static readonly string[] BlockedDomains = new[]
    {
        "bit.ly", "tinyurl.com", "goo.gl", "ow.ly", "tiny.cc" // URL shorteners - suspicious for posts
    };

    public ModerationResult Check(string content, bool hasProject)
    {
        var normalized = content?.Trim() ?? string.Empty;
        if (normalized.Length < 30)
        {
            return new ModerationResult { Status = "Rejected", Reason = "Description is too short." };
        }

        if (!hasProject)
        {
            return new ModerationResult { Status = "Rejected", Reason = "Portfolio has no project content." };
        }

        var lowered = normalized.ToLowerInvariant();
        var detectedKeyword = SpamKeywords.FirstOrDefault(keyword => lowered.Contains(keyword));
        if (detectedKeyword != null)
        {
            return new ModerationResult 
            { 
                Status = "Rejected", 
                Reason = $"Spam content detected: '{detectedKeyword}' is not allowed.",
                TriggeringKeyword = detectedKeyword
            };
        }

        // Verify links (check for malicious URLs)
        var linkCheck = VerifyLinks(content);
        if (linkCheck.Status == "Rejected")
        {
            return linkCheck;
        }

        var score = 0d;
        if (normalized.Length >= 250) score += 0.4;
        else if (normalized.Length >= 120) score += 0.25;
        else score += 0.1;

        if (hasProject) score += 0.4;
        if (normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 40) score += 0.2;

        if (score < 0.45)
        {
            return new ModerationResult { Status = "Rejected", Reason = "Content quality below minimum threshold." };
        }

        if (score < 0.70)
        {
            return new ModerationResult { Status = "PendingReview", Reason = "Requires manual review." };
        }

        return new ModerationResult { Status = "Approved", Reason = "Content passed moderation." };
    }

    /// <summary>
    /// Verify URLs in content for malicious or suspicious links.
    /// Detects URLs and validates them against whitelist/blacklist.
    /// </summary>
    public ModerationResult VerifyLinks(string content)
    {
        var result = new ModerationResult { Status = "Approved" };
        
        if (string.IsNullOrWhiteSpace(content))
            return result;

        // Extract URLs using regex
        var urlPattern = @"https?://[^\s]+";
        var matches = System.Text.RegularExpressions.Regex.Matches(content, urlPattern);
        
        if (matches.Count == 0)
            return result;

        var detectedUrls = new List<string>();
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var url = match.Value.TrimEnd('.', ',', ')', ']', '\'', '"'); // Remove trailing punctuation
            detectedUrls.Add(url);
        }

        var linkResult = new LinkVerificationResult { DetectedUrls = detectedUrls };

        // Check each URL
        foreach (var url in detectedUrls)
        {
            try
            {
                var uri = new Uri(url);
                var host = uri.Host.ToLowerInvariant();

                // Remove www prefix for comparison
                if (host.StartsWith("www."))
                    host = host.Substring(4);

                // Check blacklist first (auto-reject)
                if (BlockedDomains.Any(bd => host.EndsWith(bd)))
                {
                    linkResult.IsValid = false;
                    linkResult.RejectionReason = $"Suspicious link detected: {url}";
                    linkResult.DetectedMaliciousUrl = url;
                    
                    result.Status = "Rejected";
                    result.Reason = $"Suspicious link detected: {url}";
                    result.LinkVerification = linkResult;
                    return result;
                }

                // Check whitelist (allow if in whitelist)
                if (!AllowedDomains.Any(ad => host.EndsWith(ad)))
                {
                    // Not in whitelist - treat as suspicious but don't auto-reject
                    // This allows flexibility for new legitimate domains
                    linkResult.IsValid = false;
                    linkResult.RejectionReason = $"Unverified domain: {host}";
                }
            }
            catch (UriFormatException)
            {
                linkResult.IsValid = false;
                linkResult.RejectionReason = $"Invalid URL format: {url}";
                
                result.Status = "Rejected";
                result.Reason = $"Invalid URL format: {url}";
                result.LinkVerification = linkResult;
                return result;
            }
        }

        linkResult.IsValid = true;
        result.LinkVerification = linkResult;
        return result;
    }

    /// <summary>
    /// Comprehensive moderation for community/company posts.
    /// Checks both ban words and links. No portfolio requirement.
    /// </summary>
    public ModerationResult CheckPost(string content)
    {
        var normalized = content?.Trim() ?? string.Empty;
        
        // 1. Check minimum length
        if (normalized.Length < 5)
        {
            return new ModerationResult { Status = "Rejected", Reason = "Content is too short (minimum 5 characters)." };
        }

        // 2. Check for ban words (auto-reject)
        var lowered = normalized.ToLowerInvariant();
        var detectedKeyword = SpamKeywords.FirstOrDefault(keyword => lowered.Contains(keyword));
        if (detectedKeyword != null)
        {
            return new ModerationResult 
            { 
                Status = "Rejected", 
                Reason = $"Spam content detected: '{detectedKeyword}' is not allowed.",
                TriggeringKeyword = detectedKeyword
            };
        }

        // 3. Check for suspicious links (auto-reject)
        var linkCheck = VerifyLinks(content);
        if (linkCheck.Status == "Rejected")
        {
            return linkCheck;
        }

        // All checks passed - approve immediately (no scoring, no pending review)
        return new ModerationResult { Status = "Approved", Reason = "Content passed moderation." };
    }
}

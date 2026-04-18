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

public sealed class ModerationService
{
    private static readonly string[] SpamKeywords = ["viagra", "casino", "betting", "xxx", "spam"];

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
        if (SpamKeywords.Any(lowered.Contains))
        {
            return new ModerationResult { Status = "Rejected", Reason = "Spam content detected." };
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

        if (score < 0.75)
        {
            return new ModerationResult { Status = "PendingReview", Reason = "Requires manual review." };
        }

        return new ModerationResult { Status = "Approved", Reason = "Content passed moderation." };
    }
}

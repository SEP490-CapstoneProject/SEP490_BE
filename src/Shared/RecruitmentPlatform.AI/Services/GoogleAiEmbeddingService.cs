using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RecruitmentPlatform.AI.Abstractions;

namespace RecruitmentPlatform.AI.Services;

public sealed class GoogleAiEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _embeddingModel;
    private readonly ILogger<GoogleAiEmbeddingService> _logger;

    public GoogleAiEmbeddingService(HttpClient httpClient, IConfiguration configuration, ILogger<GoogleAiEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["GoogleAI:ApiKey"] ?? string.Empty;
        _embeddingModel = string.IsNullOrWhiteSpace(configuration["GoogleAI:EmbeddingModel"])
            ? "embedding-001"
            : configuration["GoogleAI:EmbeddingModel"]!;
        _logger = logger;

        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("⚠️ Google AI API key is empty! Key Vault integration may have failed. " +
                "Ensure: 1) Container App has managed identity enabled, " +
                "2) Identity has permission to read Key Vault secrets, " +
                "3) Secret exists as 'GoogleAI--ApiKey' in Key Vault");
        }
        else
        {
            _logger.LogInformation("✅ Google AI API key loaded successfully (length: {KeyLength} chars)", _apiKey.Length);
        }
    }

    public async Task<float[]> CreateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<float>();
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogError("❌ Cannot generate embedding: Google AI API key is not configured!");
            throw new InvalidOperationException("Google AI API key is not configured.");
        }

        try
        {
            var requestUrl = $"/v1beta/models/{_embeddingModel}:embedContent?key={_apiKey}";
            _logger.LogInformation("Generating embedding via Google AI (model: {Model}, textLength: {TextLength})", 
                _embeddingModel, text.Length);
            
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Content = JsonContent.Create(new
            {
                model = $"models/{_embeddingModel}",
                content = new
                {
                    parts = new[]
                    {
                        new { text }
                    }
                }
            });

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("❌ Google AI embedding request failed: Status={Status}, Body={Body}", 
                    response.StatusCode, body);
                throw new InvalidOperationException($"Embedding generation failed with status {response.StatusCode}: {body}");
            }

            _logger.LogInformation("✅ Embedding generated successfully");
            var payload = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: cancellationToken);
            var embedding = payload?.Embedding?.Values;
            return embedding?.ToArray() ?? Array.Empty<float>();
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            _logger.LogError(ex, "❌ Unexpected error during Google AI embedding request");
            throw new InvalidOperationException("Embedding generation failed", ex);
        }
    }

    private sealed class EmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public EmbeddingData? Embedding { get; set; }
    }

    private sealed class EmbeddingData
    {
        [JsonPropertyName("values")]
        public List<float>? Values { get; set; }
    }
}

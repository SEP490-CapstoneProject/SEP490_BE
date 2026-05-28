using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Portfolio.Application.DTOs;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Services;

public class GoogleAiPreviewGenerator
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _generativeModel;
    private readonly ILogger<GoogleAiPreviewGenerator> _logger;

    // Constants for AI generation
    private const int MaxOutputTokens = 2048;
    private const float Temperature = 0.3f;  // Low for deterministic outputs
    private const int TopK = 40;
    private const float TopP = 0.95f;

    public GoogleAiPreviewGenerator(HttpClient httpClient, IConfiguration configuration, ILogger<GoogleAiPreviewGenerator> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["GoogleAI:ApiKey"] ?? string.Empty;
        _generativeModel = string.IsNullOrWhiteSpace(configuration["GoogleAI:GenerativeModel"])
            ? "gemini-2.5-flash"
            : configuration["GoogleAI:GenerativeModel"]!;
        _logger = logger;

        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("⚠️ Google AI API key is empty! Portfolio preview generation will fail.");
        }
    }

    /// <summary>
    /// Generate portfolio preview using Gemini AI
    /// </summary>
    public async Task<(bool Success, string? PreviewJson, string? ErrorMessage, int? TokensUsed)> GeneratePreviewAsync(
        Portfolio.Domain.Entities.Portfolio portfolio, 
        IEnumerable<PortfolioBlock> blocks,
        string? highlightsDescription,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogError("❌ Cannot generate preview: Google AI API key is not configured!");
            return (false, null, "Google AI API key not configured", null);
        }

        try
        {
            // Prepare portfolio data for AI
            var portfolioData = PreparePortfolioData(portfolio, blocks);
            
            // Use provided highlights or default to portfolio name
            var highlights = highlightsDescription ?? $"Portfolio for {portfolio.Name}" ?? "[No highlights provided]";

            // Create the prompt for AI
            var prompt = CreateSystemPrompt(portfolioData, highlights);

            _logger.LogInformation("Generating portfolio preview via Google Gemini (model: {Model})", _generativeModel);

            // Call Gemini API
            var result = await CallGeminiAsync(prompt, cancellationToken);

            if (!result.Success)
            {
                _logger.LogError("❌ Gemini API call failed: {Error}", result.ErrorMessage);
                return (false, null, result.ErrorMessage, null);
            }

            // Parse and validate JSON response
            var (isValid, previewJson) = ValidateJsonResponse(result.ResponseText);

            if (!isValid)
            {
                _logger.LogError("❌ Invalid JSON response from Gemini: {Response}", result.ResponseText);
                return (false, null, "Invalid JSON response from AI", null);
            }

            _logger.LogInformation("✅ Portfolio preview generated successfully (tokens: {Tokens})", result.TokensUsed);
            return (true, previewJson, null, result.TokensUsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unexpected error during portfolio preview generation");
            return (false, null, $"Unexpected error: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Prepare portfolio data for AI processing
    /// </summary>
    private string PreparePortfolioData(Portfolio.Domain.Entities.Portfolio portfolio, IEnumerable<PortfolioBlock> blocks)
    {
        var blocksList = blocks.OrderBy(b => b.DisplayOrder).ToList();
        var blockData = string.Join("\n", blocksList.Select(b => $"- {b.BlockType?.Code}: {TruncateText(b.DataJson, 200)}"));

        return $@"Portfolio Name: {portfolio.Name}
Portfolio Status: {portfolio.Status}
Total Blocks: {blocksList.Count}

Blocks:
{blockData}";
    }

    /// <summary>
    /// Create system prompt for Gemini
    /// </summary>
    private string CreateSystemPrompt(string portfolioData, string highlights)
    {
        return $@"You are a professional portfolio summarizer. Your task is to generate a concise, JSON-structured portfolio preview for recruiters to quickly scan.

Portfolio Data:
{portfolioData}

User's Highlights to Emphasize:
{highlights}

Generate a JSON response with the following structure (total ~150 words):
{{
  ""title"": ""[Professional title/specialization - 1-2 words]"",
  ""keySkills"": ""[Top 3-5 skills separated by commas]"",
  ""specialization"": ""[Main specialization/expertise in 1 sentence]"",
  ""recentProjects"": ""[2-3 most relevant recent projects or achievements in 2-3 sentences]"",
  ""achievement"": ""[Most notable achievement in 1 sentence]"",
  ""summary"": ""[Overall professional summary in 3-4 sentences, emphasizing the highlights provided]""
}}

IMPORTANT:
- Output ONLY valid JSON, no markdown or extra text
- All fields must be strings
- Total content should be approximately 150 words
- Be professional and concise
- Emphasize the user's highlights
- Focus on what matters most to recruiters";
    }

    /// <summary>
    /// Call Gemini API for content generation
    /// </summary>
    private async Task<(bool Success, string ResponseText, string? ErrorMessage, int? TokensUsed)> CallGeminiAsync(
        string prompt, 
        CancellationToken cancellationToken)
    {
        try
        {
            var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{_generativeModel}:generateContent?key={_apiKey}";

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Content = JsonContent.Create(new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = Temperature,
                    topK = TopK,
                    topP = TopP,
                    maxOutputTokens = MaxOutputTokens,
                    responseMimeType = "application/json",
                    responseSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            title = new { type = "string" },
                            keySkills = new { type = "string" },
                            specialization = new { type = "string" },
                            recentProjects = new { type = "string" },
                            achievement = new { type = "string" },
                            summary = new { type = "string" }
                        },
                        required = new[]
                        {
                            "title",
                            "keySkills",
                            "specialization",
                            "recentProjects",
                            "achievement",
                            "summary"
                        }
                    }
                }
            });

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("❌ Gemini API request failed: Status={Status}, Body={Body}", response.StatusCode, body);
                return (false, "", $"API returned {response.StatusCode}", null);
            }

            var payload = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken: cancellationToken);

            if (payload?.Candidates == null || payload.Candidates.Count == 0)
            {
                _logger.LogError("❌ No response from Gemini API");
                return (false, "", "Empty response from API", null);
            }

            var responseText = payload.Candidates[0].Content?.Parts?[0]?.Text ?? "";
            var tokensUsed = payload.UsageMetadata?.OutputTokenCount;

            _logger.LogInformation("Gemini raw response preview: {Preview}",
                responseText.Length > 500 ? responseText[..500] : responseText);

            return (true, responseText, null, tokensUsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unexpected error calling Gemini API");
            return (false, "", $"Request error: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Validate and parse JSON response from AI
    /// </summary>
    private (bool IsValid, string? JsonString) ValidateJsonResponse(string responseText)
    {
        try
        {
            // Extract JSON from response (handle cases where AI adds extra text)
            var jsonStartIdx = responseText.IndexOf('{');
            var jsonEndIdx = responseText.LastIndexOf('}');

            if (jsonStartIdx < 0 || jsonEndIdx < 0)
            {
                _logger.LogWarning("⚠️ No JSON object found in response");
                return (false, null);
            }

            var jsonText = responseText.Substring(jsonStartIdx, jsonEndIdx - jsonStartIdx + 1);

            // Try to parse JSON to validate structure
            using var jsonDoc = JsonDocument.Parse(jsonText);
            var root = jsonDoc.RootElement;

            // Verify required fields exist
            var requiredFields = new[] { "title", "keySkills", "specialization", "recentProjects", "achievement", "summary" };
            foreach (var field in requiredFields)
            {
                if (!root.TryGetProperty(field, out _))
                {
                    _logger.LogWarning("⚠️ Missing required field in JSON: {Field}", field);
                    return (false, null);
                }
            }

            _logger.LogInformation("✅ JSON validation passed");
            return (true, jsonText);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "❌ Failed to parse JSON response");
            return (false, null);
        }
    }

    /// <summary>
    /// Truncate text to specified length
    /// </summary>
    private static string TruncateText(string text, int maxLength)
    {
        if (text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength) + "...";
    }

    #region Gemini API Response Models

    private sealed class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }

        [JsonPropertyName("usageMetadata")]
        public UsageMetadata? UsageMetadata { get; set; }
    }

    private sealed class Candidate
    {
        [JsonPropertyName("content")]
        public Content? Content { get; set; }
    }

    private sealed class Content
    {
        [JsonPropertyName("parts")]
        public List<Part>? Parts { get; set; }
    }

    private sealed class Part
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private sealed class UsageMetadata
    {
        [JsonPropertyName("inputTokenCount")]
        public int InputTokenCount { get; set; }

        [JsonPropertyName("outputTokenCount")]
        public int OutputTokenCount { get; set; }

        [JsonPropertyName("candidatesTokenCount")]
        public int CandidatesTokenCount { get; set; }
    }

    #endregion
}

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Portfolio.Application.DTOs;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Services;

/// <summary>
/// Service for generating structured visual prompts from portfolio preview data
/// These prompts are used by Imagen AI to generate preview images
/// </summary>
public class VisualPromptService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _generativeModel;
    private readonly ILogger<VisualPromptService> _logger;

    // Constants for AI generation
    private const int MaxOutputTokens = 800;
    private const float Temperature = 0.5f;  // Slightly higher for creative visual prompts
    private const int TopK = 40;
    private const float TopP = 0.95f;

    public VisualPromptService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<VisualPromptService> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["GoogleAI:ApiKey"] ?? string.Empty;
        _generativeModel = string.IsNullOrWhiteSpace(configuration["GoogleAI:GenerativeModel"])
            ? "gemini-2.5-flash"
            : configuration["GoogleAI:GenerativeModel"]!;
        _logger = logger;

        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("⚠️ Google AI API key is empty! Visual prompt generation will fail.");
        }
    }

    /// <summary>
    /// Generate a structured visual prompt from preview data and theme
    /// </summary>
    public async Task<(bool Success, VisualPromptDto? VisualPrompt, string? ErrorMessage)> GenerateVisualPromptAsync(
        string previewJson,
        string selectedTheme = "professional",
        string? recruiterPersona = null,
        string? avatarBase64 = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogError("❌ Cannot generate visual prompt: Google AI API key is not configured!");
            return (false, null, "Google AI API key not configured");
        }

        try
        {
            _logger.LogInformation("Generating visual prompt for theme: {Theme}, persona: {Persona}, hasAvatar: {HasAvatar}",
                selectedTheme, recruiterPersona ?? "default", !string.IsNullOrEmpty(avatarBase64));

            // Create the prompt for AI
            var prompt = CreateVisualPromptSystemPrompt(previewJson, selectedTheme, recruiterPersona, avatarBase64);

            // Call Gemini API
            var result = await CallGeminiAsync(prompt, cancellationToken);

            if (!result.Success)
            {
                _logger.LogError("❌ Gemini API call failed: {Error}", result.ErrorMessage);
                return (false, null, result.ErrorMessage);
            }

            // Parse and validate JSON response
            var (isValid, visualPrompt) = ParseVisualPromptResponse(result.ResponseText);

            if (!isValid || visualPrompt == null)
            {
                _logger.LogError("❌ Invalid visual prompt response from Gemini: {Response}", result.ResponseText);
                return (false, null, "Invalid visual prompt response from AI");
            }

            _logger.LogInformation("✅ Visual prompt generated successfully");
            return (true, visualPrompt, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unexpected error during visual prompt generation");
            return (false, null, $"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Create system prompt for visual prompt generation with FIXED LAYOUT
    /// </summary>
    private string CreateVisualPromptSystemPrompt(
        string previewJson,
        string selectedTheme,
        string? recruiterPersona,
        string? avatarBase64 = null)
    {
        var personaDescription = recruiterPersona switch
        {
            "FAANG" => "for FAANG tech companies (Google, Apple, Facebook, Amazon, Netflix) - focus on innovation, technology, scale",
            "Startup" => "for startup investors and founders - focus on energy, growth, disruption",
            "GameStudio" => "for game studios and creative tech companies - focus on creativity, visual appeal, playfulness",
            "AICompany" => "for AI/ML companies - focus on data, intelligence, future-tech aesthetics",
            _ => "for general professional use"
        };

        var avatarSection = string.IsNullOrEmpty(avatarBase64) 
            ? "" 
            : $"\n\nAVATAR PROVIDED:\nAvatar image (data URI): {avatarBase64.Substring(0, Math.Min(100, avatarBase64.Length))}...\nMust include this avatar as circular element in top-left corner of layout.";

        return $@"You are an expert visual design prompt engineer. Your task is to create a detailed visual prompt with MANDATORY FIXED LAYOUT that Imagen AI can use to generate a professional portfolio preview image.

Portfolio Preview Data:
{previewJson}

Design Theme: {selectedTheme} (choose from: professional, creative, minimal, startup, corporate, cyberpunk)
Target Audience: {personaDescription}
{avatarSection}

MANDATORY FIXED LAYOUT STRUCTURE:
You MUST enforce this exact layout - no exceptions:
1. TOP-LEFT: Avatar (circular, 80x80px area)
2. TOP-RIGHT: Name/Title (hero text, bold, large)
3. CENTER: Skills and Technology Badges (3-5 items, rounded pills)
4. BOTTOM: Achievement Statement (centered, prominent)

Generate a JSON response with the following structure:
{{
  ""layout"": ""fixed"",
  ""layoutStructure"": {{
    ""avatar"": {{
      ""position"": ""top-left"",
      ""size"": ""80x80px"",
      ""style"": ""circular with subtle border"",
      ""includeAvatar"": {(string.IsNullOrEmpty(avatarBase64) ? "false" : "true")}
    }},
    ""nameTitle"": {{
      ""position"": ""top-right"",
      ""content"": ""[Extract portfolio title from preview]"",
      ""fontStyle"": ""bold, large, professional, 24-28pt""
    }},
    ""skills"": {{
      ""position"": ""center"",
      ""type"": ""badge-array"",
      ""count"": 3-5,
      ""items"": [""[Skill 1]"", ""[Skill 2]"", ""[Skill 3]""],
      ""style"": ""rounded pills with colors, professional palette""
    }},
    ""achievement"": {{
      ""position"": ""bottom"",
      ""content"": ""[Extract key achievement from preview]"",
      ""fontStyle"": ""prominent, centered, 16-18pt""
    }}
  }},
  ""visualTheme"": ""[Brief description of overall visual theme]"",
  ""mainElements"": [""[Element 1]"", ""[Element 2]"", ""[Element 3]""],
  ""colorPalette"": [""[Primary Color]"", ""[Secondary Color]"", ""[Accent Color]""],
  ""style"": ""[Art style description for Imagen: photorealistic, illustrated, minimalist, etc]""
}}

CRITICAL LAYOUT ENFORCEMENT:
- Avatar MUST be circular and positioned EXACTLY in top-left corner
- Name/Title MUST be in top-right area
- Skills badges MUST be in center section
- Achievement MUST be at bottom
- Do NOT vary from this structure
- Layout must be consistent and predictable

IMPORTANT GUIDELINES:
- Output ONLY valid JSON, no markdown or extra text
- colorPalette should be hex colors or descriptive color names
- Ensure theme consistency:
  - Professional: corporate colors, clean layouts, traditional elements
  - Creative: vibrant colors, artistic elements, dynamic composition
  - Minimal: 1-2 colors, spacious layouts, typography-focused
  - Startup: bold colors, modern elements, energetic vibe
  - Corporate: brand colors, structured, authoritative
  - Cyberpunk: neon colors, futuristic elements, digital aesthetics
- Match the selected theme exactly
- Consider the recruiter persona to emphasize relevant visual signals";
    }

    /// <summary>
    /// Call Gemini API for visual prompt generation
    /// </summary>
    private async Task<(bool Success, string ResponseText, string? ErrorMessage)> CallGeminiAsync(
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
                            visualTheme = new { type = "string" },
                            mainElements = new
                            {
                                type = "array",
                                items = new { type = "string" }
                            },
                            colorPalette = new
                            {
                                type = "array",
                                items = new { type = "string" }
                            },
                            heroText = new { type = "string" },
                            style = new { type = "string" }
                        },
                        required = new[]
                        {
                            "visualTheme",
                            "mainElements",
                            "colorPalette",
                            "heroText",
                            "style"
                        }
                    }
                }
            });

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("❌ Gemini API request failed: Status={Status}, Body={Body}", response.StatusCode, body);
                return (false, "", $"API returned {response.StatusCode}");
            }

            var payload = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken: cancellationToken);

            if (payload?.Candidates == null || payload.Candidates.Count == 0)
            {
                _logger.LogError("❌ No response from Gemini API");
                return (false, "", "Empty response from API");
            }

            var responseText = payload.Candidates[0].Content?.Parts?[0]?.Text ?? "";
            return (true, responseText, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unexpected error calling Gemini API");
            return (false, "", $"Request error: {ex.Message}");
        }
    }

    /// <summary>
    /// Parse and validate visual prompt JSON response
    /// </summary>
    private (bool IsValid, VisualPromptDto? VisualPrompt) ParseVisualPromptResponse(string responseText)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(responseText))
            {
                _logger.LogWarning("⚠️ Empty response from Gemini");
                return (false, null);
            }

            // Remove markdown code blocks if present
            var cleaned = responseText.Trim();
            if (cleaned.StartsWith("```json"))
            {
                cleaned = cleaned["```json".Length..];
            }
            if (cleaned.StartsWith("```"))
            {
                cleaned = cleaned["```".Length..];
            }
            if (cleaned.EndsWith("```"))
            {
                cleaned = cleaned[..^3];
            }
            cleaned = cleaned.Trim();

            // Extract JSON from response
            var jsonStartIdx = cleaned.IndexOf('{');
            var jsonEndIdx = cleaned.LastIndexOf('}');

            if (jsonStartIdx < 0 || jsonEndIdx < 0 || jsonStartIdx >= jsonEndIdx)
            {
                _logger.LogWarning("⚠️ No JSON object found in response: {Response}", cleaned[..Math.Min(200, cleaned.Length)]);
                return (false, null);
            }

            var jsonText = cleaned.Substring(jsonStartIdx, jsonEndIdx - jsonStartIdx + 1);
            _logger.LogDebug("📝 Extracted JSON: {Json}", jsonText[..Math.Min(300, jsonText.Length)]);

            // Parse JSON
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var visualPrompt = JsonSerializer.Deserialize<VisualPromptDto>(jsonText, options);

            if (visualPrompt == null)
            {
                _logger.LogWarning("⚠️ Failed to deserialize visual prompt");
                return (false, null);
            }

            // Validate required fields - use fallback values if missing
            if (string.IsNullOrWhiteSpace(visualPrompt.VisualTheme))
            {
                visualPrompt.VisualTheme = "Modern professional portfolio";
            }

            if (visualPrompt.MainElements == null || visualPrompt.MainElements.Count == 0)
            {
                visualPrompt.MainElements = new List<string> { "Skills", "Projects", "Expertise" };
            }

            if (visualPrompt.ColorPalette == null || visualPrompt.ColorPalette.Count == 0)
            {
                visualPrompt.ColorPalette = new List<string> { "#0F172A", "#2563EB", "#F8FAFC" };
            }

            if (string.IsNullOrWhiteSpace(visualPrompt.HeroText))
            {
                visualPrompt.HeroText = "Professional";
            }

            if (string.IsNullOrWhiteSpace(visualPrompt.Style))
            {
                visualPrompt.Style = "professional";
            }

            _logger.LogInformation("✅ Visual prompt validation passed");
            return (true, visualPrompt);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "❌ Failed to parse visual prompt JSON");
            return (false, null);
        }
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

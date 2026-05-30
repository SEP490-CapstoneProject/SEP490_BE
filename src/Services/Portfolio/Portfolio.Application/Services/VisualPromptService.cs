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
    private const int MaxOutputTokens = 400;
    private const float Temperature = 0.2f;
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
                _logger.LogWarning("⚠️ Invalid visual prompt response from Gemini, using deterministic prompt");
                var fallbackPrompt = BuildDeterministicVisualPrompt(previewJson, selectedTheme, recruiterPersona, avatarBase64);
                return (true, fallbackPrompt, null);
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

    private static VisualPromptDto BuildDeterministicVisualPrompt(
        string previewBriefJson,
        string selectedTheme,
        string? recruiterPersona,
        string? avatarBase64)
    {
        string title = "Professional Portfolio";
        string summary = "Professional portfolio";
        string specialization = "Professional portfolio showcase";
        string recentProjects = "Recent projects";
        string achievement = "Professional achievement";
        var skills = new List<string> { "C#", ".NET", "React" };

        try
        {
            using var jsonDoc = JsonDocument.Parse(previewBriefJson);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
            {
                title = Truncate(titleProp.GetString(), 80) ?? title;
            }

            if (root.TryGetProperty("summary", out var summaryProp) && summaryProp.ValueKind == JsonValueKind.String)
            {
                summary = Truncate(summaryProp.GetString(), 120) ?? summary;
            }

            if (root.TryGetProperty("specialization", out var specializationProp) && specializationProp.ValueKind == JsonValueKind.String)
            {
                specialization = Truncate(specializationProp.GetString(), 140) ?? specialization;
            }

            if (root.TryGetProperty("recentProjects", out var projectsProp) && projectsProp.ValueKind == JsonValueKind.String)
            {
                recentProjects = Truncate(projectsProp.GetString(), 140) ?? recentProjects;
            }

            if (root.TryGetProperty("achievement", out var achievementProp) && achievementProp.ValueKind == JsonValueKind.String)
            {
                achievement = Truncate(achievementProp.GetString(), 140) ?? achievement;
            }

            if (root.TryGetProperty("keySkills", out var skillsProp))
            {
                skills = ParseSkillsFromBrief(skillsProp);
            }
        }
        catch
        {
            // Keep defaults.
        }

        var mainElements = new List<string> { title };
        mainElements.AddRange(skills.Take(4));
        mainElements.Add(recentProjects);
        mainElements.Add(achievement);

        return new VisualPromptDto
        {
            VisualTheme = selectedTheme switch
            {
                "creative" => "Creative portfolio showcase",
                "minimal" => "Minimal professional showcase",
                "startup" => "Startup portfolio showcase",
                "corporate" => "Corporate portfolio showcase",
                _ => "Professional portfolio showcase"
            },
            MainElements = mainElements
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => Truncate(x, 80) ?? x)
                .Take(5)
                .ToList(),
            ColorPalette = new List<string> { "#0F172A", "#2563EB", "#F8FAFC" },
            HeroText = title,
            Style = $"clean editorial layout, {string.Join(", ", skills.Take(4))}. {specialization}. {summary}. {recruiterPersona ?? "general professional use"}"
        };
    }

    private static List<string> ParseSkillsFromBrief(JsonElement skillsElement)
    {
        if (skillsElement.ValueKind == JsonValueKind.Array)
        {
            return skillsElement.EnumerateArray()
                .Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() : x.ToString())
                .Select(x => Truncate(x, 40))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Cast<string>()
                .Take(5)
                .ToList();
        }

        if (skillsElement.ValueKind == JsonValueKind.String)
        {
            return SplitSkills(skillsElement.GetString());
        }

        return new List<string>();
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

        var avatarLine = string.IsNullOrEmpty(avatarBase64)
            ? "Avatar: none"
            : "Avatar: provided circular avatar reference (must match the portfolio source avatar)";

        return $@"You are a visual prompt engineer for portfolio previews.

Brief:
{previewJson}

Theme: {selectedTheme}
Audience: {personaDescription}
{avatarLine}

Rules:
- Output valid JSON only.
- Keep it compact and parseable.
- Use the same order every time.
- Must describe 4 zones only: top-left avatar, top-right name/title, center skills/technology badges, bottom achievement.
- Ensure the brief reflects the portfolio title, skill/tech stack, project summary, and achievement from the input.
- Use 3-5 main elements max.
- Keep the style professional and legible.

Return this JSON shape:
{{
  ""visualTheme"": ""..."",
  ""mainElements"": [""..."", ""..."", ""...""],
  ""colorPalette"": [""..."", ""..."", ""...""],
  ""heroText"": ""..."",
  ""style"": ""...""
}}";
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

            var cleaned = ExtractJsonText(responseText);
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                _logger.LogWarning("⚠️ No JSON object found in response: {Response}", responseText[..Math.Min(200, responseText.Length)]);
                return (false, null);
            }

            // Parse JSON
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var visualPrompt = JsonSerializer.Deserialize<VisualPromptDto>(cleaned, options);

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

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static List<string> SplitSkills(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new List<string>();
        }

        return value
            .Split(new[] { ',', ';', '/', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => Truncate(x, 40))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .Take(5)
            .ToList();
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

    private static string? ExtractJsonText(string responseText)
    {
        var cleaned = responseText.Trim();

        if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[7..];
        }
        else if (cleaned.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[3..];
        }

        if (cleaned.EndsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[..^3];
        }

        cleaned = cleaned.Trim();

        var start = cleaned.IndexOf('{');
        var end = cleaned.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return cleaned.Substring(start, end - start + 1);
        }

        return null;
    }

    #endregion
}

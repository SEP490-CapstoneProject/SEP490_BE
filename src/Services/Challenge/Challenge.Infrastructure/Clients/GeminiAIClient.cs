using System.Text.Json;
using System.Text.RegularExpressions;
using Challenge.Application.Clients;

namespace Challenge.Infrastructure.Clients;

public class GeminiAIClient : IGeminiAIClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiAIClient> _logger;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly int _timeoutSeconds;

    public GeminiAIClient(
        HttpClient httpClient,
        ILogger<GeminiAIClient> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["GoogleAI:ApiKey"] ?? throw new InvalidOperationException("GoogleAI:ApiKey not configured");
        _model = configuration["GoogleAI:Model"] ?? "gemini-3.1-flash-lite";
        _timeoutSeconds = int.TryParse(configuration["GoogleAI:TimeoutSeconds"], out var timeout) ? timeout : 30;
        _httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);
    }

    public async Task<ChallengeAnalysisResult> AnalyzeChallengeAsync(string description, string expectedSolution)
    {
        try
        {
            _logger.LogInformation("Starting Gemini challenge analysis with model {Model}", _model);

            var prompt = $@"Analyze this challenge and provide:
1. Difficulty level (1-10)
2. Required PROFESSIONAL COMPETENCIES (reusable skills) with weights
3. Evaluation criteria (measurable, specific to this challenge version)

=== CRITICAL SKILL GENERATION RULES ===
Generate ONLY canonical, reusable, measurable professional competencies.

A valid skill MUST be:
1. REUSABLE - works across many challenges (NOT challenge-specific)
2. MEASURABLE - can realistically be evaluated by AI
3. CANONICAL - use standard industry/professional naming
4. MID-LEVEL - not too broad (Programming) or too narrow (If Statement)
5. NO CHALLENGE-SPECIFIC WORDING - avoid scenario labels
6. SHORT NAMES - competency names, not sentences/explanations
7. NO SOFT LABELS - avoid vague non-measurable concepts

GOOD SKILLS (examples):
- C#
- ASP.NET Core
- SignalR
- Database Design
- REST API Design
- Authentication
- Cryptography
- Unit Testing
- Entity Framework Core

BAD SKILLS (DO NOT GENERATE):
- SecurityEngineering (use ""Web Security"")
- PasswordHashing (use ""Cryptography"")
- Persistence Mechanisms (use ""Database Design"")
- Factorial Understanding (use ""Recursion"")
- Chat App Logic (use ""SignalR"" or ""Real-time Communication"")
- Asp Net (use ""ASP.NET Core"")
- Good Coding (vague, non-measurable)

Challenge Description:
{description}

Expected Solution:
{expectedSolution}

Respond in this exact JSON format:
{{
  ""difficultyLevel"": <number 1-10>,
  ""difficultyLabel"": ""<Easy/Medium/Hard/Expert>"",
  ""skills"": {{""skillName"": <weight as number>, ...}},
  ""criteria"": [""criterion1"", ""criterion2"", ...]
}}";

            var response = await CallGeminiAsync(prompt);
            var result = ParseAnalysisResponse(response);
            
            _logger.LogInformation("Challenge analysis completed. Difficulty: {Difficulty}, Skills: {SkillCount}", 
                result.DifficultyLabel, result.SkillWeights?.Count ?? 0);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing challenge with Gemini");
            throw;
        }
    }

    public async Task<SubmissionGradingResult> GradeSubmissionAsync(
        string challengeDescription,
        List<string> criteria,
        string userSubmission)
    {
        try
        {
            _logger.LogInformation("Starting Gemini submission grading with model {Model}", _model);

            var criteriaList = string.Join("\n", criteria.Select((c, i) => $"{i + 1}. {c}"));
            
            var prompt = $@"Grade this code submission against the provided criteria. Score each criterion from 0-10.

Challenge:
{challengeDescription}

Evaluation Criteria:
{criteriaList}

User Submission:
{userSubmission}

Respond in this exact JSON format:
{{
  ""overallScore"": <0-10>,
  ""criteriaScores"": {{""criterion_name"": <0-10>, ...}},
  ""feedback"": ""Detailed feedback about the submission"",
  ""strengths"": [""strength1"", ""strength2""],
  ""improvements"": [""improvement1"", ""improvement2""]
}}";

            var response = await CallGeminiAsync(prompt);
            var result = ParseGradingResponse(response);
            
            _logger.LogInformation("Submission grading completed. Overall score: {Score}/10", result.OverallScore);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error grading submission with Gemini");
            throw;
        }
    }

    private async Task<string> CallGeminiAsync(string prompt)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
        
        var request = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.7,
                topP = 0.9,
                topK = 40,
                maxOutputTokens = 2048,
                responseMimeType = "application/json"
            },
            safetySettings = new[]
            {
                new
                {
                    category = "HARM_CATEGORY_HARASSMENT",
                    threshold = "BLOCK_MEDIUM_AND_ABOVE"
                },
                new
                {
                    category = "HARM_CATEGORY_HATE_SPEECH",
                    threshold = "BLOCK_MEDIUM_AND_ABOVE"
                }
            }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json");

        _logger.LogDebug("Calling Gemini API with model {Model}", _model);
        
        var response = await _httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        var responseText = await response.Content.ReadAsStringAsync();
        _logger.LogDebug("Gemini API response received");
        
        return responseText;
    }

    private ChallengeAnalysisResult ParseAnalysisResponse(string response)
    {
        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;
        var textContent = ExtractTextContent(root);
        var parsedJson = ExtractJsonObject(textContent);

        double difficultyLevel;
        string? difficultyLabel;
        var skillWeights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var criteria = new List<string>();

        if (!string.IsNullOrWhiteSpace(parsedJson))
        {
            using var doc2 = JsonDocument.Parse(parsedJson);
            var data = doc2.RootElement;

            difficultyLevel = data.GetProperty("difficultyLevel").GetDouble();
            difficultyLabel = data.GetProperty("difficultyLabel").GetString();

            foreach (var prop in data.GetProperty("skills").EnumerateObject())
            {
                skillWeights[prop.Name] = prop.Value.GetDouble();
            }

            foreach (var item in data.GetProperty("criteria").EnumerateArray())
            {
                criteria.Add(item.GetString() ?? "");
            }
        }
        else
        {
            difficultyLevel = ExtractNumber(textContent, @"(?im)(?:difficulty\s*level|difficulty)\s*[:=]\s*(\d+(?:\.\d+)?)", 5d);
            difficultyLabel = ExtractTextValue(textContent, @"(?im)(?:difficulty\s*label|label)\s*[:=]\s*([A-Za-z]+)") ?? "Medium";
            criteria = ExtractListItems(textContent, @"(?im)^(?:criteria|skills?)\s*[:=]\s*(.+)$");

            foreach (var item in criteria)
            {
                skillWeights[item] = 1d;
            }
        }

        return new ChallengeAnalysisResult
        {
            Difficulty = difficultyLevel,
            DifficultyLabel = difficultyLabel,
            SkillWeights = skillWeights,
            ExtractedCriteria = criteria,
            Analysis = textContent,
            ModelName = _model,
            PromptVersion = "v1.0"
        };
    }

    private SubmissionGradingResult ParseGradingResponse(string response)
    {
        using var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;
        var textContent = ExtractTextContent(root);
        var parsedJson = ExtractJsonObject(textContent);

        if (!string.IsNullOrWhiteSpace(parsedJson))
        {
            using var doc2 = JsonDocument.Parse(parsedJson);
            var data = doc2.RootElement;

            var criteriaScores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in data.GetProperty("criteriaScores").EnumerateObject())
            {
                criteriaScores[prop.Name] = prop.Value.GetDouble();
            }

            var strengths = ExtractStringArray(data, "strengths");
            var improvements = ExtractStringArray(data, "improvements");

            return new SubmissionGradingResult
            {
                OverallScore = data.GetProperty("overallScore").GetDouble(),
                CriteriaScores = criteriaScores,
                Feedback = data.GetProperty("feedback").GetString() ?? "",
                Strengths = strengths,
                Improvements = improvements,
                ModelName = _model,
                GradedAt = DateTime.UtcNow
            };
        }

        var fallbackCriteriaScores = ExtractCriteriaScores(textContent);
        var fallbackOverall = ExtractNumber(textContent, @"(?im)(?:overall\s*score|overall|score)\s*[:=]\s*(\d+(?:\.\d+)?)", 0d);
        var fallbackFeedback = ExtractFeedback(textContent);

        return new SubmissionGradingResult
        {
            OverallScore = fallbackOverall,
            CriteriaScores = fallbackCriteriaScores,
            Feedback = fallbackFeedback,
            Strengths = new List<string>(),
            Improvements = new List<string>(),
            ModelName = _model,
            GradedAt = DateTime.UtcNow
        };
    }

    private static string ExtractTextContent(JsonElement root)
    {
        if (root.TryGetProperty("candidates", out var candidates) && candidates.ValueKind == JsonValueKind.Array)
        {
            foreach (var candidate in candidates.EnumerateArray())
            {
                if (!candidate.TryGetProperty("content", out var content))
                {
                    continue;
                }

                if (!content.TryGetProperty("parts", out var parts) || parts.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                    {
                        var value = text.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            return value;
                        }
                    }
                }
            }
        }

        if (root.TryGetProperty("text", out var rootText) && rootText.ValueKind == JsonValueKind.String)
        {
            return rootText.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string? ExtractJsonObject(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return null;
        }

        return text.Substring(start, end - start + 1);
    }

    private static double ExtractNumber(string text, string pattern, double fallback)
    {
        var match = System.Text.RegularExpressions.Regex.Match(text ?? string.Empty, pattern);
        if (match.Success && double.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private static string? ExtractTextValue(string text, string pattern)
    {
        var match = System.Text.RegularExpressions.Regex.Match(text ?? string.Empty, pattern);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static List<string> ExtractListItems(string text, string pattern)
    {
        var match = System.Text.RegularExpressions.Regex.Match(text ?? string.Empty, pattern);
        if (!match.Success)
        {
            return new List<string>();
        }

        return match.Groups[1].Value
            .Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToList();
    }

    private static Dictionary<string, double> ExtractCriteriaScores(string text)
    {
        var scores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in System.Text.RegularExpressions.Regex.Matches(
                     text ?? string.Empty,
                     @"(?im)^\s*[-*]?\s*([A-Za-z0-9 &/().,+-]+?)\s*[:=]\s*(\d+(?:\.\d+)?)\s*$"))
        {
            if (double.TryParse(match.Groups[2].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var score))
            {
                scores[match.Groups[1].Value.Trim()] = score;
            }
        }

        return scores;
    }

    private static string ExtractFeedback(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var match = System.Text.RegularExpressions.Regex.Match(text, @"(?is)(?:feedback|commentary)\s*[:=]\s*(.+)$");
        return match.Success ? match.Groups[1].Value.Trim() : text.Trim();
    }

    private static List<string> ExtractStringArray(JsonElement data, string propertyName)
    {
        var values = new List<string>();
        if (!data.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.Array)
        {
            return values;
        }

        foreach (var item in element.EnumerateArray())
        {
            var value = item.GetString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value);
            }
        }

        return values;
    }
}

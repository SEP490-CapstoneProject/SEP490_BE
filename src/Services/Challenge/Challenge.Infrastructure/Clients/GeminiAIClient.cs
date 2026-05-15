using System.Text.Json;
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
        _model = configuration["GoogleAI:Model"] ?? "gemini-2.5-flash";
        _timeoutSeconds = int.TryParse(configuration["GoogleAI:TimeoutSeconds"], out var timeout) ? timeout : 30;
        _httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);
    }

    public async Task<ChallengeAnalysisResult> AnalyzeChallengeAsync(string description, string expectedSolution)
    {
        try
        {
            _logger.LogInformation("Starting Gemini challenge analysis with model {Model}", _model);

            var prompt = $@"Analyze this programming challenge and provide:
1. Difficulty level (1-10)
2. Required skills with weights (as JSON object)
3. Evaluation criteria (as JSON array)

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
                maxOutputTokens = 2048
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

        var textContent = root
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        var jsonMatch = System.Text.RegularExpressions.Regex.Match(textContent, @"\{[\s\S]*\}");
        if (!jsonMatch.Success)
            throw new InvalidOperationException("No JSON found in Gemini response");

        using var doc2 = JsonDocument.Parse(jsonMatch.Value);
        var data = doc2.RootElement;

        var difficultyLevel = data.GetProperty("difficultyLevel").GetInt32();
        var difficultyLabel = data.GetProperty("difficultyLabel").GetString();
        
        var skillWeights = new Dictionary<string, double>();
        foreach (var prop in data.GetProperty("skills").EnumerateObject())
        {
            skillWeights[prop.Name] = prop.Value.GetDouble();
        }

        var criteria = new List<string>();
        foreach (var item in data.GetProperty("criteria").EnumerateArray())
        {
            criteria.Add(item.GetString() ?? "");
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

        var textContent = root
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        var jsonMatch = System.Text.RegularExpressions.Regex.Match(textContent, @"\{[\s\S]*\}");
        if (!jsonMatch.Success)
            throw new InvalidOperationException("No JSON found in Gemini response");

        using var doc2 = JsonDocument.Parse(jsonMatch.Value);
        var data = doc2.RootElement;

        var criteriaScores = new Dictionary<string, double>();
        foreach (var prop in data.GetProperty("criteriaScores").EnumerateObject())
        {
            criteriaScores[prop.Name] = prop.Value.GetDouble();
        }

        var strengths = new List<string>();
        if (data.TryGetProperty("strengths", out var strengthsElement))
        {
            foreach (var item in strengthsElement.EnumerateArray())
            {
                strengths.Add(item.GetString() ?? "");
            }
        }

        var improvements = new List<string>();
        if (data.TryGetProperty("improvements", out var improvementsElement))
        {
            foreach (var item in improvementsElement.EnumerateArray())
            {
                improvements.Add(item.GetString() ?? "");
            }
        }

        return new SubmissionGradingResult
        {
            OverallScore = data.GetProperty("overallScore").GetInt32(),
            CriteriaScores = criteriaScores,
            Feedback = data.GetProperty("feedback").GetString() ?? "",
            Strengths = strengths,
            Improvements = improvements,
            ModelName = _model,
            GradedAt = DateTime.UtcNow
        };
    }
}

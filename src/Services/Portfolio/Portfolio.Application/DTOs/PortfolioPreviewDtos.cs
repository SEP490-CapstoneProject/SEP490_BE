using System.Text.Json.Serialization;

namespace Portfolio.Application.DTOs;

/// <summary>
/// Request DTO for generating portfolio preview
/// </summary>
public class PortfolioPreviewGenerateRequest
{
    [JsonPropertyName("highlightsDescription")]
    public string? HighlightsDescription { get; set; }

    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "professional";

    [JsonPropertyName("recruiterPersona")]
    public string? RecruiterPersona { get; set; }
}

/// <summary>
/// Visual prompt for image generation
/// </summary>
public class VisualPromptDto
{
    [JsonPropertyName("visualTheme")]
    public string? VisualTheme { get; set; }

    [JsonPropertyName("mainElements")]
    public List<string> MainElements { get; set; } = new();

    [JsonPropertyName("colorPalette")]
    public List<string> ColorPalette { get; set; } = new();

    [JsonPropertyName("heroText")]
    public string? HeroText { get; set; }

    [JsonPropertyName("style")]
    public string? Style { get; set; }
}

/// <summary>
/// Portfolio preview output DTO - matches requirement JSON structure
/// </summary>
public class PortfolioPreviewOutputDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("portfolioId")]
    public int PortfolioId { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("shortPreview")]
    public string? ShortPreview { get; set; }

    [JsonPropertyName("highlights")]
    public List<string> Highlights { get; set; } = new();

    [JsonPropertyName("recruiterSummary")]
    public string? RecruiterSummary { get; set; }

    [JsonPropertyName("socialCaption")]
    public string? SocialCaption { get; set; }

    [JsonPropertyName("visualStyleSuggestion")]
    public string? VisualStyleSuggestion { get; set; }

    [JsonPropertyName("visualPrompt")]
    public VisualPromptDto? VisualPrompt { get; set; }

    [JsonPropertyName("theme")]
    public string? Theme { get; set; }

    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("imageId")]
    public string? ImageId { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("regeneratedCount")]
    public int RegeneratedCount { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("generationModel")]
    public string? GenerationModel { get; set; }

    [JsonPropertyName("tokensUsed")]
    public int? TokensUsed { get; set; }

    [JsonPropertyName("imagegenModel")]
    public string? ImagegenModel { get; set; }
}

/// <summary>
/// Response DTO for generate preview API
/// </summary>
public class GeneratePreviewResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("preview")]
    public PortfolioPreviewOutputDto? Preview { get; set; }
}

/// <summary>
/// Response DTO for get preview API
/// </summary>
public class PortfolioPreviewResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("portfolioId")]
    public int PortfolioId { get; set; }

    [JsonPropertyName("previewJson")]
    public Dictionary<string, object>? PreviewJson { get; set; }

    [JsonPropertyName("highlightsDescription")]
    public string? HighlightsDescription { get; set; }

    [JsonPropertyName("visualPrompt")]
    public string? VisualPrompt { get; set; }

    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("imageId")]
    public string? ImageId { get; set; }

    [JsonPropertyName("recruiterSummary")]
    public string? RecruiterSummary { get; set; }

    [JsonPropertyName("selectedTheme")]
    public string? SelectedTheme { get; set; }

    [JsonPropertyName("socialCaption")]
    public string? SocialCaption { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("regeneratedCount")]
    public int RegeneratedCount { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("generationModel")]
    public string? GenerationModel { get; set; }

    [JsonPropertyName("tokensUsed")]
    public int? TokensUsed { get; set; }
}

/// <summary>
/// Enum for recruiter personas
/// </summary>
public enum RecruiterPersonaEnum
{
    FAANG,
    Startup,
    GameStudio,
    AICompany,
    Default
}

/// <summary>
/// Enum for preview styles
/// </summary>
public enum PortfolioPreviewStyle
{
    Professional,
    Creative,
    Minimal,
    Startup,
    Corporate,
    Cyberpunk
}

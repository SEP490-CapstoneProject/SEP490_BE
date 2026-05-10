namespace Portfolio.Domain.Entities;

public class PortfolioPreview
{
    public int Id { get; set; }
    public int PortfolioId { get; set; }
    
    // Preview content - JSON structure with title, skills, projects, achievements, etc.
    public string PreviewJson { get; set; } = "{}";
    
    // User-provided highlights or default description
    public string? HighlightsDescription { get; set; }
    
    // Version control
    public int Version { get; set; } = 1;
    public int RegeneratedCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Metadata
    public bool IsActive { get; set; } = true;
    public string GenerationModel { get; set; } = "gemini-1.5-pro";
    public int? TokensUsed { get; set; }
    
    // Navigation
    public Portfolio Portfolio { get; set; } = null!;
}

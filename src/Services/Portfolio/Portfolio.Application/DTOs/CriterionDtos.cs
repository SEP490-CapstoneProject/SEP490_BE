namespace Portfolio.Application.DTOs;

public class CriterionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateCriterionRequest
{
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
}

public class UpdateCriterionRequest
{
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
}

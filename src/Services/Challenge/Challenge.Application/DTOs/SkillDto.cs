namespace Challenge.Application.DTOs;

public class CreateSkillDto
{
    public string Name { get; set; }
    public Guid CategoryId { get; set; }
}

public class SkillDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Slug { get; set; }
    public bool IsSystem { get; set; }
    public bool IsApproved { get; set; }
}

public class SkillCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}

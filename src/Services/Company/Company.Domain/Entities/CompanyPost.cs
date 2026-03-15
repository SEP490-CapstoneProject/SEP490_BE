namespace Company.Domain.Entities;

public class CompanyPost
{
    public int PostId { get; set; }
    public int CompanyId { get; set; }
    public string Position { get; set; } = "";
    public string? Address { get; set; }
    public string? Salary { get; set; }
    public string? EmploymentType { get; set; }
    public int? ExperienceYear { get; set; }
    public int? Quantity { get; set; }
    public string? JobDescription { get; set; }
    public string? RequirementsMandatory { get; set; }
    public string? RequirementsPreferred { get; set; }
    public string? Benefits { get; set; }
    public string? CoverImageVideo { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Status { get; set; } = 1;

    public CompanyEntity? Company { get; set; }
    public ICollection<CompanyPostMedia> Media { get; set; } = new List<CompanyPostMedia>();
    public ICollection<CompanyPostSave> Saves { get; set; } = new List<CompanyPostSave>();
}

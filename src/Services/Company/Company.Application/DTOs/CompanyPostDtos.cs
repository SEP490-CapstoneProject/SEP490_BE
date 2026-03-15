namespace Company.Application.DTOs;

public class CompanyPostFeedDto
{
    public int PostId { get; set; }
    public string Position { get; set; } = "";
    public string? CompanyName { get; set; }
    public string? CompanyAvatar { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? MediaType { get; set; }
    public string? MediaUrl { get; set; }
    public string? Address { get; set; }
    public string? Salary { get; set; }
    public string? EmploymentType { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsSaved { get; set; }
}

public class CompanyPostDetailDto
{
    public int PostId { get; set; }
    public int CompanyId { get; set; }
    public string Position { get; set; } = "";
    public string? CompanyName { get; set; }
    public string? CompanyAvatar { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Address { get; set; }
    public string? Salary { get; set; }
    public string? EmploymentType { get; set; }
    public int? ExperienceYear { get; set; }
    public int? Quantity { get; set; }
    public string? JobDescription { get; set; }
    public string? RequirementsMandatory { get; set; }
    public string? RequirementsPreferred { get; set; }
    public string? Benefits { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Status { get; set; }
    public List<MediaItemDto> Media { get; set; } = new();
    public bool IsSaved { get; set; }
}

public class MediaItemDto
{
    public string? Type { get; set; }
    public string? Url { get; set; }
}

public class CursorPagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public DateTime? NextCursor { get; set; }
    public bool HasMore { get; set; }
}

public class CreatePostRequest
{
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
    public int Status { get; set; } = 1;
    public string? CoverImageKey { get; set; }
}

public class UpdatePostRequest
{
    public string? Position { get; set; }
    public string? Address { get; set; }
    public string? Salary { get; set; }
    public string? EmploymentType { get; set; }
    public int? ExperienceYear { get; set; }
    public int? Quantity { get; set; }
    public string? JobDescription { get; set; }
    public string? RequirementsMandatory { get; set; }
    public string? RequirementsPreferred { get; set; }
    public string? Benefits { get; set; }
    public int? Status { get; set; }
}

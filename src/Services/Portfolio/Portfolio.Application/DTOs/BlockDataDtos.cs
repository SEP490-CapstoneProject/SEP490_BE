namespace Portfolio.Application.DTOs;

// ─── Intro ────────────────────────────────────────────────────────────────────

public class IntroDataRequest
{
    public string? AvatarKey { get; set; }
    public string? Name { get; set; }
    public string? StudyField { get; set; }
    public string? Description { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public class IntroDataDto
{
    public int Id { get; set; }
    public string? Avatar { get; set; }
    public string? Name { get; set; }
    public string? StudyField { get; set; }
    public string? Description { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

// ─── Skill ────────────────────────────────────────────────────────────────────

public class SkillDataRequest
{
    public string Name { get; set; } = string.Empty;
}

public class SkillDataDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// ─── Education ────────────────────────────────────────────────────────────────

public class EducationDataRequest
{
    public string? SchoolName { get; set; }
    public string? Time { get; set; }
    public string? Department { get; set; }
    public string? Description { get; set; }
}

public class EducationDataDto
{
    public int Id { get; set; }
    public string? SchoolName { get; set; }
    public string? Time { get; set; }
    public string? Department { get; set; }
    public string? Description { get; set; }
}

// ─── Diploma ─────────────────────────────────────────────────────────────────

public class DiplomaDataRequest
{
    public string? Name { get; set; }
    public string? Provider { get; set; }
    public DateOnly? Date { get; set; }
    public string? Link { get; set; }
}

public class DiplomaDataDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Provider { get; set; }
    public DateOnly? Date { get; set; }
    public string? Link { get; set; }
}

// ─── Experience ───────────────────────────────────────────────────────────────

public class ExperienceDataRequest
{
    public string? JobName { get; set; }
    public string? Address { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public string? Description { get; set; }
}

public class ExperienceDataDto
{
    public int Id { get; set; }
    public string? JobName { get; set; }
    public string? Address { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public string? Description { get; set; }
}

// ─── Project ─────────────────────────────────────────────────────────────────

public class ProjectDataRequest
{
    public string? ImageKey { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Role { get; set; }
    public string? Technology { get; set; }
    public List<ProjectLinkRequest> Links { get; set; } = new();
}

public class ProjectLinkRequest
{
    public string? Type { get; set; }
    public string? Link { get; set; }
}

public class ProjectDataDto
{
    public int Id { get; set; }
    public string? Image { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Role { get; set; }
    public string? Technology { get; set; }
    public List<ProjectLinkDto> Links { get; set; } = new();
}

public class ProjectLinkDto
{
    public int Id { get; set; }
    public string? Type { get; set; }
    public string? Link { get; set; }
}

// ─── Award ───────────────────────────────────────────────────────────────────

public class AwardDataRequest
{
    public string? Name { get; set; }
    public DateOnly? Date { get; set; }
    public string? Organization { get; set; }
    public string? Description { get; set; }
}

public class AwardDataDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public DateOnly? Date { get; set; }
    public string? Organization { get; set; }
    public string? Description { get; set; }
}

// ─── Activities ───────────────────────────────────────────────────────────────

public class ActivitiesDataRequest
{
    public string? Name { get; set; }
    public DateOnly? Date { get; set; }
    public string? Description { get; set; }
}

public class ActivitiesDataDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public DateOnly? Date { get; set; }
    public string? Description { get; set; }
}

// ─── OtherInfo ────────────────────────────────────────────────────────────────

public class OtherInfoDataRequest
{
    public string? Detail { get; set; }
}

public class OtherInfoDataDto
{
    public int Id { get; set; }
    public string? Detail { get; set; }
}

// ─── Reference ───────────────────────────────────────────────────────────────

public class ReferenceDataRequest
{
    public string? Name { get; set; }
    public string? Position { get; set; }
    public string? Mail { get; set; }
    public string? Phone { get; set; }
}

public class ReferenceDataDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Position { get; set; }
    public string? Mail { get; set; }
    public string? Phone { get; set; }
}

// ─── Media ───────────────────────────────────────────────────────────────────

public class MediaUploadResponse
{
    public bool Success { get; set; }
    public string? Url { get; set; }
    public string? PublicId { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

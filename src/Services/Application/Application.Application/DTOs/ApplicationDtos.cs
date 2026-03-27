using Application.Domain.Entities;

namespace Application.Application.DTOs;

// Response DTOs
public class ApplicationDto
{
    public int ApplicationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string AppliedAt { get; set; } = string.Empty; // MM/YYYY format
    public PostDto Post { get; set; } = new();
    public CompanyDto Company { get; set; } = new();
}

public class ApplicationManagerDto
{
    public int ApplicationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; }
    public int PortfolioId { get; set; }
    public int? RoomId { get; set; }
    public CandidateDto Candidate { get; set; } = new();
    public PostDto Post { get; set; } = new();
}

// Request DTOs
public class CreateApplicationRequest
{
    public int CompanyPostId { get; set; }
    public int PortfolioId { get; set; }
}

public class UpdateApplicationStatusRequest
{
    public ApplicationStatus Status { get; set; }
}

// Nested DTOs
public class PostDto
{
    public int PostId { get; set; }
    public string Position { get; set; } = string.Empty;
    public string Salary { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Media { get; set; } = string.Empty;
}

public class CompanyDto
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Logo { get; set; } = string.Empty;
}

public class CandidateDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
}

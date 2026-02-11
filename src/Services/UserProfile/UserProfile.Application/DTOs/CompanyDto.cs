using Microsoft.AspNetCore.Http;

namespace UserProfile.Application.DTOs;

public class CompanyDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? ActivityField { get; set; }
    public string? CoverImage { get; set; }
    public string? Avatar { get; set; }
    public int? TaxIdentification { get; set; }
    public string? Address { get; set; }
    public string? Description { get; set; }
}

public class CreateCompanyRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string? ActivityField { get; set; }
    public int? TaxIdentification { get; set; }
    public string? Address { get; set; }
    public string? Description { get; set; }
    public IFormFile? Avatar { get; set; }
    public IFormFile? CoverImage { get; set; }
}

public class UpdateCompanyRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string? ActivityField { get; set; }
    public int? TaxIdentification { get; set; }
    public string? Address { get; set; }
    public string? Description { get; set; }
    public IFormFile? Avatar { get; set; }
    public IFormFile? CoverImage { get; set; }
}

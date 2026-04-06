using Microsoft.AspNetCore.Http;

namespace UserProfile.Application.DTOs;

public class EmployeeDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? Email { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreateAt { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? CoverImage { get; set; }
    public string? Avatar { get; set; }
}

public class CreateEmployeeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public IFormFile? Avatar { get; set; }
    public IFormFile? CoverImage { get; set; }
}

public class UpdateEmployeeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public IFormFile? Avatar { get; set; }
    public IFormFile? CoverImage { get; set; }
}

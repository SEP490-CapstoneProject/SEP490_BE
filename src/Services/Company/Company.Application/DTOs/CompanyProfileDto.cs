namespace Company.Application.DTOs;

public class CompanyProfileDto
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? Avatar { get; set; }
}

namespace Application.Application.DTOs;

public class EmployeeDto
{
    public int EmployeeId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string CoverImage { get; set; } = string.Empty;
}

public class CompanyExternalDto
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Logo { get; set; } = string.Empty;
}

public class CompanyPostDto
{
    public int PostId { get; set; }
    public int CompanyId { get; set; }
    public string Position { get; set; } = string.Empty;
    public string Salary { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Media { get; set; } = string.Empty;
}

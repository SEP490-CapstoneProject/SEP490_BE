namespace Company.Domain.Entities;

public class CompanyPostMedia
{
    public int Id { get; set; }
    public int CompanyPostId { get; set; }
    public string? Type { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }

    public CompanyPost? CompanyPost { get; set; }
}

namespace Company.Domain.Entities;

/// <summary>
/// Company profile — used for JOIN in feed/detail queries.
/// Managed separately; mapped read-only in Company Post queries.
/// </summary>
public class CompanyEntity
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? AvatarUrl { get; set; }

    public ICollection<CompanyPost> Posts { get; set; } = new List<CompanyPost>();
}

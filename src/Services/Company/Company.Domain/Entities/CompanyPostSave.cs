namespace Company.Domain.Entities;

public class CompanyPostSave
{
    public int Id { get; set; }
    public int CompanyPostId { get; set; }
    public int UserId { get; set; }

    public CompanyPost? CompanyPost { get; set; }
}

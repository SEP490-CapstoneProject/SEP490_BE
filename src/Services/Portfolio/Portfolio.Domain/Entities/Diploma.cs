namespace Portfolio.Domain.Entities;

public class Diploma
{
    public int Id { get; set; }
    public int PortfolioBlockId { get; set; }
    public string? Name { get; set; }
    public string? Provider { get; set; }
    public DateOnly? Date { get; set; }
    public string? Link { get; set; }

    public PortfolioBlock PortfolioBlock { get; set; } = null!;
}

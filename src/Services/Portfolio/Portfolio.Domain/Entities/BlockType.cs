namespace Portfolio.Domain.Entities;

public class BlockType
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public bool IsMultiple { get; set; } = true;
    public bool IsActive { get; set; } = true;

    public ICollection<PortfolioBlock> PortfolioBlocks { get; set; } = new List<PortfolioBlock>();
}

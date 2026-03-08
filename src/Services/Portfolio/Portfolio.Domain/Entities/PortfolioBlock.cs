namespace Portfolio.Domain.Entities;

public class PortfolioBlock
{
    public int Id { get; set; }
    public int PortfolioId { get; set; }
    public int BlockTypeId { get; set; }
    public string Variant { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public string DataJson { get; set; } = "{}";

    public Portfolio Portfolio { get; set; } = null!;
    public BlockType BlockType { get; set; } = null!;
}

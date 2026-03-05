using System.Text.Json;

namespace Portfolio.Application.DTOs;

public class CreatePortfolioRequest
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<PortfolioBlockRequest> Blocks { get; set; } = new();
}

public class PortfolioBlockRequest
{
    public string Type { get; set; } = string.Empty;
    public string Variant { get; set; } = string.Empty;
    public int Order { get; set; }
    public JsonElement Data { get; set; }
}

public class CreatePortfolioResponse
{
    public int PortfolioId { get; set; }
}


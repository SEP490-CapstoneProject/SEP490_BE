using System.Text.Json;

namespace Portfolio.Application.DTOs;

public class CreatePortfolioRequest
{
    public int EmployeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsMain { get; set; } = false;
    public bool IsPublic { get; set; } = false;
    public List<PortfolioBlockRequest> Blocks { get; set; } = new();
}

public class UpdateFullPortfolioRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Status { get; set; }
    public bool? IsMain { get; set; }
    public bool? IsPublic { get; set; }
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
    public string? Message { get; set; }
}


namespace Portfolio.Application.DTOs;

public class BlockTypeDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public bool IsMultiple { get; set; }
    public bool IsActive { get; set; }
}

public class CreateBlockTypeRequest
{
    public string Code { get; set; } = string.Empty;
    public bool IsMultiple { get; set; } = true;
}

public class UpdateBlockTypeRequest
{
    public string Code { get; set; } = string.Empty;
    public bool IsMultiple { get; set; }
}

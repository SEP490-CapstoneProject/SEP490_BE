namespace Portfolio.Domain.Entities;

public class Criterion
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;   // thể loại / style
    public bool IsActive { get; set; } = true;
}

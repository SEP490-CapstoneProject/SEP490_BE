namespace Portfolio.Domain.Entities;

public class PortfolioBlock
{
    public int Id { get; set; }
    public int PortfolioId { get; set; }
    public int BlockTypeId { get; set; }
    public string Variant { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;

    public Portfolio Portfolio { get; set; } = null!;
    public BlockType BlockType { get; set; } = null!;

    // Navigation properties to block content
    public Intro? Intro { get; set; }
    public ICollection<Skill> Skills { get; set; } = new List<Skill>();
    public ICollection<Education> Educations { get; set; } = new List<Education>();
    public ICollection<Diploma> Diplomas { get; set; } = new List<Diploma>();
    public ICollection<Experience> Experiences { get; set; } = new List<Experience>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<Award> Awards { get; set; } = new List<Award>();
    public ICollection<Activities> Activities { get; set; } = new List<Activities>();
    public ICollection<OtherInfo> OtherInfos { get; set; } = new List<OtherInfo>();
    public ICollection<Reference> References { get; set; } = new List<Reference>();
}

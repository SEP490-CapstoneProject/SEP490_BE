using Challenge.Domain.Enums;
namespace Challenge.Application.DTOs;


public class CreateChallengeDto
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string ExpectedSolution { get; set; }
    public DateTime Deadline { get; set; }
}

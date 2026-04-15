namespace Portfolio.Application.Interfaces;

public class ReviewerProfileDto
{
    public int UserId { get; set; }
    public string? Name { get; set; }
    public string? Avatar { get; set; }
}

public interface IReviewerProfileClient
{
    Task<Dictionary<int, ReviewerProfileDto>> GetCompanyProfilesByUserIdsAsync(IEnumerable<int> userIds);
    Task<Dictionary<int, ReviewerProfileDto>> GetExpertProfilesByUserIdsAsync(IEnumerable<int> userIds);
}

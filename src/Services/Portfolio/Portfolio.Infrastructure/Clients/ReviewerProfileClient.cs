using System.Net.Http.Json;
using Portfolio.Application.Interfaces;

namespace Portfolio.Infrastructure.Clients;

public class ReviewerProfileClient : IReviewerProfileClient
{
    private readonly HttpClient _http;

    public ReviewerProfileClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<Dictionary<int, ReviewerProfileDto>> GetCompanyProfilesByUserIdsAsync(IEnumerable<int> userIds)
    {
        return await GetProfilesByUserIdsAsync("/api/company/batch", userIds);
    }

    public async Task<Dictionary<int, ReviewerProfileDto>> GetExpertProfilesByUserIdsAsync(IEnumerable<int> userIds)
    {
        return await GetProfilesByUserIdsAsync("/api/expert/batch", userIds);
    }

    private async Task<Dictionary<int, ReviewerProfileDto>> GetProfilesByUserIdsAsync(string endpoint, IEnumerable<int> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, ReviewerProfileDto>();
        }

        var idsParam = string.Join(",", ids);
        var response = await _http.GetAsync($"{endpoint}?userIds={Uri.EscapeDataString(idsParam)}");
        if (!response.IsSuccessStatusCode)
        {
            return new Dictionary<int, ReviewerProfileDto>();
        }

        var payload = await response.Content.ReadFromJsonAsync<List<ReviewerProfileResponseDto>>()
            ?? new List<ReviewerProfileResponseDto>();

        return payload
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => new ReviewerProfileDto
                {
                    UserId = g.Key,
                    Name = g.First().Name ?? g.First().CompanyName,
                    Avatar = g.First().Avatar
                });
    }

    private sealed class ReviewerProfileResponseDto
    {
        public int UserId { get; set; }
        public string? Name { get; set; }
        public string? CompanyName { get; set; }
        public string? Avatar { get; set; }
    }
}

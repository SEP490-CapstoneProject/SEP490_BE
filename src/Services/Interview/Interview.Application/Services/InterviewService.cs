using System.Net.Http.Json;
using Interview.Application.DTOs;
using Interview.Application.Interfaces;
using InterviewEntity = Interview.Domain.Entities.Interview;

namespace Interview.Application.Services;

public class InterviewService : IInterviewService
{
    private readonly IInterviewRepository _repo;
    private readonly IHttpClientFactory _httpFactory;

    public InterviewService(IInterviewRepository repo, IHttpClientFactory httpFactory)
    {
        _repo = repo;
        _httpFactory = httpFactory;
    }

    public async Task<InterviewEntity> CreateInterviewAsync(InterviewEntity interview)
    {
        interview.Id = 0;
        return await _repo.CreateAsync(interview);
    }

    public async Task DeleteInterviewAsync(int id)
    {
        await _repo.DeleteAsync(id);
    }

    public async Task<InterviewItemDto?> GetInterviewByIdAsync(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) return null;
        return await MapToDto(entity);
    }

    public async Task<IEnumerable<InterviewItemDto>> GetAllByUserIdAsync(int userId)
    {
        var list = await _repo.GetAllByUserIdAsync(userId);
        return await MapToDtos(list);
    }

    public async Task<IEnumerable<InterviewItemDto>> GetByDateAsync(int userId, DateTime date)
    {
        var list = await _repo.GetByDateAsync(userId, date);
        return await MapToDtos(list);
    }

    public async Task<IEnumerable<InterviewItemDto>> GetByStatusAsync(int userId, string status)
    {
        var list = await _repo.GetByStatusAsync(userId, status);
        return await MapToDtos(list);
    }

    public async Task UpdateInterviewAsync(InterviewEntity interview)
    {
        await _repo.UpdateAsync(interview);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<IEnumerable<InterviewItemDto>> MapToDtos(IEnumerable<InterviewEntity> entities)
    {
        var result = new List<InterviewItemDto>();
        foreach (var e in entities)
            result.Add(await MapToDto(e));
        return result;
    }

    private async Task<InterviewItemDto> MapToDto(InterviewEntity e)
    {
        var candidate = await FetchCandidate(e.UserId);

        return new InterviewItemDto
        {
            InterviewId = e.Id,
            Date = e.Date.ToString("yyyy-MM-dd"),
            Time = e.Time.ToString(@"hh\:mm"),
            Candidate = candidate,
            Post = new PostDto
            {
                PostId = e.PostId,
                Position = e.PostPosition
            },
            Round = e.Round,
            Type = e.Type,
            Platform = string.IsNullOrEmpty(e.Platform) ? null : e.Platform,
            Link = string.IsNullOrEmpty(e.Link) ? null : e.Link,
            Building = string.IsNullOrEmpty(e.Building) ? null : e.Building,
            Room = string.IsNullOrEmpty(e.Room) ? null : e.Room,
            Status = e.Status,
            InterviewerName = e.InterviewerName
        };
    }

    private async Task<CandidateDto> FetchCandidate(int userId)
    {
        try
        {
            var client = _httpFactory.CreateClient("userprofile");
            var response = await client.GetFromJsonAsync<UserProfileResponse>(
                $"/api/employee/by-user/{userId}");

            if (response != null)
            {
                return new CandidateDto
                {
                    UserId = userId,
                    Name = response.Name,
                    Avatar = response.Avatar ?? string.Empty,
                    CoverImage = response.CoverImage ?? string.Empty
                };
            }
        }
        catch
        {
            // If UserProfile service is unavailable, return minimal info
        }

        return new CandidateDto { UserId = userId };
    }

    // Internal model for deserializing UserProfile HTTP response
    private sealed class UserProfileResponse
    {
        public string Name { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public string? CoverImage { get; set; }
    }
}


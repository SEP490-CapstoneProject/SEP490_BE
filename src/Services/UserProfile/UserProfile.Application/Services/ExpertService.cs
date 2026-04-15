using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using UserProfile.Application.DTOs;
using UserProfile.Application.Interfaces;
using UserProfile.Domain.Entities;

namespace UserProfile.Application.Services;

public class ExpertService : IExpertService
{
    private readonly IExpertRepository _repository;
    private readonly IAuthUserClient _authUserClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExpertService> _logger;

    public ExpertService(
        IExpertRepository repository,
        IAuthUserClient authUserClient,
        IHttpClientFactory httpClientFactory,
        ILogger<ExpertService> logger)
    {
        _repository = repository;
        _authUserClient = authUserClient;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ExpertDto?> GetByIdAsync(int id)
    {
        var expert = await _repository.GetByIdAsync(id);
        if (expert == null) return null;

        var user = await _authUserClient.GetUserByIdAsync(expert.UserId);
        if (user == null)
        {
            throw new KeyNotFoundException($"Auth user {expert.UserId} not found");
        }

        return MapToDto(expert, user);
    }

    public async Task<ExpertDto?> GetByUserIdAsync(int userId)
    {
        var expert = await _repository.GetByUserIdAsync(userId);
        if (expert == null) return null;

        var user = await _authUserClient.GetUserByIdAsync(expert.UserId);
        if (user == null)
        {
            throw new KeyNotFoundException($"Auth user {expert.UserId} not found");
        }

        return MapToDto(expert, user);
    }

    public async Task<IEnumerable<ExpertDto>> GetAllAsync()
    {
        var experts = await _repository.GetAllAsync();
        var authUsers = await _authUserClient.GetUsersByIdsAsync(experts.Select(e => e.UserId));

        return experts.Select(e =>
        {
            if (!authUsers.TryGetValue(e.UserId, out var authUser))
            {
                throw new KeyNotFoundException($"Auth user {e.UserId} not found");
            }

            return MapToDto(e, authUser);
        });
    }

    public async Task<ExpertDto> CreateAsync(int userId, CreateExpertRequest request, IFormFile? avatar, IFormFile? coverImage)
    {
        if (await _repository.ExistsByUserIdAsync(userId))
        {
            throw new InvalidOperationException($"Expert profile already exists for user {userId}");
        }

        var expert = new Expert
        {
            UserId = userId,
            Name = request.Name,
            Phone = request.Phone ?? string.Empty
        };

        if (avatar != null)
        {
            expert.Avatar = await UploadToMediaServiceAsync(avatar, "experts/avatars");
        }

        if (coverImage != null)
        {
            expert.CoverImage = await UploadToMediaServiceAsync(coverImage, "experts/covers");
        }

        var created = await _repository.CreateAsync(expert);
        var createdUser = await _authUserClient.GetUserByIdAsync(created.UserId)
            ?? throw new KeyNotFoundException($"Auth user {created.UserId} not found");
        return MapToDto(created, createdUser);
    }

    public async Task<ExpertDto> UpdateAsync(int id, UpdateExpertRequest request, IFormFile? avatar, IFormFile? coverImage)
    {
        var expert = await _repository.GetByIdAsync(id);
        if (expert == null)
        {
            throw new KeyNotFoundException($"Expert with ID {id} not found");
        }

        expert.Name = request.Name;
        expert.Phone = request.Phone ?? string.Empty;

        if (avatar != null)
        {
            expert.Avatar = await UploadToMediaServiceAsync(avatar, "experts/avatars");
        }

        if (coverImage != null)
        {
            expert.CoverImage = await UploadToMediaServiceAsync(coverImage, "experts/covers");
        }

        var updated = await _repository.UpdateAsync(expert);
        var updatedUser = await _authUserClient.GetUserByIdAsync(updated.UserId)
            ?? throw new KeyNotFoundException($"Auth user {updated.UserId} not found");
        return MapToDto(updated, updatedUser);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _repository.DeleteAsync(id);
    }

    private async Task<string> UploadToMediaServiceAsync(IFormFile file, string folder)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("MediaService");
            var formData = new MultipartFormDataContent();

            var fileContent = new StreamContent(file.OpenReadStream());
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            formData.Add(fileContent, "file", file.FileName);

            var endpoint = file.ContentType.StartsWith("video/")
                ? $"/api/upload/video?folder={Uri.EscapeDataString(folder)}"
                : $"/api/upload/image?folder={Uri.EscapeDataString(folder)}";

            var response = await client.PostAsync(endpoint, formData);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<MediaUploadResponse>();
                return result?.Url ?? string.Empty;
            }

            _logger.LogWarning("Media upload failed: {StatusCode}", response.StatusCode);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading media to Media Service");
            return string.Empty;
        }
    }

    private static ExpertDto MapToDto(Expert expert, AuthUserInfoDto authUser)
    {
        return new ExpertDto
        {
            Id = expert.Id,
            UserId = expert.UserId,
            Email = authUser.Email,
            Status = authUser.Status,
            CreateAt = authUser.CreateAt,
            Name = expert.Name,
            Phone = expert.Phone,
            CoverImage = expert.CoverImage,
            Avatar = expert.Avatar
        };
    }
}

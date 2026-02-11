using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using UserProfile.Application.DTOs;
using UserProfile.Application.Interfaces;
using UserProfile.Domain.Entities;

namespace UserProfile.Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _repository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(
        IEmployeeRepository repository,
        IHttpClientFactory httpClientFactory,
        ILogger<EmployeeService> logger)
    {
        _repository = repository;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<EmployeeDto?> GetByIdAsync(int id)
    {
        var employee = await _repository.GetByIdAsync(id);
        return employee == null ? null : MapToDto(employee);
    }

    public async Task<EmployeeDto?> GetByUserIdAsync(int userId)
    {
        var employee = await _repository.GetByUserIdAsync(userId);
        return employee == null ? null : MapToDto(employee);
    }

    public async Task<IEnumerable<EmployeeDto>> GetAllAsync()
    {
        var employees = await _repository.GetAllAsync();
        return employees.Select(MapToDto);
    }

    public async Task<EmployeeDto> CreateAsync(int userId, CreateEmployeeRequest request, IFormFile? avatar, IFormFile? coverImage)
    {
        // Check if employee already exists for this user
        if (await _repository.ExistsByUserIdAsync(userId))
        {
            throw new InvalidOperationException($"Employee profile already exists for user {userId}");
        }

        var employee = new Employee
        {
            UserId = userId,
            Name = request.Name,
            Phone = request.Phone ?? string.Empty
        };

        // Upload avatar if provided
        if (avatar != null)
        {
            employee.Avatar = await UploadToMediaServiceAsync(avatar, "profiles/avatars");
        }

        // Upload cover image if provided
        if (coverImage != null)
        {
            employee.CoverImage = await UploadToMediaServiceAsync(coverImage, "profiles/covers");
        }

        var created = await _repository.CreateAsync(employee);
        return MapToDto(created);
    }

    public async Task<EmployeeDto> UpdateAsync(int id, UpdateEmployeeRequest request, IFormFile? avatar, IFormFile? coverImage)
    {
        var employee = await _repository.GetByIdAsync(id);
        if (employee == null)
        {
            throw new KeyNotFoundException($"Employee with ID {id} not found");
        }

        employee.Name = request.Name;
        employee.Phone = request.Phone ?? string.Empty;

        // Upload new avatar if provided
        if (avatar != null)
        {
            employee.Avatar = await UploadToMediaServiceAsync(avatar, "profiles/avatars");
        }

        // Upload new cover image if provided
        if (coverImage != null)
        {
            employee.CoverImage = await UploadToMediaServiceAsync(coverImage, "profiles/covers");
        }

        var updated = await _repository.UpdateAsync(employee);
        return MapToDto(updated);
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

    private static EmployeeDto MapToDto(Employee employee)
    {
        return new EmployeeDto
        {
            Id = employee.Id,
            UserId = employee.UserId,
            Name = employee.Name,
            Phone = employee.Phone,
            CoverImage = employee.CoverImage,
            Avatar = employee.Avatar
        };
    }
}

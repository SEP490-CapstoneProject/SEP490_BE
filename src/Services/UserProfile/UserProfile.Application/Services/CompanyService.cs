using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using UserProfile.Application.DTOs;
using UserProfile.Application.Interfaces;
using UserProfile.Domain.Entities;

namespace UserProfile.Application.Services;

public class CompanyService : ICompanyService
{
    private readonly ICompanyRepository _repository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CompanyService> _logger;

    public CompanyService(
        ICompanyRepository repository,
        IHttpClientFactory httpClientFactory,
        ILogger<CompanyService> logger)
    {
        _repository = repository;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<CompanyDto?> GetByIdAsync(int id)
    {
        var company = await _repository.GetByIdAsync(id);
        return company == null ? null : MapToDto(company);
    }

    public async Task<CompanyDto?> GetByUserIdAsync(int userId)
    {
        var company = await _repository.GetByUserIdAsync(userId);
        return company == null ? null : MapToDto(company);
    }

    public async Task<IEnumerable<CompanyDto>> GetAllAsync()
    {
        var companies = await _repository.GetAllAsync();
        return companies.Select(MapToDto);
    }

    public async Task<CompanyDto> CreateAsync(int userId, CreateCompanyRequest request, IFormFile? avatar, IFormFile? coverImage)
    {
        // Check if company already exists for this user
        if (await _repository.ExistsByUserIdAsync(userId))
        {
            throw new InvalidOperationException($"Company profile already exists for user {userId}");
        }

        var company = new Company
        {
            UserId = userId,
            CompanyName = request.CompanyName,
            ActivityField = request.ActivityField,
            TaxIdentification = request.TaxIdentification,
            Address = request.Address,
            Description = request.Description
        };

        // Upload avatar if provided
        if (avatar != null)
        {
            company.Avatar = await UploadToMediaServiceAsync(avatar, "companies/avatars");
        }

        // Upload cover image if provided
        if (coverImage != null)
        {
            company.CoverImage = await UploadToMediaServiceAsync(coverImage, "companies/covers");
        }

        var created = await _repository.CreateAsync(company);
        return MapToDto(created);
    }

    public async Task<CompanyDto> UpdateAsync(int id, UpdateCompanyRequest request, IFormFile? avatar, IFormFile? coverImage)
    {
        var company = await _repository.GetByIdAsync(id);
        if (company == null)
        {
            throw new KeyNotFoundException($"Company with ID {id} not found");
        }

        company.CompanyName = request.CompanyName;
        company.ActivityField = request.ActivityField;
        company.TaxIdentification = request.TaxIdentification;
        company.Address = request.Address;
        company.Description = request.Description;

        // Upload new avatar if provided
        if (avatar != null)
        {
            company.Avatar = await UploadToMediaServiceAsync(avatar, "companies/avatars");
        }

        // Upload new cover image if provided
        if (coverImage != null)
        {
            company.CoverImage = await UploadToMediaServiceAsync(coverImage, "companies/covers");
        }

        var updated = await _repository.UpdateAsync(company);
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

    private static CompanyDto MapToDto(Company company)
    {
        return new CompanyDto
        {
            Id = company.Id,
            UserId = company.UserId,
            CompanyName = company.CompanyName,
            ActivityField = company.ActivityField,
            CoverImage = company.CoverImage,
            Avatar = company.Avatar,
            TaxIdentification = company.TaxIdentification,
            Address = company.Address,
            Description = company.Description
        };
    }
}

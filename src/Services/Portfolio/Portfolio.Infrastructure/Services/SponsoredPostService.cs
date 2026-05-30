using Microsoft.Extensions.Logging;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Data;

namespace Portfolio.Infrastructure.Services;

public class SponsoredPostService : ISponsoredPostService
{
    private readonly ISponsoredPostRepository _repository;
    private readonly IRewardPointsService _pointsService;
    private readonly ICurrentUserService _currentUser;
    private readonly PortfolioDbContext _db;
    private readonly ILogger<SponsoredPostService> _logger;

    // Point to day conversion rates
    private const decimal PointsPerDay = 5m;
    private const decimal ThreeDayPoints = 12m;
    private const int ThreeDayDuration = 3;

    public SponsoredPostService(
        ISponsoredPostRepository repository,
        IRewardPointsService pointsService,
        ICurrentUserService currentUser,
        PortfolioDbContext db,
        ILogger<SponsoredPostService> logger)
    {
        _repository = repository;
        _pointsService = pointsService;
        _currentUser = currentUser;
        _db = db;
        _logger = logger;
    }

    public async Task<SponsoredPostDto> CreateSponsoredPostAsync(int recruiterId, CreateSponsoredPostRequest request)
    {
        // Validate content type
        if (!Enum.TryParse<SponsoredContentType>(request.ContentType, ignoreCase: true, out var contentType))
            throw new ArgumentException($"Invalid content type: {request.ContentType}. Must be Text, Image, or Video.");

        // Validate content based on type
        ValidateContent(contentType, request);

        // Validate points amount
        if (request.PointsToSpend <= 0)
            throw new ArgumentException("Points to spend must be greater than 0.");

        // Calculate duration from points
        var durationDays = CalculateDurationDays(request.PointsToSpend);

        // Check if recruiter has sufficient points
        var canSpend = await _pointsService.CanSpendPointsAsync(recruiterId, request.PointsToSpend);
        if (!canSpend)
            throw new InvalidOperationException($"Insufficient points. Required: {request.PointsToSpend}");

        // Create the sponsored post
        var now = DateTime.UtcNow;
        var post = new SponsoredPost
        {
            CreatedBy = recruiterId,
            ContentType = contentType,
            TextContent = request.TextContent,
            ImageUrl = request.ImageUrl,
            VideoUrl = request.VideoUrl,
            PointsSpent = request.PointsToSpend,
            DurationDays = durationDays,
            StartDate = now,
            ExpiryDate = now.AddDays(durationDays),
            Status = SponsoredPostStatus.Active,
            ClickThroughUrl = request.ClickThroughUrl,
            CreatedAt = now
        };

        // Spend the points
        var sourceId = Guid.NewGuid().ToString();
        var pointsSpent = await _pointsService.SpendPointsAsync(
            userId: recruiterId,
            points: request.PointsToSpend,
            sourceType: "SponsoredFeed",
            sourceId: sourceId);

        if (!pointsSpent)
        {
            throw new InvalidOperationException("Failed to spend points. Please try again.");
        }

        // Save sponsored post
        var created = await _repository.CreateAsync(post);

        _logger.LogInformation(
            "Sponsored post {PostId} created by recruiter {RecruiterId} using {Points} points for {Days} days",
            created.Id, recruiterId, request.PointsToSpend, durationDays);

        return MapToDto(created);
    }

    public async Task<SponsoredPostDto?> GetPostAsync(int postId)
    {
        var post = await _repository.GetByIdAsync(postId);
        return post != null ? MapToDto(post) : null;
    }

    public async Task<List<SponsoredPostDto>> GetActivePostsAsync(int skip = 0, int take = 20)
    {
        var posts = await _repository.GetActivePostsPagedAsync(skip, take);
        return posts.Select(MapToDto).ToList();
    }

    public async Task<List<SponsoredPostDto>> GetRecruiterPostsAsync(int recruiterId)
    {
        var posts = await _repository.GetByCreatorAsync(recruiterId);
        return posts.Select(MapToDto).ToList();
    }

    public async Task DeletePostAsync(int postId, int userId, bool isAdmin)
    {
        var post = await _repository.GetByIdAsync(postId)
            ?? throw new KeyNotFoundException($"Post {postId} not found.");

        if (!isAdmin && post.CreatedBy != userId)
            throw new UnauthorizedAccessException("You can only delete your own sponsored posts.");

        await _repository.DeleteAsync(postId);
        _logger.LogInformation("Sponsored post {PostId} deleted by user {UserId}", postId, userId);
    }

    public async Task PausePostAsync(int postId, int userId, bool isAdmin)
    {
        var post = await _repository.GetByIdAsync(postId)
            ?? throw new KeyNotFoundException($"Post {postId} not found.");

        if (!isAdmin && post.CreatedBy != userId)
            throw new UnauthorizedAccessException("You can only pause your own sponsored posts.");

        if (post.Status == SponsoredPostStatus.Paused)
            throw new InvalidOperationException("Post is already paused.");

        post.Status = SponsoredPostStatus.Paused;
        await _repository.UpdateAsync(post);

        _logger.LogInformation("Sponsored post {PostId} paused by user {UserId}", postId, userId);
    }

    public async Task ResumePostAsync(int postId, int userId, bool isAdmin)
    {
        var post = await _repository.GetByIdAsync(postId)
            ?? throw new KeyNotFoundException($"Post {postId} not found.");

        if (!isAdmin && post.CreatedBy != userId)
            throw new UnauthorizedAccessException("You can only resume your own sponsored posts.");

        if (post.Status != SponsoredPostStatus.Paused)
            throw new InvalidOperationException("Post is not paused.");

        if (post.ExpiryDate < DateTime.UtcNow)
            throw new InvalidOperationException("Cannot resume expired posts.");

        post.Status = SponsoredPostStatus.Active;
        await _repository.UpdateAsync(post);

        _logger.LogInformation("Sponsored post {PostId} resumed by user {UserId}", postId, userId);
    }

    public async Task RecordViewAsync(int postId)
    {
        var post = await _repository.GetByIdAsync(postId);
        if (post != null)
        {
            post.ViewCount++;
            post.CurrentImpression++;
            await _repository.UpdateAsync(post);
        }
    }

    public async Task RecordClickAsync(int postId)
    {
        var post = await _repository.GetByIdAsync(postId);
        if (post != null)
        {
            post.ClickCount++;
            post.CurrentClick++;
            await _repository.UpdateAsync(post);
        }
    }

    private void ValidateContent(SponsoredContentType contentType, CreateSponsoredPostRequest request)
    {
        switch (contentType)
        {
            case SponsoredContentType.Text:
                if (string.IsNullOrWhiteSpace(request.TextContent))
                    throw new ArgumentException("Text content is required for Text type posts.");
                if (request.TextContent.Length < 10)
                    throw new ArgumentException("Text content must be at least 10 characters.");
                if (request.TextContent.Length > 500)
                    throw new ArgumentException("Text content must not exceed 500 characters.");
                break;

            case SponsoredContentType.Image:
                if (string.IsNullOrWhiteSpace(request.ImageUrl))
                    throw new ArgumentException("Image URL is required for Image type posts.");
                if (!IsValidUrl(request.ImageUrl))
                    throw new ArgumentException("Invalid image URL format.");
                break;

            case SponsoredContentType.Video:
                if (string.IsNullOrWhiteSpace(request.VideoUrl))
                    throw new ArgumentException("Video URL is required for Video type posts.");
                if (!IsValidUrl(request.VideoUrl))
                    throw new ArgumentException("Invalid video URL format.");
                break;

            default:
                throw new ArgumentException($"Unsupported content type: {contentType}");
        }
    }

    private bool IsValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private int CalculateDurationDays(decimal points)
    {
        // 5 points = 1 day
        // 12 points = 3 days (better rate)
        // Custom: calculate as points / 5 days

        if (points == ThreeDayPoints)
            return ThreeDayDuration;

        return (int)Math.Ceiling(points / PointsPerDay);
    }

    private static SponsoredPostDto MapToDto(SponsoredPost post) => new()
    {
        Id = post.Id,
        CreatedBy = post.CreatedBy,
        ContentType = post.ContentType.ToString(),
        TextContent = post.TextContent,
        ImageUrl = post.ImageUrl,
        VideoUrl = post.VideoUrl,
        PointsSpent = post.PointsSpent,
        DurationDays = post.DurationDays,
        StartDate = post.StartDate,
        ExpiryDate = post.ExpiryDate,
        Status = post.Status.ToString(),
        ClickThroughUrl = post.ClickThroughUrl,
        ViewCount = post.ViewCount,
        ClickCount = post.ClickCount,
        CreatedAt = post.CreatedAt,
        UpdatedAt = post.UpdatedAt
    };
}

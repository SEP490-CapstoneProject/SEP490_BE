using Community.Application.DTOs;
using Community.Application.Interfaces;
using Community.Application.Services;
using Community.Domain.Entities;
using Community.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RecruitmentPlatform.AI.Services;
using Xunit;

namespace Community.Tests;

public class CommunityPostIntegrationTests
{
    private readonly CommunityService _communityService;
    private readonly CommunityDbContext _dbContext;
    private readonly Mock<ICommunityRepository> _repositoryMock;
    private readonly Mock<IUserInfoClient> _userInfoClientMock;
    private readonly Mock<IPortfolioPreviewClient> _portfolioPreviewClientMock;
    private readonly Mock<IMediaUploadClient> _mediaUploadClientMock;
    private readonly Mock<ICommunityEventPublisher> _eventPublisherMock;
    private readonly Mock<INotificationEventPublisher> _notificationPublisherMock;
    private readonly Mock<ILogger<CommunityService>> _loggerMock;

    public CommunityPostIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<CommunityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new CommunityDbContext(options);

        _repositoryMock = new Mock<ICommunityRepository>();
        _userInfoClientMock = new Mock<IUserInfoClient>();
        _portfolioPreviewClientMock = new Mock<IPortfolioPreviewClient>();
        _mediaUploadClientMock = new Mock<IMediaUploadClient>();
        _eventPublisherMock = new Mock<ICommunityEventPublisher>();
        _notificationPublisherMock = new Mock<INotificationEventPublisher>();
        _loggerMock = new Mock<ILogger<CommunityService>>();

        var moderationService = new ModerationService();

        _communityService = new CommunityService(
            _repositoryMock.Object,
            _userInfoClientMock.Object,
            _portfolioPreviewClientMock.Object,
            _mediaUploadClientMock.Object,
            _eventPublisherMock.Object,
            _notificationPublisherMock.Object,
            moderationService,
            _loggerMock.Object
        );
    }

    #region Ban Word and Malicious Link Tests

    [Fact]
    public async Task CreatePost_WithBanWord_ThrowsException()
    {
        // Arrange
        var request = new CreatePostRequest
        {
            Description = "Great casino offers for everyone. Join now!",
            Status = 1,
            PortfolioId = null
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(
            () => _communityService.CreatePostAsync(request, 1, new Dictionary<string, IFormFile>())
        );
        Assert.Contains("casino", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreatePost_WithMultipleBanWords_ThrowsException()
    {
        // Arrange
        var request = new CreatePostRequest
        {
            Description = "Spam content with viagra and xxx links everywhere",
            Status = 1,
            PortfolioId = null
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(
            () => _communityService.CreatePostAsync(request, 1, new Dictionary<string, IFormFile>())
        );
        Assert.NotNull(exception.Message);
    }

    [Fact]
    public async Task CreatePost_WithMaliciousLink_ThrowsException()
    {
        // Arrange
        var request = new CreatePostRequest
        {
            Description = "Check this out https://bit.ly/xyz for important information about development",
            Status = 1,
            PortfolioId = null
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(
            () => _communityService.CreatePostAsync(request, 1, new Dictionary<string, IFormFile>())
        );
        Assert.Contains("bit.ly", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Approved Post Tests

    [Fact]
    public async Task CreatePost_WithGoodContent_ReturnsApproved()
    {
        // Arrange
        var request = new CreatePostRequest
        {
            Description = "I'm excited to share my latest project. This portfolio demonstrates my skills in web development and system design. I've worked with React and Node.js.",
            Status = 1,
            PortfolioId = null
        };

        var createdPost = new CommunityPost
        {
            Id = 1,
            UserId = 1,
            Description = request.Description,
            Status = request.Status,
            ReviewStatus = CommunityPost.StatusActive,
            CreatedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.CreatePostAsync(It.IsAny<CommunityPost>()))
            .ReturnsAsync(createdPost);

        _repositoryMock
            .Setup(r => r.GetPostByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(createdPost);

        _repositoryMock
            .Setup(r => r.GetFeedCountsAsync(It.IsAny<List<int>>(), It.IsAny<int?>()))
            .ReturnsAsync(new FeedCountsResult { CommentCounts = new(), FavoritedPostIds = new(), SavedPostIds = new() });

        _userInfoClientMock
            .Setup(c => c.GetAuthorsBatchAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, AuthorDto>
            {
                { 1, new AuthorDto { Id = 1, Name = "Test User", Avatar = null, Role = "USER" } }
            });

        // Act
        var result = await _communityService.CreatePostAsync(request, 1, new Dictionary<string, IFormFile>());

        // Assert
        Assert.NotNull(result);
        _repositoryMock.Verify(r => r.CreatePostAsync(It.IsAny<CommunityPost>()), Times.Once);
    }

    [Fact]
    public async Task CreatePost_WithVerifiedLink_Returns201()
    {
        // Arrange
        var request = new CreatePostRequest
        {
            Description = "Check out my work at https://github.com/username. I specialize in fullstack development with React, Node.js, and cloud technologies.",
            Status = 1,
            PortfolioId = null
        };

        var createdPost = new CommunityPost
        {
            Id = 1,
            UserId = 1,
            Description = request.Description,
            Status = request.Status,
            ReviewStatus = CommunityPost.StatusActive,
            CreatedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.CreatePostAsync(It.IsAny<CommunityPost>()))
            .ReturnsAsync(createdPost);

        _repositoryMock
            .Setup(r => r.GetPostByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(createdPost);

        _repositoryMock
            .Setup(r => r.GetFeedCountsAsync(It.IsAny<List<int>>(), It.IsAny<int?>()))
            .ReturnsAsync(new FeedCountsResult { CommentCounts = new(), FavoritedPostIds = new(), SavedPostIds = new() });

        _userInfoClientMock
            .Setup(c => c.GetAuthorsBatchAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, AuthorDto>
            {
                { 1, new AuthorDto { Id = 1, Name = "Test User", Avatar = null, Role = "USER" } }
            });

        // Act
        var result = await _communityService.CreatePostAsync(request, 1, new Dictionary<string, IFormFile>());

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region Pending Review Tests

    [Fact]
    public async Task CreatePost_WithBorderlineContent_MarkedForReview()
    {
        // Arrange
        var request = new CreatePostRequest
        {
            Description = "a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a",
            Status = 1,
            PortfolioId = null
        };

        var createdPost = new CommunityPost
        {
            Id = 1,
            UserId = 1,
            Description = request.Description,
            Status = request.Status,
            ReviewStatus = CommunityPost.StatusPendingReview,
            ReviewReason = "Content requires manual review.",
            ReviewedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.CreatePostAsync(It.IsAny<CommunityPost>()))
            .ReturnsAsync(createdPost);

        _repositoryMock
            .Setup(r => r.GetPostByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(createdPost);

        _repositoryMock
            .Setup(r => r.GetFeedCountsAsync(It.IsAny<List<int>>(), It.IsAny<int?>()))
            .ReturnsAsync(new FeedCountsResult { CommentCounts = new(), FavoritedPostIds = new(), SavedPostIds = new() });

        _userInfoClientMock
            .Setup(c => c.GetAuthorsBatchAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, AuthorDto>
            {
                { 1, new AuthorDto { Id = 1, Name = "Test User", Avatar = null, Role = "USER" } }
            });

        // Act
        var result = await _communityService.CreatePostAsync(request, 1, new Dictionary<string, IFormFile>());

        // Assert
        Assert.NotNull(result);
        _notificationPublisherMock.Verify(
            n => n.PublishPostPendingReviewNotificationAsync(It.IsAny<PostPendingReviewNotificationEvent>()),
            Times.Once
        );
    }

    #endregion

    #region Admin Approve/Reject Tests

    [Fact]
    public async Task AdminApprovePost_UpdatesStatusAndNotifies()
    {
        // Arrange
        var post = new CommunityPost
        {
            Id = 1,
            UserId = 2,
            Description = "Test post",
            ReviewStatus = CommunityPost.StatusPendingReview,
            CreatedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.GetPostByIdAsync(1))
            .ReturnsAsync(post);

        _repositoryMock
            .Setup(r => r.UpdatePostAsync(It.IsAny<CommunityPost>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _communityService.ApprovePostAsync(1, "Looks good!");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(CommunityPost.StatusActive, result.ReviewStatus);
        _repositoryMock.Verify(r => r.UpdatePostAsync(It.IsAny<CommunityPost>()), Times.Once);
        _notificationPublisherMock.Verify(
            n => n.PublishPostApprovedNotificationAsync(It.IsAny<PostApprovedNotificationEvent>()),
            Times.Once
        );
    }

    [Fact]
    public async Task AdminRejectPost_UpdatesStatusAndNotifies()
    {
        // Arrange
        var post = new CommunityPost
        {
            Id = 1,
            UserId = 2,
            Description = "Test post",
            ReviewStatus = CommunityPost.StatusPendingReview,
            CreatedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.GetPostByIdAsync(1))
            .ReturnsAsync(post);

        _repositoryMock
            .Setup(r => r.UpdatePostAsync(It.IsAny<CommunityPost>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _communityService.RejectPostAsync(1, "Low quality content");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(CommunityPost.StatusRejected, result.ReviewStatus);
        Assert.Equal("Low quality content", result.ReviewReason);
        _repositoryMock.Verify(r => r.UpdatePostAsync(It.IsAny<CommunityPost>()), Times.Once);
        _notificationPublisherMock.Verify(
            n => n.PublishPostRejectedNotificationAsync(It.IsAny<PostRejectedNotificationEvent>()),
            Times.Once
        );
    }

    [Fact]
    public async Task AdminApprovePost_NonExistentPost_ThrowsException()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetPostByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((CommunityPost?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _communityService.ApprovePostAsync(999, "Approved")
        );
    }

    [Fact]
    public async Task AdminRejectPost_NonExistentPost_ThrowsException()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetPostByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((CommunityPost?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _communityService.RejectPostAsync(999, "Rejected")
        );
    }

    #endregion

    #region Get Pending Posts Tests

    [Fact]
    public async Task GetAdminPosts_WithPendingFilter_ReturnsOnlyPending()
    {
        // Arrange
        var pendingPosts = new List<CommunityPost>
        {
            new() { Id = 1, UserId = 1, ReviewStatus = CommunityPost.StatusPendingReview, Description = "Post 1" },
            new() { Id = 2, UserId = 2, ReviewStatus = CommunityPost.StatusPendingReview, Description = "Post 2" },
            new() { Id = 3, UserId = 3, ReviewStatus = CommunityPost.StatusPendingReview, Description = "Post 3" }
        };

        _repositoryMock
            .Setup(r => r.GetAdminPostsAsync(CommunityPost.StatusPendingReview, 1, 11))
            .ReturnsAsync(pendingPosts);

        _repositoryMock
            .Setup(r => r.GetFeedCountsAsync(It.IsAny<List<int>>(), It.IsAny<int?>()))
            .ReturnsAsync(new FeedCountsResult { CommentCounts = new(), FavoritedPostIds = new(), SavedPostIds = new() });

        _userInfoClientMock
            .Setup(c => c.GetAuthorsBatchAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, AuthorDto>());

        // Act
        var filter = new AdminPostFilter { Status = "3", PageNumber = 1, PageSize = 10 };
        var result = await _communityService.GetAdminPostsAsync(filter, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Items.Count);
    }

    #endregion

    #region Real-world Scenarios

    [Fact]
    public async Task RealWorldScenario_ProfessionalPortfolioPitch()
    {
        // Arrange
        var request = new CreatePostRequest
        {
            Description = "Full-stack developer with 5+ years of experience. Specialize in React, Node.js, cloud architecture. Visit portfolio: https://github.com/dev",
            Status = 1,
            PortfolioId = null
        };

        var createdPost = new CommunityPost
        {
            Id = 1,
            UserId = 1,
            Description = request.Description,
            Status = request.Status,
            ReviewStatus = CommunityPost.StatusActive,
            CreatedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.CreatePostAsync(It.IsAny<CommunityPost>()))
            .ReturnsAsync(createdPost);

        _repositoryMock
            .Setup(r => r.GetPostByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(createdPost);

        _repositoryMock
            .Setup(r => r.GetFeedCountsAsync(It.IsAny<List<int>>(), It.IsAny<int?>()))
            .ReturnsAsync(new FeedCountsResult { CommentCounts = new(), FavoritedPostIds = new(), SavedPostIds = new() });

        _userInfoClientMock
            .Setup(c => c.GetAuthorsBatchAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, AuthorDto>
            {
                { 1, new AuthorDto { Id = 1, Name = "Developer", Avatar = null, Role = "USER" } }
            });

        // Act
        var result = await _communityService.CreatePostAsync(request, 1, new Dictionary<string, IFormFile>());

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task RealWorldScenario_JobPosting()
    {
        // Arrange
        var request = new CreatePostRequest
        {
            Description = "Exciting opportunity! Looking for React developers. Required: JavaScript, React, Node.js, cloud platforms. Check portfolio: https://linkedin.com/company/myco",
            Status = 1,
            PortfolioId = null
        };

        var createdPost = new CommunityPost
        {
            Id = 1,
            UserId = 1,
            Description = request.Description,
            Status = request.Status,
            ReviewStatus = CommunityPost.StatusActive,
            CreatedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.CreatePostAsync(It.IsAny<CommunityPost>()))
            .ReturnsAsync(createdPost);

        _repositoryMock
            .Setup(r => r.GetPostByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(createdPost);

        _repositoryMock
            .Setup(r => r.GetFeedCountsAsync(It.IsAny<List<int>>(), It.IsAny<int?>()))
            .ReturnsAsync(new FeedCountsResult { CommentCounts = new(), FavoritedPostIds = new(), SavedPostIds = new() });

        _userInfoClientMock
            .Setup(c => c.GetAuthorsBatchAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, AuthorDto>
            {
                { 1, new AuthorDto { Id = 1, Name = "Company", Avatar = null, Role = "COMPANY" } }
            });

        // Act
        var result = await _communityService.CreatePostAsync(request, 1, new Dictionary<string, IFormFile>());

        // Assert
        Assert.NotNull(result);
    }

    #endregion
}

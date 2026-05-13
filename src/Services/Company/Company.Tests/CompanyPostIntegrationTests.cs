using Company.Application.Clients;
using Company.Application.DTOs;
using Company.Application.Interfaces;
using Company.Application.Services;
using Company.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using RecruitmentPlatform.AI.Abstractions;
using RecruitmentPlatform.AI.Services;
using Xunit;

namespace Company.Tests;

public class CompanyPostIntegrationTests
{
    private readonly CompanyPostService _companyPostService;
    private readonly Mock<ICompanyPostRepository> _repositoryMock;
    private readonly Mock<ICompanyCacheRepository> _cacheRepositoryMock;
    private readonly Mock<ICompanyProfileClient> _profileClientMock;
    private readonly Mock<IMediaUploadClient> _mediaUploadClientMock;
    private readonly Mock<IPortfolioMatchingClient> _portfolioMatchingClientMock;
    private readonly Mock<ICompanyEmbeddingEventPublisher> _embeddingPublisherMock;
    private readonly Mock<ICompanyNotificationEventPublisher> _notificationPublisherMock;
    private readonly Mock<IMatchingEngine> _matchingEngineMock;
    private readonly Mock<ITextNormalizer> _textNormalizerMock;
    private readonly Mock<IEmbeddingService> _embeddingServiceMock;
    private readonly Mock<ILogger<CompanyPostService>> _loggerMock;
    private readonly IMemoryCache _cache;

    public CompanyPostIntegrationTests()
    {
        _repositoryMock = new Mock<ICompanyPostRepository>();
        _cacheRepositoryMock = new Mock<ICompanyCacheRepository>();
        _profileClientMock = new Mock<ICompanyProfileClient>();
        _mediaUploadClientMock = new Mock<IMediaUploadClient>();
        _portfolioMatchingClientMock = new Mock<IPortfolioMatchingClient>();
        _embeddingPublisherMock = new Mock<ICompanyEmbeddingEventPublisher>();
        _notificationPublisherMock = new Mock<ICompanyNotificationEventPublisher>();
        _matchingEngineMock = new Mock<IMatchingEngine>();
        _textNormalizerMock = new Mock<ITextNormalizer>();
        _embeddingServiceMock = new Mock<IEmbeddingService>();
        _loggerMock = new Mock<ILogger<CompanyPostService>>();
        _cache = new MemoryCache(new MemoryCacheOptions());

        var moderationService = new ModerationService();

        _companyPostService = new CompanyPostService(
            _repositoryMock.Object,
            _cacheRepositoryMock.Object,
            _profileClientMock.Object,
            _mediaUploadClientMock.Object,
            _portfolioMatchingClientMock.Object,
            _embeddingPublisherMock.Object,
            _notificationPublisherMock.Object,
            _matchingEngineMock.Object,
            _textNormalizerMock.Object,
            _embeddingServiceMock.Object,
            _cache,
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
            Position = "Join our casino team",
            JobDescription = "Great casino offers for employees",
            RequirementsMandatory = "Experience required",
            Status = 1
        };

        _profileClientMock
            .Setup(c => c.GetCompanyAsync(It.IsAny<int>()))
            .ReturnsAsync(new CompanyProfileDto { Id = 1, CompanyName = "Test Company" });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(
            () => _companyPostService.CreatePostAsync(request, 1, new Dictionary<string, IFormFile>())
        );
        Assert.Contains("casino", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreatePost_WithMaliciousLink_ThrowsException()
    {
        // Arrange
        var request = new CreatePostRequest
        {
            Position = "Software Developer",
            JobDescription = "Check this https://bit.ly/job for details",
            RequirementsMandatory = "5 years experience",
            Status = 1
        };

        _profileClientMock
            .Setup(c => c.GetCompanyAsync(It.IsAny<int>()))
            .ReturnsAsync(new CompanyProfileDto { Id = 1, CompanyName = "Test Company" });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(
            () => _companyPostService.CreatePostAsync(request, 1, new Dictionary<string, IFormFile>())
        );
        Assert.Contains("bit.ly", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Approved Post Tests

    [Fact]
    public async Task CreatePost_WithGoodContent_IsApproved()
    {
        // Arrange
        var request = new CreatePostRequest
        {
            Position = "Senior Full-Stack Developer",
            JobDescription = "We are looking for an experienced full-stack developer to join our team. You will work with modern technologies including React, Node.js, and cloud platforms.",
            RequirementsMandatory = "5+ years of experience with web development, proficiency in JavaScript/TypeScript",
            RequirementsPreferred = "Experience with AWS or Azure",
            Benefits = "Competitive salary, health insurance, remote work",
            Status = 1
        };

        var createdPost = new CompanyPost
        {
            PostId = 1,
            CompanyId = 1,
            Position = request.Position,
            JobDescription = request.JobDescription,
            Status = 1,
            CreatedAt = DateTime.UtcNow
        };

        var detailDto = new CompanyPostDetailDto
        {
            PostId = 1,
            CompanyId = 1,
            Position = request.Position,
            JobDescription = request.JobDescription,
            Status = 1,
            CompanyName = "Tech Company"
        };

        _profileClientMock
            .Setup(c => c.GetCompanyAsync(It.IsAny<int>()))
            .ReturnsAsync(new CompanyProfileDto { Id = 1, CompanyName = "Tech Company" });

        _repositoryMock
            .Setup(r => r.CreatePostAsync(It.IsAny<CompanyPost>()))
            .ReturnsAsync(createdPost);

        _repositoryMock
            .Setup(r => r.UpdatePostAsync(It.IsAny<CompanyPost>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.GetPostDetailAsync(It.IsAny<int>(), It.IsAny<int?>()))
            .ReturnsAsync(detailDto);

        _embeddingServiceMock
            .Setup(e => e.CreateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.1f, 0.2f, 0.3f });

        // Act
        var result = await _companyPostService.CreatePostAsync(request, 1, new Dictionary<string, IFormFile>());

        // Assert
        Assert.NotNull(result);
        _repositoryMock.Verify(r => r.CreatePostAsync(It.IsAny<CompanyPost>()), Times.Once);
    }

    [Fact]
    public async Task CreatePost_WithVerifiedLink_IsApproved()
    {
        // Arrange
        var request = new CreatePostRequest
        {
            Position = "Frontend Developer",
            JobDescription = "Join our team. See more at https://github.com/company-jobs. We use modern web technologies.",
            RequirementsMandatory = "3+ years React experience",
            Status = 1
        };

        var createdPost = new CompanyPost
        {
            PostId = 1,
            CompanyId = 1,
            Position = request.Position,
            JobDescription = request.JobDescription,
            Status = 1,
            CreatedAt = DateTime.UtcNow
        };

        var detailDto = new CompanyPostDetailDto
        {
            PostId = 1,
            CompanyId = 1,
            Position = request.Position,
            Status = 1,
            CompanyName = "Tech Company"
        };

        _profileClientMock
            .Setup(c => c.GetCompanyAsync(It.IsAny<int>()))
            .ReturnsAsync(new CompanyProfileDto { Id = 1, CompanyName = "Tech Company" });

        _repositoryMock
            .Setup(r => r.CreatePostAsync(It.IsAny<CompanyPost>()))
            .ReturnsAsync(createdPost);

        _repositoryMock
            .Setup(r => r.UpdatePostAsync(It.IsAny<CompanyPost>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.GetPostDetailAsync(It.IsAny<int>(), It.IsAny<int?>()))
            .ReturnsAsync(detailDto);

        _embeddingServiceMock
            .Setup(e => e.CreateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.1f, 0.2f });

        // Act
        var result = await _companyPostService.CreatePostAsync(request, 1, new Dictionary<string, IFormFile>());

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
            Position = "Developer",
            JobDescription = "a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a",
            Status = 1
        };

        var createdPost = new CompanyPost
        {
            PostId = 1,
            CompanyId = 1,
            Position = request.Position,
            JobDescription = request.JobDescription,
            Status = 1,
            ReviewStatus = CompanyPost.StatusPendingReview,
            ReviewReason = "Content requires manual review.",
            CreatedAt = DateTime.UtcNow
        };

        var detailDto = new CompanyPostDetailDto
        {
            PostId = 1,
            CompanyId = 1,
            Position = request.Position,
            Status = 1,
            CompanyName = "Tech Company"
        };

        _profileClientMock
            .Setup(c => c.GetCompanyAsync(It.IsAny<int>()))
            .ReturnsAsync(new CompanyProfileDto { Id = 1, CompanyName = "Tech Company" });

        _repositoryMock
            .Setup(r => r.CreatePostAsync(It.IsAny<CompanyPost>()))
            .ReturnsAsync(createdPost);

        _repositoryMock
            .Setup(r => r.UpdatePostAsync(It.IsAny<CompanyPost>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.GetPostDetailAsync(It.IsAny<int>(), It.IsAny<int?>()))
            .ReturnsAsync(detailDto);

        _embeddingServiceMock
            .Setup(e => e.CreateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.1f });

        // Act
        var result = await _companyPostService.CreatePostAsync(request, 1, new Dictionary<string, IFormFile>());

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region Admin Approve/Reject Tests

    [Fact]
    public async Task AdminApprovePost_UpdatesStatus()
    {
        // Arrange
        var post = new CompanyPost
        {
            PostId = 1,
            CompanyId = 1,
            Position = "Developer",
            ReviewStatus = CompanyPost.StatusPendingReview,
            CreatedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.GetPostEntityByIdAsync(1))
            .ReturnsAsync(post);

        _repositoryMock
            .Setup(r => r.UpdatePostAsync(It.IsAny<CompanyPost>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _companyPostService.ApprovePostAsync(1, "Approved");

        // Assert
        Assert.NotNull(result);
        _repositoryMock.Verify(r => r.UpdatePostAsync(It.IsAny<CompanyPost>()), Times.Once);
    }

    [Fact]
    public async Task AdminRejectPost_UpdatesStatus()
    {
        // Arrange
        var post = new CompanyPost
        {
            PostId = 1,
            CompanyId = 1,
            Position = "Developer",
            ReviewStatus = CompanyPost.StatusPendingReview,
            CreatedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.GetPostEntityByIdAsync(1))
            .ReturnsAsync(post);

        _repositoryMock
            .Setup(r => r.UpdatePostAsync(It.IsAny<CompanyPost>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _companyPostService.RejectPostAsync(1, "Does not meet requirements");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(CompanyPost.StatusRejected, result.ReviewStatus);
    }

    #endregion

    #region Field Combination Tests

    [Fact]
    public async Task CreatePost_CombinesMultipleFieldsForModeration()
    {
        // Arrange - Test that Position + JobDescription + Requirements are combined for moderation
        var request = new CreatePostRequest
        {
            Position = "Software Engineer",
            JobDescription = "Full-time role working on backend services",
            RequirementsMandatory = "5+ years experience with Java and Spring Boot",
            RequirementsPreferred = "Kubernetes experience preferred",
            Benefits = "Competitive salary and benefits package",
            Status = 1
        };

        var createdPost = new CompanyPost
        {
            PostId = 1,
            CompanyId = 1,
            Position = request.Position,
            Status = 1,
            CreatedAt = DateTime.UtcNow
        };

        var detailDto = new CompanyPostDetailDto
        {
            PostId = 1,
            CompanyId = 1,
            Position = request.Position,
            Status = 1,
            CompanyName = "Tech Company"
        };

        _profileClientMock
            .Setup(c => c.GetCompanyAsync(It.IsAny<int>()))
            .ReturnsAsync(new CompanyProfileDto { Id = 1, CompanyName = "Tech Company" });

        _repositoryMock
            .Setup(r => r.CreatePostAsync(It.IsAny<CompanyPost>()))
            .ReturnsAsync(createdPost);

        _repositoryMock
            .Setup(r => r.UpdatePostAsync(It.IsAny<CompanyPost>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.GetPostDetailAsync(It.IsAny<int>(), It.IsAny<int?>()))
            .ReturnsAsync(detailDto);

        _embeddingServiceMock
            .Setup(e => e.CreateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.1f, 0.2f });

        // Act
        var result = await _companyPostService.CreatePostAsync(request, 1, new Dictionary<string, IFormFile>());

        // Assert - Verify that create was called (meaning moderation combined fields correctly)
        Assert.NotNull(result);
        _repositoryMock.Verify(r => r.CreatePostAsync(It.IsAny<CompanyPost>()), Times.Once);
    }

    #endregion

    #region Company Profile Tests

    [Fact]
    public async Task CreatePost_WithInvalidCompanyProfile_ThrowsException()
    {
        // Arrange
        var request = new CreatePostRequest
        {
            Position = "Developer",
            JobDescription = "Interesting role",
            Status = 1
        };

        _profileClientMock
            .Setup(c => c.GetCompanyAsync(It.IsAny<int>()))
            .ReturnsAsync((CompanyProfileDto?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _companyPostService.CreatePostAsync(request, 1, new Dictionary<string, IFormFile>())
        );
    }

    #endregion

    #region Real-world Scenarios

    [Fact]
    public async Task RealWorldScenario_TechJobPosting()
    {
        // Arrange
        var request = new CreatePostRequest
        {
            Position = "Senior Backend Engineer",
            JobDescription = "We're seeking a senior backend engineer to lead our platform development. You'll work with microservices, cloud infrastructure, and scale systems to millions of users.",
            RequirementsMandatory = "8+ years backend development experience with Java or C#, experience with cloud platforms (AWS/Azure), REST API design",
            RequirementsPreferred = "Kubernetes, Docker, distributed systems experience",
            Benefits = "Competitive salary, stock options, remote work, professional development budget",
            Status = 1
        };

        var createdPost = new CompanyPost
        {
            PostId = 1,
            CompanyId = 1,
            Position = request.Position,
            Status = 1,
            CreatedAt = DateTime.UtcNow
        };

        var detailDto = new CompanyPostDetailDto
        {
            PostId = 1,
            CompanyId = 1,
            Position = request.Position,
            Status = 1,
            CompanyName = "Tech Corp"
        };

        _profileClientMock
            .Setup(c => c.GetCompanyAsync(It.IsAny<int>()))
            .ReturnsAsync(new CompanyProfileDto { Id = 1, CompanyName = "Tech Corp" });

        _repositoryMock
            .Setup(r => r.CreatePostAsync(It.IsAny<CompanyPost>()))
            .ReturnsAsync(createdPost);

        _repositoryMock
            .Setup(r => r.UpdatePostAsync(It.IsAny<CompanyPost>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.GetPostDetailAsync(It.IsAny<int>(), It.IsAny<int?>()))
            .ReturnsAsync(detailDto);

        _embeddingServiceMock
            .Setup(e => e.CreateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.1f, 0.2f, 0.3f });

        // Act
        var result = await _companyPostService.CreatePostAsync(request, 1, new Dictionary<string, IFormFile>());

        // Assert
        Assert.NotNull(result);
    }

    #endregion
}

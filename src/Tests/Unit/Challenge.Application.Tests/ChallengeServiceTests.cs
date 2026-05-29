using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Challenge.Application.DTOs;
using Challenge.Application.Services;
using Challenge.Application.Interfaces;
using Challenge.Application.Clients; // Thêm namespace chứa IEventPublisher và IActorResolverClient
using Challenge.Domain.Repositories;
using Challenge.Domain.Entities;
using Challenge.Domain.Enums;

// Giải quyết triệt để lỗi ép kiểu Namespace bằng Alias tường minh giống file gốc của bạn
using ChallengeEntity = Challenge.Domain.Entities.Challenge;

public class ChallengeServiceTests
{
    private readonly Mock<IChallengeRepository> _mockChallengeRepository;
    private readonly Mock<ILogger<ChallengeService>> _mockLogger;
    private readonly ChallengeService _challengeService;

    public ChallengeServiceTests()
    {
        // 1. Khởi tạo Mock cho các dependency chính được dùng trực tiếp trong hàm Create
        _mockChallengeRepository = new Mock<IChallengeRepository>();
        _mockLogger = new Mock<ILogger<ChallengeService>>();

        // 2. Khởi tạo Mock bổ trợ cho tất cả các tham số còn lại trong constructor của ChallengeService
        var mockVersionRepo = new Mock<IChallengeVersionRepository>();
        var mockSkillRepo = new Mock<ISkillRepository>();
        var mockEvalCriteriaRepo = new Mock<IEvaluationCriteriaRepository>();
        var mockChallengeCriteriaRepo = new Mock<IChallengeCriteriaRepository>();
        var mockCriteriaSkillMappingRepo = new Mock<ICriteriaSkillMappingRepository>();
        var mockGeminiAiService = new Mock<IGeminiAIService>();
        var mockEventPublisher = new Mock<IEventPublisher>();
        var mockActorResolver = new Mock<IActorResolverClient>();

        // 3. Khởi tạo Service với đầy đủ 10 tham số chuẩn xác theo đúng file gốc của bạn
        _challengeService = new ChallengeService(
            _mockChallengeRepository.Object,
            mockVersionRepo.Object,
            mockSkillRepo.Object,
            mockEvalCriteriaRepo.Object,
            mockChallengeCriteriaRepo.Object,
            mockCriteriaSkillMappingRepo.Object,
            mockGeminiAiService.Object,
            mockEventPublisher.Object,
            mockActorResolver.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task CreateChallengeAsync_ValidRequest_ReturnsChallengeDtoAndPersists()
    {
        // 1. ARRANGE
        var request = new CreateChallengeDto
        {
            Title = "Code Tour 2026",
            Description = "Giai thuat nang cao",
            ExpectedSolution = "Su dung Dynamic Programming",
            Deadline = DateTime.UtcNow.AddDays(7)
        };
        int userId = 99;

        // Thiết lập Mock Repository sử dụng ChallengeEntity (đã alias từ Domain.Entities.Challenge)
        _mockChallengeRepository
            .Setup(repo => repo.AddAsync(It.IsAny<ChallengeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockChallengeRepository
            .Setup(repo => repo.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // 2. ACT
        var result = await _challengeService.CreateChallengeAsync(request, userId);

        // 3. ASSERT
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(request.Title, result.Title);
        Assert.Equal(request.Description, result.Description);
        Assert.Equal(userId, result.CreatedById);
        Assert.Equal(request.Deadline, result.Deadline);

        // Kiểm tra hàm lưu xuống cơ sở dữ liệu thực sự được gọi
        _mockChallengeRepository.Verify(
            repo => repo.AddAsync(It.Is<ChallengeEntity>(c =>
                c.Title == request.Title &&
                c.CreatedById == userId
            ), It.IsAny<CancellationToken>()),
            Times.Once
        );

        _mockChallengeRepository.Verify(
            repo => repo.ExistsAsync(result.Id, It.IsAny<CancellationToken>()),
            Times.Once
        );

        // Kiểm tra Logger ghi nhận log thông tin chính xác
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("persisted after create")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task CreateChallengeAsync_NullRequest_ThrowsArgumentNullException()
    {
        // 1. ARRANGE
        CreateChallengeDto nullRequest = null;
        int userId = 99;

        // 2. ACT & ASSERT
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _challengeService.CreateChallengeAsync(nullRequest, userId)
        );

        Assert.Equal("request", exception.ParamName);

        // Đảm bảo không tương tác với tầng DB khi dính lỗi dữ liệu đầu vào null
        _mockChallengeRepository.Verify(
            repo => repo.AddAsync(It.IsAny<ChallengeEntity>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }
    [Fact]
    public async Task UpdateChallengeAsync_ValidRequest_ReturnsUpdatedChallengeDto()
    {
        // 1. ARRANGE
        var challengeId = Guid.NewGuid();
        int userId = 99;

        var request = new UpdateChallengeDto
        {
            Title = "Code Tour 2026 - Updated",
            Description = "Giai thuat nang cao chi tiet",
            ExpectedSolution = "Su dung Quy hoach dong",
            Deadline = DateTime.UtcNow.AddDays(10)
        };

        var existingChallenge = new ChallengeEntity
        {
            Id = challengeId,
            Title = "Old Title",
            Description = "Old Description",
            ExpectedSolution = "Old Solution",
            CreatedById = userId, // Trùng với userId để pass qua hàm EnsureOwner
            Status = ChallengeStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockChallengeRepository
            .Setup(repo => repo.GetByIdAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingChallenge);

        _mockChallengeRepository
            .Setup(repo => repo.UpdateAsync(It.IsAny<ChallengeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // 2. ACT
        var result = await _challengeService.UpdateChallengeAsync(challengeId, request, userId);

        // 3. ASSERT
        Assert.NotNull(result);
        Assert.Equal(request.Title, result.Title);
        Assert.Equal(request.Description, result.Description);
        Assert.Equal(userId, result.CreatedById);
        Assert.Equal(request.Deadline, result.Deadline);

        // Kiểm tra xem thực thể lưu xuống DB có được cập nhật đúng giá trị mới không
        _mockChallengeRepository.Verify(
            repo => repo.UpdateAsync(It.Is<ChallengeEntity>(c =>
                c.Id == challengeId &&
                c.Title == request.Title &&
                c.Description == request.Description &&
                c.ExpectedSolution == request.ExpectedSolution
            ), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateChallengeAsync_NullRequest_ThrowsArgumentNullException()
    {
        // 1. ARRANGE
        var challengeId = Guid.NewGuid();
        UpdateChallengeDto nullRequest = null;
        int userId = 99;

        // 2. ACT & ASSERT
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _challengeService.UpdateChallengeAsync(challengeId, nullRequest, userId)
        );

        Assert.Equal("request", exception.ParamName);
        _mockChallengeRepository.Verify(repo => repo.UpdateAsync(It.IsAny<ChallengeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateChallengeAsync_ChallengeNotFound_ThrowsKeyNotFoundException()
    {
        // 1. ARRANGE
        var challengeId = Guid.NewGuid();
        int userId = 99;
        var request = new UpdateChallengeDto { Title = "New Title" };

        // Giả lập không tìm thấy thực thể trong DB (trả về null)
        _mockChallengeRepository
            .Setup(repo => repo.GetByIdAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChallengeEntity)null);

        // 2. ACT & ASSERT
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _challengeService.UpdateChallengeAsync(challengeId, request, userId)
        );

        Assert.Equal($"Challenge {challengeId} not found", exception.Message);
        _mockChallengeRepository.Verify(repo => repo.UpdateAsync(It.IsAny<ChallengeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateChallengeAsync_NotOwner_ThrowsUnauthorizedAccessException()
    {
        // 1. ARRANGE
        var challengeId = Guid.NewGuid();
        int ownerId = 99;
        int hackerId = 666; // Người dùng khác cố tình chỉnh sửa bản vá
        var request = new UpdateChallengeDto { Title = "Hacked Title" };

        var existingChallenge = new ChallengeEntity
        {
            Id = challengeId,
            Title = "Original Title",
            CreatedById = ownerId // Chủ sở hữu thực sự là 99
        };

        _mockChallengeRepository
            .Setup(repo => repo.GetByIdAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingChallenge);

        // 2. ACT & ASSERT
        // Truyền hackerId (666) vào hàm xử lý để kích hoạt lỗi EnsureOwner
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _challengeService.UpdateChallengeAsync(challengeId, request, hackerId)
        );

        Assert.Equal("You do not have permission to modify this challenge.", exception.Message);
        _mockChallengeRepository.Verify(repo => repo.UpdateAsync(It.IsAny<ChallengeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
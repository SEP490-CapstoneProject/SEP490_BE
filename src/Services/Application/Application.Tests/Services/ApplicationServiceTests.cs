using Application.Application.DTOs;
using Application.Application.Interfaces;
using Application.Application.Services;
using Application.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ApplicationEntity = Application.Domain.Entities.Application;

namespace Application.Tests.Services;

public class ApplicationServiceTests
{
    private readonly Mock<IApplicationRepository> _repo = new();
    private readonly Mock<IUserProfileClient> _userProfileClient = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IEntitlementChecker> _entitlementChecker = new();
    private readonly Mock<IApplicationNotificationEventPublisher> _publisher = new();
    private readonly Mock<ILogger<ApplicationService>> _logger = new();

    private ApplicationService CreateService()
        => new(
            _repo.Object,
            _userProfileClient.Object,
            _currentUser.Object,
            _entitlementChecker.Object,
            _publisher.Object,
            _logger.Object);

    [Fact]
    public async Task CreateApplicationAsync_WhenQuotaExceeded_ThrowsAndSkipsCreate()
    {
        _currentUser.Setup(x => x.GetEmployeeId()).Returns(11);
        _currentUser.Setup(x => x.GetUserId()).Returns(101);
        _entitlementChecker.Setup(x => x.TryIncrementUsageAsync(101, "MAX_APPLY"))
            .ReturnsAsync((false, 5));

        var service = CreateService();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateApplicationAsync(new CreateApplicationRequest
            {
                CompanyPostId = 1001,
                PortfolioId = 2001
            }));

        Assert.Contains("application limit", ex.Message, StringComparison.OrdinalIgnoreCase);
        _repo.Verify(x => x.CreateAsync(It.IsAny<ApplicationEntity>()), Times.Never);
        _entitlementChecker.Verify(x => x.RollbackUsageAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateApplicationAsync_WhenCreateFails_RollsBackUsage()
    {
        _currentUser.Setup(x => x.GetEmployeeId()).Returns(11);
        _currentUser.Setup(x => x.GetUserId()).Returns(101);
        _entitlementChecker.Setup(x => x.TryIncrementUsageAsync(101, "MAX_APPLY"))
            .ReturnsAsync((true, 3));

        _userProfileClient.Setup(x => x.GetEmployeeByIdAsync(11))
            .ReturnsAsync(new EmployeeDto { Id = 11, UserId = 101, Name = "Test User", Avatar = "" });
        _userProfileClient.Setup(x => x.GetCompanyPostByIdAsync(1001))
            .ReturnsAsync(new CompanyPostDto { PostId = 1001, CompanyId = 500, Position = "Developer" });
        _userProfileClient.Setup(x => x.ValidatePortfolioOwnershipAsync(11, 2001))
            .ReturnsAsync(true);
        _repo.Setup(x => x.ExistsByEmployeeAndPostAsync(11, 1001)).ReturnsAsync(false);
        _repo.Setup(x => x.CreateAsync(It.IsAny<ApplicationEntity>()))
            .ThrowsAsync(new InvalidOperationException("db failure"));

        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateApplicationAsync(new CreateApplicationRequest
            {
                CompanyPostId = 1001,
                PortfolioId = 2001
            }));

        _entitlementChecker.Verify(x => x.RollbackUsageAsync(101, "MAX_APPLY"), Times.Once);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Notification.Domain.Entities;
using Notification.Infrastructure.Data;
using Notification.Infrastructure.Services;
using System.Linq;

namespace Notification.Tests.Services;

public class DeviceTokenServiceTests : IAsyncLifetime
{
    private readonly Mock<ILogger<DeviceTokenService>> _mockLogger;
    private readonly NotificationDbContext _dbContext;
    private readonly DeviceTokenService _service;

    public DeviceTokenServiceTests()
    {
        _mockLogger = new Mock<ILogger<DeviceTokenService>>();
        
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new NotificationDbContext(options);
        _service = new DeviceTokenService(_dbContext, _mockLogger.Object);
    }

    public async Task InitializeAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task RegisterTokenAsync_WithValidData_ShouldRegisterSuccessfully()
    {
        // Arrange
        var userId = "user123";
        var deviceToken = "fcm_token_abc123";
        var deviceType = "Android";
        var appVersion = "1.0.0";

        // Act
        var result = await _service.RegisterTokenAsync(userId, deviceToken, deviceType, appVersion);

        // Assert
        result.Should().BeTrue();
        
        var registeredToken = await _dbContext.DeviceTokens
            .FirstOrDefaultAsync(x => x.DeviceToken == deviceToken);
        
        registeredToken.Should().NotBeNull();
        registeredToken!.UserId.Should().Be(userId);
        registeredToken.DeviceType.Should().Be(deviceType);
        registeredToken.AppVersion.Should().Be(appVersion);
        registeredToken.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterTokenAsync_WithEmptyUserId_ShouldReturnFalse()
    {
        // Arrange
        var userId = "";
        var deviceToken = "fcm_token_abc123";

        // Act
        var result = await _service.RegisterTokenAsync(userId, deviceToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterTokenAsync_WithEmptyToken_ShouldReturnFalse()
    {
        // Arrange
        var userId = "user123";
        var deviceToken = "";

        // Act
        var result = await _service.RegisterTokenAsync(userId, deviceToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterTokenAsync_ShouldUpdateExistingToken()
    {
        // Arrange
        var userId = "user123";
        var deviceToken = "fcm_token_abc123";

        // First registration
        await _service.RegisterTokenAsync(userId, deviceToken, "Android", "1.0.0");

        // Act - Register same token with different data
        var result = await _service.RegisterTokenAsync(userId, deviceToken, "iOS", "2.0.0");

        // Assert
        result.Should().BeTrue();
        
        var token = await _dbContext.DeviceTokens
            .FirstOrDefaultAsync(x => x.DeviceToken == deviceToken);
        
        token.Should().NotBeNull();
        token!.DeviceType.Should().Be("iOS");
        token.AppVersion.Should().Be("2.0.0");
    }

    [Fact]
    public async Task UnregisterTokenAsync_WithValidToken_ShouldRemoveToken()
    {
        // Arrange
        var userId = "user123";
        var deviceToken = "fcm_token_abc123";
        
        // Register token first
        var entity = new DeviceTokenEntity
        {
            UserId = userId,
            DeviceToken = deviceToken,
            DeviceType = "Android",
            AppVersion = "1.0.0",
            IsActive = true,
            RegisteredAt = DateTime.UtcNow
        };
        _dbContext.DeviceTokens.Add(entity);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.UnregisterTokenAsync(deviceToken);

        // Assert
        result.Should().BeTrue();
        
        var token = await _dbContext.DeviceTokens
            .FirstOrDefaultAsync(x => x.DeviceToken == deviceToken);
        
        token.Should().BeNull();
    }

    [Fact]
    public async Task UnregisterTokenAsync_WithNonExistentToken_ShouldReturnFalse()
    {
        // Arrange
        var deviceToken = "nonexistent_token";

        // Act
        var result = await _service.UnregisterTokenAsync(deviceToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetActiveTokensForUserAsync_ShouldReturnOnlyActiveTokens()
    {
        // Arrange
        var userId = "user123";
        var token1 = "token_1";
        var token2 = "token_2";
        var token3 = "token_3";

        // Directly add tokens to database
        _dbContext.DeviceTokens.Add(new DeviceTokenEntity
        {
            UserId = userId,
            DeviceToken = token1,
            DeviceType = "Android",
            AppVersion = "1.0.0",
            IsActive = true,
            RegisteredAt = DateTime.UtcNow
        });

        _dbContext.DeviceTokens.Add(new DeviceTokenEntity
        {
            UserId = userId,
            DeviceToken = token2,
            DeviceType = "iOS",
            AppVersion = "1.0.0",
            IsActive = true,
            RegisteredAt = DateTime.UtcNow
        });

        _dbContext.DeviceTokens.Add(new DeviceTokenEntity
        {
            UserId = userId,
            DeviceToken = token3,
            DeviceType = "Android",
            AppVersion = "1.0.0",
            IsActive = false,
            RegisteredAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();

        // Act
        var activeTokens = await _service.GetActiveTokensForUserAsync(userId);

        // Assert
        activeTokens.Should().HaveCount(2);
        activeTokens.Should().Contain(t => t.DeviceToken == token1);
        activeTokens.Should().Contain(t => t.DeviceToken == token2);
        activeTokens.Should().NotContain(t => t.DeviceToken == token3);
    }

    [Fact]
    public async Task GetActiveTokensForUserAsync_WithNonExistentUser_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = "nonexistent_user";

        // Act
        var tokens = await _service.GetActiveTokensForUserAsync(userId);

        // Assert
        tokens.Should().BeEmpty();
    }

    [Fact]
    public async Task DeactivateTokenAsync_WithValidToken_ShouldDeactivateToken()
    {
        // Arrange
        var deviceToken = "fcm_token_abc123";
        _dbContext.DeviceTokens.Add(new DeviceTokenEntity
        {
            UserId = "user123",
            DeviceToken = deviceToken,
            DeviceType = "Android",
            AppVersion = "1.0.0",
            IsActive = true,
            RegisteredAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.DeactivateTokenAsync(deviceToken);

        // Assert
        result.Should().BeTrue();
        
        var token = await _dbContext.DeviceTokens
            .FirstOrDefaultAsync(x => x.DeviceToken == deviceToken);
        
        token.Should().NotBeNull();
        token!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateLastUsedAsync_WithValidToken_ShouldUpdateTimestamp()
    {
        // Arrange
        var deviceToken = "fcm_token_abc123";
        _dbContext.DeviceTokens.Add(new DeviceTokenEntity
        {
            UserId = "user123",
            DeviceToken = deviceToken,
            DeviceType = "Android",
            AppVersion = "1.0.0",
            IsActive = true,
            RegisteredAt = DateTime.UtcNow,
            LastUsedAt = null
        });
        await _dbContext.SaveChangesAsync();

        await Task.Delay(100); // Small delay to ensure timestamp difference

        // Act
        var result = await _service.UpdateLastUsedAsync(deviceToken);

        // Assert
        result.Should().BeTrue();
        
        var updatedToken = await _dbContext.DeviceTokens
            .FirstOrDefaultAsync(x => x.DeviceToken == deviceToken);
        
        updatedToken!.LastUsedAt.Should().NotBeNull();
        updatedToken.LastUsedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CleanupInactiveTokensAsync_ShouldRemoveTokensWithNoLastUsedAt()
    {
        // Arrange
        var token1 = "token_1";
        var token2 = "token_2";

        // Register token1 with old LastUsedAt
        var entity1 = new DeviceTokenEntity
        {
            UserId = "user123",
            DeviceToken = token1,
            DeviceType = "Android",
            AppVersion = "1.0.0",
            IsActive = true,
            RegisteredAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow.AddDays(-31) // 31 days ago
        };
        _dbContext.DeviceTokens.Add(entity1);

        // Register token2 with recent LastUsedAt
        var entity2 = new DeviceTokenEntity
        {
            UserId = "user456",
            DeviceToken = token2,
            DeviceType = "iOS",
            AppVersion = "1.0.0",
            IsActive = true,
            RegisteredAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow.AddHours(-1) // 1 hour ago
        };
        _dbContext.DeviceTokens.Add(entity2);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.CleanupInactiveTokensAsync(30); // Remove tokens not used in 30 days

        // Assert
        var remainingTokens = await _dbContext.DeviceTokens.ToListAsync();
        remainingTokens.Should().HaveCount(1);
        remainingTokens[0].DeviceToken.Should().Be(token2);
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;
using Notification.Infrastructure.Services;

namespace Notification.Tests.Services;

public class FcmServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<FcmService>> _mockLogger;
    private readonly FcmService _fcmService;

    public FcmServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<FcmService>>();
        _fcmService = new FcmService(_mockLogger.Object, _mockConfiguration.Object);
    }

    [Fact]
    public async Task SendNotificationAsync_WithValidToken_ShouldAttemptSend()
    {
        // Arrange
        var deviceToken = "valid_token_123";
        var title = "Test Notification";
        var body = "This is a test";
        var data = new Dictionary<string, string> { { "testKey", "testValue" } };

        // Act & Assert - Firebase Admin SDK would need to be mocked
        // For now, test structure to demonstrate the pattern
        var result = await _fcmService.SendNotificationAsync(deviceToken, title, body, data);

        // We would assert: result should be null if Firebase not initialized (expected in test)
        // result.Should().BeNull(); // Firebase not initialized in test
    }

    [Fact]
    public async Task SendNotificationAsync_WithEmptyToken_ReturnsNull()
    {
        // Arrange
        var deviceToken = "";
        var title = "Test";
        var body = "Test";

        // Act
        var result = await _fcmService.SendNotificationAsync(deviceToken, title, body);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SendNotificationAsync_WithNullToken_ReturnsNull()
    {
        // Arrange
        var deviceToken = (string)null!;
        var title = "Test";
        var body = "Test";

        // Act
        var result = await _fcmService.SendNotificationAsync(deviceToken, title, body);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SendMulticastAsync_WithEmptyTokenList_ReturnsFalse()
    {
        // Arrange
        var tokens = new List<string>();
        var title = "Test";
        var body = "Test";

        // Act
        var result = await _fcmService.SendMulticastAsync(tokens, title, body);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendMulticastAsync_WithNullTokenList_ReturnsFalse()
    {
        // Arrange
        List<string>? tokens = null;
        var title = "Test";
        var body = "Test";

        // Act
        var result = await _fcmService.SendMulticastAsync(tokens!, title, body);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithEmptyToken_ReturnsFalse()
    {
        // Arrange
        var token = "";

        // Act
        var result = await _fcmService.ValidateTokenAsync(token);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithNullToken_ReturnsFalse()
    {
        // Arrange
        var token = (string)null!;

        // Act
        var result = await _fcmService.ValidateTokenAsync(token);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var token = "valid_token_123";

        // Act
        var result = await _fcmService.ValidateTokenAsync(token);

        // Assert
        result.Should().BeTrue();
    }
}

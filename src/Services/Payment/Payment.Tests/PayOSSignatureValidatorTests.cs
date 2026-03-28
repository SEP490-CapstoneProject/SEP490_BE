using Payment.Infrastructure.Providers.PayOS;
using Xunit;

namespace Payment.Tests;

public class PayOSSignatureValidatorTests
{
    private const string TestChecksumKey = "test-checksum-key-32-chars-long!";

    [Fact]
    public void ValidateSignature_ValidSignature_ReturnsTrue()
    {
        // Arrange
        var validator = new PayOSSignatureValidator(TestChecksumKey);
        var rawBody = @"{""code"":""00"",""data"":{""orderCode"":123456789,""amount"":100000,""status"":""PAID""}}";
        var signature = validator.ComputeSignature(rawBody);

        // Act
        var result = validator.ValidateSignature(rawBody, signature);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateSignature_InvalidSignature_ReturnsFalse()
    {
        // Arrange
        var validator = new PayOSSignatureValidator(TestChecksumKey);
        var rawBody = @"{""orderCode"":123456789}";
        var invalidSignature = "invalid-signature";

        // Act
        var result = validator.ValidateSignature(rawBody, invalidSignature);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateSignature_TamperedBody_ReturnsFalse()
    {
        // Arrange
        var validator = new PayOSSignatureValidator(TestChecksumKey);
        var originalBody = @"{""orderCode"":123456789,""amount"":100000}";
        var tamperedBody = @"{""orderCode"":123456789,""amount"":999999}";
        var signature = validator.ComputeSignature(originalBody);

        // Act
        var result = validator.ValidateSignature(tamperedBody, signature);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateSignature_EmptyBody_ReturnsFalse()
    {
        // Arrange
        var validator = new PayOSSignatureValidator(TestChecksumKey);

        // Act
        var result = validator.ValidateSignature("", "any-signature");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateSignature_NullSignature_ReturnsFalse()
    {
        // Arrange
        var validator = new PayOSSignatureValidator(TestChecksumKey);

        // Act
        var result = validator.ValidateSignature("{}", null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ComputeSignature_SameInput_SameOutput()
    {
        // Arrange
        var validator = new PayOSSignatureValidator(TestChecksumKey);
        var body = @"{""orderCode"":123}";

        // Act
        var sig1 = validator.ComputeSignature(body);
        var sig2 = validator.ComputeSignature(body);

        // Assert
        Assert.Equal(sig1, sig2);
    }

    [Fact]
    public void ComputeSignature_DifferentInput_DifferentOutput()
    {
        // Arrange
        var validator = new PayOSSignatureValidator(TestChecksumKey);

        // Act
        var sig1 = validator.ComputeSignature(@"{""orderCode"":123}");
        var sig2 = validator.ComputeSignature(@"{""orderCode"":456}");

        // Assert
        Assert.NotEqual(sig1, sig2);
    }
}

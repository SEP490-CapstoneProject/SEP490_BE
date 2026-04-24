using Payment.Infrastructure.Providers.PayOS;
using Xunit;

namespace Payment.Tests;

public class PayOSSignatureValidatorTests
{
    private const string TestChecksumKey = "test-checksum-key-32-chars-long!";

    [Fact]
    public void ValidateSignature_LegacyRawBodySignature_ReturnsTrue()
    {
        var validator = new PayOSSignatureValidator(TestChecksumKey);
        var rawBody = @"{""code"":""00"",""data"":{""orderCode"":123456789,""amount"":100000,""status"":""PAID""}}";
        var signature = validator.ComputeSignature(rawBody);

        var result = validator.ValidateSignature(rawBody, signature);

        Assert.True(result);
    }

    [Fact]
    public void ValidateSignature_CanonicalDataSignature_ReturnsTrue()
    {
        var validator = new PayOSSignatureValidator(TestChecksumKey);
        var rawBody = @"{
          ""code"":""00"",
          ""desc"":""success"",
          ""success"":true,
          ""data"":{
            ""orderCode"":177643460010910,
            ""amount"":9990,
            ""description"":""CSWCEWN7P34 S12P3""
          },
          ""signature"":""placeholder""
        }";

        var canonicalPayload = "amount=9990&description=CSWCEWN7P34%20S12P3&orderCode=177643460010910";
        var signature = validator.ComputeSignature(canonicalPayload);

        var result = validator.ValidateSignature(rawBody, signature);

        Assert.True(result);
    }

    [Fact]
    public void ValidateSignature_CanonicalDataRawStrategy_ReturnsTrue()
    {
        var validator = new PayOSSignatureValidator(TestChecksumKey);
        var rawBody = @"{
          ""code"":""00"",
          ""desc"":""success"",
          ""success"":true,
          ""data"":{
            ""orderCode"":177643460010910,
            ""amount"":9990,
            ""description"":""CSWCEWN7P34 S12P3""
          },
          ""signature"":""placeholder""
        }";

        var canonicalPayload = "amount=9990&description=CSWCEWN7P34 S12P3&orderCode=177643460010910";
        var signature = validator.ComputeSignature(canonicalPayload);

        var result = validator.ValidateSignature(rawBody, signature);

        Assert.True(result);
    }

    [Fact]
    public void ValidateSignature_CanonicalDataWithOptionalFields_ReturnsTrue()
    {
        var validator = new PayOSSignatureValidator(TestChecksumKey);
        var rawBody = @"{
          ""code"":""00"",
          ""desc"":""success"",
          ""success"":true,
          ""data"":{
            ""accountNumber"":""8888092868"",
            ""amount"":9990,
            ""description"":""CSWCEWN7P34 S12P3"",
            ""reference"":""3916d512-dbbe-44e5-a06f-3269d0279752"",
            ""transactionDateTime"":""2026-04-17 21:03:35"",
            ""virtualAccountNumber"":""V3CAS8888092868"",
            ""counterAccountBankId"":"""",
            ""counterAccountBankName"":"""",
            ""counterAccountName"":null,
            ""counterAccountNumber"":""0"",
            ""virtualAccountName"":"""",
            ""currency"":""VND"",
            ""orderCode"":177643460010910,
            ""paymentLinkId"":""f31c73cfb8c9425fbb342d923568bcab"",
            ""code"":""00"",
            ""desc"":""success""
          },
          ""signature"":""placeholder""
        }";

        var canonicalPayload = "amount=9990&description=CSWCEWN7P34%20S12P3&orderCode=177643460010910";
        var signature = validator.ComputeSignature(canonicalPayload);

        var result = validator.ValidateSignature(rawBody, signature);

        Assert.True(result);
    }

    [Fact]
    public void ValidateSignature_PrimitiveSortedNoEmptyStrategy_ReturnsTrue()
    {
        var validator = new PayOSSignatureValidator(TestChecksumKey);
        var rawBody = @"{
          ""code"":""00"",
          ""desc"":""success"",
          ""success"":true,
          ""data"":{
            ""orderCode"":177643460010910,
            ""amount"":9990,
            ""description"":""CSWCEWN7P34 S12P3"",
            ""counterAccountName"":null,
            ""counterAccountBankId"":"""",
            ""counterAccountNumber"":""0"",
            ""currency"":""VND"",
            ""code"":""00"",
            ""desc"":""success""
          },
          ""signature"":""placeholder""
        }";

        var canonicalPayload = "amount=9990&code=00&counterAccountNumber=0&currency=VND&desc=success&description=CSWCEWN7P34%20S12P3&orderCode=177643460010910";
        var signature = validator.ComputeSignature(canonicalPayload);

        var result = validator.ValidateSignature(rawBody, signature);

        Assert.True(result);
    }

    [Fact]
    public void ValidateSignature_PrimitiveSortedWithEmptyStrategy_ReturnsTrue()
    {
        var validator = new PayOSSignatureValidator(TestChecksumKey);
        var rawBody = @"{
          ""code"":""00"",
          ""desc"":""success"",
          ""success"":true,
          ""data"":{
            ""orderCode"":177643460010910,
            ""amount"":9990,
            ""description"":""CSWCEWN7P34 S12P3"",
            ""counterAccountName"":null,
            ""counterAccountBankId"":"""",
            ""counterAccountNumber"":""0"",
            ""currency"":""VND"",
            ""code"":""00"",
            ""desc"":""success""
          },
          ""signature"":""placeholder""
        }";

        var canonicalPayload = "amount=9990&code=00&counterAccountBankId=&counterAccountName=&counterAccountNumber=0&currency=VND&desc=success&description=CSWCEWN7P34%20S12P3&orderCode=177643460010910";
        var signature = validator.ComputeSignature(canonicalPayload);

        var result = validator.ValidateSignature(rawBody, signature);

        Assert.True(result);
    }

    [Fact]
    public void ValidateSignature_CanonicalDataTampered_ReturnsFalse()
    {
        var validator = new PayOSSignatureValidator(TestChecksumKey);
        var originalBody = @"{
          ""code"":""00"",
          ""desc"":""success"",
          ""success"":true,
          ""data"":{
            ""orderCode"":177643460010910,
            ""amount"":9990,
            ""description"":""CSWCEWN7P34 S12P3""
          },
          ""signature"":""placeholder""
        }";

        var tamperedBody = @"{
          ""code"":""00"",
          ""desc"":""success"",
          ""success"":true,
          ""data"":{
            ""orderCode"":177643460010910,
            ""amount"":10000,
            ""description"":""CSWCEWN7P34 S12P3""
          },
          ""signature"":""placeholder""
        }";

        var canonicalPayload = "amount=9990&description=CSWCEWN7P34%20S12P3&orderCode=177643460010910";
        var signature = validator.ComputeSignature(canonicalPayload);

        var result = validator.ValidateSignature(tamperedBody, signature);

        Assert.False(result);
        Assert.True(validator.ValidateSignature(originalBody, signature));
    }

    [Fact]
    public void ValidateSignature_CanonicalDataMissingKey_ReturnsFalse()
    {
        var validator = new PayOSSignatureValidator(TestChecksumKey);
        var rawBody = @"{
          ""code"":""00"",
          ""desc"":""success"",
          ""success"":true,
          ""data"":{
            ""amount"":9990,
            ""orderCode"":177643460010910
          },
          ""signature"":""placeholder""
        }";

        var signature = validator.ComputeSignature("amount=9990&orderCode=177643460010910");

        var result = validator.ValidateSignature(rawBody, signature);

        Assert.False(result);
    }

    [Fact]
    public void ValidateSignature_InvalidSignature_ReturnsFalse()
    {
        var validator = new PayOSSignatureValidator(TestChecksumKey);
        var rawBody = @"{""orderCode"":123456789}";
        var invalidSignature = "invalid-signature";

        var result = validator.ValidateSignature(rawBody, invalidSignature);

        Assert.False(result);
    }

    [Fact]
    public void ValidateSignature_EmptyBody_ReturnsFalse()
    {
        var validator = new PayOSSignatureValidator(TestChecksumKey);

        var result = validator.ValidateSignature("", "any-signature");

        Assert.False(result);
    }

    [Fact]
    public void ValidateSignature_NullSignature_ReturnsFalse()
    {
        var validator = new PayOSSignatureValidator(TestChecksumKey);

        var result = validator.ValidateSignature("{}", null!);

        Assert.False(result);
    }
}

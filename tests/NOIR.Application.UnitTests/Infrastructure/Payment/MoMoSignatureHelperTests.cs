using FluentAssertions;
using NOIR.Infrastructure.Services.Payment.Providers.MoMo;
using Xunit;

namespace NOIR.Application.UnitTests.Infrastructure.Payment;

/// <summary>
/// Unit tests for MoMo signature generation and verification.
/// </summary>
public class MoMoSignatureHelperTests
{
    private const string TestSecretKey = "klm05TvNBzhg7h7j";

    [Fact]
    public void CreateSignature_ValidData_ReturnsConsistentHash()
    {
        // Arrange
        const string rawData = "accessKey=abc&amount=10000&orderId=test123";

        // Act
        var signature1 = MoMoSignatureHelper.CreateSignature(rawData, TestSecretKey);
        var signature2 = MoMoSignatureHelper.CreateSignature(rawData, TestSecretKey);

        // Assert
        signature1.Should().NotBeNullOrEmpty();
        signature1.Should().Be(signature2, "Same input should produce same signature");
        signature1.Should().HaveLength(64, "SHA256 produces 32 bytes = 64 hex characters");
    }

    [Fact]
    public void CreateSignature_DifferentData_ReturnsDifferentHash()
    {
        // Arrange
        const string rawData1 = "accessKey=abc&amount=10000";
        const string rawData2 = "accessKey=abc&amount=20000";

        // Act
        var signature1 = MoMoSignatureHelper.CreateSignature(rawData1, TestSecretKey);
        var signature2 = MoMoSignatureHelper.CreateSignature(rawData2, TestSecretKey);

        // Assert
        signature1.Should().NotBe(signature2, "Different data should produce different signatures");
    }

    [Fact]
    public void VerifySignature_ValidSignature_ReturnsTrue()
    {
        // Arrange
        const string rawData = "accessKey=abc&amount=10000&orderId=test123";
        var signature = MoMoSignatureHelper.CreateSignature(rawData, TestSecretKey);

        // Act
        var isValid = MoMoSignatureHelper.VerifySignature(rawData, TestSecretKey, signature);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifySignature_TamperedData_ReturnsFalse()
    {
        // Arrange
        const string originalData = "accessKey=abc&amount=10000&orderId=test123";
        const string tamperedData = "accessKey=abc&amount=20000&orderId=test123";
        var signature = MoMoSignatureHelper.CreateSignature(originalData, TestSecretKey);

        // Act
        var isValid = MoMoSignatureHelper.VerifySignature(tamperedData, TestSecretKey, signature);

        // Assert
        isValid.Should().BeFalse("Tampered data should fail signature verification");
    }

    [Fact]
    public void VerifySignature_WrongKey_ReturnsFalse()
    {
        // Arrange
        const string rawData = "accessKey=abc&amount=10000";
        const string correctKey = "correctSecretKey!";
        const string wrongKey = "wrongSecretKey123";
        var signature = MoMoSignatureHelper.CreateSignature(rawData, correctKey);

        // Act
        var isValid = MoMoSignatureHelper.VerifySignature(rawData, wrongKey, signature);

        // Assert
        isValid.Should().BeFalse("Wrong key should fail signature verification");
    }

    [Fact]
    public void VerifySignature_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        const string rawData = "accessKey=abc&amount=10000";
        var signature = MoMoSignatureHelper.CreateSignature(rawData, TestSecretKey);
        var upperCaseSignature = signature.ToUpperInvariant();

        // Act
        var isValid = MoMoSignatureHelper.VerifySignature(rawData, TestSecretKey, upperCaseSignature);

        // Assert
        isValid.Should().BeTrue("Signature comparison should be case-insensitive");
    }

    [Fact]
    public void BuildPaymentSignatureData_ReturnsCorrectFormat()
    {
        // Arrange & Act
        var result = MoMoSignatureHelper.BuildPaymentSignatureData(
            accessKey: "abc",
            amount: 50000,
            extraData: "",
            ipnUrl: "https://example.com/ipn",
            orderId: "order123",
            orderInfo: "Test order",
            partnerCode: "MOMO",
            redirectUrl: "https://example.com/return",
            requestId: "req123",
            requestType: "captureWallet");

        // Assert
        result.Should().Be("accessKey=abc&amount=50000&extraData=&ipnUrl=https://example.com/ipn&orderId=order123&orderInfo=Test order&partnerCode=MOMO&redirectUrl=https://example.com/return&requestId=req123&requestType=captureWallet");
    }

    [Fact]
    public void BuildCallbackSignatureData_ReturnsCorrectFormat()
    {
        // Arrange & Act
        var result = MoMoSignatureHelper.BuildCallbackSignatureData(
            accessKey: "abc",
            amount: 50000,
            extraData: "",
            message: "Success",
            orderId: "order123",
            orderInfo: "Test order",
            orderType: "momo_wallet",
            partnerCode: "MOMO",
            payType: "qr",
            requestId: "req123",
            responseTime: 1234567890,
            resultCode: 0,
            transId: "trans123");

        // Assert
        result.Should().Contain("accessKey=abc");
        result.Should().Contain("amount=50000");
        result.Should().Contain("resultCode=0");
        result.Should().Contain("transId=trans123");
    }

    [Fact]
    public void BuildQuerySignatureData_ReturnsCorrectFormat()
    {
        // Arrange & Act
        var result = MoMoSignatureHelper.BuildQuerySignatureData(
            accessKey: "abc",
            orderId: "order123",
            partnerCode: "MOMO",
            requestId: "req123");

        // Assert
        result.Should().Be("accessKey=abc&orderId=order123&partnerCode=MOMO&requestId=req123");
    }

    [Fact]
    public void BuildRefundSignatureData_ReturnsCorrectFormat()
    {
        // Arrange & Act
        var result = MoMoSignatureHelper.BuildRefundSignatureData(
            accessKey: "abc",
            amount: 50000,
            description: "Refund for order",
            orderId: "refund123",
            partnerCode: "MOMO",
            requestId: "req123",
            transId: 123456789);

        // Assert
        result.Should().Be("accessKey=abc&amount=50000&description=Refund for order&orderId=refund123&partnerCode=MOMO&requestId=req123&transId=123456789");
    }

    [Fact]
    public void CreateSignature_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        const string rawData = "accessKey=abc&orderInfo=Payment for order #123 (special chars: äöü)";

        // Act
        var signature = MoMoSignatureHelper.CreateSignature(rawData, TestSecretKey);

        // Assert
        signature.Should().NotBeNullOrEmpty();
        signature.Should().HaveLength(64);
    }

    [Theory]
    [InlineData("", "klm05TvNBzhg7h7j")]
    [InlineData("data", "")]
    public void CreateSignature_EmptyInputs_ReturnsHash(string rawData, string secretKey)
    {
        // Act
        var signature = MoMoSignatureHelper.CreateSignature(rawData, secretKey);

        // Assert
        signature.Should().NotBeNullOrEmpty("Even empty inputs should produce a hash");
        signature.Should().HaveLength(64);
    }

    [Fact]
    public void BuildPaymentSignatureData_WithUnicodeOrderInfo_HandlesCorrectly()
    {
        // Arrange & Act
        var result = MoMoSignatureHelper.BuildPaymentSignatureData(
            accessKey: "abc",
            amount: 100000,
            extraData: "",
            ipnUrl: "https://example.com/ipn",
            orderId: "order123",
            orderInfo: "Thanh toán đơn hàng #123", // Vietnamese text
            partnerCode: "MOMO",
            redirectUrl: "https://example.com/return",
            requestId: "req123",
            requestType: "captureWallet");

        // Assert
        result.Should().Contain("orderInfo=Thanh toán đơn hàng #123");

        // Verify signature can be created
        var signature = MoMoSignatureHelper.CreateSignature(result, TestSecretKey);
        signature.Should().HaveLength(64);
    }
}

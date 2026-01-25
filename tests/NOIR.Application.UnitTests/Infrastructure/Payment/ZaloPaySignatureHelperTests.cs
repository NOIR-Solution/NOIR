using FluentAssertions;
using NOIR.Infrastructure.Services.Payment.Providers.ZaloPay;
using Xunit;

namespace NOIR.Application.UnitTests.Infrastructure.Payment;

/// <summary>
/// Unit tests for ZaloPay MAC signature generation and verification.
/// </summary>
public class ZaloPaySignatureHelperTests
{
    private const string TestKey1 = "PcY4iZIKFCIdgZvA6ueMcMHHmfRfTB3z";
    private const string TestKey2 = "kLtgPl8HHhfvMuDHPwKfgfsY4Ydm9eIz";

    [Fact]
    public void CreateMac_ValidData_ReturnsConsistentHash()
    {
        // Arrange
        const string rawData = "553|240101_order123|user|10000|1234567890|{}|[]";

        // Act
        var mac1 = ZaloPaySignatureHelper.CreateMac(rawData, TestKey1);
        var mac2 = ZaloPaySignatureHelper.CreateMac(rawData, TestKey1);

        // Assert
        mac1.Should().NotBeNullOrEmpty();
        mac1.Should().Be(mac2, "Same input should produce same MAC");
        mac1.Should().HaveLength(64, "SHA256 produces 32 bytes = 64 hex characters");
    }

    [Fact]
    public void CreateMac_DifferentData_ReturnsDifferentHash()
    {
        // Arrange
        const string rawData1 = "553|240101_order123|user|10000|1234567890|{}|[]";
        const string rawData2 = "553|240101_order123|user|20000|1234567890|{}|[]";

        // Act
        var mac1 = ZaloPaySignatureHelper.CreateMac(rawData1, TestKey1);
        var mac2 = ZaloPaySignatureHelper.CreateMac(rawData2, TestKey1);

        // Assert
        mac1.Should().NotBe(mac2, "Different data should produce different MACs");
    }

    [Fact]
    public void VerifyMac_ValidMac_ReturnsTrue()
    {
        // Arrange
        const string rawData = "553|240101_order123|user|10000|1234567890|{}|[]";
        var mac = ZaloPaySignatureHelper.CreateMac(rawData, TestKey1);

        // Act
        var isValid = ZaloPaySignatureHelper.VerifyMac(rawData, TestKey1, mac);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyMac_TamperedData_ReturnsFalse()
    {
        // Arrange
        const string originalData = "553|240101_order123|user|10000|1234567890|{}|[]";
        const string tamperedData = "553|240101_order123|user|50000|1234567890|{}|[]";
        var mac = ZaloPaySignatureHelper.CreateMac(originalData, TestKey1);

        // Act
        var isValid = ZaloPaySignatureHelper.VerifyMac(tamperedData, TestKey1, mac);

        // Assert
        isValid.Should().BeFalse("Tampered data should fail MAC verification");
    }

    [Fact]
    public void VerifyMac_WrongKey_ReturnsFalse()
    {
        // Arrange
        const string rawData = "553|240101_order123|user|10000|1234567890|{}|[]";
        var mac = ZaloPaySignatureHelper.CreateMac(rawData, TestKey1);

        // Act
        var isValid = ZaloPaySignatureHelper.VerifyMac(rawData, TestKey2, mac);

        // Assert
        isValid.Should().BeFalse("Wrong key should fail MAC verification");
    }

    [Fact]
    public void VerifyMac_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        const string rawData = "553|240101_order123|user|10000|1234567890|{}|[]";
        var mac = ZaloPaySignatureHelper.CreateMac(rawData, TestKey1);
        var upperCaseMac = mac.ToUpperInvariant();

        // Act
        var isValid = ZaloPaySignatureHelper.VerifyMac(rawData, TestKey1, upperCaseMac);

        // Assert
        isValid.Should().BeTrue("MAC comparison should be case-insensitive");
    }

    [Fact]
    public void BuildOrderMacData_ReturnsCorrectFormat()
    {
        // Arrange & Act
        var result = ZaloPaySignatureHelper.BuildOrderMacData(
            appId: "553",
            appTransId: "240101_order123",
            appUser: "user",
            amount: 50000,
            appTime: 1234567890,
            embedData: "{}",
            item: "[]");

        // Assert
        result.Should().Be("553|240101_order123|user|50000|1234567890|{}|[]");
    }

    [Fact]
    public void BuildQueryMacData_ReturnsCorrectFormat()
    {
        // Arrange & Act
        var result = ZaloPaySignatureHelper.BuildQueryMacData(
            appId: "553",
            appTransId: "240101_order123",
            key1: TestKey1);

        // Assert
        result.Should().Be($"553|240101_order123|{TestKey1}");
    }

    [Fact]
    public void BuildRefundMacData_ReturnsCorrectFormat()
    {
        // Arrange & Act
        var result = ZaloPaySignatureHelper.BuildRefundMacData(
            appId: "553",
            zpTransId: "123456789",
            amount: 50000,
            description: "Refund for order",
            timestamp: 1234567890);

        // Assert
        result.Should().Be("553|123456789|50000|Refund for order|1234567890");
    }

    [Fact]
    public void GenerateAppTransId_ReturnsCorrectFormat()
    {
        // Arrange
        const string transactionNumber = "ORDER123456";

        // Act
        var result = ZaloPaySignatureHelper.GenerateAppTransId(transactionNumber);

        // Assert
        result.Should().MatchRegex(@"^\d{6}_ORDER123456$", "Format should be yyMMdd_transactionNumber");
        result.Should().EndWith("_ORDER123456");
    }

    [Fact]
    public void GetTimestamp_ReturnsCurrentTimeMillis()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Act
        var timestamp = ZaloPaySignatureHelper.GetTimestamp();

        // Assert
        var after = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        timestamp.Should().BeInRange(before, after);
    }

    [Fact]
    public void BuildCallbackMacData_ReturnsInputUnchanged()
    {
        // Arrange
        const string data = @"{""app_id"":553,""amount"":10000}";

        // Act
        var result = ZaloPaySignatureHelper.BuildCallbackMacData(data);

        // Assert
        result.Should().Be(data, "Callback MAC data should be the raw data unchanged");
    }

    [Fact]
    public void CreateMac_WithEmbeddedJson_HandlesCorrectly()
    {
        // Arrange
        const string embedData = @"{""redirecturl"":""https://example.com/return""}";
        const string item = @"[{""name"":""Product"",""amount"":50000,""quantity"":1}]";
        var rawData = ZaloPaySignatureHelper.BuildOrderMacData(
            "553", "240101_order123", "user", 50000, 1234567890, embedData, item);

        // Act
        var mac = ZaloPaySignatureHelper.CreateMac(rawData, TestKey1);

        // Assert
        mac.Should().NotBeNullOrEmpty();
        mac.Should().HaveLength(64);
    }

    [Fact]
    public void CreateMac_WithUnicodeDescription_HandlesCorrectly()
    {
        // Arrange
        var rawData = ZaloPaySignatureHelper.BuildRefundMacData(
            appId: "553",
            zpTransId: "123456789",
            amount: 50000,
            description: "Hoàn tiền đơn hàng #123", // Vietnamese text
            timestamp: 1234567890);

        // Act
        var mac = ZaloPaySignatureHelper.CreateMac(rawData, TestKey1);

        // Assert
        mac.Should().NotBeNullOrEmpty();
        mac.Should().HaveLength(64);
    }

    [Theory]
    [InlineData("", "key123")]
    [InlineData("data", "")]
    public void CreateMac_EmptyInputs_ReturnsHash(string rawData, string key)
    {
        // Act
        var mac = ZaloPaySignatureHelper.CreateMac(rawData, key);

        // Assert
        mac.Should().NotBeNullOrEmpty("Even empty inputs should produce a MAC");
        mac.Should().HaveLength(64);
    }

    [Fact]
    public void VerifyCallback_WithKey2_ValidatesCorrectly()
    {
        // Arrange - ZaloPay uses Key2 for callback verification
        const string callbackData = @"{""app_id"":553,""amount"":10000}";
        var mac = ZaloPaySignatureHelper.CreateMac(callbackData, TestKey2);

        // Act
        var isValid = ZaloPaySignatureHelper.VerifyMac(callbackData, TestKey2, mac);

        // Assert
        isValid.Should().BeTrue("Key2 should be used for callback MAC verification");
    }
}

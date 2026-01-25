using FluentAssertions;
using NOIR.Infrastructure.Services.Payment.Providers.VnPay;
using Xunit;

namespace NOIR.Application.UnitTests.Infrastructure.Payment;

/// <summary>
/// Unit tests for VNPay signature generation and verification.
/// </summary>
public class VnPaySignatureHelperTests
{
    private const string TestHashSecret = "TESTINGKEY123456789TESTINGKEY12";

    [Fact]
    public void CreateSignature_ValidData_ReturnsConsistentHash()
    {
        // Arrange
        const string rawData = "vnp_Amount=1000000&vnp_Command=pay&vnp_TmnCode=TEST123";

        // Act
        var signature1 = VnPaySignatureHelper.CreateSignature(rawData, TestHashSecret);
        var signature2 = VnPaySignatureHelper.CreateSignature(rawData, TestHashSecret);

        // Assert
        signature1.Should().NotBeNullOrEmpty();
        signature1.Should().Be(signature2, "Same input should produce same signature");
        signature1.Should().HaveLength(128, "SHA512 produces 64 bytes = 128 hex characters");
    }

    [Fact]
    public void CreateSignature_DifferentData_ReturnsDifferentHash()
    {
        // Arrange
        const string rawData1 = "vnp_Amount=1000000&vnp_Command=pay";
        const string rawData2 = "vnp_Amount=2000000&vnp_Command=pay";

        // Act
        var signature1 = VnPaySignatureHelper.CreateSignature(rawData1, TestHashSecret);
        var signature2 = VnPaySignatureHelper.CreateSignature(rawData2, TestHashSecret);

        // Assert
        signature1.Should().NotBe(signature2, "Different data should produce different signatures");
    }

    [Fact]
    public void CreateSignature_DifferentKey_ReturnsDifferentHash()
    {
        // Arrange
        const string rawData = "vnp_Amount=1000000&vnp_Command=pay";
        const string key1 = "KEY111111111111111111111111111111";
        const string key2 = "KEY222222222222222222222222222222";

        // Act
        var signature1 = VnPaySignatureHelper.CreateSignature(rawData, key1);
        var signature2 = VnPaySignatureHelper.CreateSignature(rawData, key2);

        // Assert
        signature1.Should().NotBe(signature2, "Different keys should produce different signatures");
    }

    [Fact]
    public void VerifySignature_ValidSignature_ReturnsTrue()
    {
        // Arrange
        const string rawData = "vnp_Amount=1000000&vnp_Command=pay&vnp_TmnCode=TEST123";
        var signature = VnPaySignatureHelper.CreateSignature(rawData, TestHashSecret);

        // Act
        var isValid = VnPaySignatureHelper.VerifySignature(rawData, TestHashSecret, signature);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifySignature_TamperedData_ReturnsFalse()
    {
        // Arrange
        const string originalData = "vnp_Amount=1000000&vnp_Command=pay";
        const string tamperedData = "vnp_Amount=2000000&vnp_Command=pay";
        var signature = VnPaySignatureHelper.CreateSignature(originalData, TestHashSecret);

        // Act
        var isValid = VnPaySignatureHelper.VerifySignature(tamperedData, TestHashSecret, signature);

        // Assert
        isValid.Should().BeFalse("Tampered data should fail signature verification");
    }

    [Fact]
    public void VerifySignature_WrongKey_ReturnsFalse()
    {
        // Arrange
        const string rawData = "vnp_Amount=1000000&vnp_Command=pay";
        const string correctKey = "CORRECTKEY1234567890CORRECTKEY12";
        const string wrongKey = "WRONGKEY123456789012WRONGKEY1234";
        var signature = VnPaySignatureHelper.CreateSignature(rawData, correctKey);

        // Act
        var isValid = VnPaySignatureHelper.VerifySignature(rawData, wrongKey, signature);

        // Assert
        isValid.Should().BeFalse("Wrong key should fail signature verification");
    }

    [Fact]
    public void VerifySignature_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        const string rawData = "vnp_Amount=1000000";
        var signature = VnPaySignatureHelper.CreateSignature(rawData, TestHashSecret);
        var upperCaseSignature = signature.ToUpperInvariant();

        // Act
        var isValid = VnPaySignatureHelper.VerifySignature(rawData, TestHashSecret, upperCaseSignature);

        // Assert
        isValid.Should().BeTrue("Signature comparison should be case-insensitive");
    }

    [Fact]
    public void BuildDataString_SortsParametersAlphabetically()
    {
        // Arrange
        var parameters = new SortedDictionary<string, string>
        {
            ["vnp_Zebra"] = "last",
            ["vnp_Apple"] = "first",
            ["vnp_Middle"] = "middle"
        };

        // Act
        var result = VnPaySignatureHelper.BuildDataString(parameters);

        // Assert
        result.Should().Contain("vnp_Apple=first");
        result.IndexOf("vnp_Apple").Should().BeLessThan(result.IndexOf("vnp_Middle"));
        result.IndexOf("vnp_Middle").Should().BeLessThan(result.IndexOf("vnp_Zebra"));
    }

    [Fact]
    public void BuildDataString_SkipsEmptyValues()
    {
        // Arrange
        var parameters = new SortedDictionary<string, string>
        {
            ["vnp_Filled"] = "value",
            ["vnp_Empty"] = "",
            ["vnp_Null"] = null!
        };

        // Act
        var result = VnPaySignatureHelper.BuildDataString(parameters);

        // Assert
        result.Should().Contain("vnp_Filled=value");
        result.Should().NotContain("vnp_Empty");
        result.Should().NotContain("vnp_Null");
    }

    [Fact]
    public void BuildDataString_UrlEncodesValues()
    {
        // Arrange
        var parameters = new SortedDictionary<string, string>
        {
            ["vnp_OrderInfo"] = "Payment for order #123"
        };

        // Act
        var result = VnPaySignatureHelper.BuildDataString(parameters);

        // Assert
        result.Should().Contain("vnp_OrderInfo=Payment+for+order+%23123");
    }

    [Fact]
    public void ParseQueryString_ParsesValidQueryString()
    {
        // Arrange
        const string queryString = "vnp_Amount=1000000&vnp_Command=pay&vnp_TmnCode=TEST";

        // Act
        var result = VnPaySignatureHelper.ParseQueryString(queryString);

        // Assert
        result.Should().ContainKey("vnp_Amount");
        result["vnp_Amount"].Should().Be("1000000");
        result.Should().ContainKey("vnp_Command");
        result["vnp_Command"].Should().Be("pay");
        result.Should().HaveCount(3);
    }

    [Fact]
    public void ParseQueryString_HandlesLeadingQuestionMark()
    {
        // Arrange
        const string queryString = "?vnp_Amount=1000000&vnp_Command=pay";

        // Act
        var result = VnPaySignatureHelper.ParseQueryString(queryString);

        // Assert
        result.Should().ContainKey("vnp_Amount");
        result["vnp_Amount"].Should().Be("1000000");
    }

    [Fact]
    public void ParseQueryString_DecodesUrlEncodedValues()
    {
        // Arrange
        const string queryString = "vnp_OrderInfo=Payment+for+order+%23123";

        // Act
        var result = VnPaySignatureHelper.ParseQueryString(queryString);

        // Assert
        result["vnp_OrderInfo"].Should().Be("Payment for order #123");
    }

    [Fact]
    public void ValidateResponseSignature_ValidResponse_ReturnsTrue()
    {
        // Arrange
        var parameters = new SortedDictionary<string, string>
        {
            ["vnp_Amount"] = "1000000",
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = "TEST123"
        };
        var dataString = VnPaySignatureHelper.BuildDataString(parameters);
        var signature = VnPaySignatureHelper.CreateSignature(dataString, TestHashSecret);
        parameters["vnp_SecureHash"] = signature;

        // Act
        var isValid = VnPaySignatureHelper.ValidateResponseSignature(parameters, TestHashSecret);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateResponseSignature_MissingSignature_ReturnsFalse()
    {
        // Arrange
        var parameters = new SortedDictionary<string, string>
        {
            ["vnp_Amount"] = "1000000",
            ["vnp_Command"] = "pay"
        };

        // Act
        var isValid = VnPaySignatureHelper.ValidateResponseSignature(parameters, TestHashSecret);

        // Assert
        isValid.Should().BeFalse("Missing signature should fail validation");
    }

    [Fact]
    public void ValidateResponseSignature_TamperedData_ReturnsFalse()
    {
        // Arrange
        var parameters = new SortedDictionary<string, string>
        {
            ["vnp_Amount"] = "1000000",
            ["vnp_Command"] = "pay"
        };
        var dataString = VnPaySignatureHelper.BuildDataString(parameters);
        var signature = VnPaySignatureHelper.CreateSignature(dataString, TestHashSecret);

        // Tamper the data
        parameters["vnp_Amount"] = "2000000";
        parameters["vnp_SecureHash"] = signature;

        // Act
        var isValid = VnPaySignatureHelper.ValidateResponseSignature(parameters, TestHashSecret);

        // Assert
        isValid.Should().BeFalse("Tampered data should fail validation");
    }
}

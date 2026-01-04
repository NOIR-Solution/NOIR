namespace NOIR.Application.UnitTests.Common.Utilities;

/// <summary>
/// Unit tests for StringMaskingExtensions.
/// </summary>
public class StringMaskingExtensionsTests
{
    #region MaskEmail Tests

    [Theory]
    [InlineData("john@example.com", "jo***n@example.com")]
    [InlineData("jane.doe@company.org", "ja***e@company.org")]
    [InlineData("a@b.com", "a***@b.com")]
    [InlineData("ab@test.com", "a***@test.com")]
    [InlineData("abc@test.com", "a***@test.com")]
    [InlineData("abcd@test.com", "ab***d@test.com")]
    [InlineData("admin@noir.local", "ad***n@noir.local")]
    public void MaskEmail_WithValidEmail_ShouldMaskCorrectly(string input, string expected)
    {
        // Act
        var result = input.MaskEmail();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void MaskEmail_WithEmptyOrNull_ShouldReturnEmpty(string? input)
    {
        // Act
        var result = (input ?? string.Empty).MaskEmail();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void MaskEmail_WithNoAtSign_ShouldReturnUnchanged()
    {
        // Arrange
        var input = "notanemail";

        // Act
        var result = input.MaskEmail();

        // Assert
        result.Should().Be(input);
    }

    [Fact]
    public void MaskEmail_WithAtSignAtStart_ShouldReturnUnchanged()
    {
        // Arrange
        var input = "@example.com";

        // Act
        var result = input.MaskEmail();

        // Assert
        result.Should().Be(input);
    }

    #endregion

    #region MaskPhone Tests

    [Theory]
    [InlineData("1234567890", "******7890")]  // 10 digits, mask 6, show last 4
    [InlineData("555-123-4567", "******4567")]  // 10 digits (dashes ignored), mask 6, show last 4
    [InlineData("+1 (555) 123-4567", "*******4567")]  // 11 digits, mask 7, show last 4
    [InlineData("4567", "4567")]  // 4 or fewer digits, return as-is
    [InlineData("123", "123")]  // 3 digits, return as-is
    public void MaskPhone_WithValidPhone_ShouldMaskCorrectly(string input, string expected)
    {
        // Act
        var result = input.MaskPhone();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void MaskPhone_WithEmptyOrNull_ShouldReturnEmpty(string? input)
    {
        // Act
        var result = (input ?? string.Empty).MaskPhone();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion
}

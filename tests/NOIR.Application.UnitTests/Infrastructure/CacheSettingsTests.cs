using DataAnnotationsValidator = System.ComponentModel.DataAnnotations.Validator;
using DataAnnotationsValidationContext = System.ComponentModel.DataAnnotations.ValidationContext;
using DataAnnotationsValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for CacheSettings.
/// Tests configuration defaults and validation.
/// </summary>
public class CacheSettingsTests
{
    #region Default Values Tests

    [Fact]
    public void CacheSettings_ShouldHaveCorrectSectionName()
    {
        // Assert
        CacheSettings.SectionName.Should().Be("Cache");
    }

    [Fact]
    public void CacheSettings_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var settings = new CacheSettings();

        // Assert
        settings.DefaultExpirationMinutes.Should().Be(30);
        settings.PermissionExpirationMinutes.Should().Be(60);
        settings.UserProfileExpirationMinutes.Should().Be(15);
        settings.BlogPostExpirationMinutes.Should().Be(5);
        settings.FailSafeMaxDurationMinutes.Should().Be(120);
        settings.FactorySoftTimeoutMs.Should().Be(100);
        settings.FactoryHardTimeoutMs.Should().Be(2000);
        settings.EnableBackplane.Should().BeFalse();
        settings.RedisConnectionString.Should().BeNull();
    }

    #endregion

    #region Property Assignment Tests

    [Fact]
    public void CacheSettings_ShouldAllowCustomDefaultExpiration()
    {
        // Arrange
        var settings = new CacheSettings();

        // Act
        settings.DefaultExpirationMinutes = 60;

        // Assert
        settings.DefaultExpirationMinutes.Should().Be(60);
    }

    [Fact]
    public void CacheSettings_ShouldAllowCustomPermissionExpiration()
    {
        // Arrange
        var settings = new CacheSettings();

        // Act
        settings.PermissionExpirationMinutes = 120;

        // Assert
        settings.PermissionExpirationMinutes.Should().Be(120);
    }

    [Fact]
    public void CacheSettings_ShouldAllowCustomUserProfileExpiration()
    {
        // Arrange
        var settings = new CacheSettings();

        // Act
        settings.UserProfileExpirationMinutes = 30;

        // Assert
        settings.UserProfileExpirationMinutes.Should().Be(30);
    }

    [Fact]
    public void CacheSettings_ShouldAllowCustomBlogPostExpiration()
    {
        // Arrange
        var settings = new CacheSettings();

        // Act
        settings.BlogPostExpirationMinutes = 10;

        // Assert
        settings.BlogPostExpirationMinutes.Should().Be(10);
    }

    [Fact]
    public void CacheSettings_ShouldAllowCustomFailSafeDuration()
    {
        // Arrange
        var settings = new CacheSettings();

        // Act
        settings.FailSafeMaxDurationMinutes = 240;

        // Assert
        settings.FailSafeMaxDurationMinutes.Should().Be(240);
    }

    [Fact]
    public void CacheSettings_ShouldAllowCustomSoftTimeout()
    {
        // Arrange
        var settings = new CacheSettings();

        // Act
        settings.FactorySoftTimeoutMs = 200;

        // Assert
        settings.FactorySoftTimeoutMs.Should().Be(200);
    }

    [Fact]
    public void CacheSettings_ShouldAllowCustomHardTimeout()
    {
        // Arrange
        var settings = new CacheSettings();

        // Act
        settings.FactoryHardTimeoutMs = 5000;

        // Assert
        settings.FactoryHardTimeoutMs.Should().Be(5000);
    }

    [Fact]
    public void CacheSettings_ShouldAllowEnableBackplane()
    {
        // Arrange
        var settings = new CacheSettings();

        // Act
        settings.EnableBackplane = true;

        // Assert
        settings.EnableBackplane.Should().BeTrue();
    }

    [Fact]
    public void CacheSettings_ShouldAllowRedisConnectionString()
    {
        // Arrange
        var settings = new CacheSettings();
        var connectionString = "localhost:6379";

        // Act
        settings.RedisConnectionString = connectionString;

        // Assert
        settings.RedisConnectionString.Should().Be(connectionString);
    }

    #endregion

    #region Validation Attribute Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1441)]
    public void DefaultExpirationMinutes_ValidationAttribute_ShouldRejectInvalidValues(int value)
    {
        // Arrange
        var settings = new CacheSettings { DefaultExpirationMinutes = value };
        var validationContext = new DataAnnotationsValidationContext(settings) { MemberName = nameof(CacheSettings.DefaultExpirationMinutes) };
        var results = new List<DataAnnotationsValidationResult>();

        // Act
        var isValid = DataAnnotationsValidator.TryValidateProperty(
            settings.DefaultExpirationMinutes,
            validationContext,
            results);

        // Assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(1440)]
    public void DefaultExpirationMinutes_ValidationAttribute_ShouldAcceptValidValues(int value)
    {
        // Arrange
        var settings = new CacheSettings { DefaultExpirationMinutes = value };
        var validationContext = new DataAnnotationsValidationContext(settings) { MemberName = nameof(CacheSettings.DefaultExpirationMinutes) };
        var results = new List<DataAnnotationsValidationResult>();

        // Act
        var isValid = DataAnnotationsValidator.TryValidateProperty(
            settings.DefaultExpirationMinutes,
            validationContext,
            results);

        // Assert
        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(9)]
    [InlineData(-1)]
    [InlineData(10001)]
    public void FactorySoftTimeoutMs_ValidationAttribute_ShouldRejectInvalidValues(int value)
    {
        // Arrange
        var settings = new CacheSettings { FactorySoftTimeoutMs = value };
        var validationContext = new DataAnnotationsValidationContext(settings) { MemberName = nameof(CacheSettings.FactorySoftTimeoutMs) };
        var results = new List<DataAnnotationsValidationResult>();

        // Act
        var isValid = DataAnnotationsValidator.TryValidateProperty(
            settings.FactorySoftTimeoutMs,
            validationContext,
            results);

        // Assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(10000)]
    public void FactorySoftTimeoutMs_ValidationAttribute_ShouldAcceptValidValues(int value)
    {
        // Arrange
        var settings = new CacheSettings { FactorySoftTimeoutMs = value };
        var validationContext = new DataAnnotationsValidationContext(settings) { MemberName = nameof(CacheSettings.FactorySoftTimeoutMs) };
        var results = new List<DataAnnotationsValidationResult>();

        // Act
        var isValid = DataAnnotationsValidator.TryValidateProperty(
            settings.FactorySoftTimeoutMs,
            validationContext,
            results);

        // Assert
        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(99)]
    [InlineData(-1)]
    [InlineData(60001)]
    public void FactoryHardTimeoutMs_ValidationAttribute_ShouldRejectInvalidValues(int value)
    {
        // Arrange
        var settings = new CacheSettings { FactoryHardTimeoutMs = value };
        var validationContext = new DataAnnotationsValidationContext(settings) { MemberName = nameof(CacheSettings.FactoryHardTimeoutMs) };
        var results = new List<DataAnnotationsValidationResult>();

        // Act
        var isValid = DataAnnotationsValidator.TryValidateProperty(
            settings.FactoryHardTimeoutMs,
            validationContext,
            results);

        // Assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(100)]
    [InlineData(2000)]
    [InlineData(60000)]
    public void FactoryHardTimeoutMs_ValidationAttribute_ShouldAcceptValidValues(int value)
    {
        // Arrange
        var settings = new CacheSettings { FactoryHardTimeoutMs = value };
        var validationContext = new DataAnnotationsValidationContext(settings) { MemberName = nameof(CacheSettings.FactoryHardTimeoutMs) };
        var results = new List<DataAnnotationsValidationResult>();

        // Act
        var isValid = DataAnnotationsValidator.TryValidateProperty(
            settings.FactoryHardTimeoutMs,
            validationContext,
            results);

        // Assert
        isValid.Should().BeTrue();
    }

    #endregion
}

using NOIR.Application.Features.Shipping.Commands.ConfigureShippingProvider;

namespace NOIR.Application.UnitTests.Features.Shipping.Validators;

/// <summary>
/// Unit tests for ConfigureShippingProviderCommandValidator.
/// Tests all validation rules for configuring a shipping provider.
/// </summary>
public class ConfigureShippingProviderCommandValidatorTests
{
    private readonly ConfigureShippingProviderCommandValidator _validator = new();

    private static ConfigureShippingProviderCommand CreateValidCommand() => new(
        ProviderCode: ShippingProviderCode.GHTK,
        DisplayName: "Giao Hang Tiet Kiem",
        Environment: GatewayEnvironment.Sandbox,
        Credentials: new Dictionary<string, string> { { "apiToken", "test-token" } },
        SupportedServices: new List<ShippingServiceType> { ShippingServiceType.Standard },
        SortOrder: 1,
        IsActive: true,
        SupportsCod: true,
        SupportsInsurance: false,
        ApiBaseUrl: "https://services.giaohangtietkiem.vn",
        TrackingUrlTemplate: "https://track.ghtk.vn/{tracking}");

    #region Valid Command

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region ProviderCode Validation

    [Fact]
    public async Task Validate_WhenProviderCodeIsInvalid_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { ProviderCode = (ShippingProviderCode)999 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProviderCode)
            .WithErrorMessage("Invalid shipping provider code.");
    }

    #endregion

    #region DisplayName Validation

    [Fact]
    public async Task Validate_WhenDisplayNameIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { DisplayName = "" };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DisplayName)
            .WithErrorMessage("Display name is required.");
    }

    [Fact]
    public async Task Validate_WhenDisplayNameExceeds200Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { DisplayName = new string('A', 201) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DisplayName)
            .WithErrorMessage("Display name must not exceed 200 characters.");
    }

    #endregion

    #region Environment Validation

    [Fact]
    public async Task Validate_WhenEnvironmentIsInvalid_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Environment = (GatewayEnvironment)999 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Environment)
            .WithErrorMessage("Invalid environment.");
    }

    #endregion

    #region Credentials Validation

    [Fact]
    public async Task Validate_WhenCredentialsIsNull_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Credentials = null! };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Credentials)
            .WithErrorMessage("Credentials are required.");
    }

    #endregion

    #region SupportedServices Validation

    [Fact]
    public async Task Validate_WhenSupportedServicesIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { SupportedServices = new List<ShippingServiceType>() };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SupportedServices)
            .WithErrorMessage("At least one supported service is required.");
    }

    #endregion

    #region SortOrder Validation

    [Fact]
    public async Task Validate_WhenSortOrderIsNegative_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { SortOrder = -1 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SortOrder)
            .WithErrorMessage("Sort order must be non-negative.");
    }

    [Fact]
    public async Task Validate_WhenSortOrderIsZero_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { SortOrder = 0 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SortOrder);
    }

    #endregion

    #region ApiBaseUrl Validation

    [Fact]
    public async Task Validate_WhenApiBaseUrlIsInvalid_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { ApiBaseUrl = "not-a-url" };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ApiBaseUrl)
            .WithErrorMessage("API base URL must be a valid URL.");
    }

    [Fact]
    public async Task Validate_WhenApiBaseUrlIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { ApiBaseUrl = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ApiBaseUrl);
    }

    [Fact]
    public async Task Validate_WhenApiBaseUrlIsEmpty_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { ApiBaseUrl = "" };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ApiBaseUrl);
    }

    [Fact]
    public async Task Validate_WhenApiBaseUrlIsValidUrl_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { ApiBaseUrl = "https://api.example.com" };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ApiBaseUrl);
    }

    #endregion

    #region TrackingUrlTemplate Validation

    [Fact]
    public async Task Validate_WhenTrackingUrlTemplateExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { TrackingUrlTemplate = new string('a', 501) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TrackingUrlTemplate)
            .WithErrorMessage("Tracking URL template must not exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WhenTrackingUrlTemplateIs500Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { TrackingUrlTemplate = new string('a', 500) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TrackingUrlTemplate);
    }

    #endregion
}

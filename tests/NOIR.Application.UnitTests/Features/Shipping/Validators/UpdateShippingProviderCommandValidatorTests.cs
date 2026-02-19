using NOIR.Application.Features.Shipping.Commands.UpdateShippingProvider;

namespace NOIR.Application.UnitTests.Features.Shipping.Validators;

/// <summary>
/// Unit tests for UpdateShippingProviderCommandValidator.
/// Tests all validation rules for updating a shipping provider.
/// </summary>
public class UpdateShippingProviderCommandValidatorTests
{
    private readonly UpdateShippingProviderCommandValidator _validator = new();

    private static UpdateShippingProviderCommand CreateValidCommand() => new(
        ProviderId: Guid.NewGuid(),
        DisplayName: "Updated Provider",
        Environment: GatewayEnvironment.Production,
        SortOrder: 2);

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

    [Fact]
    public async Task Validate_WhenAllOptionalFieldsAreNull_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdateShippingProviderCommand(ProviderId: Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region ProviderId Validation

    [Fact]
    public async Task Validate_WhenProviderIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { ProviderId = Guid.Empty };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProviderId)
            .WithErrorMessage("Provider ID is required.");
    }

    #endregion

    #region DisplayName Validation

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

    [Fact]
    public async Task Validate_WhenDisplayNameIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { DisplayName = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DisplayName);
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

    [Fact]
    public async Task Validate_WhenEnvironmentIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Environment = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Environment);
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
    public async Task Validate_WhenSortOrderIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { SortOrder = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SortOrder);
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
    public async Task Validate_WhenApiBaseUrlIsValidUrl_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { ApiBaseUrl = "https://api.ghtk.vn" };

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
    public async Task Validate_WhenTrackingUrlTemplateIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { TrackingUrlTemplate = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TrackingUrlTemplate);
    }

    #endregion

    #region MinWeightGrams Validation

    [Fact]
    public async Task Validate_WhenMinWeightGramsIsNegative_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { MinWeightGrams = -1 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MinWeightGrams)
            .WithErrorMessage("Minimum weight must be non-negative.");
    }

    [Fact]
    public async Task Validate_WhenMinWeightGramsIsZero_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { MinWeightGrams = 0 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MinWeightGrams);
    }

    [Fact]
    public async Task Validate_WhenMinWeightGramsIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { MinWeightGrams = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MinWeightGrams);
    }

    #endregion

    #region MaxWeightGrams Validation

    [Fact]
    public async Task Validate_WhenMaxWeightGramsIsZero_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { MaxWeightGrams = 0 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxWeightGrams)
            .WithErrorMessage("Maximum weight must be positive.");
    }

    [Fact]
    public async Task Validate_WhenMaxWeightGramsIsNegative_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { MaxWeightGrams = -1 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxWeightGrams)
            .WithErrorMessage("Maximum weight must be positive.");
    }

    [Fact]
    public async Task Validate_WhenMaxWeightGramsIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { MaxWeightGrams = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MaxWeightGrams);
    }

    #endregion

    #region MinCodAmount Validation

    [Fact]
    public async Task Validate_WhenMinCodAmountIsNegative_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { MinCodAmount = -1m };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MinCodAmount)
            .WithErrorMessage("Minimum COD amount must be non-negative.");
    }

    [Fact]
    public async Task Validate_WhenMinCodAmountIsZero_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { MinCodAmount = 0m };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MinCodAmount);
    }

    [Fact]
    public async Task Validate_WhenMinCodAmountIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { MinCodAmount = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MinCodAmount);
    }

    #endregion

    #region MaxCodAmount Validation

    [Fact]
    public async Task Validate_WhenMaxCodAmountIsZero_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { MaxCodAmount = 0m };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxCodAmount)
            .WithErrorMessage("Maximum COD amount must be positive.");
    }

    [Fact]
    public async Task Validate_WhenMaxCodAmountIsNegative_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { MaxCodAmount = -1m };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxCodAmount)
            .WithErrorMessage("Maximum COD amount must be positive.");
    }

    [Fact]
    public async Task Validate_WhenMaxCodAmountIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { MaxCodAmount = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MaxCodAmount);
    }

    #endregion
}

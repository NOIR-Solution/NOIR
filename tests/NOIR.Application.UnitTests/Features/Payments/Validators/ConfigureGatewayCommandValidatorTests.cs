using NOIR.Application.Features.Payments.Commands.ConfigureGateway;

namespace NOIR.Application.UnitTests.Features.Payments.Validators;

/// <summary>
/// Unit tests for ConfigureGatewayCommandValidator.
/// Tests all validation rules for configuring a payment gateway.
/// </summary>
public class ConfigureGatewayCommandValidatorTests
{
    private readonly ConfigureGatewayCommandValidator _validator = new();

    private static ConfigureGatewayCommand CreateValidCommand() => new(
        Provider: "MoMo",
        DisplayName: "MoMo E-Wallet",
        Environment: GatewayEnvironment.Sandbox,
        Credentials: new Dictionary<string, string> { { "apiKey", "test-key" } },
        SupportedMethods: new List<PaymentMethod> { PaymentMethod.EWallet },
        SortOrder: 1,
        IsActive: true);

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

    #region Provider Validation

    [Fact]
    public async Task Validate_WhenProviderIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Provider = "" };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Provider)
            .WithErrorMessage("Provider is required.");
    }

    [Fact]
    public async Task Validate_WhenProviderExceeds50Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Provider = new string('A', 51) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Provider)
            .WithErrorMessage("Provider cannot exceed 50 characters.");
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
            .WithErrorMessage("Display name cannot exceed 200 characters.");
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
            .WithErrorMessage("Invalid gateway environment.");
    }

    #endregion

    #region Credentials Validation

    [Fact]
    public async Task Validate_WhenCredentialsIsNull_ShouldNotHaveError()
    {
        // Note: The validator's .When(x => x.Credentials is not null) skips
        // all rules (including NotNull) when Credentials is null.
        // Arrange
        var command = CreateValidCommand() with { Credentials = null! };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Credentials);
    }

    [Fact]
    public async Task Validate_WhenCredentialsIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Credentials = new Dictionary<string, string>() };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Credentials)
            .WithErrorMessage("At least one credential is required.");
    }

    #endregion

    #region SupportedMethods Validation

    [Fact]
    public async Task Validate_WhenSupportedMethodsIsNull_ShouldNotHaveError()
    {
        // Note: The validator's .When(x => x.SupportedMethods is not null) skips
        // all rules (including NotNull) when SupportedMethods is null.
        // Arrange
        var command = CreateValidCommand() with { SupportedMethods = null! };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SupportedMethods);
    }

    [Fact]
    public async Task Validate_WhenSupportedMethodsIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { SupportedMethods = new List<PaymentMethod>() };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SupportedMethods)
            .WithErrorMessage("At least one supported payment method is required.");
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
}

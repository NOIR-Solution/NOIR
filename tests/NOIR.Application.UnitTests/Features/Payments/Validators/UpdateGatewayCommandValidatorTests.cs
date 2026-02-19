using NOIR.Application.Features.Payments.Commands.UpdateGateway;

namespace NOIR.Application.UnitTests.Features.Payments.Validators;

/// <summary>
/// Unit tests for UpdateGatewayCommandValidator.
/// Tests all validation rules for updating a payment gateway.
/// </summary>
public class UpdateGatewayCommandValidatorTests
{
    private readonly UpdateGatewayCommandValidator _validator = new();

    private static UpdateGatewayCommand CreateValidCommand() => new(
        GatewayId: Guid.NewGuid(),
        DisplayName: "Updated Gateway",
        Environment: GatewayEnvironment.Production,
        Credentials: null,
        SupportedMethods: null,
        SortOrder: 2,
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

    [Fact]
    public async Task Validate_WhenAllOptionalFieldsAreNull_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdateGatewayCommand(
            GatewayId: Guid.NewGuid(),
            DisplayName: null,
            Environment: null,
            Credentials: null,
            SupportedMethods: null,
            SortOrder: null,
            IsActive: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region GatewayId Validation

    [Fact]
    public async Task Validate_WhenGatewayIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { GatewayId = Guid.Empty };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.GatewayId)
            .WithErrorMessage("Gateway ID is required.");
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
            .WithErrorMessage("Display name cannot exceed 200 characters.");
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
            .WithErrorMessage("Invalid gateway environment.");
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
}

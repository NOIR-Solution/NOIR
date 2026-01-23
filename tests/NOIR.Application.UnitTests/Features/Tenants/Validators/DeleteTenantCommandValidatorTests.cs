namespace NOIR.Application.UnitTests.Features.Tenants.Validators;

using NOIR.Application.Features.Tenants.Commands.DeleteTenant;

/// <summary>
/// Unit tests for DeleteTenantCommandValidator.
/// Tests validation rules for tenant deletion.
/// </summary>
public class DeleteTenantCommandValidatorTests
{
    private readonly DeleteTenantCommandValidator _validator;

    public DeleteTenantCommandValidatorTests()
    {
        _validator = new DeleteTenantCommandValidator(CreateLocalizationMock());
    }

    /// <summary>
    /// Creates a mock ILocalizationService that returns the expected English messages.
    /// </summary>
    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();
        mock.Setup(x => x["validation.tenantId.required"]).Returns("Tenant ID is required");
        return mock.Object;
    }

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new DeleteTenantCommand(Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenCommandHasTenantName_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new DeleteTenantCommand(Guid.NewGuid(), "Test Tenant");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region TenantId Validation

    [Fact]
    public async Task Validate_WhenTenantIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new DeleteTenantCommand(Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId)
            .WithErrorMessage("Tenant ID is required");
    }

    [Fact]
    public async Task Validate_WhenTenantIdIsEmptyWithTenantName_ShouldHaveError()
    {
        // Arrange
        var command = new DeleteTenantCommand(Guid.Empty, "Test Tenant");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId)
            .WithErrorMessage("Tenant ID is required");
    }

    [Fact]
    public async Task Validate_WhenTenantIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new DeleteTenantCommand(Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    #endregion

    #region TenantName Validation

    [Fact]
    public async Task Validate_WhenTenantNameIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new DeleteTenantCommand(Guid.NewGuid(), TenantName: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenTenantNameIsEmpty_ShouldNotHaveError()
    {
        // Arrange
        var command = new DeleteTenantCommand(Guid.NewGuid(), TenantName: "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}

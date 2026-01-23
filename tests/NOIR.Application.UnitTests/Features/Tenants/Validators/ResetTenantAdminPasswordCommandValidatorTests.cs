namespace NOIR.Application.UnitTests.Features.Tenants.Validators;

using NOIR.Application.Features.Tenants.Commands.ResetTenantAdminPassword;

/// <summary>
/// Unit tests for ResetTenantAdminPasswordCommandValidator.
/// Tests validation rules for resetting tenant admin password.
/// </summary>
public class ResetTenantAdminPasswordCommandValidatorTests
{
    private readonly ResetTenantAdminPasswordCommandValidator _validator;

    public ResetTenantAdminPasswordCommandValidatorTests()
    {
        _validator = new ResetTenantAdminPasswordCommandValidator(CreateLocalizationMock());
    }

    /// <summary>
    /// Creates a mock ILocalizationService that returns the expected English messages.
    /// </summary>
    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();
        mock.Setup(x => x["validation.required"]).Returns("This field is required");
        mock.Setup(x => x["validation.password.minLength"]).Returns("Password must be at least 6 characters");
        return mock.Object;
    }

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new ResetTenantAdminPasswordCommand(
            TenantId: "tenant-123",
            NewPassword: "newpassword123");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenPasswordIsExactlyMinLength_ShouldNotHaveError()
    {
        // Arrange
        var command = new ResetTenantAdminPasswordCommand(
            TenantId: "tenant-123",
            NewPassword: "123456");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region TenantId Validation

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WhenTenantIdIsEmptyOrWhitespace_ShouldHaveError(string? tenantId)
    {
        // Arrange
        var command = new ResetTenantAdminPasswordCommand(
            TenantId: tenantId!,
            NewPassword: "newpassword123");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public async Task Validate_WhenTenantIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new ResetTenantAdminPasswordCommand(
            TenantId: "tenant-123",
            NewPassword: "newpassword123");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
    }

    #endregion

    #region NewPassword Validation

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WhenNewPasswordIsEmptyOrWhitespace_ShouldHaveError(string? password)
    {
        // Arrange
        var command = new ResetTenantAdminPasswordCommand(
            TenantId: "tenant-123",
            NewPassword: password!);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public async Task Validate_WhenNewPasswordIsTooShort_ShouldHaveError()
    {
        // Arrange
        var command = new ResetTenantAdminPasswordCommand(
            TenantId: "tenant-123",
            NewPassword: "12345");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("Password must be at least 6 characters");
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("password123")]
    [InlineData("very-long-secure-password-with-special-chars!@#")]
    public async Task Validate_WhenNewPasswordIsValid_ShouldNotHaveError(string password)
    {
        // Arrange
        var command = new ResetTenantAdminPasswordCommand(
            TenantId: "tenant-123",
            NewPassword: password);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.NewPassword);
    }

    #endregion
}

namespace NOIR.Application.UnitTests.Features.TenantSettings.Validators;

using NOIR.Application.Features.TenantSettings.Commands.TestTenantSmtpConnection;

/// <summary>
/// Unit tests for TestTenantSmtpConnectionCommandValidator.
/// </summary>
public class TestTenantSmtpConnectionCommandValidatorTests
{
    private readonly TestTenantSmtpConnectionCommandValidator _validator;

    public TestTenantSmtpConnectionCommandValidatorTests()
    {
        _validator = new TestTenantSmtpConnectionCommandValidator();
    }

    #region RecipientEmail Validation

    [Fact]
    public async Task Validate_WhenRecipientEmailIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new TestTenantSmtpConnectionCommand(string.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RecipientEmail)
            .WithErrorMessage("Recipient email is required.");
    }

    [Fact]
    public async Task Validate_WhenRecipientEmailIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new TestTenantSmtpConnectionCommand(null!);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RecipientEmail);
    }

    [Fact]
    public async Task Validate_WhenRecipientEmailIsWhitespace_ShouldHaveError()
    {
        // Arrange
        var command = new TestTenantSmtpConnectionCommand("   ");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RecipientEmail);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    [InlineData("@domain.com")]
    public async Task Validate_WhenRecipientEmailIsInvalid_ShouldHaveError(string email)
    {
        // Arrange
        var command = new TestTenantSmtpConnectionCommand(email);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RecipientEmail)
            .WithErrorMessage("A valid email address is required.");
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co")]
    [InlineData("admin@company.org")]
    public async Task Validate_WhenRecipientEmailIsValid_ShouldNotHaveError(string email)
    {
        // Arrange
        var command = new TestTenantSmtpConnectionCommand(email);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}

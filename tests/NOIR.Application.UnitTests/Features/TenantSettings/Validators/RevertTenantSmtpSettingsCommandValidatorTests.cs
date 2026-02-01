namespace NOIR.Application.UnitTests.Features.TenantSettings.Validators;

using NOIR.Application.Features.TenantSettings.Commands.RevertTenantSmtpSettings;

/// <summary>
/// Unit tests for RevertTenantSmtpSettingsCommandValidator.
/// </summary>
public class RevertTenantSmtpSettingsCommandValidatorTests
{
    private readonly RevertTenantSmtpSettingsCommandValidator _validator;

    public RevertTenantSmtpSettingsCommandValidatorTests()
    {
        _validator = new RevertTenantSmtpSettingsCommandValidator();
    }

    [Fact]
    public async Task Validate_WithDefaultCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange - RevertTenantSmtpSettingsCommand has no required parameters
        var command = new RevertTenantSmtpSettingsCommand();

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert - No validation rules, so should always pass
        result.ShouldNotHaveAnyValidationErrors();
    }
}

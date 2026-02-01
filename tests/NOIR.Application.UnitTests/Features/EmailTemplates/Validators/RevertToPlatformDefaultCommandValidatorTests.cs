namespace NOIR.Application.UnitTests.Features.EmailTemplates.Validators;

using NOIR.Application.Features.EmailTemplates.Commands.RevertToPlatformDefault;

/// <summary>
/// Unit tests for RevertToPlatformDefaultCommandValidator.
/// </summary>
public class RevertToPlatformDefaultCommandValidatorTests
{
    private readonly RevertToPlatformDefaultCommandValidator _validator;

    public RevertToPlatformDefaultCommandValidatorTests()
    {
        _validator = new RevertToPlatformDefaultCommandValidator();
    }

    [Fact]
    public async Task Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new RevertToPlatformDefaultCommand(Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Email template ID is required.");
    }

    [Fact]
    public async Task Validate_WhenIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new RevertToPlatformDefaultCommand(Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

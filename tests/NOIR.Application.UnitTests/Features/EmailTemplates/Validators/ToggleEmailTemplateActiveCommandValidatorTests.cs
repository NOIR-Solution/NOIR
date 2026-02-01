namespace NOIR.Application.UnitTests.Features.EmailTemplates.Validators;

using NOIR.Application.Features.EmailTemplates.Commands.ToggleEmailTemplateActive;

/// <summary>
/// Unit tests for ToggleEmailTemplateActiveCommandValidator.
/// </summary>
public class ToggleEmailTemplateActiveCommandValidatorTests
{
    private readonly ToggleEmailTemplateActiveCommandValidator _validator;

    public ToggleEmailTemplateActiveCommandValidatorTests()
    {
        _validator = new ToggleEmailTemplateActiveCommandValidator();
    }

    [Fact]
    public async Task Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new ToggleEmailTemplateActiveCommand(Guid.Empty, IsActive: true);

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
        var command = new ToggleEmailTemplateActiveCommand(Guid.NewGuid(), IsActive: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Validate_WithAnyIsActiveValue_ShouldNotHaveError(bool isActive)
    {
        // Arrange
        var command = new ToggleEmailTemplateActiveCommand(Guid.NewGuid(), isActive);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

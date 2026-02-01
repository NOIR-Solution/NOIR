using NOIR.Application.Features.LegalPages.Commands.RevertLegalPageToDefault;

namespace NOIR.Application.UnitTests.Features.LegalPages;

/// <summary>
/// Unit tests for RevertLegalPageToDefaultCommandValidator.
/// </summary>
public class RevertLegalPageToDefaultCommandValidatorTests
{
    private readonly RevertLegalPageToDefaultCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WhenIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new RevertLegalPageToDefaultCommand(Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new RevertLegalPageToDefaultCommand(Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Legal page ID is required.");
    }
}

using NOIR.Application.Features.EmailTemplates.Commands.RevertToPlatformDefault;
using NOIR.Application.Features.EmailTemplates.Commands.ToggleEmailTemplateActive;

namespace NOIR.Application.UnitTests.Features.EmailTemplates.Validators;

/// <summary>
/// Unit tests for additional email template command validators.
/// </summary>
public class EmailTemplateCommandValidatorTests
{
    #region RevertToPlatformDefaultCommandValidator Tests

    public class RevertToPlatformDefaultCommandValidatorTests
    {
        private readonly RevertToPlatformDefaultCommandValidator _validator = new();

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
    }

    #endregion

    #region ToggleEmailTemplateActiveCommandValidator Tests

    public class ToggleEmailTemplateActiveCommandValidatorTests
    {
        private readonly ToggleEmailTemplateActiveCommandValidator _validator = new();

        [Fact]
        public async Task Validate_WhenIdIsValid_ShouldNotHaveError()
        {
            // Arrange
            var command = new ToggleEmailTemplateActiveCommand(Guid.NewGuid(), true);

            // Act
            var result = await _validator.TestValidateAsync(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public async Task Validate_WhenIdIsEmpty_ShouldHaveError()
        {
            // Arrange
            var command = new ToggleEmailTemplateActiveCommand(Guid.Empty, true);

            // Act
            var result = await _validator.TestValidateAsync(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Email template ID is required.");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Validate_WhenIsActiveIsSet_ShouldNotHaveError(bool isActive)
        {
            // Arrange
            var command = new ToggleEmailTemplateActiveCommand(Guid.NewGuid(), isActive, "TestTemplate");

            // Act
            var result = await _validator.TestValidateAsync(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }

    #endregion
}

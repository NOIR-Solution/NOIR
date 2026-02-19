using NOIR.Application.Features.ProductAttributes.Commands.DeleteProductAttribute;

namespace NOIR.Application.UnitTests.Features.ProductAttributes.Validators;

/// <summary>
/// Unit tests for DeleteProductAttributeCommandValidator.
/// Tests all validation rules for deleting a product attribute.
/// </summary>
public class DeleteProductAttributeCommandValidatorTests
{
    private readonly DeleteProductAttributeCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WhenIdIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new DeleteProductAttributeCommand(Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new DeleteProductAttributeCommand(Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Attribute ID is required.");
    }
}

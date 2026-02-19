using NOIR.Application.Features.Products.Commands.DeleteProductImage;

namespace NOIR.Application.UnitTests.Features.Products.Commands.DeleteProductImage;

/// <summary>
/// Unit tests for DeleteProductImageCommandValidator.
/// </summary>
public class DeleteProductImageCommandValidatorTests
{
    private readonly DeleteProductImageCommandValidator _validator;

    public DeleteProductImageCommandValidatorTests()
    {
        _validator = new DeleteProductImageCommandValidator();
    }

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        // Arrange
        var command = new DeleteProductImageCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyProductId_ShouldFail()
    {
        // Arrange
        var command = new DeleteProductImageCommand(Guid.Empty, Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductId)
            .WithErrorMessage("Product ID is required.");
    }

    [Fact]
    public async Task Validate_WithEmptyImageId_ShouldFail()
    {
        // Arrange
        var command = new DeleteProductImageCommand(Guid.NewGuid(), Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ImageId)
            .WithErrorMessage("Image ID is required.");
    }
}

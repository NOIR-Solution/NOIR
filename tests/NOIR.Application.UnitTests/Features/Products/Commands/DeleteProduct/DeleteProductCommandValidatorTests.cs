using NOIR.Application.Features.Products.Commands.DeleteProduct;

namespace NOIR.Application.UnitTests.Features.Products.Commands.DeleteProduct;

/// <summary>
/// Unit tests for DeleteProductCommandValidator.
/// </summary>
public class DeleteProductCommandValidatorTests
{
    private readonly DeleteProductCommandValidator _validator;

    public DeleteProductCommandValidatorTests()
    {
        _validator = new DeleteProductCommandValidator();
    }

    [Fact]
    public async Task Validate_WithValidId_ShouldPass()
    {
        // Arrange
        var command = new DeleteProductCommand(Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyId_ShouldFail()
    {
        // Arrange
        var command = new DeleteProductCommand(Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Product ID is required.");
    }
}

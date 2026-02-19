using NOIR.Application.Features.Products.Commands.DeleteProductCategory;

namespace NOIR.Application.UnitTests.Features.Products.Commands.DeleteProductCategory;

/// <summary>
/// Unit tests for DeleteProductCategoryCommandValidator.
/// </summary>
public class DeleteProductCategoryCommandValidatorTests
{
    private readonly DeleteProductCategoryCommandValidator _validator;

    public DeleteProductCategoryCommandValidatorTests()
    {
        _validator = new DeleteProductCategoryCommandValidator();
    }

    [Fact]
    public async Task Validate_WithValidId_ShouldPass()
    {
        // Arrange
        var command = new DeleteProductCategoryCommand(Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyId_ShouldFail()
    {
        // Arrange
        var command = new DeleteProductCategoryCommand(Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Category ID is required.");
    }
}

using NOIR.Application.Features.Products.Commands.DuplicateProduct;

namespace NOIR.Application.UnitTests.Features.Products.Commands.DuplicateProduct;

/// <summary>
/// Unit tests for DuplicateProductCommandValidator.
/// </summary>
public class DuplicateProductCommandValidatorTests
{
    private readonly DuplicateProductCommandValidator _validator;

    public DuplicateProductCommandValidatorTests()
    {
        _validator = new DuplicateProductCommandValidator();
    }

    [Fact]
    public async Task Validate_WithValidId_ShouldPass()
    {
        // Arrange
        var command = new DuplicateProductCommand(Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyId_ShouldFail()
    {
        // Arrange
        var command = new DuplicateProductCommand(Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Product ID is required.");
    }
}

using NOIR.Application.Features.Products.Commands.DeleteProductOption;

namespace NOIR.Application.UnitTests.Features.Products.Commands.DeleteProductOption;

/// <summary>
/// Unit tests for DeleteProductOptionCommandValidator.
/// </summary>
public class DeleteProductOptionCommandValidatorTests
{
    private readonly DeleteProductOptionCommandValidator _validator;

    public DeleteProductOptionCommandValidatorTests()
    {
        _validator = new DeleteProductOptionCommandValidator();
    }

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        // Arrange
        var command = new DeleteProductOptionCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyProductId_ShouldFail()
    {
        // Arrange
        var command = new DeleteProductOptionCommand(Guid.Empty, Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductId)
            .WithErrorMessage("Product ID is required.");
    }

    [Fact]
    public async Task Validate_WithEmptyOptionId_ShouldFail()
    {
        // Arrange
        var command = new DeleteProductOptionCommand(Guid.NewGuid(), Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OptionId)
            .WithErrorMessage("Option ID is required.");
    }
}

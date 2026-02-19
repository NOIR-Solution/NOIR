using NOIR.Application.Features.Products.Commands.DeleteProductOptionValue;

namespace NOIR.Application.UnitTests.Features.Products.Commands.DeleteProductOptionValue;

/// <summary>
/// Unit tests for DeleteProductOptionValueCommandValidator.
/// </summary>
public class DeleteProductOptionValueCommandValidatorTests
{
    private readonly DeleteProductOptionValueCommandValidator _validator;

    public DeleteProductOptionValueCommandValidatorTests()
    {
        _validator = new DeleteProductOptionValueCommandValidator();
    }

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        // Arrange
        var command = new DeleteProductOptionValueCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyProductId_ShouldFail()
    {
        // Arrange
        var command = new DeleteProductOptionValueCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid());

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
        var command = new DeleteProductOptionValueCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OptionId)
            .WithErrorMessage("Option ID is required.");
    }

    [Fact]
    public async Task Validate_WithEmptyValueId_ShouldFail()
    {
        // Arrange
        var command = new DeleteProductOptionValueCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ValueId)
            .WithErrorMessage("Value ID is required.");
    }
}

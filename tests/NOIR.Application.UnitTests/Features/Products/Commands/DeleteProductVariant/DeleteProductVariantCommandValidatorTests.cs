using NOIR.Application.Features.Products.Commands.DeleteProductVariant;

namespace NOIR.Application.UnitTests.Features.Products.Commands.DeleteProductVariant;

/// <summary>
/// Unit tests for DeleteProductVariantCommandValidator.
/// </summary>
public class DeleteProductVariantCommandValidatorTests
{
    private readonly DeleteProductVariantCommandValidator _validator;

    public DeleteProductVariantCommandValidatorTests()
    {
        _validator = new DeleteProductVariantCommandValidator();
    }

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        // Arrange
        var command = new DeleteProductVariantCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyProductId_ShouldFail()
    {
        // Arrange
        var command = new DeleteProductVariantCommand(Guid.Empty, Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductId)
            .WithErrorMessage("Product ID is required.");
    }

    [Fact]
    public async Task Validate_WithEmptyVariantId_ShouldFail()
    {
        // Arrange
        var command = new DeleteProductVariantCommand(Guid.NewGuid(), Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.VariantId)
            .WithErrorMessage("Variant ID is required.");
    }

    [Fact]
    public async Task Validate_WithBothEmpty_ShouldFailForBoth()
    {
        // Arrange
        var command = new DeleteProductVariantCommand(Guid.Empty, Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductId);
        result.ShouldHaveValidationErrorFor(x => x.VariantId);
    }
}

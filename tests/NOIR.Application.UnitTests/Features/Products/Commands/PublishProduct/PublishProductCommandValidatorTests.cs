using NOIR.Application.Features.Products.Commands.PublishProduct;

namespace NOIR.Application.UnitTests.Features.Products.Commands.PublishProduct;

/// <summary>
/// Unit tests for PublishProductCommandValidator.
/// </summary>
public class PublishProductCommandValidatorTests
{
    private readonly PublishProductCommandValidator _validator;

    public PublishProductCommandValidatorTests()
    {
        _validator = new PublishProductCommandValidator();
    }

    [Fact]
    public async Task Validate_WithValidId_ShouldPass()
    {
        // Arrange
        var command = new PublishProductCommand(Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyId_ShouldFail()
    {
        // Arrange
        var command = new PublishProductCommand(Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Product ID is required.");
    }
}

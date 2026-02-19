using NOIR.Application.Features.Products.Commands.SetPrimaryProductImage;

namespace NOIR.Application.UnitTests.Features.Products.Commands.SetPrimaryProductImage;

/// <summary>
/// Unit tests for SetPrimaryProductImageCommandValidator.
/// </summary>
public class SetPrimaryProductImageCommandValidatorTests
{
    private readonly SetPrimaryProductImageCommandValidator _validator;

    public SetPrimaryProductImageCommandValidatorTests()
    {
        _validator = new SetPrimaryProductImageCommandValidator();
    }

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        // Arrange
        var command = new SetPrimaryProductImageCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyProductId_ShouldFail()
    {
        // Arrange
        var command = new SetPrimaryProductImageCommand(Guid.Empty, Guid.NewGuid());

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
        var command = new SetPrimaryProductImageCommand(Guid.NewGuid(), Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ImageId)
            .WithErrorMessage("Image ID is required.");
    }
}

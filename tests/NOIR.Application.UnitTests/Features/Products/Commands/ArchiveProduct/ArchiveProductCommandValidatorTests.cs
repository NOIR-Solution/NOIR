using NOIR.Application.Features.Products.Commands.ArchiveProduct;

namespace NOIR.Application.UnitTests.Features.Products.Commands.ArchiveProduct;

/// <summary>
/// Unit tests for ArchiveProductCommandValidator.
/// </summary>
public class ArchiveProductCommandValidatorTests
{
    private readonly ArchiveProductCommandValidator _validator;

    public ArchiveProductCommandValidatorTests()
    {
        _validator = new ArchiveProductCommandValidator();
    }

    [Fact]
    public async Task Validate_WithValidId_ShouldPass()
    {
        // Arrange
        var command = new ArchiveProductCommand(Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyId_ShouldFail()
    {
        // Arrange
        var command = new ArchiveProductCommand(Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Product ID is required.");
    }
}

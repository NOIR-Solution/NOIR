using NOIR.Application.Features.Products.Commands.AddProductImage;

namespace NOIR.Application.UnitTests.Features.Products.Commands.AddProductImage;

/// <summary>
/// Unit tests for AddProductImageCommandValidator.
/// </summary>
public class AddProductImageCommandValidatorTests
{
    private readonly AddProductImageCommandValidator _validator;

    public AddProductImageCommandValidatorTests()
    {
        _validator = new AddProductImageCommandValidator();
    }

    private static AddProductImageCommand CreateValidCommand() =>
        new(
            ProductId: Guid.NewGuid(),
            Url: "https://example.com/image.jpg",
            AltText: "Product image",
            SortOrder: 0,
            IsPrimary: false);

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ===== ProductId Validation =====

    [Fact]
    public async Task Validate_WithEmptyProductId_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { ProductId = Guid.Empty };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductId)
            .WithErrorMessage("Product ID is required.");
    }

    // ===== Url Validation =====

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WithEmptyUrl_ShouldFail(string? url)
    {
        // Arrange
        var command = CreateValidCommand() with { Url = url! };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Url);
    }

    [Fact]
    public async Task Validate_WithUrlExceeding500Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Url = new string('A', 501) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Url)
            .WithErrorMessage("Image URL cannot exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WithUrlAt500Characters_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { Url = new string('A', 500) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Url);
    }

    // ===== AltText Validation =====

    [Fact]
    public async Task Validate_WithAltTextExceeding200Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { AltText = new string('A', 201) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AltText)
            .WithErrorMessage("Alt text cannot exceed 200 characters.");
    }

    [Fact]
    public async Task Validate_WithNullAltText_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { AltText = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.AltText);
    }

    // ===== SortOrder Validation =====

    [Fact]
    public async Task Validate_WithNegativeSortOrder_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { SortOrder = -1 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SortOrder)
            .WithErrorMessage("Sort order must be non-negative.");
    }

    [Fact]
    public async Task Validate_WithZeroSortOrder_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { SortOrder = 0 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SortOrder);
    }
}

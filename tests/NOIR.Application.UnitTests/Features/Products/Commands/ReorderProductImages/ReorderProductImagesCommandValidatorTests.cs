using NOIR.Application.Features.Products.Commands.ReorderProductImages;
using NOIR.Application.Features.Products.DTOs;

namespace NOIR.Application.UnitTests.Features.Products.Commands.ReorderProductImages;

/// <summary>
/// Unit tests for ReorderProductImagesCommandValidator.
/// </summary>
public class ReorderProductImagesCommandValidatorTests
{
    private readonly ReorderProductImagesCommandValidator _validator;

    public ReorderProductImagesCommandValidatorTests()
    {
        _validator = new ReorderProductImagesCommandValidator();
    }

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        // Arrange
        var command = new ReorderProductImagesCommand(
            Guid.NewGuid(),
            new List<ImageSortOrderItem>
            {
                new(Guid.NewGuid(), 0),
                new(Guid.NewGuid(), 1)
            });

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
        var command = new ReorderProductImagesCommand(
            Guid.Empty,
            new List<ImageSortOrderItem> { new(Guid.NewGuid(), 0) });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductId)
            .WithErrorMessage("Product ID is required.");
    }

    // ===== Items Validation =====

    [Fact]
    public async Task Validate_WithEmptyItems_ShouldFail()
    {
        // Arrange
        var command = new ReorderProductImagesCommand(
            Guid.NewGuid(),
            new List<ImageSortOrderItem>());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("At least one image must be provided for reordering.");
    }

    // ===== Item Child Validation =====

    [Fact]
    public async Task Validate_WithItemHavingEmptyImageId_ShouldFail()
    {
        // Arrange
        var command = new ReorderProductImagesCommand(
            Guid.NewGuid(),
            new List<ImageSortOrderItem> { new(Guid.Empty, 0) });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithItemHavingNegativeSortOrder_ShouldFail()
    {
        // Arrange
        var command = new ReorderProductImagesCommand(
            Guid.NewGuid(),
            new List<ImageSortOrderItem> { new(Guid.NewGuid(), -1) });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithMultipleValidItems_ShouldPass()
    {
        // Arrange
        var command = new ReorderProductImagesCommand(
            Guid.NewGuid(),
            new List<ImageSortOrderItem>
            {
                new(Guid.NewGuid(), 0),
                new(Guid.NewGuid(), 1),
                new(Guid.NewGuid(), 2)
            });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

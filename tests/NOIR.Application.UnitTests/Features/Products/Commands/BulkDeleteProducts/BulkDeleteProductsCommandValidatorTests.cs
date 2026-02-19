using NOIR.Application.Features.Products.Commands.BulkDeleteProducts;

namespace NOIR.Application.UnitTests.Features.Products.Commands.BulkDeleteProducts;

/// <summary>
/// Unit tests for BulkDeleteProductsCommandValidator.
/// </summary>
public class BulkDeleteProductsCommandValidatorTests
{
    private readonly BulkDeleteProductsCommandValidator _validator;

    public BulkDeleteProductsCommandValidatorTests()
    {
        _validator = new BulkDeleteProductsCommandValidator();
    }

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        // Arrange
        var command = new BulkDeleteProductsCommand(new List<Guid> { Guid.NewGuid(), Guid.NewGuid() });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithSingleValidId_ShouldPass()
    {
        // Arrange
        var command = new BulkDeleteProductsCommand(new List<Guid> { Guid.NewGuid() });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ===== ProductIds NotEmpty Validation =====

    [Fact]
    public async Task Validate_WithEmptyList_ShouldFail()
    {
        // Arrange
        var command = new BulkDeleteProductsCommand(new List<Guid>());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductIds)
            .WithErrorMessage("At least one product ID is required.");
    }

    // ===== ProductIds Count Validation =====

    [Fact]
    public async Task Validate_WithMoreThan1000Products_ShouldFail()
    {
        // Arrange
        var productIds = Enumerable.Range(0, 1001).Select(_ => Guid.NewGuid()).ToList();
        var command = new BulkDeleteProductsCommand(productIds);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithExactly1000Products_ShouldPass()
    {
        // Arrange
        var productIds = Enumerable.Range(0, 1000).Select(_ => Guid.NewGuid()).ToList();
        var command = new BulkDeleteProductsCommand(productIds);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ===== Individual ProductId Validation =====

    [Fact]
    public async Task Validate_WithEmptyGuidInList_ShouldFail()
    {
        // Arrange
        var command = new BulkDeleteProductsCommand(new List<Guid> { Guid.NewGuid(), Guid.Empty });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}

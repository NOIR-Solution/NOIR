using NOIR.Application.Features.Products.Commands.BulkPublishProducts;

namespace NOIR.Application.UnitTests.Features.Products.Commands.BulkPublishProducts;

/// <summary>
/// Unit tests for BulkPublishProductsCommandValidator.
/// </summary>
public class BulkPublishProductsCommandValidatorTests
{
    private readonly BulkPublishProductsCommandValidator _validator;

    public BulkPublishProductsCommandValidatorTests()
    {
        _validator = new BulkPublishProductsCommandValidator();
    }

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        // Arrange
        var command = new BulkPublishProductsCommand(new List<Guid> { Guid.NewGuid(), Guid.NewGuid() });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithSingleValidId_ShouldPass()
    {
        // Arrange
        var command = new BulkPublishProductsCommand(new List<Guid> { Guid.NewGuid() });

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
        var command = new BulkPublishProductsCommand(new List<Guid>());

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
        var command = new BulkPublishProductsCommand(productIds);

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
        var command = new BulkPublishProductsCommand(productIds);

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
        var command = new BulkPublishProductsCommand(new List<Guid> { Guid.NewGuid(), Guid.Empty });

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}

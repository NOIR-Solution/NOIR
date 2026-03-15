using NOIR.Application.Features.Hr.Commands.ReorderDepartments;
using NOIR.Application.Features.Hr.DTOs;

namespace NOIR.Application.UnitTests.Features.Hr.Commands.ReorderDepartments;

/// <summary>
/// Unit tests for ReorderDepartmentsCommandValidator.
/// Tests all validation rules for reordering departments.
/// </summary>
public class ReorderDepartmentsCommandValidatorTests
{
    private readonly ReorderDepartmentsCommandValidator _validator = new();

    #region Items Validation

    [Fact]
    public async Task Validate_WhenItemsIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new ReorderDepartmentsCommand(new List<ReorderItem>());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("At least one item is required.");
    }

    [Fact]
    public async Task Validate_WhenItemHasEmptyId_ShouldHaveError()
    {
        // Arrange
        var items = new List<ReorderItem>
        {
            new(Guid.Empty, 0)
        };
        var command = new ReorderDepartmentsCommand(items);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.ShouldBe(false);
    }

    [Fact]
    public async Task Validate_WhenItemHasNegativeSortOrder_ShouldHaveError()
    {
        // Arrange
        var items = new List<ReorderItem>
        {
            new(Guid.NewGuid(), -1)
        };
        var command = new ReorderDepartmentsCommand(items);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.ShouldBe(false);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenSingleValidItem_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var items = new List<ReorderItem>
        {
            new(Guid.NewGuid(), 0)
        };
        var command = new ReorderDepartmentsCommand(items);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenMultipleValidItems_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var items = new List<ReorderItem>
        {
            new(Guid.NewGuid(), 0),
            new(Guid.NewGuid(), 1),
            new(Guid.NewGuid(), 2)
        };
        var command = new ReorderDepartmentsCommand(items);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenSortOrderIsZero_ShouldNotHaveError()
    {
        // Arrange
        var items = new List<ReorderItem>
        {
            new(Guid.NewGuid(), 0)
        };
        var command = new ReorderDepartmentsCommand(items);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}

using NOIR.Application.Features.Products.Commands.AddProductOption;
using NOIR.Application.Features.Products.DTOs;

namespace NOIR.Application.UnitTests.Features.Products.Commands.AddProductOption;

/// <summary>
/// Unit tests for AddProductOptionCommandValidator.
/// </summary>
public class AddProductOptionCommandValidatorTests
{
    private readonly AddProductOptionCommandValidator _validator;

    public AddProductOptionCommandValidatorTests()
    {
        _validator = new AddProductOptionCommandValidator();
    }

    private static AddProductOptionCommand CreateValidCommand() =>
        new(
            ProductId: Guid.NewGuid(),
            Name: "Color",
            DisplayName: "Product Color",
            SortOrder: 0,
            Values: new List<AddProductOptionValueRequest>
            {
                new("red", "Red", "#FF0000", null, 0),
                new("blue", "Blue", "#0000FF", null, 1)
            });

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

    [Fact]
    public async Task Validate_WithNullValues_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { Values = null };

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

    // ===== Name Validation =====

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WithEmptyName_ShouldFail(string? name)
    {
        // Arrange
        var command = CreateValidCommand() with { Name = name! };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Validate_WithNameExceeding50Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Name = new string('A', 51) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Option name cannot exceed 50 characters.");
    }

    [Fact]
    public async Task Validate_WithNameAt50Characters_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { Name = new string('A', 50) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    // ===== DisplayName Validation =====

    [Fact]
    public async Task Validate_WithDisplayNameExceeding100Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { DisplayName = new string('A', 101) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DisplayName)
            .WithErrorMessage("Display name cannot exceed 100 characters.");
    }

    [Fact]
    public async Task Validate_WithNullDisplayName_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { DisplayName = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DisplayName);
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

    // ===== Values Child Validation =====

    [Fact]
    public async Task Validate_WithValueHavingEmptyValue_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            Values = new List<AddProductOptionValueRequest>
            {
                new("", "Red", "#FF0000", null, 0)
            }
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithValueExceeding50Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            Values = new List<AddProductOptionValueRequest>
            {
                new(new string('A', 51), "Red", null, null, 0)
            }
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithDisplayValueExceeding100Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            Values = new List<AddProductOptionValueRequest>
            {
                new("red", new string('A', 101), null, null, 0)
            }
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("#FF0000")]
    [InlineData("#00ff00")]
    [InlineData("#0000FF")]
    [InlineData("#AbCdEf")]
    public async Task Validate_WithValidColorCode_ShouldPass(string colorCode)
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            Values = new List<AddProductOptionValueRequest>
            {
                new("red", "Red", colorCode, null, 0)
            }
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("FF0000")]
    [InlineData("#FFF")]
    [InlineData("#GGGGGG")]
    [InlineData("red")]
    [InlineData("#FF00001")]
    public async Task Validate_WithInvalidColorCode_ShouldFail(string colorCode)
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            Values = new List<AddProductOptionValueRequest>
            {
                new("red", "Red", colorCode, null, 0)
            }
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithSwatchUrlExceeding500Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            Values = new List<AddProductOptionValueRequest>
            {
                new("red", "Red", null, new string('A', 501), 0)
            }
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithNullColorCodeAndSwatchUrl_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            Values = new List<AddProductOptionValueRequest>
            {
                new("red", null, null, null, 0)
            }
        };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

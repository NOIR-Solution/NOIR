using NOIR.Application.Features.ProductAttributes.Commands.UpdateProductAttribute;

namespace NOIR.Application.UnitTests.Features.ProductAttributes.Validators;

/// <summary>
/// Unit tests for UpdateProductAttributeCommandValidator.
/// Tests all validation rules for updating a product attribute.
/// </summary>
public class UpdateProductAttributeCommandValidatorTests
{
    private readonly UpdateProductAttributeCommandValidator _validator = new();

    private static UpdateProductAttributeCommand CreateValidCommand() => new(
        Id: Guid.NewGuid(),
        Code: "color",
        Name: "Color",
        IsFilterable: true,
        IsSearchable: true,
        IsRequired: false,
        IsVariantAttribute: true,
        ShowInProductCard: true,
        ShowInSpecifications: true,
        IsGlobal: false,
        Unit: null,
        ValidationRegex: null,
        MinValue: null,
        MaxValue: null,
        MaxLength: null,
        DefaultValue: null,
        Placeholder: null,
        HelpText: null,
        SortOrder: 0,
        IsActive: true);

    #region Valid Command

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Id Validation

    [Fact]
    public async Task Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Id = Guid.Empty };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Attribute ID is required.");
    }

    #endregion

    #region Code Validation

    [Fact]
    public async Task Validate_WhenCodeIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Code = "" };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code)
            .WithErrorMessage("Attribute code is required.");
    }

    [Fact]
    public async Task Validate_WhenCodeExceeds100Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Code = new string('a', 101) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code)
            .WithErrorMessage("Attribute code must not exceed 100 characters.");
    }

    [Theory]
    [InlineData("Color")]
    [InlineData("my-code")]
    [InlineData("CODE")]
    public async Task Validate_WhenCodeHasInvalidFormat_ShouldHaveError(string code)
    {
        // Arrange
        var command = CreateValidCommand() with { Code = code };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code)
            .WithErrorMessage("Code can only contain lowercase letters, numbers, and underscores.");
    }

    [Theory]
    [InlineData("color")]
    [InlineData("screen_size")]
    [InlineData("weight123")]
    public async Task Validate_WhenCodeHasValidFormat_ShouldNotHaveError(string code)
    {
        // Arrange
        var command = CreateValidCommand() with { Code = code };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Code);
    }

    #endregion

    #region Name Validation

    [Fact]
    public async Task Validate_WhenNameIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Name = "" };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Attribute name is required.");
    }

    [Fact]
    public async Task Validate_WhenNameExceeds200Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Name = new string('A', 201) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Attribute name must not exceed 200 characters.");
    }

    #endregion

    #region Unit Validation

    [Fact]
    public async Task Validate_WhenUnitExceeds50Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Unit = new string('A', 51) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Unit)
            .WithErrorMessage("Unit must not exceed 50 characters.");
    }

    #endregion

    #region ValidationRegex Validation

    [Fact]
    public async Task Validate_WhenValidationRegexExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { ValidationRegex = new string('A', 501) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ValidationRegex)
            .WithErrorMessage("Validation regex must not exceed 500 characters.");
    }

    #endregion

    #region MinValue/MaxValue Validation

    [Fact]
    public async Task Validate_WhenMinValueIsGreaterThanMaxValue_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { MinValue = 100m, MaxValue = 50m };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MinValue)
            .WithErrorMessage("Minimum value must be less than or equal to maximum value.");
    }

    [Fact]
    public async Task Validate_WhenMinValueEqualsMaxValue_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { MinValue = 50m, MaxValue = 50m };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MinValue);
    }

    #endregion

    #region MaxLength Validation

    [Fact]
    public async Task Validate_WhenMaxLengthIsZero_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { MaxLength = 0 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxLength)
            .WithErrorMessage("Maximum length must be greater than 0.");
    }

    #endregion

    #region DefaultValue/Placeholder/HelpText Validation

    [Fact]
    public async Task Validate_WhenDefaultValueExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { DefaultValue = new string('A', 501) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DefaultValue)
            .WithErrorMessage("Default value must not exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WhenPlaceholderExceeds200Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Placeholder = new string('A', 201) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Placeholder)
            .WithErrorMessage("Placeholder must not exceed 200 characters.");
    }

    [Fact]
    public async Task Validate_WhenHelpTextExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { HelpText = new string('A', 501) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.HelpText)
            .WithErrorMessage("Help text must not exceed 500 characters.");
    }

    #endregion

    #region SortOrder Validation

    [Fact]
    public async Task Validate_WhenSortOrderIsNegative_ShouldHaveError()
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
    public async Task Validate_WhenSortOrderIsZero_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { SortOrder = 0 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SortOrder);
    }

    #endregion
}

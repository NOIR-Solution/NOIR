using NOIR.Application.Features.ProductAttributes.Commands.CreateProductAttribute;

namespace NOIR.Application.UnitTests.Features.ProductAttributes.Validators;

/// <summary>
/// Unit tests for CreateProductAttributeCommandValidator.
/// Tests all validation rules for creating a product attribute.
/// </summary>
public class CreateProductAttributeCommandValidatorTests
{
    private readonly CreateProductAttributeCommandValidator _validator = new();

    private static CreateProductAttributeCommand CreateValidCommand() => new(
        Code: "color",
        Name: "Color",
        Type: "Select");

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

    [Fact]
    public async Task Validate_WhenAllOptionalFieldsProvided_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new CreateProductAttributeCommand(
            Code: "weight_kg",
            Name: "Weight (kg)",
            Type: "Decimal",
            IsFilterable: true,
            IsSearchable: true,
            IsRequired: true,
            IsVariantAttribute: false,
            ShowInProductCard: true,
            ShowInSpecifications: true,
            IsGlobal: false,
            Unit: "kg",
            ValidationRegex: @"^\d+(\.\d{1,2})?$",
            MinValue: 0.1m,
            MaxValue: 1000m,
            MaxLength: 10,
            DefaultValue: "0",
            Placeholder: "Enter weight",
            HelpText: "Product weight in kilograms");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
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
    [InlineData("code with spaces")]
    [InlineData("CODE")]
    [InlineData("code!")]
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
    [InlineData("a")]
    [InlineData("a1_b2_c3")]
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

    #region Type Validation

    [Fact]
    public async Task Validate_WhenTypeIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Type = "" };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Type)
            .WithErrorMessage("Attribute type is required.");
    }

    [Theory]
    [InlineData("InvalidType")]
    [InlineData("Dropdown")]
    [InlineData("String")]
    public async Task Validate_WhenTypeIsInvalid_ShouldHaveError(string type)
    {
        // Arrange
        var command = CreateValidCommand() with { Type = type };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Type)
            .WithErrorMessage("Invalid attribute type.");
    }

    [Theory]
    [InlineData("Select")]
    [InlineData("MultiSelect")]
    [InlineData("Text")]
    [InlineData("TextArea")]
    [InlineData("Number")]
    [InlineData("Decimal")]
    [InlineData("Boolean")]
    [InlineData("Date")]
    [InlineData("Color")]
    [InlineData("select")]
    [InlineData("text")]
    public async Task Validate_WhenTypeIsValid_ShouldNotHaveError(string type)
    {
        // Arrange
        var command = CreateValidCommand() with { Type = type };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Type);
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

    [Fact]
    public async Task Validate_WhenUnitIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Unit = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Unit);
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

    [Fact]
    public async Task Validate_WhenValidationRegexIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { ValidationRegex = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ValidationRegex);
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

    [Fact]
    public async Task Validate_WhenMinValueIsLessThanMaxValue_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { MinValue = 10m, MaxValue = 100m };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MinValue);
    }

    [Fact]
    public async Task Validate_WhenOnlyMinValueIsSet_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { MinValue = 10m, MaxValue = null };

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

    [Fact]
    public async Task Validate_WhenMaxLengthIsNegative_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { MaxLength = -1 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxLength)
            .WithErrorMessage("Maximum length must be greater than 0.");
    }

    [Fact]
    public async Task Validate_WhenMaxLengthIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { MaxLength = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MaxLength);
    }

    [Fact]
    public async Task Validate_WhenMaxLengthIsPositive_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { MaxLength = 255 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MaxLength);
    }

    #endregion

    #region DefaultValue Validation

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
    public async Task Validate_WhenDefaultValueIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { DefaultValue = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DefaultValue);
    }

    #endregion

    #region Placeholder Validation

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
    public async Task Validate_WhenPlaceholderIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Placeholder = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Placeholder);
    }

    #endregion

    #region HelpText Validation

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

    [Fact]
    public async Task Validate_WhenHelpTextIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { HelpText = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.HelpText);
    }

    #endregion
}

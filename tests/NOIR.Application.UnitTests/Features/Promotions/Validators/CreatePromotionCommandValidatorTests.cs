namespace NOIR.Application.UnitTests.Features.Promotions.Validators;

/// <summary>
/// Unit tests for CreatePromotionCommandValidator.
/// Tests all validation rules for creating a promotion.
/// </summary>
public class CreatePromotionCommandValidatorTests
{
    private readonly CreatePromotionCommandValidator _validator = new();

    private static CreatePromotionCommand CreateValidCommand(
        string name = "Summer Sale",
        string code = "SUMMER2026",
        string? description = null,
        PromotionType promotionType = PromotionType.VoucherCode,
        DiscountType discountType = DiscountType.Percentage,
        decimal discountValue = 20m,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        PromotionApplyLevel applyLevel = PromotionApplyLevel.Cart,
        decimal? maxDiscountAmount = null,
        decimal? minOrderValue = null,
        int? minItemQuantity = null,
        int? usageLimitTotal = null,
        int? usageLimitPerUser = null)
    {
        return new CreatePromotionCommand(
            name,
            code,
            description,
            promotionType,
            discountType,
            discountValue,
            startDate ?? DateTimeOffset.UtcNow.AddDays(1),
            endDate ?? DateTimeOffset.UtcNow.AddDays(30),
            applyLevel,
            maxDiscountAmount,
            minOrderValue,
            minItemQuantity,
            usageLimitTotal,
            usageLimitPerUser);
    }

    #region Name Validation

    [Fact]
    public async Task Validate_WhenNameIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(name: "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Promotion name is required.");
    }

    [Fact]
    public async Task Validate_WhenNameExceeds200Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(name: new string('A', 201));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Promotion name cannot exceed 200 characters.");
    }

    [Fact]
    public async Task Validate_WhenNameIs200Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(name: new string('A', 200));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region Code Validation

    [Fact]
    public async Task Validate_WhenCodeIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(code: "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code)
            .WithErrorMessage("Promotion code is required.");
    }

    [Fact]
    public async Task Validate_WhenCodeExceeds50Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(code: new string('A', 51));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code)
            .WithErrorMessage("Promotion code cannot exceed 50 characters.");
    }

    [Theory]
    [InlineData("INVALID CODE")]
    [InlineData("INVALID.CODE")]
    [InlineData("INVALID@CODE")]
    [InlineData("CODE!")]
    public async Task Validate_WhenCodeHasInvalidFormat_ShouldHaveError(string code)
    {
        // Arrange
        var command = CreateValidCommand(code: code);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code)
            .WithErrorMessage("Promotion code can only contain letters, numbers, hyphens, and underscores.");
    }

    [Theory]
    [InlineData("SUMMER2026")]
    [InlineData("flash-sale")]
    [InlineData("CODE_123")]
    [InlineData("A")]
    [InlineData("VOUCHER-2026_V2")]
    public async Task Validate_WhenCodeHasValidFormat_ShouldNotHaveError(string code)
    {
        // Arrange
        var command = CreateValidCommand(code: code);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Code);
    }

    #endregion

    #region Description Validation

    [Fact]
    public async Task Validate_WhenDescriptionExceeds1000Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(description: new string('A', 1001));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 1000 characters.");
    }

    [Fact]
    public async Task Validate_WhenDescriptionIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(description: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public async Task Validate_WhenDescriptionIs1000Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(description: new string('A', 1000));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    #endregion

    #region DiscountValue Validation

    [Fact]
    public async Task Validate_WhenDiscountValueIsZero_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(discountValue: 0m);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DiscountValue)
            .WithErrorMessage("Discount value must be greater than zero.");
    }

    [Fact]
    public async Task Validate_WhenDiscountValueIsNegative_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(discountValue: -10m);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DiscountValue)
            .WithErrorMessage("Discount value must be greater than zero.");
    }

    [Fact]
    public async Task Validate_WhenPercentageExceeds100_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(
            discountType: DiscountType.Percentage,
            discountValue: 101m);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DiscountValue)
            .WithErrorMessage("Percentage discount cannot exceed 100%.");
    }

    [Fact]
    public async Task Validate_WhenPercentageIs100_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(
            discountType: DiscountType.Percentage,
            discountValue: 100m);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DiscountValue);
    }

    [Fact]
    public async Task Validate_WhenFixedAmountExceeds100_ShouldNotHaveError()
    {
        // Arrange - Fixed amount discount of 200 should be fine (only percentage is limited)
        var command = CreateValidCommand(
            discountType: DiscountType.FixedAmount,
            discountValue: 200m);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DiscountValue);
    }

    #endregion

    #region Date Validation

    [Fact]
    public async Task Validate_WhenEndDateIsBeforeStartDate_ShouldHaveError()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(10);
        var endDate = DateTimeOffset.UtcNow.AddDays(5);
        var command = CreateValidCommand(startDate: startDate, endDate: endDate);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EndDate)
            .WithErrorMessage("End date must be after start date.");
    }

    [Fact]
    public async Task Validate_WhenEndDateEqualsStartDate_ShouldHaveError()
    {
        // Arrange
        var date = DateTimeOffset.UtcNow.AddDays(10);
        var command = CreateValidCommand(startDate: date, endDate: date);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EndDate)
            .WithErrorMessage("End date must be after start date.");
    }

    [Fact]
    public async Task Validate_WhenEndDateIsAfterStartDate_ShouldNotHaveError()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(1);
        var endDate = DateTimeOffset.UtcNow.AddDays(30);
        var command = CreateValidCommand(startDate: startDate, endDate: endDate);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.EndDate);
    }

    #endregion

    #region Optional Fields Validation

    [Fact]
    public async Task Validate_WhenMaxDiscountAmountIsZero_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(maxDiscountAmount: 0m);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor("MaxDiscountAmount.Value")
            .WithErrorMessage("Maximum discount amount must be greater than zero.");
    }

    [Fact]
    public async Task Validate_WhenMaxDiscountAmountIsPositive_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(maxDiscountAmount: 100000m);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor("MaxDiscountAmount.Value");
    }

    [Fact]
    public async Task Validate_WhenMinOrderValueIsNegative_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(minOrderValue: -1m);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor("MinOrderValue.Value")
            .WithErrorMessage("Minimum order value must be non-negative.");
    }

    [Fact]
    public async Task Validate_WhenMinOrderValueIsZero_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(minOrderValue: 0m);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor("MinOrderValue.Value");
    }

    [Fact]
    public async Task Validate_WhenMinItemQuantityIsZero_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(minItemQuantity: 0);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor("MinItemQuantity.Value")
            .WithErrorMessage("Minimum item quantity must be greater than zero.");
    }

    [Fact]
    public async Task Validate_WhenUsageLimitTotalIsZero_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(usageLimitTotal: 0);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor("UsageLimitTotal.Value")
            .WithErrorMessage("Total usage limit must be greater than zero.");
    }

    [Fact]
    public async Task Validate_WhenUsageLimitPerUserIsZero_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(usageLimitPerUser: 0);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor("UsageLimitPerUser.Value")
            .WithErrorMessage("Per-user usage limit must be greater than zero.");
    }

    [Fact]
    public async Task Validate_WhenOptionalFieldsAreNull_ShouldNotHaveErrors()
    {
        // Arrange
        var command = CreateValidCommand(
            maxDiscountAmount: null,
            minOrderValue: null,
            minItemQuantity: null,
            usageLimitTotal: null,
            usageLimitPerUser: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor("MaxDiscountAmount.Value");
        result.ShouldNotHaveValidationErrorFor("MinOrderValue.Value");
        result.ShouldNotHaveValidationErrorFor("MinItemQuantity.Value");
        result.ShouldNotHaveValidationErrorFor("UsageLimitTotal.Value");
        result.ShouldNotHaveValidationErrorFor("UsageLimitPerUser.Value");
    }

    #endregion

    #region Enum Validation

    [Fact]
    public async Task Validate_WhenPromotionTypeIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(promotionType: PromotionType.FlashSale);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PromotionType);
    }

    [Fact]
    public async Task Validate_WhenDiscountTypeIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(discountType: DiscountType.FixedAmount);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DiscountType);
    }

    [Fact]
    public async Task Validate_WhenApplyLevelIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(applyLevel: PromotionApplyLevel.Product);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ApplyLevel);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new CreatePromotionCommand(
            "Summer Sale 2026",
            "SUMMER2026",
            "Get 20% off on all summer items",
            PromotionType.VoucherCode,
            DiscountType.Percentage,
            20m,
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(90),
            PromotionApplyLevel.Cart,
            100000m,
            200000m,
            1,
            1000,
            3);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenMinimalValidCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new CreatePromotionCommand(
            "Sale",
            "SALE",
            null,
            PromotionType.VoucherCode,
            DiscountType.FixedAmount,
            50000m,
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(30));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}

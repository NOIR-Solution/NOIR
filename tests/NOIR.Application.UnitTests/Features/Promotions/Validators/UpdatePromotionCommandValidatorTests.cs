namespace NOIR.Application.UnitTests.Features.Promotions.Validators;

/// <summary>
/// Unit tests for UpdatePromotionCommandValidator.
/// Tests all validation rules for updating a promotion.
/// </summary>
public class UpdatePromotionCommandValidatorTests
{
    private readonly UpdatePromotionCommandValidator _validator = new();

    private static UpdatePromotionCommand CreateValidCommand(
        Guid? id = null,
        string name = "Updated Sale",
        string code = "UPDATED2026",
        string? description = null,
        PromotionType promotionType = PromotionType.VoucherCode,
        DiscountType discountType = DiscountType.Percentage,
        decimal discountValue = 25m,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        PromotionApplyLevel applyLevel = PromotionApplyLevel.Cart,
        decimal? maxDiscountAmount = null,
        decimal? minOrderValue = null,
        int? minItemQuantity = null,
        int? usageLimitTotal = null,
        int? usageLimitPerUser = null)
    {
        return new UpdatePromotionCommand(
            id ?? Guid.NewGuid(),
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

    #region Id Validation

    [Fact]
    public async Task Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(id: Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Promotion ID is required.");
    }

    [Fact]
    public async Task Validate_WhenIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(id: Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Id);
    }

    #endregion

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
    [InlineData("CODE.123")]
    [InlineData("CODE@PROMO")]
    [InlineData("CODE+PROMO")]
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
    [InlineData("VALID123")]
    [InlineData("valid-code")]
    [InlineData("CODE_V2")]
    [InlineData("A")]
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
    public async Task Validate_WhenPercentageExceeds100_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(
            discountType: DiscountType.Percentage,
            discountValue: 150m);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DiscountValue)
            .WithErrorMessage("Percentage discount cannot exceed 100%.");
    }

    [Fact]
    public async Task Validate_WhenPercentageIs50_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(
            discountType: DiscountType.Percentage,
            discountValue: 50m);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DiscountValue);
    }

    [Fact]
    public async Task Validate_WhenFixedAmountOver100_ShouldNotHaveError()
    {
        // Arrange - Fixed amount can exceed 100
        var command = CreateValidCommand(
            discountType: DiscountType.FixedAmount,
            discountValue: 500000m);

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
    public async Task Validate_WhenMinOrderValueIsNegative_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(minOrderValue: -100m);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor("MinOrderValue.Value")
            .WithErrorMessage("Minimum order value must be non-negative.");
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

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdatePromotionCommand(
            Guid.NewGuid(),
            "Updated Flash Sale",
            "FLASH2026",
            "Updated flash sale description",
            PromotionType.FlashSale,
            DiscountType.Percentage,
            30m,
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(60),
            PromotionApplyLevel.Cart,
            150000m,
            300000m,
            2,
            2000,
            5);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenMinimalValidCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdatePromotionCommand(
            Guid.NewGuid(),
            "Sale",
            "SALE",
            null,
            PromotionType.VoucherCode,
            DiscountType.FixedAmount,
            50000m,
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(30),
            PromotionApplyLevel.Cart);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}

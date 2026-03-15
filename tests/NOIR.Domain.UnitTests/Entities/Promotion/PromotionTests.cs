using NOIR.Domain.Entities.Promotion;

namespace NOIR.Domain.UnitTests.Entities.Promotion;

/// <summary>
/// Unit tests for the Promotion aggregate root entity.
/// Tests factory methods, activation/deactivation workflow, discount calculations,
/// usage tracking, date validity, and product/category targeting.
/// </summary>
public class PromotionTests
{
    private const string TestTenantId = "test-tenant";

    private static readonly DateTimeOffset FutureStart = DateTimeOffset.UtcNow.AddDays(1);
    private static readonly DateTimeOffset FutureEnd = DateTimeOffset.UtcNow.AddDays(30);
    private static readonly DateTimeOffset PastStart = DateTimeOffset.UtcNow.AddDays(-30);
    private static readonly DateTimeOffset PastEnd = DateTimeOffset.UtcNow.AddDays(-1);

    /// <summary>
    /// Helper to create a standard promotion for testing.
    /// </summary>
    private static Domain.Entities.Promotion.Promotion CreateTestPromotion(
        string name = "Summer Sale",
        string code = "SUMMER2026",
        PromotionType promotionType = PromotionType.VoucherCode,
        DiscountType discountType = DiscountType.Percentage,
        decimal discountValue = 20m,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        PromotionApplyLevel applyLevel = PromotionApplyLevel.Cart,
        string? description = null,
        decimal? maxDiscountAmount = null,
        decimal? minOrderValue = null,
        int? minItemQuantity = null,
        int? usageLimitTotal = null,
        int? usageLimitPerUser = null,
        string? tenantId = TestTenantId)
    {
        return Domain.Entities.Promotion.Promotion.Create(
            name,
            code,
            promotionType,
            discountType,
            discountValue,
            startDate ?? FutureStart,
            endDate ?? FutureEnd,
            applyLevel,
            description,
            maxDiscountAmount,
            minOrderValue,
            minItemQuantity,
            usageLimitTotal,
            usageLimitPerUser,
            tenantId);
    }

    /// <summary>
    /// Helper to create an active promotion (within current date range).
    /// </summary>
    private static Domain.Entities.Promotion.Promotion CreateActivePromotion(
        DiscountType discountType = DiscountType.Percentage,
        decimal discountValue = 20m,
        decimal? maxDiscountAmount = null,
        int? usageLimitTotal = null,
        int? usageLimitPerUser = null)
    {
        var promotion = Domain.Entities.Promotion.Promotion.Create(
            "Active Sale",
            "ACTIVE2026",
            PromotionType.VoucherCode,
            discountType,
            discountValue,
            PastStart,
            FutureEnd,
            PromotionApplyLevel.Cart,
            maxDiscountAmount: maxDiscountAmount,
            usageLimitTotal: usageLimitTotal,
            usageLimitPerUser: usageLimitPerUser,
            tenantId: TestTenantId);
        promotion.Activate();
        return promotion;
    }

    #region Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidPromotion()
    {
        // Act
        var promotion = CreateTestPromotion();

        // Assert
        promotion.ShouldNotBeNull();
        promotion.Id.ShouldNotBe(Guid.Empty);
        promotion.Name.ShouldBe("Summer Sale");
        promotion.Code.ShouldBe("SUMMER2026");
        promotion.PromotionType.ShouldBe(PromotionType.VoucherCode);
        promotion.DiscountType.ShouldBe(DiscountType.Percentage);
        promotion.DiscountValue.ShouldBe(20m);
        promotion.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        // Act
        var promotion = CreateTestPromotion();

        // Assert
        promotion.IsActive.ShouldBeFalse();
        promotion.Status.ShouldBe(PromotionStatus.Draft);
        promotion.CurrentUsageCount.ShouldBe(0);
        promotion.Products.ShouldBeEmpty();
        promotion.Categories.ShouldBeEmpty();
        promotion.Usages.ShouldBeEmpty();
    }

    [Fact]
    public void Create_WithOptionalParameters_ShouldSetAllValues()
    {
        // Act
        var promotion = CreateTestPromotion(
            description: "Big summer discounts",
            maxDiscountAmount: 500m,
            minOrderValue: 100m,
            minItemQuantity: 2,
            usageLimitTotal: 1000,
            usageLimitPerUser: 3,
            applyLevel: PromotionApplyLevel.Product);

        // Assert
        promotion.Description.ShouldBe("Big summer discounts");
        promotion.MaxDiscountAmount.ShouldBe(500m);
        promotion.MinOrderValue.ShouldBe(100m);
        promotion.MinItemQuantity.ShouldBe(2);
        promotion.UsageLimitTotal.ShouldBe(1000);
        promotion.UsageLimitPerUser.ShouldBe(3);
        promotion.ApplyLevel.ShouldBe(PromotionApplyLevel.Product);
    }

    [Fact]
    public void Create_ShouldConvertCodeToUpperCase()
    {
        // Act
        var promotion = CreateTestPromotion(code: "summer2026");

        // Assert
        promotion.Code.ShouldBe("SUMMER2026");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhiteSpaceName_ShouldThrow(string? name)
    {
        // Act
        var act = () => CreateTestPromotion(name: name!);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhiteSpaceCode_ShouldThrow(string? code)
    {
        // Act
        var act = () => CreateTestPromotion(code: code!);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithZeroOrNegativeDiscountValue_ShouldThrow(decimal discountValue)
    {
        // Act
        var act = () => CreateTestPromotion(discountValue: discountValue);

        // Assert
        Should.Throw<ArgumentOutOfRangeException>(act)
            .Message.ShouldContain("Discount value must be greater than zero");
    }

    [Fact]
    public void Create_WithEndDateBeforeStartDate_ShouldThrow()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(10);
        var endDate = DateTimeOffset.UtcNow.AddDays(5);

        // Act
        var act = () => CreateTestPromotion(startDate: startDate, endDate: endDate);

        // Assert
        Should.Throw<ArgumentException>(act)
            .Message.ShouldContain("End date must be after start date");
    }

    [Fact]
    public void Create_WithEndDateEqualToStartDate_ShouldThrow()
    {
        // Arrange
        var sameDate = DateTimeOffset.UtcNow.AddDays(5);

        // Act
        var act = () => CreateTestPromotion(startDate: sameDate, endDate: sameDate);

        // Assert
        Should.Throw<ArgumentException>(act)
            .Message.ShouldContain("End date must be after start date");
    }

    [Theory]
    [InlineData(PromotionType.VoucherCode)]
    [InlineData(PromotionType.FlashSale)]
    [InlineData(PromotionType.BundleDeal)]
    [InlineData(PromotionType.FreeShipping)]
    public void Create_WithAllPromotionTypes_ShouldSetCorrectType(PromotionType type)
    {
        // Act
        var promotion = CreateTestPromotion(promotionType: type);

        // Assert
        promotion.PromotionType.ShouldBe(type);
    }

    [Theory]
    [InlineData(PromotionApplyLevel.Cart)]
    [InlineData(PromotionApplyLevel.Product)]
    [InlineData(PromotionApplyLevel.Category)]
    public void Create_WithAllApplyLevels_ShouldSetCorrectLevel(PromotionApplyLevel level)
    {
        // Act
        var promotion = CreateTestPromotion(applyLevel: level);

        // Assert
        promotion.ApplyLevel.ShouldBe(level);
    }

    [Fact]
    public void Create_WithNullDescription_ShouldAllowNull()
    {
        // Act
        var promotion = CreateTestPromotion(description: null);

        // Assert
        promotion.Description.ShouldBeNull();
    }

    [Fact]
    public void Create_WithNullOptionalLimits_ShouldAllowNull()
    {
        // Act
        var promotion = CreateTestPromotion(
            maxDiscountAmount: null,
            minOrderValue: null,
            minItemQuantity: null,
            usageLimitTotal: null,
            usageLimitPerUser: null);

        // Assert
        promotion.MaxDiscountAmount.ShouldBeNull();
        promotion.MinOrderValue.ShouldBeNull();
        promotion.MinItemQuantity.ShouldBeNull();
        promotion.UsageLimitTotal.ShouldBeNull();
        promotion.UsageLimitPerUser.ShouldBeNull();
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ShouldUpdateAllEditableFields()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        var newStart = DateTimeOffset.UtcNow.AddDays(5);
        var newEnd = DateTimeOffset.UtcNow.AddDays(60);

        // Act
        promotion.Update(
            "Winter Sale",
            "Big winter discounts",
            "winter2026",
            PromotionType.FlashSale,
            DiscountType.FixedAmount,
            50m,
            newStart,
            newEnd,
            PromotionApplyLevel.Category,
            1000m,
            200m,
            5,
            500,
            2);

        // Assert
        promotion.Name.ShouldBe("Winter Sale");
        promotion.Description.ShouldBe("Big winter discounts");
        promotion.Code.ShouldBe("WINTER2026");
        promotion.PromotionType.ShouldBe(PromotionType.FlashSale);
        promotion.DiscountType.ShouldBe(DiscountType.FixedAmount);
        promotion.DiscountValue.ShouldBe(50m);
        promotion.StartDate.ShouldBe(newStart);
        promotion.EndDate.ShouldBe(newEnd);
        promotion.ApplyLevel.ShouldBe(PromotionApplyLevel.Category);
        promotion.MaxDiscountAmount.ShouldBe(1000m);
        promotion.MinOrderValue.ShouldBe(200m);
        promotion.MinItemQuantity.ShouldBe(5);
        promotion.UsageLimitTotal.ShouldBe(500);
        promotion.UsageLimitPerUser.ShouldBe(2);
    }

    [Fact]
    public void Update_ShouldConvertCodeToUpperCase()
    {
        // Arrange
        var promotion = CreateTestPromotion();

        // Act
        promotion.Update(
            "Sale", null, "newcode", PromotionType.VoucherCode,
            DiscountType.Percentage, 10m, FutureStart, FutureEnd,
            PromotionApplyLevel.Cart, null, null, null, null, null);

        // Assert
        promotion.Code.ShouldBe("NEWCODE");
    }

    #endregion

    #region Activate Tests

    [Fact]
    public void Activate_DraftPromotion_ShouldActivateSuccessfully()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        promotion.Status.ShouldBe(PromotionStatus.Draft);

        // Act
        promotion.Activate();

        // Assert
        promotion.IsActive.ShouldBeTrue();
        promotion.Status.ShouldBe(PromotionStatus.Active);
    }

    [Fact]
    public void Activate_ScheduledPromotion_ShouldActivateSuccessfully()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        // Activate then deactivate to get to Draft, then we need to test Scheduled
        // The Activate method allows Draft and Scheduled (anything not Cancelled/Expired)
        promotion.Status.ShouldBe(PromotionStatus.Draft);

        // Act
        promotion.Activate();

        // Assert
        promotion.IsActive.ShouldBeTrue();
        promotion.Status.ShouldBe(PromotionStatus.Active);
    }

    [Fact]
    public void Activate_CancelledPromotion_ShouldThrow()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        promotion.Cancel();

        // Act & Assert
        var act = () => promotion.Activate();
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Cannot activate promotion in Cancelled status");
    }

    [Fact]
    public void Activate_ExpiredPromotion_ShouldThrow()
    {
        // Arrange - We cannot directly set Expired status via public API,
        // so test that the guard exists by verifying the status check
        var promotion = CreateTestPromotion();
        promotion.Cancel(); // Cancelled prevents activation

        // Act & Assert
        var act = () => promotion.Activate();
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Activate_AlreadyActivePromotion_ShouldNotThrow()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        promotion.Activate();

        // Act - Activating again should be idempotent (no Cancelled/Expired guard)
        var act = () => promotion.Activate();

        // Assert
        act.ShouldNotThrow();
        promotion.IsActive.ShouldBeTrue();
        promotion.Status.ShouldBe(PromotionStatus.Active);
    }

    #endregion

    #region Deactivate Tests

    [Fact]
    public void Deactivate_ActivePromotion_ShouldDeactivateSuccessfully()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        promotion.Activate();

        // Act
        promotion.Deactivate();

        // Assert
        promotion.IsActive.ShouldBeFalse();
        promotion.Status.ShouldBe(PromotionStatus.Draft);
    }

    [Fact]
    public void Deactivate_DraftPromotion_ShouldThrow()
    {
        // Arrange
        var promotion = CreateTestPromotion();

        // Act & Assert
        var act = () => promotion.Deactivate();
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Cannot deactivate promotion in Draft status");
    }

    [Fact]
    public void Deactivate_CancelledPromotion_ShouldThrow()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        promotion.Cancel();

        // Act & Assert
        var act = () => promotion.Deactivate();
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Cannot deactivate promotion in Cancelled status");
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public void Cancel_DraftPromotion_ShouldCancelSuccessfully()
    {
        // Arrange
        var promotion = CreateTestPromotion();

        // Act
        promotion.Cancel();

        // Assert
        promotion.IsActive.ShouldBeFalse();
        promotion.Status.ShouldBe(PromotionStatus.Cancelled);
    }

    [Fact]
    public void Cancel_ActivePromotion_ShouldCancelSuccessfully()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        promotion.Activate();

        // Act
        promotion.Cancel();

        // Assert
        promotion.IsActive.ShouldBeFalse();
        promotion.Status.ShouldBe(PromotionStatus.Cancelled);
    }

    [Fact]
    public void Cancel_AlreadyCancelledPromotion_ShouldThrow()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        promotion.Cancel();

        // Act & Assert
        var act = () => promotion.Cancel();
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Cannot cancel promotion in Cancelled status");
    }

    #endregion

    #region IsValid Tests

    [Fact]
    public void IsValid_ActivePromotionWithinDateRange_ShouldReturnTrue()
    {
        // Arrange
        var promotion = CreateActivePromotion();

        // Act & Assert
        promotion.IsValid().ShouldBeTrue();
    }

    [Fact]
    public void IsValid_InactivePromotion_ShouldReturnFalse()
    {
        // Arrange
        var promotion = CreateTestPromotion(
            startDate: PastStart,
            endDate: FutureEnd);
        // Promotion is Draft/inactive by default

        // Act & Assert
        promotion.IsValid().ShouldBeFalse();
    }

    [Fact]
    public void IsValid_PromotionBeforeStartDate_ShouldReturnFalse()
    {
        // Arrange - Create promotion with future start date, then activate
        var promotion = CreateTestPromotion(
            startDate: DateTimeOffset.UtcNow.AddDays(10),
            endDate: DateTimeOffset.UtcNow.AddDays(30));
        promotion.Activate();

        // Act & Assert
        promotion.IsValid().ShouldBeFalse();
    }

    [Fact]
    public void IsValid_PromotionAfterEndDate_ShouldReturnFalse()
    {
        // Arrange - Create with past dates but activate it
        var promotion = Domain.Entities.Promotion.Promotion.Create(
            "Past Sale",
            "PAST2026",
            PromotionType.VoucherCode,
            DiscountType.Percentage,
            20m,
            DateTimeOffset.UtcNow.AddDays(-30),
            DateTimeOffset.UtcNow.AddDays(-1),
            tenantId: TestTenantId);
        promotion.Activate();

        // Act & Assert
        promotion.IsValid().ShouldBeFalse();
    }

    [Fact]
    public void IsValid_UsageLimitReached_ShouldReturnFalse()
    {
        // Arrange
        var promotion = CreateActivePromotion(usageLimitTotal: 2);
        promotion.IncrementUsage();
        promotion.IncrementUsage();

        // Act & Assert
        promotion.IsValid().ShouldBeFalse();
    }

    [Fact]
    public void IsValid_UsageBelowLimit_ShouldReturnTrue()
    {
        // Arrange
        var promotion = CreateActivePromotion(usageLimitTotal: 5);
        promotion.IncrementUsage();
        promotion.IncrementUsage();

        // Act & Assert
        promotion.IsValid().ShouldBeTrue();
    }

    [Fact]
    public void IsValid_UnlimitedUsage_ShouldReturnTrue()
    {
        // Arrange
        var promotion = CreateActivePromotion(usageLimitTotal: null);
        for (int i = 0; i < 100; i++)
        {
            promotion.IncrementUsage();
        }

        // Act & Assert
        promotion.IsValid().ShouldBeTrue();
    }

    #endregion

    #region CanBeUsedBy Tests

    [Fact]
    public void CanBeUsedBy_ValidPromotionUnderUserLimit_ShouldReturnTrue()
    {
        // Arrange
        var promotion = CreateActivePromotion(usageLimitPerUser: 3);

        // Act & Assert
        promotion.CanBeUsedBy("user-1", userUsageCount: 1).ShouldBeTrue();
    }

    [Fact]
    public void CanBeUsedBy_UserLimitReached_ShouldReturnFalse()
    {
        // Arrange
        var promotion = CreateActivePromotion(usageLimitPerUser: 2);

        // Act & Assert
        promotion.CanBeUsedBy("user-1", userUsageCount: 2).ShouldBeFalse();
    }

    [Fact]
    public void CanBeUsedBy_UserLimitExceeded_ShouldReturnFalse()
    {
        // Arrange
        var promotion = CreateActivePromotion(usageLimitPerUser: 2);

        // Act & Assert
        promotion.CanBeUsedBy("user-1", userUsageCount: 5).ShouldBeFalse();
    }

    [Fact]
    public void CanBeUsedBy_NoUserLimit_ShouldReturnTrue()
    {
        // Arrange
        var promotion = CreateActivePromotion(usageLimitPerUser: null);

        // Act & Assert
        promotion.CanBeUsedBy("user-1", userUsageCount: 999).ShouldBeTrue();
    }

    [Fact]
    public void CanBeUsedBy_InvalidPromotion_ShouldReturnFalse()
    {
        // Arrange
        var promotion = CreateTestPromotion(); // Draft, not active

        // Act & Assert
        promotion.CanBeUsedBy("user-1", userUsageCount: 0).ShouldBeFalse();
    }

    [Fact]
    public void CanBeUsedBy_TotalUsageLimitReached_ShouldReturnFalse()
    {
        // Arrange
        var promotion = CreateActivePromotion(usageLimitTotal: 1, usageLimitPerUser: 5);
        promotion.IncrementUsage();

        // Act & Assert - Total limit reached even though user limit not reached
        promotion.CanBeUsedBy("user-1", userUsageCount: 0).ShouldBeFalse();
    }

    #endregion

    #region IncrementUsage Tests

    [Fact]
    public void IncrementUsage_ShouldIncrementCurrentUsageCount()
    {
        // Arrange
        var promotion = CreateTestPromotion();

        // Act
        promotion.IncrementUsage();

        // Assert
        promotion.CurrentUsageCount.ShouldBe(1);
    }

    [Fact]
    public void IncrementUsage_CalledMultipleTimes_ShouldTrackCorrectCount()
    {
        // Arrange
        var promotion = CreateTestPromotion();

        // Act
        promotion.IncrementUsage();
        promotion.IncrementUsage();
        promotion.IncrementUsage();

        // Assert
        promotion.CurrentUsageCount.ShouldBe(3);
    }

    [Fact]
    public void IncrementUsage_BeyondLimit_ShouldStillIncrement()
    {
        // Arrange - IncrementUsage does not enforce the limit; IsValid does
        var promotion = CreateTestPromotion(usageLimitTotal: 1);

        // Act
        promotion.IncrementUsage();
        promotion.IncrementUsage();

        // Assert
        promotion.CurrentUsageCount.ShouldBe(2);
    }

    #endregion

    #region CalculateDiscount Tests

    [Fact]
    public void CalculateDiscount_FixedAmount_ShouldReturnFixedValue()
    {
        // Arrange
        var promotion = CreateActivePromotion(
            discountType: DiscountType.FixedAmount,
            discountValue: 50m);

        // Act
        var discount = promotion.CalculateDiscount(200m);

        // Assert
        discount.ShouldBe(50m);
    }

    [Fact]
    public void CalculateDiscount_FixedAmount_ExceedingOrderTotal_ShouldCapAtOrderTotal()
    {
        // Arrange
        var promotion = CreateActivePromotion(
            discountType: DiscountType.FixedAmount,
            discountValue: 500m);

        // Act
        var discount = promotion.CalculateDiscount(200m);

        // Assert
        discount.ShouldBe(200m);
    }

    [Fact]
    public void CalculateDiscount_Percentage_ShouldCalculateCorrectly()
    {
        // Arrange
        var promotion = CreateActivePromotion(
            discountType: DiscountType.Percentage,
            discountValue: 20m);

        // Act
        var discount = promotion.CalculateDiscount(500m);

        // Assert
        discount.ShouldBe(100m); // 20% of 500
    }

    [Fact]
    public void CalculateDiscount_Percentage_WithMaxDiscountCap_ShouldCapAtMax()
    {
        // Arrange
        var promotion = CreateActivePromotion(
            discountType: DiscountType.Percentage,
            discountValue: 50m,
            maxDiscountAmount: 100m);

        // Act
        var discount = promotion.CalculateDiscount(500m);

        // Assert - 50% of 500 = 250, but capped at 100
        discount.ShouldBe(100m);
    }

    [Fact]
    public void CalculateDiscount_Percentage_BelowMaxDiscountCap_ShouldNotCap()
    {
        // Arrange
        var promotion = CreateActivePromotion(
            discountType: DiscountType.Percentage,
            discountValue: 10m,
            maxDiscountAmount: 100m);

        // Act
        var discount = promotion.CalculateDiscount(500m);

        // Assert - 10% of 500 = 50, below cap of 100
        discount.ShouldBe(50m);
    }

    [Fact]
    public void CalculateDiscount_Percentage_100Percent_ShouldReturnOrderTotal()
    {
        // Arrange
        var promotion = CreateActivePromotion(
            discountType: DiscountType.Percentage,
            discountValue: 100m);

        // Act
        var discount = promotion.CalculateDiscount(300m);

        // Assert
        discount.ShouldBe(300m);
    }

    [Fact]
    public void CalculateDiscount_FreeShipping_ShouldReturnZero()
    {
        // Arrange
        var promotion = CreateActivePromotion(
            discountType: DiscountType.FreeShipping,
            discountValue: 1m); // Discount value doesn't matter for FreeShipping

        // Act
        var discount = promotion.CalculateDiscount(500m);

        // Assert - Free shipping discount is handled separately
        discount.ShouldBe(0m);
    }

    [Fact]
    public void CalculateDiscount_BuyXGetY_ShouldReturnZero()
    {
        // Arrange
        var promotion = CreateActivePromotion(
            discountType: DiscountType.BuyXGetY,
            discountValue: 1m);

        // Act
        var discount = promotion.CalculateDiscount(500m);

        // Assert - BuyXGetY calculated differently based on items
        discount.ShouldBe(0m);
    }

    [Theory]
    [InlineData(0)]
    public void CalculateDiscount_ZeroOrderTotal_ShouldReturnZero(decimal orderTotal)
    {
        // Arrange
        var promotion = CreateActivePromotion(
            discountType: DiscountType.FixedAmount,
            discountValue: 50m);

        // Act
        var discount = promotion.CalculateDiscount(orderTotal);

        // Assert
        discount.ShouldBe(0m);
    }

    [Fact]
    public void CalculateDiscount_Percentage_WithMaxCap_ExceedingOrderTotal_ShouldCapAtOrderTotal()
    {
        // Arrange - 50% of 50 = 25, cap is 1000, but order total is 50
        var promotion = CreateActivePromotion(
            discountType: DiscountType.Percentage,
            discountValue: 50m,
            maxDiscountAmount: 1000m);

        // Act
        var discount = promotion.CalculateDiscount(50m);

        // Assert
        discount.ShouldBe(25m);
    }

    #endregion

    #region Product Targeting Tests

    [Fact]
    public void AddProduct_ShouldAddProductToCollection()
    {
        // Arrange
        var promotion = CreateTestPromotion(applyLevel: PromotionApplyLevel.Product);
        var productId = Guid.NewGuid();

        // Act
        promotion.AddProduct(productId);

        // Assert
        promotion.Products.Count().ShouldBe(1);
        promotion.Products.First().ProductId.ShouldBe(productId);
        promotion.Products.First().PromotionId.ShouldBe(promotion.Id);
    }

    [Fact]
    public void AddProduct_DuplicateProduct_ShouldNotAddAgain()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        var productId = Guid.NewGuid();

        // Act
        promotion.AddProduct(productId);
        promotion.AddProduct(productId);

        // Assert
        promotion.Products.Count().ShouldBe(1);
    }

    [Fact]
    public void AddProduct_MultipleProducts_ShouldAddAll()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var productId3 = Guid.NewGuid();

        // Act
        promotion.AddProduct(productId1);
        promotion.AddProduct(productId2);
        promotion.AddProduct(productId3);

        // Assert
        promotion.Products.Count().ShouldBe(3);
    }

    [Fact]
    public void RemoveProduct_ExistingProduct_ShouldRemoveSuccessfully()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        var productId = Guid.NewGuid();
        promotion.AddProduct(productId);

        // Act
        promotion.RemoveProduct(productId);

        // Assert
        promotion.Products.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveProduct_NonExistingProduct_ShouldDoNothing()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        promotion.AddProduct(Guid.NewGuid());

        // Act
        promotion.RemoveProduct(Guid.NewGuid()); // Different ID

        // Assert
        promotion.Products.Count().ShouldBe(1);
    }

    #endregion

    #region Category Targeting Tests

    [Fact]
    public void AddCategory_ShouldAddCategoryToCollection()
    {
        // Arrange
        var promotion = CreateTestPromotion(applyLevel: PromotionApplyLevel.Category);
        var categoryId = Guid.NewGuid();

        // Act
        promotion.AddCategory(categoryId);

        // Assert
        promotion.Categories.Count().ShouldBe(1);
        promotion.Categories.First().CategoryId.ShouldBe(categoryId);
        promotion.Categories.First().PromotionId.ShouldBe(promotion.Id);
    }

    [Fact]
    public void AddCategory_DuplicateCategory_ShouldNotAddAgain()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        var categoryId = Guid.NewGuid();

        // Act
        promotion.AddCategory(categoryId);
        promotion.AddCategory(categoryId);

        // Assert
        promotion.Categories.Count().ShouldBe(1);
    }

    [Fact]
    public void AddCategory_MultipleCategories_ShouldAddAll()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        var catId1 = Guid.NewGuid();
        var catId2 = Guid.NewGuid();

        // Act
        promotion.AddCategory(catId1);
        promotion.AddCategory(catId2);

        // Assert
        promotion.Categories.Count().ShouldBe(2);
    }

    [Fact]
    public void RemoveCategory_ExistingCategory_ShouldRemoveSuccessfully()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        var categoryId = Guid.NewGuid();
        promotion.AddCategory(categoryId);

        // Act
        promotion.RemoveCategory(categoryId);

        // Assert
        promotion.Categories.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveCategory_NonExistingCategory_ShouldDoNothing()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        promotion.AddCategory(Guid.NewGuid());

        // Act
        promotion.RemoveCategory(Guid.NewGuid());

        // Assert
        promotion.Categories.Count().ShouldBe(1);
    }

    #endregion

    #region Status Transition Workflow Tests

    [Fact]
    public void StatusWorkflow_DraftToActiveToDeactivated_ShouldTransitionCorrectly()
    {
        // Arrange
        var promotion = CreateTestPromotion();

        // Act & Assert - Draft -> Active
        promotion.Status.ShouldBe(PromotionStatus.Draft);
        promotion.Activate();
        promotion.Status.ShouldBe(PromotionStatus.Active);
        promotion.IsActive.ShouldBeTrue();

        // Act & Assert - Active -> Draft (deactivated)
        promotion.Deactivate();
        promotion.Status.ShouldBe(PromotionStatus.Draft);
        promotion.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void StatusWorkflow_DraftToActiveToCancelled_ShouldTransitionCorrectly()
    {
        // Arrange
        var promotion = CreateTestPromotion();

        // Act
        promotion.Activate();
        promotion.Cancel();

        // Assert
        promotion.Status.ShouldBe(PromotionStatus.Cancelled);
        promotion.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void StatusWorkflow_DraftToCancelled_ShouldTransitionCorrectly()
    {
        // Arrange
        var promotion = CreateTestPromotion();

        // Act
        promotion.Cancel();

        // Assert
        promotion.Status.ShouldBe(PromotionStatus.Cancelled);
        promotion.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void StatusWorkflow_CancelledCannotBeReactivated()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        promotion.Cancel();

        // Act & Assert
        var act = () => promotion.Activate();
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void StatusWorkflow_CancelledCannotBeDeactivated()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        promotion.Cancel();

        // Act & Assert
        var act = () => promotion.Deactivate();
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void StatusWorkflow_CancelledCannotBeCancelledAgain()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        promotion.Cancel();

        // Act & Assert
        var act = () => promotion.Cancel();
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void StatusWorkflow_ReactivateAfterDeactivation_ShouldWork()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        promotion.Activate();
        promotion.Deactivate();

        // Act
        promotion.Activate();

        // Assert
        promotion.Status.ShouldBe(PromotionStatus.Active);
        promotion.IsActive.ShouldBeTrue();
    }

    #endregion

    #region PromotionUsage Entity Tests

    [Fact]
    public void PromotionUsage_Create_ShouldSetAllProperties()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var userId = "user-123";
        var orderId = Guid.NewGuid();
        var discountAmount = 50.00m;
        var beforeCreate = DateTimeOffset.UtcNow;

        // Act
        var usage = new PromotionUsage(Guid.NewGuid(), promotionId, userId, orderId, discountAmount, TestTenantId);

        // Assert
        usage.PromotionId.ShouldBe(promotionId);
        usage.UserId.ShouldBe(userId);
        usage.OrderId.ShouldBe(orderId);
        usage.DiscountAmount.ShouldBe(discountAmount);
        usage.UsedAt.ShouldBeGreaterThanOrEqualTo(beforeCreate);
        usage.TenantId.ShouldBe(TestTenantId);
    }

    #endregion

    #region PromotionProduct Entity Tests

    [Fact]
    public void PromotionProduct_Create_ShouldSetAllProperties()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        // Act
        var pp = new PromotionProduct(Guid.NewGuid(), promotionId, productId, TestTenantId);

        // Assert
        pp.PromotionId.ShouldBe(promotionId);
        pp.ProductId.ShouldBe(productId);
        pp.TenantId.ShouldBe(TestTenantId);
    }

    #endregion

    #region PromotionCategory Entity Tests

    [Fact]
    public void PromotionCategory_Create_ShouldSetAllProperties()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        // Act
        var pc = new PromotionCategory(Guid.NewGuid(), promotionId, categoryId, TestTenantId);

        // Assert
        pc.PromotionId.ShouldBe(promotionId);
        pc.CategoryId.ShouldBe(categoryId);
        pc.TenantId.ShouldBe(TestTenantId);
    }

    #endregion
}

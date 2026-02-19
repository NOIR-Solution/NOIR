using NOIR.Domain.Entities.Promotion;

namespace NOIR.Domain.UnitTests.Entities.Promotion;

/// <summary>
/// Unit tests for the PromotionCategory junction entity.
/// Tests constructor initialization and property setting.
/// </summary>
public class PromotionCategoryTests
{
    private const string TestTenantId = "test-tenant";

    #region Constructor Tests

    [Fact]
    public void Constructor_WithAllParameters_ShouldSetAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var promotionId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        // Act
        var pc = new PromotionCategory(id, promotionId, categoryId, TestTenantId);

        // Assert
        pc.Id.Should().Be(id);
        pc.PromotionId.Should().Be(promotionId);
        pc.CategoryId.Should().Be(categoryId);
        pc.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Constructor_WithNullTenantId_ShouldAllowNull()
    {
        // Arrange
        var id = Guid.NewGuid();
        var promotionId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        // Act
        var pc = new PromotionCategory(id, promotionId, categoryId, null);

        // Assert
        pc.TenantId.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldPreserveExactIds()
    {
        // Arrange
        var id = Guid.NewGuid();
        var promotionId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        // Act
        var pc = new PromotionCategory(id, promotionId, categoryId, TestTenantId);

        // Assert
        pc.Id.Should().Be(id);
        pc.PromotionId.Should().NotBe(pc.CategoryId);
    }

    [Fact]
    public void Constructor_MultiplInstances_ShouldBeIndependent()
    {
        // Arrange
        var promo1 = Guid.NewGuid();
        var promo2 = Guid.NewGuid();
        var cat1 = Guid.NewGuid();
        var cat2 = Guid.NewGuid();

        // Act
        var pc1 = new PromotionCategory(Guid.NewGuid(), promo1, cat1, TestTenantId);
        var pc2 = new PromotionCategory(Guid.NewGuid(), promo2, cat2, TestTenantId);

        // Assert
        pc1.PromotionId.Should().NotBe(pc2.PromotionId);
        pc1.CategoryId.Should().NotBe(pc2.CategoryId);
        pc1.Id.Should().NotBe(pc2.Id);
    }

    #endregion

    #region Integration with Promotion Aggregate

    [Fact]
    public void AddCategory_ViaPromotionAggregate_ShouldCreatePromotionCategory()
    {
        // Arrange
        var promotion = NOIR.Domain.Entities.Promotion.Promotion.Create(
            "Test Promo", "TEST01", PromotionType.VoucherCode,
            DiscountType.Percentage, 10m,
            DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(30),
            PromotionApplyLevel.Category, tenantId: TestTenantId);
        var categoryId = Guid.NewGuid();

        // Act
        promotion.AddCategory(categoryId);

        // Assert
        promotion.Categories.Should().HaveCount(1);
        var pc = promotion.Categories.First();
        pc.PromotionId.Should().Be(promotion.Id);
        pc.CategoryId.Should().Be(categoryId);
    }

    [Fact]
    public void AddAndRemoveCategory_ViaPromotionAggregate_ShouldWorkCorrectly()
    {
        // Arrange
        var promotion = NOIR.Domain.Entities.Promotion.Promotion.Create(
            "Test Promo", "TEST01", PromotionType.VoucherCode,
            DiscountType.Percentage, 10m,
            DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(30),
            tenantId: TestTenantId);
        var catId1 = Guid.NewGuid();
        var catId2 = Guid.NewGuid();

        // Act
        promotion.AddCategory(catId1);
        promotion.AddCategory(catId2);
        promotion.RemoveCategory(catId1);

        // Assert
        promotion.Categories.Should().HaveCount(1);
        promotion.Categories.First().CategoryId.Should().Be(catId2);
    }

    #endregion
}

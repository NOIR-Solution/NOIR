using NOIR.Domain.Entities.Promotion;

namespace NOIR.Domain.UnitTests.Entities.Promotion;

/// <summary>
/// Unit tests for the PromotionProduct junction entity.
/// Tests constructor initialization and integration with Promotion aggregate.
/// </summary>
public class PromotionProductTests
{
    private const string TestTenantId = "test-tenant";

    #region Constructor Tests

    [Fact]
    public void Constructor_WithAllParameters_ShouldSetAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var promotionId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        // Act
        var pp = new PromotionProduct(id, promotionId, productId, TestTenantId);

        // Assert
        pp.Id.Should().Be(id);
        pp.PromotionId.Should().Be(promotionId);
        pp.ProductId.Should().Be(productId);
        pp.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Constructor_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var pp = new PromotionProduct(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null);

        // Assert
        pp.TenantId.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldPreserveExactIds()
    {
        // Arrange
        var id = Guid.NewGuid();
        var promotionId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        // Act
        var pp = new PromotionProduct(id, promotionId, productId, TestTenantId);

        // Assert
        pp.Id.Should().Be(id);
        pp.PromotionId.Should().Be(promotionId);
        pp.ProductId.Should().Be(productId);
    }

    [Fact]
    public void Constructor_MultipleInstances_ShouldBeIndependent()
    {
        // Arrange & Act
        var pp1 = new PromotionProduct(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TestTenantId);
        var pp2 = new PromotionProduct(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TestTenantId);

        // Assert
        pp1.Id.Should().NotBe(pp2.Id);
        pp1.PromotionId.Should().NotBe(pp2.PromotionId);
        pp1.ProductId.Should().NotBe(pp2.ProductId);
    }

    #endregion

    #region Integration with Promotion Aggregate

    [Fact]
    public void AddProduct_ViaPromotionAggregate_ShouldCreatePromotionProduct()
    {
        // Arrange
        var promotion = NOIR.Domain.Entities.Promotion.Promotion.Create(
            "Product Sale", "PRODSALE", PromotionType.VoucherCode,
            DiscountType.Percentage, 15m,
            DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(30),
            PromotionApplyLevel.Product, tenantId: TestTenantId);
        var productId = Guid.NewGuid();

        // Act
        promotion.AddProduct(productId);

        // Assert
        promotion.Products.Should().HaveCount(1);
        var pp = promotion.Products.First();
        pp.PromotionId.Should().Be(promotion.Id);
        pp.ProductId.Should().Be(productId);
    }

    [Fact]
    public void AddProduct_Duplicate_ShouldNotAddAgain()
    {
        // Arrange
        var promotion = NOIR.Domain.Entities.Promotion.Promotion.Create(
            "Sale", "SALE01", PromotionType.VoucherCode,
            DiscountType.Percentage, 10m,
            DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(30),
            tenantId: TestTenantId);
        var productId = Guid.NewGuid();

        // Act
        promotion.AddProduct(productId);
        promotion.AddProduct(productId);

        // Assert
        promotion.Products.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveProduct_ShouldRemoveCorrectProduct()
    {
        // Arrange
        var promotion = NOIR.Domain.Entities.Promotion.Promotion.Create(
            "Sale", "SALE01", PromotionType.VoucherCode,
            DiscountType.Percentage, 10m,
            DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(30),
            tenantId: TestTenantId);
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        promotion.AddProduct(productId1);
        promotion.AddProduct(productId2);

        // Act
        promotion.RemoveProduct(productId1);

        // Assert
        promotion.Products.Should().HaveCount(1);
        promotion.Products.First().ProductId.Should().Be(productId2);
    }

    #endregion
}

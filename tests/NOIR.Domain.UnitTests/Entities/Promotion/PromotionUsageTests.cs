using NOIR.Domain.Entities.Promotion;

namespace NOIR.Domain.UnitTests.Entities.Promotion;

/// <summary>
/// Unit tests for the PromotionUsage entity.
/// Tests constructor initialization, timestamp generation, and property values.
/// </summary>
public class PromotionUsageTests
{
    private const string TestTenantId = "test-tenant";

    #region Constructor Tests

    [Fact]
    public void Constructor_WithAllParameters_ShouldSetAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var promotionId = Guid.NewGuid();
        var userId = "user-123";
        var orderId = Guid.NewGuid();
        var discountAmount = 50_000m;
        var beforeCreate = DateTimeOffset.UtcNow;

        // Act
        var usage = new PromotionUsage(id, promotionId, userId, orderId, discountAmount, TestTenantId);

        // Assert
        usage.Id.Should().Be(id);
        usage.PromotionId.Should().Be(promotionId);
        usage.UserId.Should().Be(userId);
        usage.OrderId.Should().Be(orderId);
        usage.DiscountAmount.Should().Be(discountAmount);
        usage.TenantId.Should().Be(TestTenantId);
        usage.UsedAt.Should().BeOnOrAfter(beforeCreate);
    }

    [Fact]
    public void Constructor_ShouldSetUsedAtToCurrentTime()
    {
        // Arrange
        var beforeCreate = DateTimeOffset.UtcNow;

        // Act
        var usage = new PromotionUsage(
            Guid.NewGuid(), Guid.NewGuid(), "user-1", Guid.NewGuid(), 100m, TestTenantId);

        // Assert
        var afterCreate = DateTimeOffset.UtcNow;
        usage.UsedAt.Should().BeOnOrAfter(beforeCreate);
        usage.UsedAt.Should().BeOnOrBefore(afterCreate);
    }

    [Fact]
    public void Constructor_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var usage = new PromotionUsage(
            Guid.NewGuid(), Guid.NewGuid(), "user-1", Guid.NewGuid(), 100m, null);

        // Assert
        usage.TenantId.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithZeroDiscount_ShouldAllowZero()
    {
        // Act
        var usage = new PromotionUsage(
            Guid.NewGuid(), Guid.NewGuid(), "user-1", Guid.NewGuid(), 0m, TestTenantId);

        // Assert
        usage.DiscountAmount.Should().Be(0m);
    }

    [Fact]
    public void Constructor_WithLargeDiscount_ShouldStoreValue()
    {
        // Act
        var usage = new PromotionUsage(
            Guid.NewGuid(), Guid.NewGuid(), "user-1", Guid.NewGuid(), 10_000_000m, TestTenantId);

        // Assert
        usage.DiscountAmount.Should().Be(10_000_000m);
    }

    [Fact]
    public void Constructor_MultipleInstances_ShouldHaveIndependentTimestamps()
    {
        // Act
        var usage1 = new PromotionUsage(
            Guid.NewGuid(), Guid.NewGuid(), "user-1", Guid.NewGuid(), 100m, TestTenantId);
        var usage2 = new PromotionUsage(
            Guid.NewGuid(), Guid.NewGuid(), "user-2", Guid.NewGuid(), 200m, TestTenantId);

        // Assert
        usage1.UsedAt.Should().BeOnOrBefore(usage2.UsedAt);
    }

    [Fact]
    public void Constructor_ShouldPreserveUserId()
    {
        // Act
        var usage = new PromotionUsage(
            Guid.NewGuid(), Guid.NewGuid(), "specific-user-id", Guid.NewGuid(), 100m, TestTenantId);

        // Assert
        usage.UserId.Should().Be("specific-user-id");
    }

    #endregion
}

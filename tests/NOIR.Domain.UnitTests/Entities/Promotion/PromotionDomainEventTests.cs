using NOIR.Domain.Entities.Promotion;
using NOIR.Domain.Events.Promotion;

namespace NOIR.Domain.UnitTests.Entities.Promotion;

/// <summary>
/// Unit tests verifying that the Promotion aggregate root raises
/// the correct domain events for creation, activation, deactivation, and usage.
/// </summary>
public class PromotionDomainEventTests
{
    private const string TestTenantId = "test-tenant";

    private static readonly DateTimeOffset FutureStart = DateTimeOffset.UtcNow.AddDays(1);
    private static readonly DateTimeOffset FutureEnd = DateTimeOffset.UtcNow.AddDays(30);
    private static readonly DateTimeOffset PastStart = DateTimeOffset.UtcNow.AddDays(-30);

    private static Domain.Entities.Promotion.Promotion CreateTestPromotion(
        string name = "Summer Sale",
        string code = "SUMMER2026",
        PromotionType promotionType = PromotionType.VoucherCode,
        DiscountType discountType = DiscountType.Percentage,
        decimal discountValue = 20m,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
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
            tenantId: tenantId);
    }

    #region Create Domain Event

    [Fact]
    public void Create_ShouldRaisePromotionCreatedEvent()
    {
        // Act
        var promotion = CreateTestPromotion();

        // Assert
        promotion.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PromotionCreatedEvent>();
    }

    [Fact]
    public void Create_ShouldRaiseEventWithCorrectPromotionId()
    {
        // Act
        var promotion = CreateTestPromotion();

        // Assert
        var domainEvent = promotion.DomainEvents.OfType<PromotionCreatedEvent>().Single();
        domainEvent.PromotionId.Should().Be(promotion.Id);
    }

    [Fact]
    public void Create_ShouldRaiseEventWithUpperCaseCode()
    {
        // Act
        var promotion = CreateTestPromotion(code: "summer2026");

        // Assert
        var domainEvent = promotion.DomainEvents.OfType<PromotionCreatedEvent>().Single();
        domainEvent.Code.Should().Be("SUMMER2026");
    }

    [Fact]
    public void Create_ShouldRaiseEventWithCorrectName()
    {
        // Act
        var promotion = CreateTestPromotion(name: "Winter Clearance");

        // Assert
        var domainEvent = promotion.DomainEvents.OfType<PromotionCreatedEvent>().Single();
        domainEvent.Name.Should().Be("Winter Clearance");
    }

    #endregion

    #region Activate Domain Event

    [Fact]
    public void Activate_ShouldRaisePromotionActivatedEvent()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        promotion.ClearDomainEvents();

        // Act
        promotion.Activate();

        // Assert
        promotion.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PromotionActivatedEvent>();
    }

    [Fact]
    public void Activate_ShouldRaiseEventWithCorrectProperties()
    {
        // Arrange
        var promotion = CreateTestPromotion(code: "FLASH50");
        promotion.ClearDomainEvents();

        // Act
        promotion.Activate();

        // Assert
        var domainEvent = promotion.DomainEvents.OfType<PromotionActivatedEvent>().Single();
        domainEvent.PromotionId.Should().Be(promotion.Id);
        domainEvent.Code.Should().Be("FLASH50");
    }

    [Fact]
    public void Activate_AlreadyActive_ShouldStillRaiseEvent()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        promotion.Activate();
        promotion.ClearDomainEvents();

        // Act - Activating again is idempotent but still raises event
        promotion.Activate();

        // Assert
        promotion.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PromotionActivatedEvent>();
    }

    #endregion

    #region Deactivate Domain Event

    [Fact]
    public void Deactivate_ShouldRaisePromotionDeactivatedEvent()
    {
        // Arrange - Must be Active to deactivate
        var promotion = CreateTestPromotion();
        promotion.Activate();
        promotion.ClearDomainEvents();

        // Act
        promotion.Deactivate();

        // Assert
        promotion.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PromotionDeactivatedEvent>();
    }

    [Fact]
    public void Deactivate_ShouldRaiseEventWithCorrectProperties()
    {
        // Arrange
        var promotion = CreateTestPromotion(code: "WINTER25");
        promotion.Activate();
        promotion.ClearDomainEvents();

        // Act
        promotion.Deactivate();

        // Assert
        var domainEvent = promotion.DomainEvents.OfType<PromotionDeactivatedEvent>().Single();
        domainEvent.PromotionId.Should().Be(promotion.Id);
        domainEvent.Code.Should().Be("WINTER25");
    }

    #endregion

    #region IncrementUsage (PromotionApplied) Domain Event

    [Fact]
    public void IncrementUsage_ShouldRaisePromotionAppliedEvent()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        promotion.ClearDomainEvents();

        // Act
        promotion.IncrementUsage();

        // Assert
        promotion.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PromotionAppliedEvent>();
    }

    [Fact]
    public void IncrementUsage_ShouldRaiseEventWithCorrectProperties()
    {
        // Arrange
        var promotion = CreateTestPromotion(code: "APPLY10");
        promotion.ClearDomainEvents();

        // Act
        promotion.IncrementUsage();

        // Assert
        var domainEvent = promotion.DomainEvents.OfType<PromotionAppliedEvent>().Single();
        domainEvent.PromotionId.Should().Be(promotion.Id);
        domainEvent.Code.Should().Be("APPLY10");
        domainEvent.NewUsageCount.Should().Be(1);
    }

    [Fact]
    public void IncrementUsage_MultipleTimes_ShouldRaiseEventWithCorrectUsageCount()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        promotion.IncrementUsage();
        promotion.IncrementUsage();
        promotion.ClearDomainEvents();

        // Act
        promotion.IncrementUsage();

        // Assert
        var domainEvent = promotion.DomainEvents.OfType<PromotionAppliedEvent>().Single();
        domainEvent.NewUsageCount.Should().Be(3);
    }

    [Fact]
    public void IncrementUsage_MultipleTimes_ShouldRaiseEventForEachCall()
    {
        // Arrange
        var promotion = CreateTestPromotion();
        promotion.ClearDomainEvents();

        // Act
        promotion.IncrementUsage();
        promotion.IncrementUsage();
        promotion.IncrementUsage();

        // Assert
        promotion.DomainEvents.OfType<PromotionAppliedEvent>().Should().HaveCount(3);
    }

    #endregion
}

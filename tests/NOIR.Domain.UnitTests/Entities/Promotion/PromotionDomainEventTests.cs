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
        promotion.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<PromotionCreatedEvent>();
    }

    [Fact]
    public void Create_ShouldRaiseEventWithCorrectPromotionId()
    {
        // Act
        var promotion = CreateTestPromotion();

        // Assert
        var domainEvent = promotion.DomainEvents.OfType<PromotionCreatedEvent>().Single();
        domainEvent.PromotionId.ShouldBe(promotion.Id);
    }

    [Fact]
    public void Create_ShouldRaiseEventWithUpperCaseCode()
    {
        // Act
        var promotion = CreateTestPromotion(code: "summer2026");

        // Assert
        var domainEvent = promotion.DomainEvents.OfType<PromotionCreatedEvent>().Single();
        domainEvent.Code.ShouldBe("SUMMER2026");
    }

    [Fact]
    public void Create_ShouldRaiseEventWithCorrectName()
    {
        // Act
        var promotion = CreateTestPromotion(name: "Winter Clearance");

        // Assert
        var domainEvent = promotion.DomainEvents.OfType<PromotionCreatedEvent>().Single();
        domainEvent.Name.ShouldBe("Winter Clearance");
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
        promotion.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<PromotionActivatedEvent>();
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
        domainEvent.PromotionId.ShouldBe(promotion.Id);
        domainEvent.Code.ShouldBe("FLASH50");
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
        promotion.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<PromotionActivatedEvent>();
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
        promotion.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<PromotionDeactivatedEvent>();
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
        domainEvent.PromotionId.ShouldBe(promotion.Id);
        domainEvent.Code.ShouldBe("WINTER25");
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
        promotion.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<PromotionAppliedEvent>();
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
        domainEvent.PromotionId.ShouldBe(promotion.Id);
        domainEvent.Code.ShouldBe("APPLY10");
        domainEvent.NewUsageCount.ShouldBe(1);
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
        domainEvent.NewUsageCount.ShouldBe(3);
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
        promotion.DomainEvents.OfType<PromotionAppliedEvent>().Count().ShouldBe(3);
    }

    #endregion
}

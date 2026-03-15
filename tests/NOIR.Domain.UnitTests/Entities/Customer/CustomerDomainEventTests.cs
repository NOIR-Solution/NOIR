using NOIR.Domain.Entities.Customer;
using NOIR.Domain.Events.Customer;

namespace NOIR.Domain.UnitTests.Entities.Customer;

/// <summary>
/// Unit tests verifying that the Customer aggregate root raises
/// the correct domain events for all state-changing operations.
/// </summary>
public class CustomerDomainEventTests
{
    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "user-123";
    private const string TestEmail = "john@example.com";
    private const string TestFirstName = "John";
    private const string TestLastName = "Doe";
    private const string TestPhone = "+84912345678";

    private static NOIR.Domain.Entities.Customer.Customer CreateTestCustomer(
        string? userId = TestUserId,
        string email = TestEmail,
        string firstName = TestFirstName,
        string lastName = TestLastName,
        string? phone = TestPhone,
        string? tenantId = TestTenantId)
    {
        return NOIR.Domain.Entities.Customer.Customer.Create(userId, email, firstName, lastName, phone, tenantId);
    }

    #region Create Domain Event

    [Fact]
    public void Create_ShouldRaiseCustomerCreatedEvent()
    {
        // Act
        var customer = CreateTestCustomer();

        // Assert
        customer.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<CustomerCreatedEvent>();
    }

    [Fact]
    public void Create_ShouldRaiseEventWithCorrectCustomerId()
    {
        // Act
        var customer = CreateTestCustomer();

        // Assert
        var domainEvent = customer.DomainEvents.OfType<CustomerCreatedEvent>().Single();
        domainEvent.CustomerId.ShouldBe(customer.Id);
    }

    [Fact]
    public void Create_ShouldRaiseEventWithCorrectEmail()
    {
        // Act
        var customer = CreateTestCustomer(email: "jane@example.com");

        // Assert
        var domainEvent = customer.DomainEvents.OfType<CustomerCreatedEvent>().Single();
        domainEvent.Email.ShouldBe("jane@example.com");
    }

    [Fact]
    public void Create_ShouldRaiseEventWithCorrectFirstAndLastName()
    {
        // Act
        var customer = CreateTestCustomer(firstName: "Jane", lastName: "Smith");

        // Assert
        var domainEvent = customer.DomainEvents.OfType<CustomerCreatedEvent>().Single();
        domainEvent.FirstName.ShouldBe("Jane");
        domainEvent.LastName.ShouldBe("Smith");
    }

    #endregion

    #region UpdateProfile Domain Event

    [Fact]
    public void UpdateProfile_ShouldRaiseCustomerUpdatedEvent()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.ClearDomainEvents();

        // Act
        customer.UpdateProfile("Jane", "Smith", "jane@example.com", "+84987654321");

        // Assert
        customer.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<CustomerUpdatedEvent>();
    }

    [Fact]
    public void UpdateProfile_ShouldRaiseEventWithCorrectProperties()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.ClearDomainEvents();

        // Act
        customer.UpdateProfile("Jane", "Smith", "jane@example.com", null);

        // Assert
        var domainEvent = customer.DomainEvents.OfType<CustomerUpdatedEvent>().Single();
        domainEvent.CustomerId.ShouldBe(customer.Id);
        domainEvent.Email.ShouldBe("jane@example.com");
    }

    #endregion

    #region Deactivate Domain Event

    [Fact]
    public void Deactivate_ShouldRaiseCustomerDeactivatedEvent()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.ClearDomainEvents();

        // Act
        customer.Deactivate();

        // Assert
        customer.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<CustomerDeactivatedEvent>();
    }

    [Fact]
    public void Deactivate_ShouldRaiseEventWithCorrectProperties()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.ClearDomainEvents();

        // Act
        customer.Deactivate();

        // Assert
        var domainEvent = customer.DomainEvents.OfType<CustomerDeactivatedEvent>().Single();
        domainEvent.CustomerId.ShouldBe(customer.Id);
        domainEvent.Email.ShouldBe(TestEmail);
    }

    #endregion

    #region RecalculateSegment Domain Event

    [Fact]
    public void RecalculateSegment_WhenSegmentChanges_ShouldRaiseCustomerSegmentChangedEvent()
    {
        // Arrange - Customer starts as New, move to Active
        var customer = CreateTestCustomer();
        customer.UpdateRfmMetrics(DateTimeOffset.UtcNow.AddDays(-10), 5, 1_000_000m);
        customer.ClearDomainEvents();

        // Act
        customer.RecalculateSegment();

        // Assert
        customer.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<CustomerSegmentChangedEvent>();
    }

    [Fact]
    public void RecalculateSegment_WhenSegmentChanges_ShouldRaiseEventWithOldAndNewSegment()
    {
        // Arrange - Customer starts as New, move to Active
        var customer = CreateTestCustomer();
        customer.UpdateRfmMetrics(DateTimeOffset.UtcNow.AddDays(-10), 5, 1_000_000m);
        customer.ClearDomainEvents();

        // Act
        customer.RecalculateSegment();

        // Assert
        var domainEvent = customer.DomainEvents.OfType<CustomerSegmentChangedEvent>().Single();
        domainEvent.CustomerId.ShouldBe(customer.Id);
        domainEvent.OldSegment.ShouldBe(CustomerSegment.New);
        domainEvent.NewSegment.ShouldBe(CustomerSegment.Active);
    }

    [Fact]
    public void RecalculateSegment_WhenSegmentStaysSame_ShouldNotRaiseEvent()
    {
        // Arrange - Customer is New with 0 orders, recalculate should stay New
        var customer = CreateTestCustomer();
        customer.ClearDomainEvents();

        // Act
        customer.RecalculateSegment();

        // Assert
        customer.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void RecalculateSegment_FromActiveToAtRisk_ShouldRaiseEventWithCorrectSegments()
    {
        // Arrange - First move to Active, then move to AtRisk
        var customer = CreateTestCustomer();
        customer.UpdateRfmMetrics(DateTimeOffset.UtcNow.AddDays(-10), 5, 1_000_000m);
        customer.RecalculateSegment();
        customer.Segment.ShouldBe(CustomerSegment.Active);

        // Update to older order date (31-90 days = AtRisk)
        customer.UpdateRfmMetrics(DateTimeOffset.UtcNow.AddDays(-60), 5, 1_000_000m);
        customer.ClearDomainEvents();

        // Act
        customer.RecalculateSegment();

        // Assert
        var domainEvent = customer.DomainEvents.OfType<CustomerSegmentChangedEvent>().Single();
        domainEvent.OldSegment.ShouldBe(CustomerSegment.Active);
        domainEvent.NewSegment.ShouldBe(CustomerSegment.AtRisk);
    }

    #endregion

    #region UpdateTier Domain Event

    [Fact]
    public void AddLoyaltyPoints_WhenTierChanges_ShouldRaiseCustomerTierChangedEvent()
    {
        // Arrange - Standard tier, add enough points for Silver (5000+)
        var customer = CreateTestCustomer();
        customer.ClearDomainEvents();

        // Act
        customer.AddLoyaltyPoints(5000);

        // Assert - Should have both TierChanged and LoyaltyPointsAdded events
        customer.DomainEvents.ShouldContain(e => e is CustomerTierChangedEvent);
    }

    [Fact]
    public void AddLoyaltyPoints_WhenTierChanges_ShouldRaiseEventWithOldAndNewTier()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.ClearDomainEvents();

        // Act
        customer.AddLoyaltyPoints(5000);

        // Assert
        var domainEvent = customer.DomainEvents.OfType<CustomerTierChangedEvent>().Single();
        domainEvent.CustomerId.ShouldBe(customer.Id);
        domainEvent.OldTier.ShouldBe(CustomerTier.Standard);
        domainEvent.NewTier.ShouldBe(CustomerTier.Silver);
    }

    [Fact]
    public void AddLoyaltyPoints_WhenTierStaysSame_ShouldNotRaiseTierChangedEvent()
    {
        // Arrange - Add small amount that won't change tier from Standard
        var customer = CreateTestCustomer();
        customer.ClearDomainEvents();

        // Act
        customer.AddLoyaltyPoints(100);

        // Assert - Should have LoyaltyPointsAdded but NOT TierChanged
        customer.DomainEvents.ShouldNotContain(e => e is CustomerTierChangedEvent);
    }

    [Fact]
    public void AddLoyaltyPoints_TierProgressionFromSilverToGold_ShouldRaiseCorrectEvent()
    {
        // Arrange - Get to Silver first
        var customer = CreateTestCustomer();
        customer.AddLoyaltyPoints(5000); // Silver
        customer.ClearDomainEvents();

        // Act - Add enough to reach Gold (10000 total)
        customer.AddLoyaltyPoints(5000);

        // Assert
        var domainEvent = customer.DomainEvents.OfType<CustomerTierChangedEvent>().Single();
        domainEvent.OldTier.ShouldBe(CustomerTier.Silver);
        domainEvent.NewTier.ShouldBe(CustomerTier.Gold);
    }

    #endregion

    #region AddLoyaltyPoints Domain Event

    [Fact]
    public void AddLoyaltyPoints_ShouldRaiseCustomerLoyaltyPointsAddedEvent()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.ClearDomainEvents();

        // Act
        customer.AddLoyaltyPoints(500);

        // Assert
        customer.DomainEvents.ShouldContain(e => e is CustomerLoyaltyPointsAddedEvent);
    }

    [Fact]
    public void AddLoyaltyPoints_ShouldRaiseEventWithCorrectProperties()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.ClearDomainEvents();

        // Act
        customer.AddLoyaltyPoints(500);

        // Assert
        var domainEvent = customer.DomainEvents.OfType<CustomerLoyaltyPointsAddedEvent>().Single();
        domainEvent.CustomerId.ShouldBe(customer.Id);
        domainEvent.Points.ShouldBe(500);
        domainEvent.NewBalance.ShouldBe(500);
    }

    [Fact]
    public void AddLoyaltyPoints_MultipleTimes_ShouldRaiseEventWithCorrectBalance()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.AddLoyaltyPoints(300);
        customer.ClearDomainEvents();

        // Act
        customer.AddLoyaltyPoints(200);

        // Assert
        var domainEvent = customer.DomainEvents.OfType<CustomerLoyaltyPointsAddedEvent>().Single();
        domainEvent.Points.ShouldBe(200);
        domainEvent.NewBalance.ShouldBe(500);
    }

    #endregion

    #region RedeemLoyaltyPoints Domain Event

    [Fact]
    public void RedeemLoyaltyPoints_ShouldRaiseCustomerLoyaltyPointsRedeemedEvent()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.AddLoyaltyPoints(500);
        customer.ClearDomainEvents();

        // Act
        customer.RedeemLoyaltyPoints(200);

        // Assert
        customer.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<CustomerLoyaltyPointsRedeemedEvent>();
    }

    [Fact]
    public void RedeemLoyaltyPoints_ShouldRaiseEventWithCorrectProperties()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.AddLoyaltyPoints(500);
        customer.ClearDomainEvents();

        // Act
        customer.RedeemLoyaltyPoints(200);

        // Assert
        var domainEvent = customer.DomainEvents.OfType<CustomerLoyaltyPointsRedeemedEvent>().Single();
        domainEvent.CustomerId.ShouldBe(customer.Id);
        domainEvent.Points.ShouldBe(200);
        domainEvent.NewBalance.ShouldBe(300);
    }

    [Fact]
    public void RedeemLoyaltyPoints_ExactBalance_ShouldRaiseEventWithZeroBalance()
    {
        // Arrange
        var customer = CreateTestCustomer();
        customer.AddLoyaltyPoints(300);
        customer.ClearDomainEvents();

        // Act
        customer.RedeemLoyaltyPoints(300);

        // Assert
        var domainEvent = customer.DomainEvents.OfType<CustomerLoyaltyPointsRedeemedEvent>().Single();
        domainEvent.Points.ShouldBe(300);
        domainEvent.NewBalance.ShouldBe(0);
    }

    #endregion
}

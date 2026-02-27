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
        customer.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CustomerCreatedEvent>();
    }

    [Fact]
    public void Create_ShouldRaiseEventWithCorrectCustomerId()
    {
        // Act
        var customer = CreateTestCustomer();

        // Assert
        var domainEvent = customer.DomainEvents.OfType<CustomerCreatedEvent>().Single();
        domainEvent.CustomerId.Should().Be(customer.Id);
    }

    [Fact]
    public void Create_ShouldRaiseEventWithCorrectEmail()
    {
        // Act
        var customer = CreateTestCustomer(email: "jane@example.com");

        // Assert
        var domainEvent = customer.DomainEvents.OfType<CustomerCreatedEvent>().Single();
        domainEvent.Email.Should().Be("jane@example.com");
    }

    [Fact]
    public void Create_ShouldRaiseEventWithCorrectFirstAndLastName()
    {
        // Act
        var customer = CreateTestCustomer(firstName: "Jane", lastName: "Smith");

        // Assert
        var domainEvent = customer.DomainEvents.OfType<CustomerCreatedEvent>().Single();
        domainEvent.FirstName.Should().Be("Jane");
        domainEvent.LastName.Should().Be("Smith");
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
        customer.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CustomerUpdatedEvent>();
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
        domainEvent.CustomerId.Should().Be(customer.Id);
        domainEvent.Email.Should().Be("jane@example.com");
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
        customer.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CustomerDeactivatedEvent>();
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
        domainEvent.CustomerId.Should().Be(customer.Id);
        domainEvent.Email.Should().Be(TestEmail);
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
        customer.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CustomerSegmentChangedEvent>();
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
        domainEvent.CustomerId.Should().Be(customer.Id);
        domainEvent.OldSegment.Should().Be(CustomerSegment.New);
        domainEvent.NewSegment.Should().Be(CustomerSegment.Active);
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
        customer.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RecalculateSegment_FromActiveToAtRisk_ShouldRaiseEventWithCorrectSegments()
    {
        // Arrange - First move to Active, then move to AtRisk
        var customer = CreateTestCustomer();
        customer.UpdateRfmMetrics(DateTimeOffset.UtcNow.AddDays(-10), 5, 1_000_000m);
        customer.RecalculateSegment();
        customer.Segment.Should().Be(CustomerSegment.Active);

        // Update to older order date (31-90 days = AtRisk)
        customer.UpdateRfmMetrics(DateTimeOffset.UtcNow.AddDays(-60), 5, 1_000_000m);
        customer.ClearDomainEvents();

        // Act
        customer.RecalculateSegment();

        // Assert
        var domainEvent = customer.DomainEvents.OfType<CustomerSegmentChangedEvent>().Single();
        domainEvent.OldSegment.Should().Be(CustomerSegment.Active);
        domainEvent.NewSegment.Should().Be(CustomerSegment.AtRisk);
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
        customer.DomainEvents.Should().Contain(e => e is CustomerTierChangedEvent);
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
        domainEvent.CustomerId.Should().Be(customer.Id);
        domainEvent.OldTier.Should().Be(CustomerTier.Standard);
        domainEvent.NewTier.Should().Be(CustomerTier.Silver);
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
        customer.DomainEvents.Should().NotContain(e => e is CustomerTierChangedEvent);
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
        domainEvent.OldTier.Should().Be(CustomerTier.Silver);
        domainEvent.NewTier.Should().Be(CustomerTier.Gold);
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
        customer.DomainEvents.Should().Contain(e => e is CustomerLoyaltyPointsAddedEvent);
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
        domainEvent.CustomerId.Should().Be(customer.Id);
        domainEvent.Points.Should().Be(500);
        domainEvent.NewBalance.Should().Be(500);
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
        domainEvent.Points.Should().Be(200);
        domainEvent.NewBalance.Should().Be(500);
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
        customer.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CustomerLoyaltyPointsRedeemedEvent>();
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
        domainEvent.CustomerId.Should().Be(customer.Id);
        domainEvent.Points.Should().Be(200);
        domainEvent.NewBalance.Should().Be(300);
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
        domainEvent.Points.Should().Be(300);
        domainEvent.NewBalance.Should().Be(0);
    }

    #endregion
}

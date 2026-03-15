using NOIR.Domain.Entities.Customer;

namespace NOIR.Domain.UnitTests.Entities.Customer;

/// <summary>
/// Unit tests for the CustomerGroupMembership junction entity.
/// Tests factory method and property assignments.
/// </summary>
public class CustomerGroupMembershipTests
{
    private const string TestTenantId = "test-tenant";

    #region Create Factory Tests

    [Fact]
    public void Create_WithValidParameters_ShouldCreateValidMembership()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        // Act
        var membership = CustomerGroupMembership.Create(groupId, customerId, TestTenantId);

        // Assert
        membership.ShouldNotBeNull();
        membership.Id.ShouldNotBe(Guid.Empty);
        membership.CustomerGroupId.ShouldBe(groupId);
        membership.CustomerId.ShouldBe(customerId);
        membership.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        // Act
        var membership = CustomerGroupMembership.Create(groupId, customerId);

        // Assert
        membership.TenantId.ShouldBeNull();
    }

    [Fact]
    public void Create_MultipleCalls_ShouldGenerateUniqueIds()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        // Act
        var membership1 = CustomerGroupMembership.Create(groupId, customerId, TestTenantId);
        var membership2 = CustomerGroupMembership.Create(groupId, customerId, TestTenantId);

        // Assert
        membership1.Id.ShouldNotBe(membership2.Id);
    }

    [Fact]
    public void Create_WithDifferentGroupAndCustomer_ShouldTrackBothIds()
    {
        // Arrange
        var groupId1 = Guid.NewGuid();
        var groupId2 = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        // Act
        var membership1 = CustomerGroupMembership.Create(groupId1, customerId, TestTenantId);
        var membership2 = CustomerGroupMembership.Create(groupId2, customerId, TestTenantId);

        // Assert
        membership1.CustomerGroupId.ShouldBe(groupId1);
        membership2.CustomerGroupId.ShouldBe(groupId2);
        membership1.CustomerId.ShouldBe(customerId);
        membership2.CustomerId.ShouldBe(customerId);
    }

    [Fact]
    public void Create_WithEmptyGuids_ShouldSetEmptyGuids()
    {
        // Act
        var membership = CustomerGroupMembership.Create(Guid.Empty, Guid.Empty, TestTenantId);

        // Assert
        membership.CustomerGroupId.ShouldBe(Guid.Empty);
        membership.CustomerId.ShouldBe(Guid.Empty);
        membership.Id.ShouldNotBe(Guid.Empty); // Id is always auto-generated
    }

    #endregion
}

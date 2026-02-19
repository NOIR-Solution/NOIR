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
        membership.Should().NotBeNull();
        membership.Id.Should().NotBe(Guid.Empty);
        membership.CustomerGroupId.Should().Be(groupId);
        membership.CustomerId.Should().Be(customerId);
        membership.TenantId.Should().Be(TestTenantId);
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
        membership.TenantId.Should().BeNull();
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
        membership1.Id.Should().NotBe(membership2.Id);
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
        membership1.CustomerGroupId.Should().Be(groupId1);
        membership2.CustomerGroupId.Should().Be(groupId2);
        membership1.CustomerId.Should().Be(customerId);
        membership2.CustomerId.Should().Be(customerId);
    }

    [Fact]
    public void Create_WithEmptyGuids_ShouldSetEmptyGuids()
    {
        // Act
        var membership = CustomerGroupMembership.Create(Guid.Empty, Guid.Empty, TestTenantId);

        // Assert
        membership.CustomerGroupId.Should().Be(Guid.Empty);
        membership.CustomerId.Should().Be(Guid.Empty);
        membership.Id.Should().NotBe(Guid.Empty); // Id is always auto-generated
    }

    #endregion
}

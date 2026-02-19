using NOIR.Domain.Entities.Customer;

namespace NOIR.Domain.UnitTests.Entities.Customer;

/// <summary>
/// Unit tests for the CustomerGroup aggregate root entity.
/// Tests factory methods, updates, activation, member count management, slug generation, and deletion.
/// </summary>
public class CustomerGroupTests
{
    private const string TestTenantId = "test-tenant";
    private const string TestGroupName = "VIP Customers";
    private const string TestDescription = "High-value customers with premium benefits";

    private static CustomerGroup CreateTestGroup(
        string name = TestGroupName,
        string? description = TestDescription,
        string? tenantId = TestTenantId)
    {
        return CustomerGroup.Create(name, description, tenantId);
    }

    #region Create Factory Tests

    [Fact]
    public void Create_WithAllParameters_ShouldCreateValidGroup()
    {
        // Act
        var group = CreateTestGroup();

        // Assert
        group.Should().NotBeNull();
        group.Id.Should().NotBe(Guid.Empty);
        group.Name.Should().Be(TestGroupName);
        group.Description.Should().Be(TestDescription);
        group.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetIsActiveTrue()
    {
        // Act
        var group = CreateTestGroup();

        // Assert
        group.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldInitializeMemberCountToZero()
    {
        // Act
        var group = CreateTestGroup();

        // Assert
        group.MemberCount.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldInitializeEmptyMemberships()
    {
        // Act
        var group = CreateTestGroup();

        // Assert
        group.Memberships.Should().NotBeNull();
        group.Memberships.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithNullDescription_ShouldAllowNull()
    {
        // Act
        var group = CreateTestGroup(description: null);

        // Assert
        group.Description.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var group = CreateTestGroup(tenantId: null);

        // Assert
        group.TenantId.Should().BeNull();
    }

    [Fact]
    public void Create_MultipleCalls_ShouldGenerateUniqueIds()
    {
        // Act
        var group1 = CreateTestGroup();
        var group2 = CreateTestGroup();

        // Assert
        group1.Id.Should().NotBe(group2.Id);
    }

    #endregion

    #region Slug Generation Tests

    [Fact]
    public void Create_ShouldGenerateSlugFromName()
    {
        // Act
        var group = CreateTestGroup(name: "VIP Customers");

        // Assert
        group.Slug.Should().Be("vip-customers");
    }

    [Fact]
    public void Create_WithSpecialCharacters_ShouldRemoveSpecialChars()
    {
        // Act
        var group = CreateTestGroup(name: "Premium & Gold Members!");

        // Assert
        group.Slug.Should().Be("premium-gold-members");
    }

    [Fact]
    public void Create_WithMultipleSpaces_ShouldCollapseToSingleDash()
    {
        // Act
        var group = CreateTestGroup(name: "VIP   Customers");

        // Assert
        group.Slug.Should().Be("vip-customers");
    }

    [Fact]
    public void Create_WithLeadingTrailingSpaces_ShouldTrim()
    {
        // Act
        var group = CreateTestGroup(name: "  VIP Customers  ");

        // Assert
        group.Slug.Should().Be("vip-customers");
    }

    [Fact]
    public void Create_WithUppercaseName_ShouldLowercaseSlug()
    {
        // Act
        var group = CreateTestGroup(name: "PREMIUM GROUP");

        // Assert
        group.Slug.Should().Be("premium-group");
    }

    [Fact]
    public void Create_WithHyphens_ShouldPreserveHyphens()
    {
        // Act
        var group = CreateTestGroup(name: "first-time-buyers");

        // Assert
        group.Slug.Should().Be("first-time-buyers");
    }

    [Fact]
    public void Create_WithNumbers_ShouldPreserveNumbers()
    {
        // Act
        var group = CreateTestGroup(name: "Tier 1 Customers");

        // Assert
        group.Slug.Should().Be("tier-1-customers");
    }

    [Fact]
    public void Create_WithConsecutiveSpecialChars_ShouldCollapseToSingleDash()
    {
        // Act
        var group = CreateTestGroup(name: "VIP---Customers");

        // Assert
        group.Slug.Should().Be("vip-customers");
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ShouldChangeNameAndDescription()
    {
        // Arrange
        var group = CreateTestGroup();
        var newName = "Premium Customers";
        var newDescription = "Updated description";

        // Act
        group.Update(newName, newDescription);

        // Assert
        group.Name.Should().Be(newName);
        group.Description.Should().Be(newDescription);
    }

    [Fact]
    public void Update_ShouldRegenerateSlug()
    {
        // Arrange
        var group = CreateTestGroup(name: "Original Name");

        // Act
        group.Update("New Name", "Description");

        // Assert
        group.Slug.Should().Be("new-name");
    }

    [Fact]
    public void Update_WithNullDescription_ShouldClearDescription()
    {
        // Arrange
        var group = CreateTestGroup();

        // Act
        group.Update("Updated Name", null);

        // Assert
        group.Description.Should().BeNull();
    }

    [Fact]
    public void Update_ShouldNotAffectMemberCount()
    {
        // Arrange
        var group = CreateTestGroup();
        group.IncrementMemberCount(5);

        // Act
        group.Update("Updated Name", "Updated Desc");

        // Assert
        group.MemberCount.Should().Be(5);
    }

    [Fact]
    public void Update_ShouldNotAffectIsActive()
    {
        // Arrange
        var group = CreateTestGroup();
        group.Deactivate();

        // Act
        group.Update("Updated Name", "Updated Desc");

        // Assert
        group.IsActive.Should().BeFalse();
    }

    #endregion

    #region Activate / Deactivate / SetActive Tests

    [Fact]
    public void Deactivate_ActiveGroup_ShouldSetIsActiveFalse()
    {
        // Arrange
        var group = CreateTestGroup();

        // Act
        group.Deactivate();

        // Assert
        group.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_InactiveGroup_ShouldSetIsActiveTrue()
    {
        // Arrange
        var group = CreateTestGroup();
        group.Deactivate();

        // Act
        group.Activate();

        // Assert
        group.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SetActive_WithTrue_ShouldActivate()
    {
        // Arrange
        var group = CreateTestGroup();
        group.Deactivate();

        // Act
        group.SetActive(true);

        // Assert
        group.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SetActive_WithFalse_ShouldDeactivate()
    {
        // Arrange
        var group = CreateTestGroup();

        // Act
        group.SetActive(false);

        // Assert
        group.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_AlreadyInactive_ShouldRemainInactive()
    {
        // Arrange
        var group = CreateTestGroup();
        group.Deactivate();

        // Act
        group.Deactivate();

        // Assert
        group.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_AlreadyActive_ShouldRemainActive()
    {
        // Arrange
        var group = CreateTestGroup();

        // Act
        group.Activate();

        // Assert
        group.IsActive.Should().BeTrue();
    }

    #endregion

    #region IncrementMemberCount Tests

    [Fact]
    public void IncrementMemberCount_Default_ShouldIncrementByOne()
    {
        // Arrange
        var group = CreateTestGroup();

        // Act
        group.IncrementMemberCount();

        // Assert
        group.MemberCount.Should().Be(1);
    }

    [Fact]
    public void IncrementMemberCount_WithSpecificCount_ShouldIncrementByCount()
    {
        // Arrange
        var group = CreateTestGroup();

        // Act
        group.IncrementMemberCount(5);

        // Assert
        group.MemberCount.Should().Be(5);
    }

    [Fact]
    public void IncrementMemberCount_MultipleTimes_ShouldAccumulate()
    {
        // Arrange
        var group = CreateTestGroup();

        // Act
        group.IncrementMemberCount(3);
        group.IncrementMemberCount(2);
        group.IncrementMemberCount();

        // Assert
        group.MemberCount.Should().Be(6);
    }

    #endregion

    #region DecrementMemberCount Tests

    [Fact]
    public void DecrementMemberCount_Default_ShouldDecrementByOne()
    {
        // Arrange
        var group = CreateTestGroup();
        group.IncrementMemberCount(5);

        // Act
        group.DecrementMemberCount();

        // Assert
        group.MemberCount.Should().Be(4);
    }

    [Fact]
    public void DecrementMemberCount_WithSpecificCount_ShouldDecrementByCount()
    {
        // Arrange
        var group = CreateTestGroup();
        group.IncrementMemberCount(10);

        // Act
        group.DecrementMemberCount(3);

        // Assert
        group.MemberCount.Should().Be(7);
    }

    [Fact]
    public void DecrementMemberCount_BelowZero_ShouldClampToZero()
    {
        // Arrange
        var group = CreateTestGroup();
        group.IncrementMemberCount(2);

        // Act
        group.DecrementMemberCount(5);

        // Assert
        group.MemberCount.Should().Be(0);
    }

    [Fact]
    public void DecrementMemberCount_FromZero_ShouldRemainZero()
    {
        // Arrange
        var group = CreateTestGroup();

        // Act
        group.DecrementMemberCount();

        // Assert
        group.MemberCount.Should().Be(0);
    }

    [Fact]
    public void DecrementMemberCount_ExactCount_ShouldReachZero()
    {
        // Arrange
        var group = CreateTestGroup();
        group.IncrementMemberCount(5);

        // Act
        group.DecrementMemberCount(5);

        // Assert
        group.MemberCount.Should().Be(0);
    }

    #endregion

    #region UpdateMemberCount Tests

    [Fact]
    public void UpdateMemberCount_ShouldSetExactCount()
    {
        // Arrange
        var group = CreateTestGroup();

        // Act
        group.UpdateMemberCount(42);

        // Assert
        group.MemberCount.Should().Be(42);
    }

    [Fact]
    public void UpdateMemberCount_ShouldOverridePreviousCount()
    {
        // Arrange
        var group = CreateTestGroup();
        group.IncrementMemberCount(10);

        // Act
        group.UpdateMemberCount(5);

        // Assert
        group.MemberCount.Should().Be(5);
    }

    [Fact]
    public void UpdateMemberCount_ToZero_ShouldSetToZero()
    {
        // Arrange
        var group = CreateTestGroup();
        group.IncrementMemberCount(10);

        // Act
        group.UpdateMemberCount(0);

        // Assert
        group.MemberCount.Should().Be(0);
    }

    #endregion

    #region MarkAsDeleted Tests

    [Fact]
    public void MarkAsDeleted_ShouldNotThrow()
    {
        // Arrange
        var group = CreateTestGroup();

        // Act
        var act = () => group.MarkAsDeleted();

        // Assert - Soft delete handled by interceptor; method should complete without error
        act.Should().NotThrow();
    }

    #endregion
}

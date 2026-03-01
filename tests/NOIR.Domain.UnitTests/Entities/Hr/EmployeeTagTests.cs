using NOIR.Domain.Entities.Hr;

namespace NOIR.Domain.UnitTests.Entities.Hr;

/// <summary>
/// Unit tests for the EmployeeTag aggregate root entity.
/// Tests factory method, updates, activation/deactivation.
/// </summary>
public class EmployeeTagTests
{
    private const string TestTenantId = "test-tenant";
    private const string TestName = "Senior Developer";
    private const string TestDescription = "Senior-level developers";
    private const string TestColor = "#ef4444";
    private const string DefaultColor = "#6366f1";

    private static EmployeeTag CreateTestTag(
        string name = TestName,
        EmployeeTagCategory category = EmployeeTagCategory.Skill,
        string? tenantId = TestTenantId,
        string? color = null,
        string? description = TestDescription,
        int sortOrder = 0)
    {
        return EmployeeTag.Create(name, category, tenantId, color, description, sortOrder);
    }

    #region Create Factory Tests

    [Fact]
    public void Create_WithValidData_ShouldCreateTag()
    {
        // Act
        var tag = CreateTestTag(color: TestColor);

        // Assert
        tag.Should().NotBeNull();
        tag.Id.Should().NotBe(Guid.Empty);
        tag.Name.Should().Be(TestName);
        tag.Category.Should().Be(EmployeeTagCategory.Skill);
        tag.Color.Should().Be(TestColor);
        tag.Description.Should().Be(TestDescription);
        tag.SortOrder.Should().Be(0);
        tag.IsActive.Should().BeTrue();
        tag.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_WithNullName_ShouldThrow()
    {
        // Act
        var act = () => CreateTestTag(name: null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        // Act
        var act = () => CreateTestTag(name: "");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithWhitespaceName_ShouldThrow()
    {
        // Act
        var act = () => CreateTestTag(name: "   ");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithDefaultColor_ShouldSetDefaultIndigo()
    {
        // Act
        var tag = CreateTestTag(color: null);

        // Assert
        tag.Color.Should().Be(DefaultColor);
    }

    [Fact]
    public void Create_WithCustomColor_ShouldSetColor()
    {
        // Act
        var tag = CreateTestTag(color: TestColor);

        // Assert
        tag.Color.Should().Be(TestColor);
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        // Act
        var tag = CreateTestTag(name: "  Senior Developer  ");

        // Assert
        tag.Name.Should().Be("Senior Developer");
    }

    [Fact]
    public void Create_ShouldTrimColorAndDescription()
    {
        // Act
        var tag = CreateTestTag(color: "  #ef4444  ", description: "  Test description  ");

        // Assert
        tag.Color.Should().Be("#ef4444");
        tag.Description.Should().Be("Test description");
    }

    [Fact]
    public void Create_WithNullOptionalFields_ShouldAllowNulls()
    {
        // Act
        var tag = CreateTestTag(description: null);

        // Assert
        tag.Description.Should().BeNull();
    }

    [Fact]
    public void Create_MultipleCalls_ShouldGenerateUniqueIds()
    {
        // Act
        var tag1 = CreateTestTag();
        var tag2 = CreateTestTag();

        // Assert
        tag1.Id.Should().NotBe(tag2.Id);
    }

    [Fact]
    public void Create_WithSortOrder_ShouldSetSortOrder()
    {
        // Act
        var tag = CreateTestTag(sortOrder: 5);

        // Assert
        tag.SortOrder.Should().Be(5);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_WithValidData_ShouldUpdateAllProperties()
    {
        // Arrange
        var tag = CreateTestTag();

        // Act
        tag.Update("Lead Developer", EmployeeTagCategory.Seniority, "#3b82f6", "Team leads", 3);

        // Assert
        tag.Name.Should().Be("Lead Developer");
        tag.Category.Should().Be(EmployeeTagCategory.Seniority);
        tag.Color.Should().Be("#3b82f6");
        tag.Description.Should().Be("Team leads");
        tag.SortOrder.Should().Be(3);
    }

    [Fact]
    public void Update_WithNullName_ShouldThrow()
    {
        // Arrange
        var tag = CreateTestTag();

        // Act
        var act = () => tag.Update(null!, EmployeeTagCategory.Skill, null, null, 0);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_WithNullColor_ShouldKeepExistingColor()
    {
        // Arrange
        var tag = CreateTestTag(color: TestColor);

        // Act
        tag.Update("Updated", EmployeeTagCategory.Skill, null, null, 0);

        // Assert
        tag.Color.Should().Be(TestColor);
    }

    [Fact]
    public void Update_ShouldTrimNameAndDescription()
    {
        // Arrange
        var tag = CreateTestTag();

        // Act
        tag.Update("  Trimmed Name  ", EmployeeTagCategory.Skill, null, "  Trimmed desc  ", 0);

        // Assert
        tag.Name.Should().Be("Trimmed Name");
        tag.Description.Should().Be("Trimmed desc");
    }

    #endregion

    #region Deactivate / Activate Tests

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        // Arrange
        var tag = CreateTestTag();

        // Act
        tag.Deactivate();

        // Assert
        tag.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveTrue()
    {
        // Arrange
        var tag = CreateTestTag();
        tag.Deactivate();

        // Act
        tag.Activate();

        // Assert
        tag.IsActive.Should().BeTrue();
    }

    #endregion
}

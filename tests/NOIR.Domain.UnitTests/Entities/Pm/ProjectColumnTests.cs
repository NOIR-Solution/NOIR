using NOIR.Domain.Entities.Pm;

namespace NOIR.Domain.UnitTests.Entities.Pm;

/// <summary>
/// Unit tests for the ProjectColumn entity.
/// Tests factory method and update.
/// </summary>
public class ProjectColumnTests
{
    private const string TestTenantId = "test-tenant";
    private static readonly Guid TestProjectId = Guid.NewGuid();

    #region Create Factory Tests

    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        // Act
        var column = ProjectColumn.Create(
            TestProjectId, "In Progress", 1, TestTenantId,
            color: "#3B82F6", wipLimit: 5);

        // Assert
        column.ShouldNotBeNull();
        column.Id.ShouldNotBe(Guid.Empty);
        column.ProjectId.ShouldBe(TestProjectId);
        column.Name.ShouldBe("In Progress");
        column.SortOrder.ShouldBe(1);
        column.Color.ShouldBe("#3B82F6");
        column.WipLimit.ShouldBe(5);
        column.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_WithDefaults_ShouldHaveNullColorAndWipLimit()
    {
        // Act
        var column = ProjectColumn.Create(TestProjectId, "Todo", 0, TestTenantId);

        // Assert
        column.Color.ShouldBeNull();
        column.WipLimit.ShouldBeNull();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        // Act & Assert
        var act = () => ProjectColumn.Create(TestProjectId, "", 0, TestTenantId);
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        // Act
        var column = ProjectColumn.Create(TestProjectId, "  Done  ", 3, TestTenantId);

        // Assert
        column.Name.ShouldBe("Done");
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ShouldUpdateAllProperties()
    {
        // Arrange
        var column = ProjectColumn.Create(TestProjectId, "Todo", 0, TestTenantId);

        // Act
        column.Update("In Review", 2, "#F59E0B", 3);

        // Assert
        column.Name.ShouldBe("In Review");
        column.SortOrder.ShouldBe(2);
        column.Color.ShouldBe("#F59E0B");
        column.WipLimit.ShouldBe(3);
    }

    #endregion
}

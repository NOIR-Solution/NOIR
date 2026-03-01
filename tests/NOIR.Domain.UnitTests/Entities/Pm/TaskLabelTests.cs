using NOIR.Domain.Entities.Pm;

namespace NOIR.Domain.UnitTests.Entities.Pm;

/// <summary>
/// Unit tests for the TaskLabel entity.
/// Tests factory method and update behavior.
/// </summary>
public class TaskLabelTests
{
    private const string TestTenantId = "test-tenant";
    private static readonly Guid TestProjectId = Guid.NewGuid();

    #region Create Factory Tests

    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        // Act
        var label = TaskLabel.Create(TestProjectId, "Bug", "#EF4444", TestTenantId);

        // Assert
        label.Should().NotBeNull();
        label.Id.Should().NotBe(Guid.Empty);
        label.ProjectId.Should().Be(TestProjectId);
        label.Name.Should().Be("Bug");
        label.Color.Should().Be("#EF4444");
        label.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        // Act & Assert
        var act = () => TaskLabel.Create(TestProjectId, "", "#EF4444", TestTenantId);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithWhitespaceName_ShouldThrow()
    {
        // Act & Assert
        var act = () => TaskLabel.Create(TestProjectId, "   ", "#EF4444", TestTenantId);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyColor_ShouldThrow()
    {
        // Act & Assert
        var act = () => TaskLabel.Create(TestProjectId, "Bug", "", TestTenantId);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        // Act
        var label = TaskLabel.Create(TestProjectId, "  Bug  ", "#EF4444", TestTenantId);

        // Assert
        label.Name.Should().Be("Bug");
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldSetNull()
    {
        // Act
        var label = TaskLabel.Create(TestProjectId, "Bug", "#EF4444", null);

        // Assert
        label.TenantId.Should().BeNull();
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ShouldChangeNameAndColor()
    {
        // Arrange
        var label = TaskLabel.Create(TestProjectId, "Bug", "#EF4444", TestTenantId);

        // Act
        label.Update("Feature", "#3B82F6");

        // Assert
        label.Name.Should().Be("Feature");
        label.Color.Should().Be("#3B82F6");
    }

    [Fact]
    public void Update_ShouldTrimName()
    {
        // Arrange
        var label = TaskLabel.Create(TestProjectId, "Bug", "#EF4444", TestTenantId);

        // Act
        label.Update("  Enhancement  ", "#10B981");

        // Assert
        label.Name.Should().Be("Enhancement");
    }

    [Fact]
    public void Update_ShouldNotChangeProjectId()
    {
        // Arrange
        var label = TaskLabel.Create(TestProjectId, "Bug", "#EF4444", TestTenantId);
        var originalProjectId = label.ProjectId;

        // Act
        label.Update("Feature", "#3B82F6");

        // Assert
        label.ProjectId.Should().Be(originalProjectId);
    }

    #endregion
}

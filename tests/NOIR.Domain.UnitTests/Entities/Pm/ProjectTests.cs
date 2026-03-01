using NOIR.Domain.Entities.Pm;

namespace NOIR.Domain.UnitTests.Entities.Pm;

/// <summary>
/// Unit tests for the Project entity.
/// Tests factory method, update, archive, complete, and reactivate lifecycle.
/// </summary>
public class ProjectTests
{
    private const string TestTenantId = "test-tenant";

    private static Project CreateActiveProject() =>
        Project.Create("Test Project", "test-project", "PRJ-20260301-000001", TestTenantId, description: "A test project");

    #region Create Factory Tests

    [Fact]
    public void Create_ShouldSetAllProperties_AndStatusActive()
    {
        // Arrange
        var ownerId = Guid.NewGuid();

        // Act
        var project = Project.Create(
            "Enterprise App", "enterprise-app", "PRJ-20260301-000002", TestTenantId,
            description: "Main app", startDate: DateTimeOffset.UtcNow,
            endDate: DateTimeOffset.UtcNow.AddMonths(6),
            dueDate: DateTimeOffset.UtcNow.AddMonths(5),
            ownerId: ownerId, budget: 100000m, currency: "USD",
            color: "#FF0000", icon: "rocket",
            visibility: ProjectVisibility.Internal);

        // Assert
        project.Should().NotBeNull();
        project.Id.Should().NotBe(Guid.Empty);
        project.ProjectCode.Should().Be("PRJ-20260301-000002");
        project.Name.Should().Be("Enterprise App");
        project.Slug.Should().Be("enterprise-app");
        project.Description.Should().Be("Main app");
        project.Status.Should().Be(ProjectStatus.Active);
        project.StartDate.Should().NotBeNull();
        project.EndDate.Should().NotBeNull();
        project.DueDate.Should().NotBeNull();
        project.OwnerId.Should().Be(ownerId);
        project.Budget.Should().Be(100000m);
        project.Currency.Should().Be("USD");
        project.Color.Should().Be("#FF0000");
        project.Icon.Should().Be("rocket");
        project.Visibility.Should().Be(ProjectVisibility.Internal);
        project.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_WithDefaults_ShouldUseDefaultValues()
    {
        // Act
        var project = Project.Create("Simple Project", "simple-project", "PRJ-20260301-000003", TestTenantId);

        // Assert
        project.Status.Should().Be(ProjectStatus.Active);
        project.Description.Should().BeNull();
        project.StartDate.Should().BeNull();
        project.EndDate.Should().BeNull();
        project.DueDate.Should().BeNull();
        project.OwnerId.Should().BeNull();
        project.Budget.Should().BeNull();
        project.Currency.Should().Be("VND");
        project.Color.Should().BeNull();
        project.Icon.Should().BeNull();
        project.Visibility.Should().Be(ProjectVisibility.Private);
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        // Act & Assert
        var act = () => Project.Create("", "slug", "PRJ-20260301-000004", TestTenantId);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptySlug_ShouldThrow()
    {
        // Act & Assert
        var act = () => Project.Create("Name", "", "PRJ-20260301-000005", TestTenantId);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldTrimNameAndSlug()
    {
        // Act
        var project = Project.Create("  Padded Name  ", "  padded-slug  ", "PRJ-20260301-000006", TestTenantId);

        // Assert
        project.Name.Should().Be("Padded Name");
        project.Slug.Should().Be("padded-slug");
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ShouldUpdateAllProperties()
    {
        // Arrange
        var project = CreateActiveProject();
        var newOwnerId = Guid.NewGuid();

        // Act
        project.Update(
            "Updated Name", "updated-slug", "New description",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMonths(3),
            DateTimeOffset.UtcNow.AddMonths(2),
            newOwnerId, 50000m, "EUR", "#00FF00", "star",
            ProjectVisibility.Public);

        // Assert
        project.Name.Should().Be("Updated Name");
        project.Slug.Should().Be("updated-slug");
        project.Description.Should().Be("New description");
        project.OwnerId.Should().Be(newOwnerId);
        project.Budget.Should().Be(50000m);
        project.Currency.Should().Be("EUR");
        project.Color.Should().Be("#00FF00");
        project.Icon.Should().Be("star");
        project.Visibility.Should().Be(ProjectVisibility.Public);
    }

    #endregion

    #region Archive Tests

    [Fact]
    public void Archive_ShouldSetStatusArchived_AndRaiseEvent()
    {
        // Arrange
        var project = CreateActiveProject();

        // Act
        project.Archive();

        // Assert
        project.Status.Should().Be(ProjectStatus.Archived);
        project.DomainEvents.Should().Contain(e => e is Events.Pm.ProjectArchivedEvent);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ShouldThrow()
    {
        // Arrange
        var project = CreateActiveProject();
        project.Archive();

        // Act & Assert
        var act = () => project.Archive();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Project is already archived.");
    }

    #endregion

    #region Complete Tests

    [Fact]
    public void Complete_WhenActive_ShouldSetStatusCompleted_AndSetEndDate_AndRaiseEvent()
    {
        // Arrange
        var project = CreateActiveProject();
        var beforeComplete = DateTimeOffset.UtcNow;

        // Act
        project.Complete();

        // Assert
        project.Status.Should().Be(ProjectStatus.Completed);
        project.EndDate.Should().NotBeNull();
        project.EndDate.Should().BeOnOrAfter(beforeComplete);
        project.DomainEvents.Should().Contain(e => e is Events.Pm.ProjectCompletedEvent);
    }

    [Fact]
    public void Complete_WhenArchived_ShouldThrow()
    {
        // Arrange
        var project = CreateActiveProject();
        project.Archive();

        // Act & Assert
        var act = () => project.Complete();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Only active or on-hold projects can be completed.");
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_ShouldThrow()
    {
        // Arrange
        var project = CreateActiveProject();
        project.Complete();

        // Act & Assert
        var act = () => project.Complete();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Only active or on-hold projects can be completed.");
    }

    #endregion

    #region Reactivate Tests

    [Fact]
    public void Reactivate_WhenArchived_ShouldSetStatusActive()
    {
        // Arrange
        var project = CreateActiveProject();
        project.Archive();

        // Act
        project.Reactivate();

        // Assert
        project.Status.Should().Be(ProjectStatus.Active);
    }

    [Fact]
    public void Reactivate_WhenCompleted_ShouldSetStatusActive()
    {
        // Arrange
        var project = CreateActiveProject();
        project.Complete();

        // Act
        project.Reactivate();

        // Assert
        project.Status.Should().Be(ProjectStatus.Active);
    }

    [Fact]
    public void Reactivate_WhenAlreadyActive_ShouldThrow()
    {
        // Arrange
        var project = CreateActiveProject();

        // Act & Assert
        var act = () => project.Reactivate();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Project is already active.");
    }

    #endregion
}

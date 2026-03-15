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
        project.ShouldNotBeNull();
        project.Id.ShouldNotBe(Guid.Empty);
        project.ProjectCode.ShouldBe("PRJ-20260301-000002");
        project.Name.ShouldBe("Enterprise App");
        project.Slug.ShouldBe("enterprise-app");
        project.Description.ShouldBe("Main app");
        project.Status.ShouldBe(ProjectStatus.Active);
        project.StartDate.ShouldNotBeNull();
        project.EndDate.ShouldNotBeNull();
        project.DueDate.ShouldNotBeNull();
        project.OwnerId.ShouldBe(ownerId);
        project.Budget.ShouldBe(100000m);
        project.Currency.ShouldBe("USD");
        project.Color.ShouldBe("#FF0000");
        project.Icon.ShouldBe("rocket");
        project.Visibility.ShouldBe(ProjectVisibility.Internal);
        project.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_WithDefaults_ShouldUseDefaultValues()
    {
        // Act
        var project = Project.Create("Simple Project", "simple-project", "PRJ-20260301-000003", TestTenantId);

        // Assert
        project.Status.ShouldBe(ProjectStatus.Active);
        project.Description.ShouldBeNull();
        project.StartDate.ShouldBeNull();
        project.EndDate.ShouldBeNull();
        project.DueDate.ShouldBeNull();
        project.OwnerId.ShouldBeNull();
        project.Budget.ShouldBeNull();
        project.Currency.ShouldBe("VND");
        project.Color.ShouldBeNull();
        project.Icon.ShouldBeNull();
        project.Visibility.ShouldBe(ProjectVisibility.Private);
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        // Act & Assert
        var act = () => Project.Create("", "slug", "PRJ-20260301-000004", TestTenantId);
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_WithEmptySlug_ShouldThrow()
    {
        // Act & Assert
        var act = () => Project.Create("Name", "", "PRJ-20260301-000005", TestTenantId);
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_ShouldTrimNameAndSlug()
    {
        // Act
        var project = Project.Create("  Padded Name  ", "  padded-slug  ", "PRJ-20260301-000006", TestTenantId);

        // Assert
        project.Name.ShouldBe("Padded Name");
        project.Slug.ShouldBe("padded-slug");
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
        project.Name.ShouldBe("Updated Name");
        project.Slug.ShouldBe("updated-slug");
        project.Description.ShouldBe("New description");
        project.OwnerId.ShouldBe(newOwnerId);
        project.Budget.ShouldBe(50000m);
        project.Currency.ShouldBe("EUR");
        project.Color.ShouldBe("#00FF00");
        project.Icon.ShouldBe("star");
        project.Visibility.ShouldBe(ProjectVisibility.Public);
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
        project.Status.ShouldBe(ProjectStatus.Archived);
        project.DomainEvents.ShouldContain(e => e is Events.Pm.ProjectArchivedEvent);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ShouldThrow()
    {
        // Arrange
        var project = CreateActiveProject();
        project.Archive();

        // Act & Assert
        var act = () => project.Archive();
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Project is already archived.");
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
        project.Status.ShouldBe(ProjectStatus.Completed);
        project.EndDate.ShouldNotBeNull();
        project.EndDate!.Value.ShouldBeGreaterThanOrEqualTo(beforeComplete);
        project.DomainEvents.ShouldContain(e => e is Events.Pm.ProjectCompletedEvent);
    }

    [Fact]
    public void Complete_WhenArchived_ShouldThrow()
    {
        // Arrange
        var project = CreateActiveProject();
        project.Archive();

        // Act & Assert
        var act = () => project.Complete();
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Only active or on-hold projects can be completed.");
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_ShouldThrow()
    {
        // Arrange
        var project = CreateActiveProject();
        project.Complete();

        // Act & Assert
        var act = () => project.Complete();
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Only active or on-hold projects can be completed.");
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
        project.Status.ShouldBe(ProjectStatus.Active);
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
        project.Status.ShouldBe(ProjectStatus.Active);
    }

    [Fact]
    public void Reactivate_WhenAlreadyActive_ShouldThrow()
    {
        // Arrange
        var project = CreateActiveProject();

        // Act & Assert
        var act = () => project.Reactivate();
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Project is already active.");
    }

    #endregion

    #region ChangeStatus Tests

    [Fact]
    public void ChangeStatus_ToCompleted_ShouldSetEndDate()
    {
        // Arrange
        var project = CreateActiveProject();
        var beforeChange = DateTimeOffset.UtcNow;

        // Act
        project.ChangeStatus(ProjectStatus.Completed);

        // Assert
        project.Status.ShouldBe(ProjectStatus.Completed);
        project.EndDate.ShouldNotBeNull();
        project.EndDate!.Value.ShouldBeGreaterThanOrEqualTo(beforeChange);
    }

    [Fact]
    public void ChangeStatus_ToOnHold_ShouldNotSetEndDate()
    {
        // Arrange
        var project = CreateActiveProject();

        // Act
        project.ChangeStatus(ProjectStatus.OnHold);

        // Assert
        project.Status.ShouldBe(ProjectStatus.OnHold);
        project.EndDate.ShouldBeNull();
    }

    #endregion
}

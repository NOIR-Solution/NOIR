using NOIR.Domain.Entities.Pm;

namespace NOIR.Domain.UnitTests.Entities.Pm;

/// <summary>
/// Unit tests for the ProjectMember entity.
/// Tests factory method and ChangeRole behavior.
/// </summary>
public class ProjectMemberTests
{
    private const string TestTenantId = "test-tenant";
    private static readonly Guid TestProjectId = Guid.NewGuid();
    private static readonly Guid TestEmployeeId = Guid.NewGuid();

    #region Create Factory Tests

    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        // Act
        var member = ProjectMember.Create(
            TestProjectId, TestEmployeeId, ProjectMemberRole.Member, TestTenantId);

        // Assert
        member.ShouldNotBeNull();
        member.Id.ShouldNotBe(Guid.Empty);
        member.ProjectId.ShouldBe(TestProjectId);
        member.EmployeeId.ShouldBe(TestEmployeeId);
        member.Role.ShouldBe(ProjectMemberRole.Member);
        member.JoinedAt.ShouldBe(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        member.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_WithOwnerRole_ShouldSetOwner()
    {
        // Act
        var member = ProjectMember.Create(
            TestProjectId, TestEmployeeId, ProjectMemberRole.Owner, TestTenantId);

        // Assert
        member.Role.ShouldBe(ProjectMemberRole.Owner);
    }

    [Fact]
    public void Create_WithManagerRole_ShouldSetManager()
    {
        // Act
        var member = ProjectMember.Create(
            TestProjectId, TestEmployeeId, ProjectMemberRole.Manager, TestTenantId);

        // Assert
        member.Role.ShouldBe(ProjectMemberRole.Manager);
    }

    [Fact]
    public void Create_WithViewerRole_ShouldSetViewer()
    {
        // Act
        var member = ProjectMember.Create(
            TestProjectId, TestEmployeeId, ProjectMemberRole.Viewer, TestTenantId);

        // Assert
        member.Role.ShouldBe(ProjectMemberRole.Viewer);
    }

    [Fact]
    public void Create_ShouldSetJoinedAtToCurrentTime()
    {
        // Arrange
        var beforeCreate = DateTimeOffset.UtcNow;

        // Act
        var member = ProjectMember.Create(
            TestProjectId, TestEmployeeId, ProjectMemberRole.Member, TestTenantId);

        // Assert
        member.JoinedAt.ShouldBeGreaterThanOrEqualTo(beforeCreate);
        member.JoinedAt.ShouldBeLessThanOrEqualTo(DateTimeOffset.UtcNow);
    }

    #endregion

    #region ChangeRole Tests

    [Fact]
    public void ChangeRole_ShouldUpdateRole()
    {
        // Arrange
        var member = ProjectMember.Create(
            TestProjectId, TestEmployeeId, ProjectMemberRole.Member, TestTenantId);

        // Act
        member.ChangeRole(ProjectMemberRole.Manager);

        // Assert
        member.Role.ShouldBe(ProjectMemberRole.Manager);
    }

    [Fact]
    public void ChangeRole_FromMemberToOwner_ShouldUpdate()
    {
        // Arrange
        var member = ProjectMember.Create(
            TestProjectId, TestEmployeeId, ProjectMemberRole.Member, TestTenantId);

        // Act
        member.ChangeRole(ProjectMemberRole.Owner);

        // Assert
        member.Role.ShouldBe(ProjectMemberRole.Owner);
    }

    [Fact]
    public void ChangeRole_FromOwnerToViewer_ShouldUpdate()
    {
        // Arrange
        var member = ProjectMember.Create(
            TestProjectId, TestEmployeeId, ProjectMemberRole.Owner, TestTenantId);

        // Act
        member.ChangeRole(ProjectMemberRole.Viewer);

        // Assert
        member.Role.ShouldBe(ProjectMemberRole.Viewer);
    }

    [Fact]
    public void ChangeRole_ShouldNotChangeOtherProperties()
    {
        // Arrange
        var member = ProjectMember.Create(
            TestProjectId, TestEmployeeId, ProjectMemberRole.Member, TestTenantId);
        var originalProjectId = member.ProjectId;
        var originalEmployeeId = member.EmployeeId;
        var originalJoinedAt = member.JoinedAt;

        // Act
        member.ChangeRole(ProjectMemberRole.Manager);

        // Assert
        member.ProjectId.ShouldBe(originalProjectId);
        member.EmployeeId.ShouldBe(originalEmployeeId);
        member.JoinedAt.ShouldBe(originalJoinedAt);
    }

    #endregion
}

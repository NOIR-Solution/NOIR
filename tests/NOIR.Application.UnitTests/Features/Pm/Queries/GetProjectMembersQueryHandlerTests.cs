using NOIR.Application.Features.Pm.Queries.GetProjectMembers;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Queries;

public class GetProjectMembersQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly GetProjectMembersQueryHandler _handler;

    private const string TestTenantId = "tenant-123";

    public GetProjectMembersQueryHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _handler = new GetProjectMembersQueryHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_MembersExist_ShouldReturnMemberList()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var members = new List<ProjectMember>
        {
            ProjectMember.Create(projectId, Guid.NewGuid(), ProjectMemberRole.Owner, TestTenantId),
            ProjectMember.Create(projectId, Guid.NewGuid(), ProjectMemberRole.Member, TestTenantId)
        };
        var membersDbSet = members.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectMembers).Returns(membersDbSet.Object);

        var query = new GetProjectMembersQuery(projectId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Count().ShouldBe(2);
    }

    [Fact]
    public async Task Handle_NoMembers_ShouldReturnEmptyList()
    {
        // Arrange
        var emptyMembers = new List<ProjectMember>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectMembers).Returns(emptyMembers.Object);

        var query = new GetProjectMembersQuery(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectRoles()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var member = ProjectMember.Create(projectId, Guid.NewGuid(), ProjectMemberRole.Manager, TestTenantId);
        var members = new List<ProjectMember> { member }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectMembers).Returns(members.Object);

        var query = new GetProjectMembersQuery(projectId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.First().Role.ShouldBe(ProjectMemberRole.Manager);
    }
}

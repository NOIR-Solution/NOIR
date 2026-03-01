using NOIR.Application.Features.Pm.Commands.AddProjectMember;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class AddProjectMemberCommandHandlerTests
{
    private readonly Mock<IRepository<Project, Guid>> _projectRepoMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly AddProjectMemberCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public AddProjectMemberCommandHandlerTests()
    {
        _projectRepoMock = new Mock<IRepository<Project, Guid>>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new AddProjectMemberCommandHandler(
            _projectRepoMock.Object,
            _dbContextMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldAddMember()
    {
        // Arrange
        var project = Project.Create("Test", "test", "PRJ-20260301-000001", TestTenantId);
        var employeeId = Guid.NewGuid();

        _projectRepoMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        // Use a backing list that captures the added member for the reload query
        var backingList = new List<ProjectMember>();
        var mockDbSet = backingList.BuildMockDbSet();
        mockDbSet.Setup(x => x.Add(It.IsAny<ProjectMember>()))
            .Callback<ProjectMember>(m => backingList.Add(m));

        // After Add, re-setup so the reload query finds the member
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(_ =>
            {
                // Re-setup the DbSet with the now-populated list for the reload query
                var updatedDbSet = backingList.BuildMockDbSet();
                _dbContextMock.Setup(x => x.ProjectMembers).Returns(updatedDbSet.Object);
            })
            .ReturnsAsync(1);

        _dbContextMock.Setup(x => x.ProjectMembers).Returns(mockDbSet.Object);

        var command = new AddProjectMemberCommand(project.Id, employeeId, ProjectMemberRole.Member);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        backingList.Should().HaveCount(1);
        backingList[0].EmployeeId.Should().Be(employeeId);
        backingList[0].Role.Should().Be(ProjectMemberRole.Member);
    }

    [Fact]
    public async Task Handle_AlreadyMember_ShouldReturnError()
    {
        // Arrange
        var project = Project.Create("Test", "test", "PRJ-20260301-000001", TestTenantId);
        var employeeId = Guid.NewGuid();

        _projectRepoMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        // Existing member
        var existingMember = ProjectMember.Create(project.Id, employeeId, ProjectMemberRole.Member, TestTenantId);
        var members = new List<ProjectMember> { existingMember }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectMembers).Returns(members.Object);

        var command = new AddProjectMemberCommand(project.Id, employeeId, ProjectMemberRole.Member);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ShouldReturnError()
    {
        // Arrange
        _projectRepoMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var command = new AddProjectMemberCommand(Guid.NewGuid(), Guid.NewGuid(), ProjectMemberRole.Member);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }
}

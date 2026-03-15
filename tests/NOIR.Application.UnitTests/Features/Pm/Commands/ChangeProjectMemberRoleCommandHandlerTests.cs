using NOIR.Application.Features.Pm.Commands.ChangeProjectMemberRole;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class ChangeProjectMemberRoleCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ChangeProjectMemberRoleCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public ChangeProjectMemberRoleCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new ChangeProjectMemberRoleCommandHandler(
            _dbContextMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldChangeRole()
    {
        // Arrange
        var member = ProjectMember.Create(Guid.NewGuid(), Guid.NewGuid(), ProjectMemberRole.Member, TestTenantId);
        var members = new List<ProjectMember> { member }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectMembers).Returns(members.Object);

        var command = new ChangeProjectMemberRoleCommand(Guid.NewGuid(), member.Id, ProjectMemberRole.Manager);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Role.ShouldBe(ProjectMemberRole.Manager);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MemberNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var emptyMembers = new List<ProjectMember>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectMembers).Returns(emptyMembers.Object);

        var command = new ChangeProjectMemberRoleCommand(Guid.NewGuid(), Guid.NewGuid(), ProjectMemberRole.Manager);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChanges()
    {
        // Arrange
        var member = ProjectMember.Create(Guid.NewGuid(), Guid.NewGuid(), ProjectMemberRole.Viewer, TestTenantId);
        var members = new List<ProjectMember> { member }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectMembers).Returns(members.Object);

        var command = new ChangeProjectMemberRoleCommand(Guid.NewGuid(), member.Id, ProjectMemberRole.Member);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

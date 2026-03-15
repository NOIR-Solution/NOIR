using NOIR.Application.Features.Pm.Commands.RemoveProjectMember;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class RemoveProjectMemberCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly RemoveProjectMemberCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public RemoveProjectMemberCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new RemoveProjectMemberCommandHandler(
            _dbContextMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldRemoveMember()
    {
        // Arrange
        var member = ProjectMember.Create(Guid.NewGuid(), Guid.NewGuid(), ProjectMemberRole.Member, TestTenantId);
        var members = new List<ProjectMember> { member }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectMembers).Returns(members.Object);

        var command = new RemoveProjectMemberCommand(member.ProjectId, member.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OwnerRole_ShouldReturnError()
    {
        // Arrange
        var owner = ProjectMember.Create(Guid.NewGuid(), Guid.NewGuid(), ProjectMemberRole.Owner, TestTenantId);
        var members = new List<ProjectMember> { owner }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectMembers).Returns(members.Object);

        var command = new RemoveProjectMemberCommand(owner.ProjectId, owner.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MemberNotFound_ShouldReturnError()
    {
        // Arrange
        var emptyMembers = new List<ProjectMember>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectMembers).Returns(emptyMembers.Object);

        var command = new RemoveProjectMemberCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
    }
}

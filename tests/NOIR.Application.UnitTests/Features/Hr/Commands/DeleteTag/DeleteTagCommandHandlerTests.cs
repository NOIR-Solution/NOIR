using NOIR.Application.Features.Hr.Commands.DeleteTag;
using NOIR.Application.Features.Hr.Specifications;
using NOIR.Domain.Entities.Hr;

namespace NOIR.Application.UnitTests.Features.Hr.Commands.DeleteTag;

public class DeleteTagCommandHandlerTests
{
    private readonly Mock<IRepository<EmployeeTag, Guid>> _tagRepositoryMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly DeleteTagCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public DeleteTagCommandHandlerTests()
    {
        _tagRepositoryMock = new Mock<IRepository<EmployeeTag, Guid>>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new DeleteTagCommandHandler(
            _tagRepositoryMock.Object,
            _dbContextMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    private void SetupAssignmentsDbSet(List<EmployeeTagAssignment> assignments)
    {
        var mockDbSet = assignments.BuildMockDbSet();
        _dbContextMock.Setup(x => x.EmployeeTagAssignments).Returns(mockDbSet.Object);
    }

    [Fact]
    public async Task Handle_WithValidId_ShouldSoftDeleteTag()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var command = new DeleteTagCommand(tagId);
        var existingTag = EmployeeTag.Create("Senior", EmployeeTagCategory.Skill, TestTenantId);

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeTagByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTag);

        SetupAssignmentsDbSet([]);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        _tagRepositoryMock.Verify(
            x => x.Remove(existingTag),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var command = new DeleteTagCommand(Guid.NewGuid());

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeTagByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmployeeTag?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        _tagRepositoryMock.Verify(
            x => x.Remove(It.IsAny<EmployeeTag>()),
            Times.Never);
    }
}

using NOIR.Application.Features.Hr.Commands.AssignTagsToEmployee;
using NOIR.Application.Features.Hr.DTOs;
using NOIR.Application.Features.Hr.Specifications;
using NOIR.Domain.Entities.Hr;

namespace NOIR.Application.UnitTests.Features.Hr.Commands.AssignTagsToEmployee;

public class AssignTagsToEmployeeCommandHandlerTests
{
    private readonly Mock<IRepository<Employee, Guid>> _employeeRepositoryMock;
    private readonly Mock<IRepository<EmployeeTag, Guid>> _tagRepositoryMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly AssignTagsToEmployeeCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public AssignTagsToEmployeeCommandHandlerTests()
    {
        _employeeRepositoryMock = new Mock<IRepository<Employee, Guid>>();
        _tagRepositoryMock = new Mock<IRepository<EmployeeTag, Guid>>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _handler = new AssignTagsToEmployeeCommandHandler(
            _employeeRepositoryMock.Object,
            _tagRepositoryMock.Object,
            _dbContextMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object);
    }

    private void SetupAssignmentsDbSet(List<EmployeeTagAssignment> assignments)
    {
        var mockDbSet = assignments.BuildMockDbSet();
        _dbContextMock.Setup(x => x.EmployeeTagAssignments).Returns(mockDbSet.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldAssignTags()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var tagId1 = Guid.NewGuid();
        var tagId2 = Guid.NewGuid();
        var command = new AssignTagsToEmployeeCommand(employeeId, new List<Guid> { tagId1, tagId2 });

        var tag1 = EmployeeTag.Create("Senior", EmployeeTagCategory.Skill, TestTenantId);
        var tag2 = EmployeeTag.Create("Backend", EmployeeTagCategory.Team, TestTenantId);

        _employeeRepositoryMock
            .Setup(x => x.ExistsAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _tagRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<EmployeeTagsByIdsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmployeeTag> { tag1, tag2 });

        SetupAssignmentsDbSet([]);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentEmployee_ShouldReturnNotFound()
    {
        // Arrange
        var command = new AssignTagsToEmployeeCommand(Guid.NewGuid(), new List<Guid> { Guid.NewGuid() });

        _employeeRepositoryMock
            .Setup(x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNonExistentTag_ShouldReturnNotFound()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var command = new AssignTagsToEmployeeCommand(employeeId, new List<Guid> { tagId });

        _employeeRepositoryMock
            .Setup(x => x.ExistsAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Return empty list — none of the requested tags found
        _tagRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<EmployeeTagsByIdsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmployeeTag>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithAlreadyAssignedTag_ShouldSkipDuplicate()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var command = new AssignTagsToEmployeeCommand(employeeId, new List<Guid> { tagId });

        var tag = EmployeeTag.Create("Senior", EmployeeTagCategory.Skill, TestTenantId);
        var existingAssignment = EmployeeTagAssignment.Create(employeeId, tagId, TestTenantId);

        _employeeRepositoryMock
            .Setup(x => x.ExistsAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _tagRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<EmployeeTagsByIdsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmployeeTag> { tag });

        SetupAssignmentsDbSet([existingAssignment]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Should not call SaveChanges since no new assignments were added
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

using NOIR.Application.Common.DTOs;
using NOIR.Application.Features.Hr.Commands.BulkAssignTags;
using NOIR.Application.Features.Hr.Specifications;
using NOIR.Domain.Entities.Hr;

namespace NOIR.Application.UnitTests.Features.Hr.Commands.BulkAssignTags;

public class BulkAssignTagsCommandHandlerTests
{
    private readonly Mock<IRepository<Employee, Guid>> _employeeRepositoryMock;
    private readonly Mock<IRepository<EmployeeTag, Guid>> _tagRepositoryMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly BulkAssignTagsCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public BulkAssignTagsCommandHandlerTests()
    {
        _employeeRepositoryMock = new Mock<IRepository<Employee, Guid>>();
        _tagRepositoryMock = new Mock<IRepository<EmployeeTag, Guid>>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new BulkAssignTagsCommandHandler(
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
    public async Task Handle_WithValidCommand_ShouldAssignTagsToAllEmployees()
    {
        // Arrange
        var empId1 = Guid.NewGuid();
        var empId2 = Guid.NewGuid();
        var tagId1 = Guid.NewGuid();
        var tagId2 = Guid.NewGuid();

        var tag1 = EmployeeTag.Create("Senior", EmployeeTagCategory.Skill, TestTenantId);
        var tag2 = EmployeeTag.Create("Backend", EmployeeTagCategory.Team, TestTenantId);

        var command = new BulkAssignTagsCommand(
            new List<Guid> { empId1, empId2 },
            new List<Guid> { tagId1, tagId2 });

        _tagRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<EmployeeTagsByIdsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmployeeTag> { tag1, tag2 });

        SetupAssignmentsDbSet([]);

        _employeeRepositoryMock
            .Setup(x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Success.ShouldBe(2);
        result.Value.Failed.ShouldBe(0);
        result.Value.Errors.ShouldBeEmpty();

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithSomeAlreadyAssigned_ShouldSkipDuplicates()
    {
        // Arrange
        var empId = Guid.NewGuid();
        var tagId1 = Guid.NewGuid();
        var tagId2 = Guid.NewGuid();

        var tag1 = EmployeeTag.Create("Senior", EmployeeTagCategory.Skill, TestTenantId);
        var tag2 = EmployeeTag.Create("Backend", EmployeeTagCategory.Team, TestTenantId);

        var existingAssignment = EmployeeTagAssignment.Create(empId, tagId1, TestTenantId);

        var command = new BulkAssignTagsCommand(
            new List<Guid> { empId },
            new List<Guid> { tagId1, tagId2 });

        _tagRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<EmployeeTagsByIdsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmployeeTag> { tag1, tag2 });

        SetupAssignmentsDbSet([existingAssignment]);

        _employeeRepositoryMock
            .Setup(x => x.ExistsAsync(empId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Success.ShouldBe(1);
        result.Value.Failed.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_WithNonExistentEmployee_ShouldReturnError()
    {
        // Arrange
        var empId1 = Guid.NewGuid();
        var empId2 = Guid.NewGuid();
        var tagId = Guid.NewGuid();

        var tag = EmployeeTag.Create("Senior", EmployeeTagCategory.Skill, TestTenantId);

        var command = new BulkAssignTagsCommand(
            new List<Guid> { empId1, empId2 },
            new List<Guid> { tagId });

        _tagRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<EmployeeTagsByIdsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmployeeTag> { tag });

        SetupAssignmentsDbSet([]);

        _employeeRepositoryMock
            .Setup(x => x.ExistsAsync(empId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _employeeRepositoryMock
            .Setup(x => x.ExistsAsync(empId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Success.ShouldBe(1);
        result.Value.Failed.ShouldBe(1);
        result.Value.Errors.Count().ShouldBe(1);
        result.Value.Errors[0].EntityId.ShouldBe(empId2);
        result.Value.Errors[0].Message.ShouldContain("not found");
    }

    [Fact]
    public async Task Handle_WithNonExistentTag_ShouldReturnError()
    {
        // Arrange
        var empId = Guid.NewGuid();
        var tagId = Guid.NewGuid();

        var command = new BulkAssignTagsCommand(
            new List<Guid> { empId },
            new List<Guid> { tagId });

        // Return empty list — tag doesn't exist
        _tagRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<EmployeeTagsByIdsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmployeeTag>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
    }
}

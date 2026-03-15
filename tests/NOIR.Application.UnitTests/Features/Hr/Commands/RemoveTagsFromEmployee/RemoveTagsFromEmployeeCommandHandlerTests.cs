using NOIR.Application.Features.Hr.Commands.RemoveTagsFromEmployee;
using NOIR.Application.Features.Hr.DTOs;
using NOIR.Domain.Entities.Hr;

namespace NOIR.Application.UnitTests.Features.Hr.Commands.RemoveTagsFromEmployee;

public class RemoveTagsFromEmployeeCommandHandlerTests
{
    private readonly Mock<IRepository<Employee, Guid>> _employeeRepositoryMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly RemoveTagsFromEmployeeCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public RemoveTagsFromEmployeeCommandHandlerTests()
    {
        _employeeRepositoryMock = new Mock<IRepository<Employee, Guid>>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new RemoveTagsFromEmployeeCommandHandler(
            _employeeRepositoryMock.Object,
            _dbContextMock.Object,
            _unitOfWorkMock.Object);
    }

    private void SetupAssignmentsDbSet(List<EmployeeTagAssignment> assignments)
    {
        var mockDbSet = assignments.BuildMockDbSet();
        _dbContextMock.Setup(x => x.EmployeeTagAssignments).Returns(mockDbSet.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldRemoveTags()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var tagId1 = Guid.NewGuid();
        var tagId2 = Guid.NewGuid();
        var command = new RemoveTagsFromEmployeeCommand(employeeId, new List<Guid> { tagId1, tagId2 });

        var assignment1 = EmployeeTagAssignment.Create(employeeId, tagId1, TestTenantId);
        var assignment2 = EmployeeTagAssignment.Create(employeeId, tagId2, TestTenantId);

        _employeeRepositoryMock
            .Setup(x => x.ExistsAsync(employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        SetupAssignmentsDbSet([assignment1, assignment2]);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentEmployee_ShouldReturnNotFound()
    {
        // Arrange
        var command = new RemoveTagsFromEmployeeCommand(Guid.NewGuid(), new List<Guid> { Guid.NewGuid() });

        _employeeRepositoryMock
            .Setup(x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
    }
}

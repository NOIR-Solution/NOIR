using NOIR.Application.Features.Pm.Commands.AddTaskComment;
using NOIR.Application.Features.Hr.Specifications;
using NOIR.Domain.Entities.Hr;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class AddTaskCommentCommandHandlerTests
{
    private readonly Mock<IRepository<ProjectTask, Guid>> _taskRepoMock;
    private readonly Mock<IRepository<Employee, Guid>> _employeeRepoMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly AddTaskCommentCommandHandler _handler;

    private const string TestTenantId = "tenant-123";
    private const string TestUserId = "user-123";

    public AddTaskCommentCommandHandlerTests()
    {
        _taskRepoMock = new Mock<IRepository<ProjectTask, Guid>>();
        _employeeRepoMock = new Mock<IRepository<Employee, Guid>>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _currentUserMock.Setup(x => x.UserId).Returns(TestUserId);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Setup DbSet mock
        var emptyComments = new List<TaskComment>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.TaskComments).Returns(emptyComments.Object);

        _handler = new AddTaskCommentCommandHandler(
            _taskRepoMock.Object,
            _employeeRepoMock.Object,
            _dbContextMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    private static Employee CreateTestEmployee() =>
        Employee.Create("EMP-001", "John", "Doe", "john@test.com",
            Guid.NewGuid(), DateTimeOffset.UtcNow, EmploymentType.FullTime, TestTenantId);

    [Fact]
    public async Task Handle_ValidRequest_ShouldAddComment()
    {
        // Arrange
        var task = ProjectTask.Create(Guid.NewGuid(), "PRJ-1", "Test Task", TestTenantId);
        _taskRepoMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var employee = CreateTestEmployee();
        _employeeRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        var command = new AddTaskCommentCommand(task.Id, "Great progress!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().Be("Great progress!");
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TaskNotFound_ShouldReturnError()
    {
        // Arrange
        _taskRepoMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTask?)null);

        var command = new AddTaskCommentCommand(Guid.NewGuid(), "Comment");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NoEmployeeLinked_ShouldReturnError()
    {
        // Arrange
        var task = ProjectTask.Create(Guid.NewGuid(), "PRJ-1", "Test Task", TestTenantId);
        _taskRepoMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _employeeRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var command = new AddTaskCommentCommand(task.Id, "Comment");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }
}

using NOIR.Application.Features.Pm.Commands.CreateTask;
using NOIR.Application.Features.Pm.Specifications;
using NOIR.Application.Features.Hr.Specifications;
using NOIR.Domain.Entities.Hr;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class CreateTaskCommandHandlerTests
{
    private readonly Mock<IRepository<ProjectTask, Guid>> _taskRepoMock;
    private readonly Mock<IRepository<Project, Guid>> _projectRepoMock;
    private readonly Mock<IRepository<Employee, Guid>> _employeeRepoMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<ITaskNumberGenerator> _taskNumberGenMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly CreateTaskCommandHandler _handler;

    private const string TestTenantId = "tenant-123";
    private const string TestUserId = "user-123";

    public CreateTaskCommandHandlerTests()
    {
        _taskRepoMock = new Mock<IRepository<ProjectTask, Guid>>();
        _projectRepoMock = new Mock<IRepository<Project, Guid>>();
        _employeeRepoMock = new Mock<IRepository<Employee, Guid>>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _taskNumberGenMock = new Mock<ITaskNumberGenerator>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _currentUserMock.Setup(x => x.UserId).Returns(TestUserId);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _taskRepoMock
            .Setup(x => x.AddAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectTask t, CancellationToken _) => t);
        _taskNumberGenMock
            .Setup(x => x.GenerateNextAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("PRJ-001");

        _handler = new CreateTaskCommandHandler(
            _taskRepoMock.Object,
            _projectRepoMock.Object,
            _employeeRepoMock.Object,
            _dbContextMock.Object,
            _taskNumberGenMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    private static Employee CreateTestEmployee() =>
        Employee.Create("EMP-001", "John", "Doe", "john@test.com",
            Guid.NewGuid(), DateTimeOffset.UtcNow, EmploymentType.FullTime, TestTenantId);

    [Fact]
    public async Task Handle_ValidRequest_ShouldCreateTask()
    {
        // Arrange
        var project = Project.Create("Test Project", "test-project", "PRJ-20260301-000001", TestTenantId);
        _projectRepoMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var employee = CreateTestEmployee();
        _employeeRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        // Mock first column
        var column = ProjectColumn.Create(project.Id, "Todo", 0, TestTenantId);
        var columns = new List<ProjectColumn> { column }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectColumns).Returns(columns.Object);

        // Mock reload
        var createdTask = ProjectTask.Create(project.Id, "PRJ-001", "Test Task", TestTenantId);
        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdTask);

        var command = new CreateTaskCommand(project.Id, "Test Task");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _taskRepoMock.Verify(
            x => x.AddAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoColumnIdSpecified_ShouldAssignFirstColumn()
    {
        // Arrange
        var project = Project.Create("Test Project", "test-project", "PRJ-20260301-000001", TestTenantId);
        _projectRepoMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var employee = CreateTestEmployee();
        _employeeRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        var column = ProjectColumn.Create(project.Id, "Todo", 0, TestTenantId);
        var columns = new List<ProjectColumn> { column }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectColumns).Returns(columns.Object);

        var createdTask = ProjectTask.Create(project.Id, "PRJ-001", "Test Task", TestTenantId, columnId: column.Id);
        _taskRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<TaskByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdTask);

        var command = new CreateTaskCommand(project.Id, "Test Task"); // No ColumnId

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _dbContextMock.Verify(x => x.ProjectColumns, Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_ProjectNotFound_ShouldReturnError()
    {
        // Arrange
        _projectRepoMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var command = new CreateTaskCommand(Guid.NewGuid(), "Test Task");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _taskRepoMock.Verify(
            x => x.AddAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

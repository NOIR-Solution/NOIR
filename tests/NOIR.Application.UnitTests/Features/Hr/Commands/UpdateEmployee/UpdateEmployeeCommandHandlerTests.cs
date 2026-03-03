using NOIR.Application.Features.Hr.Commands.UpdateEmployee;
using NOIR.Application.Features.Hr.DTOs;
using NOIR.Application.Features.Hr.Specifications;
using NOIR.Domain.Entities.Hr;

namespace NOIR.Application.UnitTests.Features.Hr.Commands.UpdateEmployee;

public class UpdateEmployeeCommandHandlerTests
{
    private readonly Mock<IRepository<Employee, Guid>> _employeeRepositoryMock;
    private readonly Mock<IRepository<Department, Guid>> _departmentRepositoryMock;
    private readonly Mock<IEmployeeHierarchyService> _hierarchyServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly UpdateEmployeeCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public UpdateEmployeeCommandHandlerTests()
    {
        _employeeRepositoryMock = new Mock<IRepository<Employee, Guid>>();
        _departmentRepositoryMock = new Mock<IRepository<Department, Guid>>();
        _hierarchyServiceMock = new Mock<IEmployeeHierarchyService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new UpdateEmployeeCommandHandler(
            _employeeRepositoryMock.Object,
            _departmentRepositoryMock.Object,
            _hierarchyServiceMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    private static Employee CreateTestEmployee(Guid? departmentId = null) =>
        Employee.Create(
            "EMP-001", "John", "Doe", "john@example.com",
            departmentId ?? Guid.NewGuid(),
            DateTimeOffset.UtcNow, EmploymentType.FullTime, TestTenantId);

    [Fact]
    public async Task Handle_ValidRequest_UpdatesEmployeeAndReturnsSuccess()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        var employee = CreateTestEmployee(departmentId);
        var department = Department.Create("Engineering", "ENG", TestTenantId);
        var command = new UpdateEmployeeCommand(
            employeeId, "Jane", "Smith", "john@example.com",
            departmentId, EmploymentType.FullTime);

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmployeeNotFound_ReturnsError()
    {
        // Arrange
        var command = new UpdateEmployeeCommand(
            Guid.NewGuid(), "Jane", "Smith", "jane@example.com",
            Guid.NewGuid(), EmploymentType.FullTime);

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-HR-010");
    }

    [Fact]
    public async Task Handle_DepartmentChange_ValidatesAndUpdates()
    {
        // Arrange
        var oldDeptId = Guid.NewGuid();
        var newDeptId = Guid.NewGuid();
        var employee = CreateTestEmployee(oldDeptId);
        var newDepartment = Department.Create("Marketing", "MKT", TestTenantId);
        var command = new UpdateEmployeeCommand(
            Guid.NewGuid(), "John", "Doe", "john@example.com",
            newDeptId, EmploymentType.FullTime);

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newDepartment);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ManagerChange_ValidatesHierarchy()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var employee = CreateTestEmployee();
        var manager = CreateTestEmployee();
        var department = Department.Create("Engineering", "ENG", TestTenantId);

        var command = new UpdateEmployeeCommand(
            employeeId, "John", "Doe", "john@example.com",
            Guid.NewGuid(), EmploymentType.FullTime,
            ManagerId: managerId);

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        _hierarchyServiceMock
            .Setup(x => x.GetAncestorChainAsync(managerId, 20, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HierarchyChain(1, new HashSet<Guid>()));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _hierarchyServiceMock.Verify(
            x => x.GetAncestorChainAsync(managerId, 20, TestTenantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SelfAsManager_ReturnsError()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = CreateTestEmployee();
        var department = Department.Create("Engineering", "ENG", TestTenantId);

        var command = new UpdateEmployeeCommand(
            employeeId, "John", "Doe", "john@example.com",
            Guid.NewGuid(), EmploymentType.FullTime,
            ManagerId: employeeId);

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("cannot be their own manager");
    }

    [Fact]
    public async Task Handle_CircularManagerHierarchy_ReturnsError()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var employee = CreateTestEmployee();
        var manager = CreateTestEmployee();
        var department = Department.Create("Engineering", "ENG", TestTenantId);

        var command = new UpdateEmployeeCommand(
            employeeId, "John", "Doe", "john@example.com",
            Guid.NewGuid(), EmploymentType.FullTime,
            ManagerId: managerId);

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        // The ancestor chain contains the employee itself -> circular
        _hierarchyServiceMock
            .Setup(x => x.GetAncestorChainAsync(managerId, 20, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HierarchyChain(3, new HashSet<Guid> { employeeId }));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("circular");
    }
}

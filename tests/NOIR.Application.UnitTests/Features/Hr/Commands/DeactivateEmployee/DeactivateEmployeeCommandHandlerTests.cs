using NOIR.Application.Features.Hr.Commands.DeactivateEmployee;
using NOIR.Application.Features.Hr.DTOs;
using NOIR.Application.Features.Hr.Specifications;
using NOIR.Domain.Entities.Hr;

namespace NOIR.Application.UnitTests.Features.Hr.Commands.DeactivateEmployee;

public class DeactivateEmployeeCommandHandlerTests
{
    private readonly Mock<IRepository<Employee, Guid>> _employeeRepositoryMock;
    private readonly Mock<IRepository<Department, Guid>> _departmentRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly DeactivateEmployeeCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public DeactivateEmployeeCommandHandlerTests()
    {
        _employeeRepositoryMock = new Mock<IRepository<Employee, Guid>>();
        _departmentRepositoryMock = new Mock<IRepository<Department, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new DeactivateEmployeeCommandHandler(
            _employeeRepositoryMock.Object,
            _departmentRepositoryMock.Object,
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
    public async Task Handle_ValidRequest_DeactivatesAndCascades()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = CreateTestEmployee();
        var command = new DeactivateEmployeeCommand(employeeId, EmployeeStatus.Resigned);

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _employeeRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<EmployeesByManagerIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>());

        _departmentRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<DepartmentsByManagerIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmployeeNotFound_ReturnsError()
    {
        // Arrange
        var command = new DeactivateEmployeeCommand(Guid.NewGuid(), EmployeeStatus.Resigned);

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe("NOIR-HR-010");
    }

    [Fact]
    public async Task Handle_CascadesDirectReportsManagerId()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = CreateTestEmployee();
        var directReport = CreateTestEmployee();
        var command = new DeactivateEmployeeCommand(employeeId, EmployeeStatus.Terminated);

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _employeeRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<EmployeesByManagerIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee> { directReport });

        _departmentRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<DepartmentsByManagerIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        // Direct report's manager should have been set to null via UpdateManager
        directReport.ManagerId.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_CascadesDepartmentManagerId()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = CreateTestEmployee();
        var managedDept = Department.Create("Engineering", "ENG", TestTenantId);
        var command = new DeactivateEmployeeCommand(employeeId, EmployeeStatus.Resigned);

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _employeeRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<EmployeesByManagerIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>());

        _departmentRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<DepartmentsByManagerIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department> { managedDept });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        // Department manager should have been nulled via Update
        managedDept.ManagerId.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_MultipleDirectReports_NullsAllManagerIds()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = CreateTestEmployee();
        var report1 = CreateTestEmployee();
        report1.UpdateManager(employeeId);
        var report2 = CreateTestEmployee();
        report2.UpdateManager(employeeId);
        var report3 = CreateTestEmployee();
        report3.UpdateManager(employeeId);
        var command = new DeactivateEmployeeCommand(employeeId, EmployeeStatus.Terminated);

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _employeeRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<EmployeesByManagerIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee> { report1, report2, report3 });

        _departmentRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<DepartmentsByManagerIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        report1.ManagerId.ShouldBeNull();
        report2.ManagerId.ShouldBeNull();
        report3.ManagerId.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_IsDepartmentManagerAndHasDirectReports_CascadesBoth()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = CreateTestEmployee();
        var directReport = CreateTestEmployee();
        directReport.UpdateManager(employeeId);
        var managedDept = Department.Create("Engineering", "ENG", TestTenantId, managerId: employeeId);
        var command = new DeactivateEmployeeCommand(employeeId, EmployeeStatus.Resigned);

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _employeeRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<EmployeesByManagerIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee> { directReport });

        _departmentRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<DepartmentsByManagerIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department> { managedDept });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        directReport.ManagerId.ShouldBeNull();
        managedDept.ManagerId.ShouldBeNull();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmployeeWithTerminatedStatus_SetsCorrectStatus()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var employee = CreateTestEmployee();
        var command = new DeactivateEmployeeCommand(employeeId, EmployeeStatus.Terminated);

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _employeeRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<EmployeesByManagerIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>());

        _departmentRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<DepartmentsByManagerIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        employee.Status.ShouldBe(EmployeeStatus.Terminated);
        employee.EndDate.ShouldNotBeNull();
    }
}

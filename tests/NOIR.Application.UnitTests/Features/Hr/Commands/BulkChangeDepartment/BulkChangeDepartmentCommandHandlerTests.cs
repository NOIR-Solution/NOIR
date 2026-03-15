using NOIR.Application.Common.DTOs;
using NOIR.Application.Features.Hr.Commands.BulkChangeDepartment;
using NOIR.Application.Features.Hr.Specifications;
using NOIR.Domain.Entities.Hr;

namespace NOIR.Application.UnitTests.Features.Hr.Commands.BulkChangeDepartment;

public class BulkChangeDepartmentCommandHandlerTests
{
    private readonly Mock<IRepository<Employee, Guid>> _employeeRepositoryMock;
    private readonly Mock<IRepository<Department, Guid>> _departmentRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly BulkChangeDepartmentCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public BulkChangeDepartmentCommandHandlerTests()
    {
        _employeeRepositoryMock = new Mock<IRepository<Employee, Guid>>();
        _departmentRepositoryMock = new Mock<IRepository<Department, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new BulkChangeDepartmentCommandHandler(
            _employeeRepositoryMock.Object,
            _departmentRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static Department CreateTestDepartment(Guid? id = null)
    {
        var dept = Department.Create("Engineering", "ENG", TestTenantId);
        if (id.HasValue)
        {
            typeof(Department).GetProperty("Id")!.SetValue(dept, id.Value);
        }
        return dept;
    }

    private static Employee CreateTestEmployee(Guid? id = null, Guid? departmentId = null)
    {
        var deptId = departmentId ?? Guid.NewGuid();
        var emp = Employee.Create(
            $"EMP-{Guid.NewGuid().ToString()[..6]}",
            "Test", "Employee",
            $"test-{Guid.NewGuid().ToString()[..6]}@example.com",
            deptId,
            DateTimeOffset.UtcNow,
            EmploymentType.FullTime,
            TestTenantId);

        if (id.HasValue)
        {
            typeof(Employee).GetProperty("Id")!.SetValue(emp, id.Value);
        }
        return emp;
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldChangeDepartmentForAllEmployees()
    {
        // Arrange
        var newDeptId = Guid.NewGuid();
        var dept = CreateTestDepartment(newDeptId);
        var empId1 = Guid.NewGuid();
        var empId2 = Guid.NewGuid();
        var emp1 = CreateTestEmployee(empId1);
        var emp2 = CreateTestEmployee(empId2);

        var command = new BulkChangeDepartmentCommand(
            new List<Guid> { empId1, empId2 },
            newDeptId);

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dept);

        _employeeRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<EmployeesByIdsForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee> { emp1, emp2 });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Success.ShouldBe(2);
        result.Value.Failed.ShouldBe(0);
        result.Value.Errors.ShouldBeEmpty();

        emp1.DepartmentId.ShouldBe(newDeptId);
        emp2.DepartmentId.ShouldBe(newDeptId);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentDepartment_ShouldReturnNotFound()
    {
        // Arrange
        var command = new BulkChangeDepartmentCommand(
            new List<Guid> { Guid.NewGuid() },
            Guid.NewGuid());

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Department?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
    }

    [Fact]
    public async Task Handle_WithNonExistentEmployee_ShouldReturnError()
    {
        // Arrange
        var newDeptId = Guid.NewGuid();
        var dept = CreateTestDepartment(newDeptId);
        var existingEmpId = Guid.NewGuid();
        var missingEmpId = Guid.NewGuid();
        var emp = CreateTestEmployee(existingEmpId);

        var command = new BulkChangeDepartmentCommand(
            new List<Guid> { existingEmpId, missingEmpId },
            newDeptId);

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dept);

        // Only return the existing employee, not the missing one
        _employeeRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<EmployeesByIdsForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee> { emp });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Success.ShouldBe(1);
        result.Value.Failed.ShouldBe(1);
        result.Value.Errors.Count().ShouldBe(1);
        result.Value.Errors[0].EntityId.ShouldBe(missingEmpId);
        result.Value.Errors[0].Message.ShouldContain("not found");
    }
}

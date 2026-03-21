using NOIR.Application.Features.Hr.DTOs;
using NOIR.Application.Features.Hr.Queries.GetOrgChart;
using NOIR.Application.Features.Hr.Specifications;
using NOIR.Domain.Entities.Hr;

namespace NOIR.Application.UnitTests.Features.Hr.Queries.GetOrgChart;

public class GetOrgChartQueryHandlerTests
{
    private readonly Mock<IRepository<Department, Guid>> _departmentRepoMock;
    private readonly Mock<IRepository<Employee, Guid>> _employeeRepoMock;
    private readonly GetOrgChartQueryHandler _handler;

    private const string TestTenantId = "test-tenant";

    public GetOrgChartQueryHandlerTests()
    {
        _departmentRepoMock = new Mock<IRepository<Department, Guid>>();
        _employeeRepoMock = new Mock<IRepository<Employee, Guid>>();
        _handler = new GetOrgChartQueryHandler(
            _departmentRepoMock.Object,
            _employeeRepoMock.Object);
    }

    private static Department CreateDept(string name, string code, Guid? parentId = null)
    {
        return Department.Create(name, code, TestTenantId, parentDepartmentId: parentId);
    }

    private static Employee CreateEmployee(
        string firstName, string lastName, Guid departmentId,
        string? position = null, Guid? managerId = null)
    {
        return Employee.Create(
            $"EMP-{Guid.NewGuid().ToString()[..6]}",
            firstName, lastName,
            $"{firstName.ToLower()}.{lastName.ToLower()}@test.com",
            departmentId,
            DateTimeOffset.UtcNow,
            EmploymentType.FullTime,
            TestTenantId,
            position: position,
            managerId: managerId);
    }

    [Fact]
    public async Task Handle_NoDepartments_ReturnsEmptyList()
    {
        // Arrange
        _departmentRepoMock
            .Setup(r => r.ListAsync(It.IsAny<AllDepartmentsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department>());

        _employeeRepoMock
            .Setup(r => r.ListAsync(It.IsAny<OrgChartEmployeesSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>());

        // Act
        var result = await _handler.Handle(new GetOrgChartQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_DepartmentsOnly_ReturnsFlatDepartmentNodes()
    {
        // Arrange
        var eng = CreateDept("Engineering", "ENG");
        var fe = CreateDept("Frontend", "FE", eng.Id);
        var be = CreateDept("Backend", "BE", eng.Id);

        _departmentRepoMock
            .Setup(r => r.ListAsync(It.IsAny<AllDepartmentsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department> { eng, fe, be });

        _employeeRepoMock
            .Setup(r => r.ListAsync(It.IsAny<OrgChartEmployeesSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>());

        // Act
        var result = await _handler.Handle(new GetOrgChartQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(3);

        var engNode = result.Value.First(n => n.Name == "Engineering");
        engNode.Type.ShouldBe(OrgChartNodeType.Department);
        engNode.ParentId.ShouldBeNull(); // root

        var feNode = result.Value.First(n => n.Name == "Frontend");
        feNode.ParentId.ShouldBe(eng.Id);

        var beNode = result.Value.First(n => n.Name == "Backend");
        beNode.ParentId.ShouldBe(eng.Id);
    }

    [Fact]
    public async Task Handle_WithEmployees_ReturnsDepartmentsAndEmployees()
    {
        // Arrange
        var eng = CreateDept("Engineering", "ENG");
        var emp1 = CreateEmployee("Alice", "Smith", eng.Id, "Lead");
        var emp2 = CreateEmployee("Bob", "Jones", eng.Id, "Developer");

        _departmentRepoMock
            .Setup(r => r.ListAsync(It.IsAny<AllDepartmentsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department> { eng });

        _employeeRepoMock
            .Setup(r => r.ListAsync(It.IsAny<OrgChartEmployeesSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee> { emp1, emp2 });

        // Act
        var result = await _handler.Handle(new GetOrgChartQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(3); // 1 dept + 2 employees

        var deptNodes = result.Value.Where(n => n.Type == OrgChartNodeType.Department).ToList();
        var empNodes = result.Value.Where(n => n.Type == OrgChartNodeType.Employee).ToList();

        deptNodes.Count.ShouldBe(1);
        empNodes.Count.ShouldBe(2);

        // Employees should have ParentId = department.Id
        empNodes.ShouldAllBe(n => n.ParentId == eng.Id);

        // Department should have employee count
        deptNodes[0].EmployeeCount.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_WithManagerRelationships_SetsManagerId()
    {
        // Arrange
        var eng = CreateDept("Engineering", "ENG");
        var manager = CreateEmployee("Alice", "Manager", eng.Id, "VP");
        var report = CreateEmployee("Bob", "Report", eng.Id, "Developer", managerId: manager.Id);

        _departmentRepoMock
            .Setup(r => r.ListAsync(It.IsAny<AllDepartmentsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department> { eng });

        _employeeRepoMock
            .Setup(r => r.ListAsync(It.IsAny<OrgChartEmployeesSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee> { manager, report });

        // Act
        var result = await _handler.Handle(new GetOrgChartQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        var managerNode = result.Value.First(n => n.Name == "Alice Manager");
        managerNode.ManagerId.ShouldBeNull(); // no manager above

        var reportNode = result.Value.First(n => n.Name == "Bob Report");
        reportNode.ManagerId.ShouldBe(manager.Id); // reports to Alice
    }

    [Fact]
    public async Task Handle_ManagerOutsideResultSet_ManagerIdSetToNull()
    {
        // Arrange: manager in department A, report in department B, filter by department B only
        var deptA = CreateDept("Dept A", "DA");
        var deptB = CreateDept("Dept B", "DB");
        var manager = CreateEmployee("Alice", "Manager", deptA.Id, "VP");
        var report = CreateEmployee("Bob", "Report", deptB.Id, "Dev", managerId: manager.Id);

        _departmentRepoMock
            .Setup(r => r.ListAsync(It.IsAny<AllDepartmentsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department> { deptA, deptB });

        // When filtering by deptB, only deptB employees are returned
        _employeeRepoMock
            .Setup(r => r.ListAsync(It.IsAny<OrgChartEmployeesSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee> { report }); // manager not included

        // Act
        var result = await _handler.Handle(new GetOrgChartQuery(deptB.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        var reportNode = result.Value.First(n => n.Type == OrgChartNodeType.Employee);
        reportNode.ManagerId.ShouldBeNull(); // manager not in result set → null
    }

    [Fact]
    public async Task Handle_DepartmentFilter_ReturnsOnlySubtree()
    {
        // Arrange
        var root = CreateDept("Root", "RT");
        var child = CreateDept("Child", "CH", root.Id);
        var other = CreateDept("Other", "OT"); // separate tree

        _departmentRepoMock
            .Setup(r => r.ListAsync(It.IsAny<AllDepartmentsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department> { root, child, other });

        _employeeRepoMock
            .Setup(r => r.ListAsync(It.IsAny<OrgChartEmployeesSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>());

        // Act — filter by root department
        var result = await _handler.Handle(new GetOrgChartQuery(root.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        var deptNames = result.Value.Select(n => n.Name).ToList();
        deptNames.ShouldContain("Root");
        deptNames.ShouldContain("Child");
        deptNames.ShouldNotContain("Other"); // excluded by filter
    }
}

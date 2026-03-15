using NOIR.Application.Features.Hr.DTOs;
using NOIR.Application.Features.Hr.Queries.GetHrReports;
using NOIR.Domain.Entities.Hr;

namespace NOIR.Application.UnitTests.Features.Hr.Queries.GetHrReports;

public class GetHrReportsQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly GetHrReportsQueryHandler _handler;

    private const string TestTenantId = "test-tenant";

    public GetHrReportsQueryHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _handler = new GetHrReportsQueryHandler(_dbContextMock.Object);
    }

    private static Department CreateTestDepartment(string name, string code)
    {
        return Department.Create(name, code, TestTenantId);
    }

    private static Employee CreateTestEmployee(
        Guid departmentId,
        EmployeeStatus status = EmployeeStatus.Active,
        EmploymentType employmentType = EmploymentType.FullTime)
    {
        return Employee.Create(
            $"EMP-{Guid.NewGuid().ToString()[..6]}",
            "Test",
            "Employee",
            $"test-{Guid.NewGuid().ToString()[..6]}@example.com",
            departmentId,
            DateTimeOffset.UtcNow,
            employmentType,
            TestTenantId);
    }

    [Fact]
    public async Task Handle_ShouldReturnAllReportData()
    {
        // Arrange
        var dept1 = CreateTestDepartment("Engineering", "ENG");
        var dept2 = CreateTestDepartment("Marketing", "MKT");

        var emp1 = CreateTestEmployee(dept1.Id);
        var emp2 = CreateTestEmployee(dept1.Id, employmentType: EmploymentType.PartTime);
        var emp3 = CreateTestEmployee(dept2.Id);

        // Set up navigation property via reflection for GroupBy
        typeof(Employee).GetProperty("Department")!.SetValue(emp1, dept1);
        typeof(Employee).GetProperty("Department")!.SetValue(emp2, dept1);
        typeof(Employee).GetProperty("Department")!.SetValue(emp3, dept2);

        var employees = new List<Employee> { emp1, emp2, emp3 };
        var departments = new List<Department> { dept1, dept2 };

        var tag = EmployeeTag.Create("Senior", EmployeeTagCategory.Skill, TestTenantId);
        var assignment = EmployeeTagAssignment.Create(emp1.Id, tag.Id, TestTenantId);
        typeof(EmployeeTagAssignment).GetProperty("EmployeeTag")!.SetValue(assignment, tag);
        var assignments = new List<EmployeeTagAssignment> { assignment };

        var employeeMockDbSet = employees.BuildMockDbSet();
        var departmentMockDbSet = departments.BuildMockDbSet();
        var assignmentMockDbSet = assignments.BuildMockDbSet();

        _dbContextMock.Setup(x => x.Employees).Returns(employeeMockDbSet.Object);
        _dbContextMock.Setup(x => x.Departments).Returns(departmentMockDbSet.Object);
        _dbContextMock.Setup(x => x.EmployeeTagAssignments).Returns(assignmentMockDbSet.Object);

        var query = new GetHrReportsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var reports = result.Value;

        reports.TotalDepartments.ShouldBeGreaterThanOrEqualTo(1);
        reports.TotalActiveEmployees.ShouldBeGreaterThanOrEqualTo(1);
        reports.HeadcountByDepartment.ShouldNotBeNull();
        reports.TagDistribution.ShouldNotBeNull();
        reports.EmploymentTypeBreakdown.ShouldNotBeNull();
        reports.StatusBreakdown.ShouldNotBeNull();
    }
}

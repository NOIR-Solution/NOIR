using Microsoft.EntityFrameworkCore;
using NOIR.Application.Features.Hr.Queries.ExportEmployees;
using NOIR.Application.Features.Hr.Specifications;
using NOIR.Application.Features.Reports.DTOs;
using NOIR.Application.UnitTests.Common;
using NOIR.Domain.Entities.Hr;

namespace NOIR.Application.UnitTests.Features.Hr.Queries.ExportEmployees;

public class ExportEmployeesQueryHandlerTests
{
    private readonly Mock<IRepository<Employee, Guid>> _employeeRepositoryMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IExcelExportService> _excelExportServiceMock;
    private readonly Mock<ILogger<ExportEmployeesQueryHandler>> _loggerMock;
    private readonly ExportEmployeesQueryHandler _handler;

    private const string TestTenantId = "test-tenant";

    public ExportEmployeesQueryHandlerTests()
    {
        _employeeRepositoryMock = new Mock<IRepository<Employee, Guid>>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _excelExportServiceMock = new Mock<IExcelExportService>();
        _loggerMock = new Mock<ILogger<ExportEmployeesQueryHandler>>();

        // Setup empty tag assignments DbSet for default behavior
        SetupEmptyTagAssignments();

        _handler = new ExportEmployeesQueryHandler(
            _employeeRepositoryMock.Object,
            _dbContextMock.Object,
            _excelExportServiceMock.Object,
            _loggerMock.Object);
    }

    private void SetupEmptyTagAssignments()
    {
        var emptyAssignments = new List<EmployeeTagAssignment>().AsQueryable();
        var mockDbSet = new Mock<DbSet<EmployeeTagAssignment>>();

        mockDbSet.As<IQueryable<EmployeeTagAssignment>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<EmployeeTagAssignment>(emptyAssignments.Provider));
        mockDbSet.As<IQueryable<EmployeeTagAssignment>>()
            .Setup(m => m.Expression)
            .Returns(emptyAssignments.Expression);
        mockDbSet.As<IQueryable<EmployeeTagAssignment>>()
            .Setup(m => m.ElementType)
            .Returns(emptyAssignments.ElementType);
        mockDbSet.As<IQueryable<EmployeeTagAssignment>>()
            .Setup(m => m.GetEnumerator())
            .Returns(emptyAssignments.GetEnumerator());

        _dbContextMock.Setup(x => x.EmployeeTagAssignments).Returns(mockDbSet.Object);
    }

    private static Employee CreateTestEmployee(
        string firstName = "John",
        string lastName = "Doe",
        string email = "john@example.com")
    {
        var deptId = Guid.NewGuid();
        return Employee.Create(
            "EMP-001",
            firstName,
            lastName,
            email,
            deptId,
            DateTimeOffset.UtcNow,
            EmploymentType.FullTime,
            TestTenantId);
    }

    private void SetupRepositoryToReturn(List<Employee> employees)
    {
        _employeeRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<EmployeesForExportSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employees);
    }

    [Fact]
    public async Task Handle_WithExcelFormat_ShouldReturnExcelFile()
    {
        // Arrange
        var employees = new List<Employee> { CreateTestEmployee() };
        SetupRepositoryToReturn(employees);

        var excelBytes = new byte[] { 0x50, 0x4B, 0x03, 0x04 };
        _excelExportServiceMock
            .Setup(x => x.CreateExcelFile(
                "Employees",
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<IReadOnlyList<IReadOnlyList<object?>>>()))
            .Returns(excelBytes);

        var query = new ExportEmployeesQuery(Format: ExportFormat.Excel);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ContentType.ShouldBe("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        result.Value.FileName.ShouldStartWith("employees-");
        result.Value.FileName.ShouldEndWith(".xlsx");
        result.Value.FileBytes.ShouldBe(excelBytes);

        _excelExportServiceMock.Verify(
            x => x.CreateExcelFile("Employees", It.IsAny<IReadOnlyList<string>>(), It.IsAny<IReadOnlyList<IReadOnlyList<object?>>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCsvFormat_ShouldReturnCsvFile()
    {
        // Arrange
        var employees = new List<Employee>
        {
            CreateTestEmployee("Alice", "Smith", "alice@test.com"),
            CreateTestEmployee("Bob", "Jones", "bob@test.com")
        };
        SetupRepositoryToReturn(employees);

        var query = new ExportEmployeesQuery(Format: ExportFormat.CSV);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ContentType.ShouldBe("text/csv");
        result.Value.FileName.ShouldStartWith("employees-");
        result.Value.FileName.ShouldEndWith(".csv");
        result.Value.FileBytes.ShouldNotBeEmpty();

        var csvContent = Encoding.UTF8.GetString(result.Value.FileBytes);
        csvContent.ShouldContain("FirstName");
        csvContent.ShouldContain("LastName");
        csvContent.ShouldContain("Email");
        csvContent.ShouldContain("Alice");
        csvContent.ShouldContain("Bob");
    }

    [Fact]
    public async Task Handle_WithFilters_ShouldApplyFilters()
    {
        // Arrange
        var employees = new List<Employee> { CreateTestEmployee() };
        SetupRepositoryToReturn(employees);

        var query = new ExportEmployeesQuery(
            Format: ExportFormat.CSV,
            DepartmentId: Guid.NewGuid(),
            Status: EmployeeStatus.Active,
            EmploymentType: EmploymentType.FullTime);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);

        _employeeRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<EmployeesForExportSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

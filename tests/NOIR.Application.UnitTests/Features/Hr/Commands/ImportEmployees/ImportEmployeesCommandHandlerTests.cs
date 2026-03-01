using NOIR.Application.Features.Hr.Commands.ImportEmployees;
using NOIR.Application.Features.Hr.DTOs;
using NOIR.Application.Features.Hr.Specifications;
using NOIR.Domain.Entities.Hr;

namespace NOIR.Application.UnitTests.Features.Hr.Commands.ImportEmployees;

public class ImportEmployeesCommandHandlerTests
{
    private readonly Mock<IRepository<Employee, Guid>> _employeeRepositoryMock;
    private readonly Mock<IRepository<Department, Guid>> _departmentRepositoryMock;
    private readonly Mock<IEmployeeCodeGenerator> _codeGeneratorMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILogger<ImportEmployeesCommandHandler>> _loggerMock;
    private readonly ImportEmployeesCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public ImportEmployeesCommandHandlerTests()
    {
        _employeeRepositoryMock = new Mock<IRepository<Employee, Guid>>();
        _departmentRepositoryMock = new Mock<IRepository<Department, Guid>>();
        _codeGeneratorMock = new Mock<IEmployeeCodeGenerator>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();
        _loggerMock = new Mock<ILogger<ImportEmployeesCommandHandler>>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var codeCounter = 0;
        _codeGeneratorMock
            .Setup(x => x.GenerateNextAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => $"EMP-{++codeCounter:D6}");

        _handler = new ImportEmployeesCommandHandler(
            _employeeRepositoryMock.Object,
            _departmentRepositoryMock.Object,
            _codeGeneratorMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    private void SetupDepartments(params Department[] departments)
    {
        _departmentRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<AllDepartmentsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(departments.ToList());
    }

    private void SetupNoExistingEmployees()
    {
        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByEmailSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        _employeeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee e, CancellationToken _) => e);
    }

    private static byte[] CreateCsvBytes(params string[] lines)
    {
        return Encoding.UTF8.GetBytes(string.Join("\n", lines));
    }

    private static Department CreateTestDepartment(string code = "ENG")
    {
        return Department.Create("Engineering", code, TestTenantId);
    }

    [Fact]
    public async Task Handle_WithValidCsv_ShouldImportAllRows()
    {
        // Arrange
        var dept = CreateTestDepartment("ENG");
        SetupDepartments(dept);
        SetupNoExistingEmployees();

        var csvBytes = CreateCsvBytes(
            "FirstName,LastName,Email,DepartmentCode,JoinDate,EmploymentType",
            "Alice,Smith,alice@example.com,ENG,2026-01-15,FullTime",
            "Bob,Jones,bob@example.com,ENG,2026-02-01,PartTime");

        var command = new ImportEmployeesCommand(csvBytes, "employees.csv");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SuccessCount.Should().Be(2);
        result.Value.FailedCount.Should().Be(0);
        result.Value.Errors.Should().BeEmpty();

        _employeeRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ShouldReportError()
    {
        // Arrange
        var dept = CreateTestDepartment("ENG");
        SetupDepartments(dept);

        // First call succeeds (no existing), second call finds existing
        var existingEmployee = Employee.Create(
            "EMP-001", "Existing", "User", "alice@example.com",
            dept.Id, DateTimeOffset.UtcNow, EmploymentType.FullTime, TestTenantId);

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByEmailSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEmployee);

        _employeeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee e, CancellationToken _) => e);

        var csvBytes = CreateCsvBytes(
            "FirstName,LastName,Email,DepartmentCode,JoinDate,EmploymentType",
            "Alice,Smith,alice@example.com,ENG,2026-01-15,FullTime");

        var command = new ImportEmployeesCommand(csvBytes, "employees.csv");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FailedCount.Should().Be(1);
        result.Value.Errors.Should().HaveCount(1);
        result.Value.Errors[0].Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task Handle_WithInvalidDepartmentCode_ShouldReportError()
    {
        // Arrange
        var dept = CreateTestDepartment("ENG");
        SetupDepartments(dept);
        SetupNoExistingEmployees();

        var csvBytes = CreateCsvBytes(
            "FirstName,LastName,Email,DepartmentCode,JoinDate,EmploymentType",
            "Alice,Smith,alice@example.com,INVALID_DEPT,2026-01-15,FullTime");

        var command = new ImportEmployeesCommand(csvBytes, "employees.csv");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FailedCount.Should().Be(1);
        result.Value.Errors.Should().HaveCount(1);
        result.Value.Errors[0].Message.Should().Contain("INVALID_DEPT");
        result.Value.Errors[0].Message.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WithEmptyFile_ShouldReturnError()
    {
        // Arrange — CSV with only a header, no data rows
        var csvBytes = CreateCsvBytes("FirstName,LastName,Email,DepartmentCode,JoinDate,EmploymentType");

        var command = new ImportEmployeesCommand(csvBytes, "employees.csv");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert — handler requires at least header + 1 data row
        result.IsFailure.Should().BeTrue();
    }
}

using NOIR.Application.Features.Hr.Commands.CreateEmployee;
using NOIR.Application.Features.Hr.DTOs;
using NOIR.Application.Features.Hr.Specifications;
using NOIR.Domain.Entities.Hr;

namespace NOIR.Application.UnitTests.Features.Hr.Commands.CreateEmployee;

public class CreateEmployeeCommandHandlerTests
{
    private readonly Mock<IRepository<Employee, Guid>> _employeeRepositoryMock;
    private readonly Mock<IRepository<Department, Guid>> _departmentRepositoryMock;
    private readonly Mock<IEmployeeCodeGenerator> _codeGeneratorMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly CreateEmployeeCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public CreateEmployeeCommandHandlerTests()
    {
        _employeeRepositoryMock = new Mock<IRepository<Employee, Guid>>();
        _departmentRepositoryMock = new Mock<IRepository<Department, Guid>>();
        _codeGeneratorMock = new Mock<IEmployeeCodeGenerator>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _codeGeneratorMock
            .Setup(x => x.GenerateNextAsync(TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("EMP-001");

        _handler = new CreateEmployeeCommandHandler(
            _employeeRepositoryMock.Object,
            _departmentRepositoryMock.Object,
            _codeGeneratorMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    private static CreateEmployeeCommand CreateValidCommand(
        Guid? departmentId = null,
        Guid? managerId = null,
        string? userId = null) =>
        new(
            FirstName: "John",
            LastName: "Doe",
            Email: "john@example.com",
            DepartmentId: departmentId ?? Guid.NewGuid(),
            JoinDate: DateTimeOffset.UtcNow,
            EmploymentType: EmploymentType.FullTime,
            Phone: "+84912345678",
            Position: "Engineer",
            ManagerId: managerId,
            UserId: userId);

    [Fact]
    public async Task Handle_ValidRequest_CreatesEmployeeAndReturnsSuccess()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var command = CreateValidCommand(departmentId: departmentId);

        var department = Department.Create("Engineering", "ENG", TestTenantId);

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByEmailSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.EmployeeCode.Should().Be("EMP-001");
        result.Value.FirstName.Should().Be("John");
        result.Value.LastName.Should().Be("Doe");

        _employeeRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsError()
    {
        // Arrange
        var command = CreateValidCommand();
        var existingEmployee = Employee.Create(
            "EMP-000", "Jane", "Doe", "john@example.com",
            Guid.NewGuid(), DateTimeOffset.UtcNow, EmploymentType.FullTime, TestTenantId);

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByEmailSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEmployee);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-HR-001");
    }

    [Fact]
    public async Task Handle_InvalidDepartment_ReturnsError()
    {
        // Arrange
        var command = CreateValidCommand();

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByEmailSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Department?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-HR-002");
    }

    [Fact]
    public async Task Handle_ShouldGenerateEmployeeCode()
    {
        // Arrange
        var command = CreateValidCommand();
        var department = Department.Create("Engineering", "ENG", TestTenantId);

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByEmailSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _codeGeneratorMock.Verify(
            x => x.GenerateNextAsync(TestTenantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidManager_ReturnsError()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var command = CreateValidCommand(managerId: managerId);
        var department = Department.Create("Engineering", "ENG", TestTenantId);

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByEmailSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        // Manager not found
        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-HR-003");
    }

    [Fact]
    public async Task Handle_UserAlreadyLinked_ReturnsError()
    {
        // Arrange
        var command = CreateValidCommand(userId: "user-123");
        var department = Department.Create("Engineering", "ENG", TestTenantId);
        var linkedEmployee = Employee.Create(
            "EMP-000", "Jane", "Smith", "jane@example.com",
            Guid.NewGuid(), DateTimeOffset.UtcNow, EmploymentType.FullTime, TestTenantId,
            userId: "user-123");

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByEmailSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(linkedEmployee);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-HR-004");
    }
}

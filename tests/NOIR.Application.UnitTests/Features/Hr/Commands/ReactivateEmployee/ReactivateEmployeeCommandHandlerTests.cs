using NOIR.Application.Features.Hr.Commands.ReactivateEmployee;
using NOIR.Application.Features.Hr.DTOs;
using NOIR.Application.Features.Hr.Specifications;
using NOIR.Domain.Entities.Hr;

namespace NOIR.Application.UnitTests.Features.Hr.Commands.ReactivateEmployee;

public class ReactivateEmployeeCommandHandlerTests
{
    private readonly Mock<IRepository<Employee, Guid>> _employeeRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly ReactivateEmployeeCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public ReactivateEmployeeCommandHandlerTests()
    {
        _employeeRepositoryMock = new Mock<IRepository<Employee, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new ReactivateEmployeeCommandHandler(
            _employeeRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReactivatesEmployee()
    {
        // Arrange
        var employee = Employee.Create(
            "EMP-001", "John", "Doe", "john@example.com",
            Guid.NewGuid(), DateTimeOffset.UtcNow, EmploymentType.FullTime, TestTenantId);
        employee.Deactivate(EmployeeStatus.Resigned);
        var command = new ReactivateEmployeeCommand(Guid.NewGuid());

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        employee.Status.Should().Be(EmployeeStatus.Active);
        employee.EndDate.Should().BeNull();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmployeeNotFound_ReturnsError()
    {
        // Arrange
        var command = new ReactivateEmployeeCommand(Guid.NewGuid());

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
    public async Task Handle_TerminatedEmployee_ReactivatesSuccessfully()
    {
        // Arrange
        var employee = Employee.Create(
            "EMP-001", "John", "Doe", "john@example.com",
            Guid.NewGuid(), DateTimeOffset.UtcNow, EmploymentType.FullTime, TestTenantId);
        employee.Deactivate(EmployeeStatus.Terminated);
        var command = new ReactivateEmployeeCommand(Guid.NewGuid());

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        employee.Status.Should().Be(EmployeeStatus.Active);
        employee.EndDate.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsDtoWithCorrectValues()
    {
        // Arrange
        var employee = Employee.Create(
            "EMP-002", "Jane", "Smith", "jane@example.com",
            Guid.NewGuid(), DateTimeOffset.UtcNow, EmploymentType.PartTime, TestTenantId);
        employee.Deactivate(EmployeeStatus.Resigned);
        var command = new ReactivateEmployeeCommand(Guid.NewGuid());

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(EmployeeStatus.Active);
        result.Value.EndDate.Should().BeNull();
        result.Value.EmployeeCode.Should().Be("EMP-002");
        result.Value.FirstName.Should().Be("Jane");
        result.Value.LastName.Should().Be("Smith");
    }
}

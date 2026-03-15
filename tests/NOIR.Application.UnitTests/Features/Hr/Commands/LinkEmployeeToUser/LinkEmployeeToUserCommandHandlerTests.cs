using NOIR.Application.Features.Hr.Commands.LinkEmployeeToUser;
using NOIR.Application.Features.Hr.DTOs;
using NOIR.Application.Features.Hr.Specifications;
using NOIR.Domain.Entities.Hr;

namespace NOIR.Application.UnitTests.Features.Hr.Commands.LinkEmployeeToUser;

public class LinkEmployeeToUserCommandHandlerTests
{
    private readonly Mock<IRepository<Employee, Guid>> _employeeRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly LinkEmployeeToUserCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public LinkEmployeeToUserCommandHandlerTests()
    {
        _employeeRepositoryMock = new Mock<IRepository<Employee, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new LinkEmployeeToUserCommandHandler(
            _employeeRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_LinksUserToEmployee()
    {
        // Arrange
        var employee = Employee.Create(
            "EMP-001", "John", "Doe", "john@example.com",
            Guid.NewGuid(), DateTimeOffset.UtcNow, EmploymentType.FullTime, TestTenantId);
        var command = new LinkEmployeeToUserCommand(Guid.NewGuid(), "user-456");

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        employee.UserId.ShouldBe("user-456");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmployeeNotFound_ReturnsError()
    {
        // Arrange
        var command = new LinkEmployeeToUserCommand(Guid.NewGuid(), "user-456");

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
    public async Task Handle_UserAlreadyLinked_ReturnsError()
    {
        // Arrange
        var employee = Employee.Create(
            "EMP-001", "John", "Doe", "john@example.com",
            Guid.NewGuid(), DateTimeOffset.UtcNow, EmploymentType.FullTime, TestTenantId);
        var otherEmployee = Employee.Create(
            "EMP-002", "Jane", "Smith", "jane@example.com",
            Guid.NewGuid(), DateTimeOffset.UtcNow, EmploymentType.FullTime, TestTenantId);
        var command = new LinkEmployeeToUserCommand(Guid.NewGuid(), "user-456");

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherEmployee);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe("NOIR-HR-004");
    }

    [Fact]
    public async Task Handle_ReLinkToDifferentUser_Succeeds()
    {
        // Arrange
        var employee = Employee.Create(
            "EMP-001", "John", "Doe", "john@example.com",
            Guid.NewGuid(), DateTimeOffset.UtcNow, EmploymentType.FullTime, TestTenantId);
        employee.LinkToUser("old-user-123");
        var command = new LinkEmployeeToUserCommand(Guid.NewGuid(), "new-user-789");

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        employee.UserId.ShouldBe("new-user-789");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsDtoWithLinkedStatus()
    {
        // Arrange
        var employee = Employee.Create(
            "EMP-001", "John", "Doe", "john@example.com",
            Guid.NewGuid(), DateTimeOffset.UtcNow, EmploymentType.FullTime, TestTenantId);
        var command = new LinkEmployeeToUserCommand(Guid.NewGuid(), "user-456");

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _employeeRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.HasUserAccount.ShouldBe(true);
        result.Value.UserId.ShouldBe("user-456");
    }
}

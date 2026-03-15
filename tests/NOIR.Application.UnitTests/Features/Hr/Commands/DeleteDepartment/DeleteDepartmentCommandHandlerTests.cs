using NOIR.Application.Features.Hr.Commands.DeleteDepartment;
using NOIR.Application.Features.Hr.Specifications;
using NOIR.Domain.Entities.Hr;

namespace NOIR.Application.UnitTests.Features.Hr.Commands.DeleteDepartment;

public class DeleteDepartmentCommandHandlerTests
{
    private readonly Mock<IRepository<Department, Guid>> _departmentRepositoryMock;
    private readonly Mock<IRepository<Employee, Guid>> _employeeRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly DeleteDepartmentCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public DeleteDepartmentCommandHandlerTests()
    {
        _departmentRepositoryMock = new Mock<IRepository<Department, Guid>>();
        _employeeRepositoryMock = new Mock<IRepository<Employee, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new DeleteDepartmentCommandHandler(
            _departmentRepositoryMock.Object,
            _employeeRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_EmptyDepartment_DeletesSuccessfully()
    {
        // Arrange
        var deptId = Guid.NewGuid();
        var department = Department.Create("Engineering", "ENG", TestTenantId);
        var command = new DeleteDepartmentCommand(deptId);

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        _employeeRepositoryMock
            .Setup(x => x.AnyAsync(It.IsAny<EmployeesByDepartmentSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _departmentRepositoryMock
            .Setup(x => x.AnyAsync(It.IsAny<ActiveSubDepartmentsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldBe(true);

        _departmentRepositoryMock.Verify(x => x.Remove(department), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DepartmentNotFound_ReturnsError()
    {
        // Arrange
        var command = new DeleteDepartmentCommand(Guid.NewGuid());

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Department?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe("NOIR-HR-022");
    }

    [Fact]
    public async Task Handle_DepartmentWithActiveEmployees_ReturnsError()
    {
        // Arrange
        var deptId = Guid.NewGuid();
        var department = Department.Create("Engineering", "ENG", TestTenantId);
        var command = new DeleteDepartmentCommand(deptId);

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        _employeeRepositoryMock
            .Setup(x => x.AnyAsync(It.IsAny<EmployeesByDepartmentSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe("NOIR-HR-023");
        result.Error.Message.ShouldContain("has employees");

        _departmentRepositoryMock.Verify(x => x.Remove(It.IsAny<Department>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DepartmentWithActiveSubDepartments_ReturnsError()
    {
        // Arrange
        var deptId = Guid.NewGuid();
        var department = Department.Create("Engineering", "ENG", TestTenantId);
        var command = new DeleteDepartmentCommand(deptId);

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        _employeeRepositoryMock
            .Setup(x => x.AnyAsync(It.IsAny<EmployeesByDepartmentSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _departmentRepositoryMock
            .Setup(x => x.AnyAsync(It.IsAny<ActiveSubDepartmentsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe("NOIR-HR-024");
        result.Error.Message.ShouldContain("sub-departments");

        _departmentRepositoryMock.Verify(x => x.Remove(It.IsAny<Department>()), Times.Never);
    }
}

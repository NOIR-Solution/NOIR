using NOIR.Application.Features.Hr.Commands.UpdateDepartment;
using NOIR.Application.Features.Hr.DTOs;
using NOIR.Application.Features.Hr.Specifications;
using NOIR.Domain.Entities.Hr;

namespace NOIR.Application.UnitTests.Features.Hr.Commands.UpdateDepartment;

public class UpdateDepartmentCommandHandlerTests
{
    private readonly Mock<IRepository<Department, Guid>> _departmentRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly UpdateDepartmentCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public UpdateDepartmentCommandHandlerTests()
    {
        _departmentRepositoryMock = new Mock<IRepository<Department, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new UpdateDepartmentCommandHandler(
            _departmentRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesDepartment()
    {
        // Arrange
        var deptId = Guid.NewGuid();
        var department = Department.Create("Engineering", "ENG", TestTenantId);
        var command = new UpdateDepartmentCommand(deptId, "Marketing", "MKT", "Marketing Dept");

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByCodeSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Department?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DepartmentNotFound_ReturnsError()
    {
        // Arrange
        var command = new UpdateDepartmentCommand(Guid.NewGuid(), "Marketing", "MKT");

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
    public async Task Handle_CircularParent_ReturnsError()
    {
        // Arrange
        var deptId = Guid.NewGuid();
        var department = Department.Create("Engineering", "ENG", TestTenantId);
        var command = new UpdateDepartmentCommand(deptId, "Engineering", "ENG", ParentDepartmentId: deptId);

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Message.ShouldContain("cannot be its own parent");
    }

    [Fact]
    public async Task Handle_DuplicateCode_ReturnsError()
    {
        // Arrange
        var deptId = Guid.NewGuid();
        var department = Department.Create("Engineering", "ENG", TestTenantId);
        var existingWithCode = Department.Create("Other", "MKT", TestTenantId);
        var command = new UpdateDepartmentCommand(deptId, "Engineering", "MKT");

        _departmentRepositoryMock
            .SetupSequence(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByCodeSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingWithCode);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe("NOIR-HR-020");
    }
}

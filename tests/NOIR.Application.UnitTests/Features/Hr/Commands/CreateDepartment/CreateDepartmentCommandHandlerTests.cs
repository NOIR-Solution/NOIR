using NOIR.Application.Features.Hr.Commands.CreateDepartment;
using NOIR.Application.Features.Hr.DTOs;
using NOIR.Application.Features.Hr.Specifications;
using NOIR.Domain.Entities.Hr;

namespace NOIR.Application.UnitTests.Features.Hr.Commands.CreateDepartment;

public class CreateDepartmentCommandHandlerTests
{
    private readonly Mock<IRepository<Department, Guid>> _departmentRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly CreateDepartmentCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public CreateDepartmentCommandHandlerTests()
    {
        _departmentRepositoryMock = new Mock<IRepository<Department, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new CreateDepartmentCommandHandler(
            _departmentRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesDepartment()
    {
        // Arrange
        var command = new CreateDepartmentCommand("Engineering", "ENG", "Engineering Department");

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByCodeSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Department?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Name.ShouldBe("Engineering");
        result.Value.Code.ShouldBe("ENG");

        _departmentRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateCode_ReturnsError()
    {
        // Arrange
        var command = new CreateDepartmentCommand("Engineering", "ENG");
        var existing = Department.Create("Old Engineering", "ENG", TestTenantId);

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByCodeSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe("NOIR-HR-020");
    }

    [Fact]
    public async Task Handle_InvalidParent_ReturnsError()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var command = new CreateDepartmentCommand("Sub-Engineering", "SUB-ENG", ParentDepartmentId: parentId);

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByCodeSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Department?)null);

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Department?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe("NOIR-HR-021");
    }

    [Fact]
    public async Task Handle_ValidParent_CreatesDepartmentWithParent()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var parent = Department.Create("Engineering", "ENG", TestTenantId);
        var command = new CreateDepartmentCommand("Frontend", "FE", ParentDepartmentId: parentId);

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByCodeSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Department?)null);

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(parent);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ParentDepartmentName.ShouldBe("Engineering");
    }
}

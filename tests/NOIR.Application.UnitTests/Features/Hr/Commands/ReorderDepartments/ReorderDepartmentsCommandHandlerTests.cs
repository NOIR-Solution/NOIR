using NOIR.Application.Features.Hr.Commands.ReorderDepartments;
using NOIR.Application.Features.Hr.DTOs;
using NOIR.Application.Features.Hr.Specifications;
using NOIR.Domain.Entities.Hr;

namespace NOIR.Application.UnitTests.Features.Hr.Commands.ReorderDepartments;

public class ReorderDepartmentsCommandHandlerTests
{
    private readonly Mock<IRepository<Department, Guid>> _departmentRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ReorderDepartmentsCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public ReorderDepartmentsCommandHandlerTests()
    {
        _departmentRepositoryMock = new Mock<IRepository<Department, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new ReorderDepartmentsCommandHandler(
            _departmentRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesSortOrders()
    {
        // Arrange
        var dept1Id = Guid.NewGuid();
        var dept2Id = Guid.NewGuid();
        var dept1 = Department.Create("Engineering", "ENG", TestTenantId);
        var dept2 = Department.Create("Marketing", "MKT", TestTenantId);

        var items = new List<ReorderItem>
        {
            new(dept1Id, 1),
            new(dept2Id, 0)
        };
        var command = new ReorderDepartmentsCommand(items);

        _departmentRepositoryMock
            .SetupSequence(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dept1)
            .ReturnsAsync(dept2);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldBe(true);
        dept1.SortOrder.ShouldBe(1);
        dept2.SortOrder.ShouldBe(0);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentDepartment_SkipsAndSucceeds()
    {
        // Arrange
        var items = new List<ReorderItem>
        {
            new(Guid.NewGuid(), 0)
        };
        var command = new ReorderDepartmentsCommand(items);

        _departmentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<DepartmentByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Department?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

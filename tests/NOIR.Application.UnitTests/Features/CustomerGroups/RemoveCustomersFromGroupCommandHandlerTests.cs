using NOIR.Application.Features.CustomerGroups.Commands.RemoveCustomersFromGroup;
using NOIR.Application.Features.CustomerGroups.Specifications;

namespace NOIR.Application.UnitTests.Features.CustomerGroups;

/// <summary>
/// Unit tests for RemoveCustomersFromGroupCommandHandler.
/// </summary>
public class RemoveCustomersFromGroupCommandHandlerTests
{
    private readonly Mock<IRepository<CustomerGroup, Guid>> _groupRepositoryMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly RemoveCustomersFromGroupCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public RemoveCustomersFromGroupCommandHandlerTests()
    {
        _groupRepositoryMock = new Mock<IRepository<CustomerGroup, Guid>>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new RemoveCustomersFromGroupCommandHandler(
            _groupRepositoryMock.Object,
            _dbContextMock.Object,
            _unitOfWorkMock.Object);
    }

    private static CustomerGroup CreateTestGroup()
    {
        return CustomerGroup.Create("VIP Customers", "Top-tier customers", TestTenantId);
    }

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithExistingMemberships_ShouldRemoveAndReturnSuccess()
    {
        // Arrange
        var group = CreateTestGroup();
        var customerId = Guid.NewGuid();
        var membership = CustomerGroupMembership.Create(group.Id, customerId, TestTenantId);
        var command = new RemoveCustomersFromGroupCommand(group.Id, [customerId]) { UserId = "user-1" };

        _groupRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        // Increment member count to simulate existing state
        group.IncrementMemberCount(1);

        var memberships = new List<CustomerGroupMembership> { membership }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.CustomerGroupMemberships).Returns(memberships.Object);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        group.MemberCount.Should().Be(0); // Was incremented to 1 in setup, then decremented by handler
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoMembershipsExist_ShouldReturnSuccessWithoutSaving()
    {
        // Arrange
        var group = CreateTestGroup();
        var command = new RemoveCustomersFromGroupCommand(group.Id, [Guid.NewGuid()]) { UserId = "user-1" };

        _groupRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        var emptyMemberships = new List<CustomerGroupMembership>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.CustomerGroupMemberships).Returns(emptyMemberships.Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenGroupNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = new RemoveCustomersFromGroupCommand(Guid.NewGuid(), [Guid.NewGuid()]) { UserId = "user-1" };

        _groupRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerGroup?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.CustomerGroup.NotFound);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        var group = CreateTestGroup();
        var command = new RemoveCustomersFromGroupCommand(group.Id, [Guid.NewGuid()]) { UserId = "user-1" };
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _groupRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        var emptyMemberships = new List<CustomerGroupMembership>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.CustomerGroupMemberships).Returns(emptyMemberships.Object);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _groupRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<CustomerGroupByIdForUpdateSpec>(), token),
            Times.Once);
    }

    #endregion
}

using NOIR.Application.Features.CustomerGroups.Commands.AssignCustomersToGroup;
using NOIR.Application.Features.CustomerGroups.Specifications;

namespace NOIR.Application.UnitTests.Features.CustomerGroups;

/// <summary>
/// Unit tests for AssignCustomersToGroupCommandHandler.
/// </summary>
public class AssignCustomersToGroupCommandHandlerTests
{
    private readonly Mock<IRepository<CustomerGroup, Guid>> _groupRepositoryMock;
    private readonly Mock<IRepository<Domain.Entities.Customer.Customer, Guid>> _customerRepositoryMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly AssignCustomersToGroupCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public AssignCustomersToGroupCommandHandlerTests()
    {
        _groupRepositoryMock = new Mock<IRepository<CustomerGroup, Guid>>();
        _customerRepositoryMock = new Mock<IRepository<Domain.Entities.Customer.Customer, Guid>>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _handler = new AssignCustomersToGroupCommandHandler(
            _groupRepositoryMock.Object,
            _customerRepositoryMock.Object,
            _dbContextMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object);
    }

    private static CustomerGroup CreateTestGroup()
    {
        return CustomerGroup.Create("VIP Customers", "Top-tier customers", TestTenantId);
    }

    private static Domain.Entities.Customer.Customer CreateTestCustomer()
    {
        return Domain.Entities.Customer.Customer.Create(
            null, "test@example.com", "John", "Doe", null, TestTenantId);
    }

    private void SetupEmptyMemberships()
    {
        var emptyMemberships = new List<CustomerGroupMembership>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.CustomerGroupMemberships).Returns(emptyMemberships.Object);
    }

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithNewCustomers_ShouldAssignAndReturnSuccess()
    {
        // Arrange
        var group = CreateTestGroup();
        var customer = CreateTestCustomer();
        var command = new AssignCustomersToGroupCommand(group.Id, [customer.Id]) { UserId = "user-1" };

        _groupRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        SetupEmptyMemberships();

        _customerRepositoryMock.Setup(x => x.ListAsync(
            It.IsAny<CustomersByIdsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([customer]);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAllCustomersAlreadyMembers_ShouldReturnSuccessWithoutSaving()
    {
        // Arrange
        var group = CreateTestGroup();
        var customer = CreateTestCustomer();
        var command = new AssignCustomersToGroupCommand(group.Id, [customer.Id]) { UserId = "user-1" };

        _groupRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        // Existing memberships contain this customer
        var existingMembership = CustomerGroupMembership.Create(group.Id, customer.Id, TestTenantId);
        var memberships = new List<CustomerGroupMembership> { existingMembership }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.CustomerGroupMemberships).Returns(memberships.Object);

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
        var command = new AssignCustomersToGroupCommand(Guid.NewGuid(), [Guid.NewGuid()]) { UserId = "user-1" };

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

    [Fact]
    public async Task Handle_WhenCustomerNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var group = CreateTestGroup();
        var missingCustomerId = Guid.NewGuid();
        var command = new AssignCustomersToGroupCommand(group.Id, [missingCustomerId]) { UserId = "user-1" };

        _groupRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        SetupEmptyMemberships();

        // Return empty list - customer not found
        _customerRepositoryMock.Setup(x => x.ListAsync(
            It.IsAny<CustomersByIdsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.CustomerGroup.CustomerNotFound);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSomeCustomersNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var group = CreateTestGroup();
        var existingCustomer = CreateTestCustomer();
        var missingCustomerId = Guid.NewGuid();
        var command = new AssignCustomersToGroupCommand(
            group.Id, [existingCustomer.Id, missingCustomerId]) { UserId = "user-1" };

        _groupRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        SetupEmptyMemberships();

        // Only return the existing customer — missingCustomerId is absent
        _customerRepositoryMock.Setup(x => x.ListAsync(
            It.IsAny<CustomersByIdsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingCustomer]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.CustomerGroup.CustomerNotFound);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        var group = CreateTestGroup();
        var customer = CreateTestCustomer();
        var command = new AssignCustomersToGroupCommand(group.Id, [customer.Id]) { UserId = "user-1" };
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _groupRepositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        SetupEmptyMemberships();

        _customerRepositoryMock.Setup(x => x.ListAsync(
            It.IsAny<CustomersByIdsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([customer]);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _groupRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<CustomerGroupByIdForUpdateSpec>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(token), Times.Once);
    }

    #endregion
}

using NOIR.Application.Common.DTOs;
using NOIR.Application.Features.Customers.Commands.BulkActivateCustomers;

namespace NOIR.Application.UnitTests.Features.Customers;

/// <summary>
/// Unit tests for BulkActivateCustomersCommandHandler.
/// Tests bulk customer activation scenarios with mocked dependencies.
/// </summary>
public class BulkActivateCustomersCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Customer, Guid>> _customerRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly BulkActivateCustomersCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public BulkActivateCustomersCommandHandlerTests()
    {
        _customerRepositoryMock = new Mock<IRepository<Customer, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new BulkActivateCustomersCommandHandler(
            _customerRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static BulkActivateCustomersCommand CreateTestCommand(List<Guid>? customerIds = null)
    {
        return new BulkActivateCustomersCommand(customerIds ?? new List<Guid> { Guid.NewGuid() });
    }

    private static Customer CreateTestCustomer(
        Guid? id = null,
        string email = "customer@example.com",
        string firstName = "John",
        string lastName = "Doe",
        bool isActive = true)
    {
        var customer = Customer.Create(null, email, firstName, lastName, null, TestTenantId);

        if (id.HasValue)
        {
            typeof(Customer).GetProperty("Id")!.SetValue(customer, id.Value);
        }

        if (!isActive)
        {
            customer.Deactivate();
        }

        return customer;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithInactiveCustomers_ShouldActivateAll()
    {
        // Arrange
        var customerId1 = Guid.NewGuid();
        var customerId2 = Guid.NewGuid();
        var customerIds = new List<Guid> { customerId1, customerId2 };

        var customer1 = CreateTestCustomer(customerId1, "c1@example.com", "John", "Doe", isActive: false);
        var customer2 = CreateTestCustomer(customerId2, "c2@example.com", "Jane", "Smith", isActive: false);

        var command = CreateTestCommand(customerIds);

        _customerRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CustomersByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer> { customer1, customer2 });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Success.ShouldBe(2);
        result.Value.Failed.ShouldBe(0);
        result.Value.Errors.ShouldBeEmpty();

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WithAlreadyActive_ShouldReturnErrors()
    {
        // Arrange
        var inactiveId = Guid.NewGuid();
        var activeId = Guid.NewGuid();
        var customerIds = new List<Guid> { inactiveId, activeId };

        var inactiveCustomer = CreateTestCustomer(inactiveId, "inactive@example.com", "John", "Doe", isActive: false);
        var activeCustomer = CreateTestCustomer(activeId, "active@example.com", "Jane", "Smith", isActive: true);

        var command = CreateTestCommand(customerIds);

        _customerRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CustomersByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer> { inactiveCustomer, activeCustomer });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Success.ShouldBe(1);
        result.Value.Failed.ShouldBe(1);
        result.Value.Errors.Count().ShouldBe(1);
        result.Value.Errors[0].EntityId.ShouldBe(activeId);
        result.Value.Errors[0].Message.ShouldContain("already active");
    }

    #endregion
}

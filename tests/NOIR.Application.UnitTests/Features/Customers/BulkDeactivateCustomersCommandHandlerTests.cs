using NOIR.Application.Common.DTOs;
using NOIR.Application.Features.Customers.Commands.BulkDeactivateCustomers;

namespace NOIR.Application.UnitTests.Features.Customers;

/// <summary>
/// Unit tests for BulkDeactivateCustomersCommandHandler.
/// Tests bulk customer deactivation scenarios with mocked dependencies.
/// </summary>
public class BulkDeactivateCustomersCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Customer, Guid>> _customerRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly BulkDeactivateCustomersCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public BulkDeactivateCustomersCommandHandlerTests()
    {
        _customerRepositoryMock = new Mock<IRepository<Customer, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new BulkDeactivateCustomersCommandHandler(
            _customerRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static BulkDeactivateCustomersCommand CreateTestCommand(List<Guid>? customerIds = null)
    {
        return new BulkDeactivateCustomersCommand(customerIds ?? new List<Guid> { Guid.NewGuid() });
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
    public async Task Handle_WithActiveCustomers_ShouldDeactivateAll()
    {
        // Arrange
        var customerId1 = Guid.NewGuid();
        var customerId2 = Guid.NewGuid();
        var customerId3 = Guid.NewGuid();
        var customerIds = new List<Guid> { customerId1, customerId2, customerId3 };

        var customer1 = CreateTestCustomer(customerId1, "c1@example.com", "John", "Doe");
        var customer2 = CreateTestCustomer(customerId2, "c2@example.com", "Jane", "Smith");
        var customer3 = CreateTestCustomer(customerId3, "c3@example.com", "Bob", "Lee");

        var command = CreateTestCommand(customerIds);

        _customerRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CustomersByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer> { customer1, customer2, customer3 });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Success.ShouldBe(3);
        result.Value.Failed.ShouldBe(0);
        result.Value.Errors.ShouldBeEmpty();

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WithAlreadyInactive_ShouldReturnErrors()
    {
        // Arrange
        var activeId = Guid.NewGuid();
        var inactiveId = Guid.NewGuid();
        var customerIds = new List<Guid> { activeId, inactiveId };

        var activeCustomer = CreateTestCustomer(activeId, "active@example.com", "John", "Doe", isActive: true);
        var inactiveCustomer = CreateTestCustomer(inactiveId, "inactive@example.com", "Jane", "Smith", isActive: false);

        var command = CreateTestCommand(customerIds);

        _customerRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CustomersByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer> { activeCustomer, inactiveCustomer });

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
        result.Value.Errors[0].EntityId.ShouldBe(inactiveId);
        result.Value.Errors[0].Message.ShouldContain("already inactive");
    }

    [Fact]
    public async Task Handle_WithNonExistentIds_ShouldReturnErrors()
    {
        // Arrange
        var nonExistentId1 = Guid.NewGuid();
        var nonExistentId2 = Guid.NewGuid();
        var customerIds = new List<Guid> { nonExistentId1, nonExistentId2 };

        var command = CreateTestCommand(customerIds);

        _customerRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CustomersByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer>());

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Success.ShouldBe(0);
        result.Value.Failed.ShouldBe(2);
        result.Value.Errors.Count().ShouldBe(2);
        result.Value.Errors.ShouldAllBe(e => e.Message.Contains("not found"));
    }

    #endregion
}

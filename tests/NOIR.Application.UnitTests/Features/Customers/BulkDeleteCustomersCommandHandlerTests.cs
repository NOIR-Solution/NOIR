using NOIR.Application.Common.DTOs;
using NOIR.Application.Features.Customers.Commands.BulkDeleteCustomers;

namespace NOIR.Application.UnitTests.Features.Customers;

/// <summary>
/// Unit tests for BulkDeleteCustomersCommandHandler.
/// Tests bulk customer soft-delete scenarios with mocked dependencies.
/// </summary>
public class BulkDeleteCustomersCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Customer, Guid>> _customerRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly BulkDeleteCustomersCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public BulkDeleteCustomersCommandHandlerTests()
    {
        _customerRepositoryMock = new Mock<IRepository<Customer, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new BulkDeleteCustomersCommandHandler(
            _customerRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static BulkDeleteCustomersCommand CreateTestCommand(List<Guid>? customerIds = null)
    {
        return new BulkDeleteCustomersCommand(customerIds ?? new List<Guid> { Guid.NewGuid() });
    }

    private static Customer CreateTestCustomer(
        Guid? id = null,
        string email = "customer@example.com",
        string firstName = "John",
        string lastName = "Doe")
    {
        var customer = Customer.Create(null, email, firstName, lastName, null, TestTenantId);

        if (id.HasValue)
        {
            typeof(Customer).GetProperty("Id")!.SetValue(customer, id.Value);
        }

        return customer;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCustomers_ShouldDeleteAll()
    {
        // Arrange
        var customerId1 = Guid.NewGuid();
        var customerId2 = Guid.NewGuid();
        var customerIds = new List<Guid> { customerId1, customerId2 };

        var customer1 = CreateTestCustomer(customerId1, "c1@example.com", "John", "Doe");
        var customer2 = CreateTestCustomer(customerId2, "c2@example.com", "Jane", "Smith");

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

        _customerRepositoryMock.Verify(
            x => x.Remove(It.IsAny<Customer>()),
            Times.Exactly(2));

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Failure Scenarios

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

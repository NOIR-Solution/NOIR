namespace NOIR.Application.UnitTests.Features.Customers;

/// <summary>
/// Unit tests for DeleteCustomerAddressCommandHandler.
/// Tests deleting customer addresses with mocked dependencies.
/// </summary>
public class DeleteCustomerAddressCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Customer, Guid>> _customerRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly DeleteCustomerAddressCommandHandler _handler;

    public DeleteCustomerAddressCommandHandlerTests()
    {
        _customerRepositoryMock = new Mock<IRepository<Customer, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new DeleteCustomerAddressCommandHandler(
            _customerRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static Customer CreateTestCustomerWithAddress(out Guid addressId)
    {
        var customer = Customer.Create(null, "john@example.com", "John", "Doe", null, "tenant-123");

        var address = CustomerAddress.Create(
            customer.Id,
            AddressType.Shipping,
            "John Doe",
            "0901234567",
            "123 Main St",
            "Ho Chi Minh",
            isDefault: false,
            tenantId: "tenant-123");

        customer.Addresses.Add(address);
        addressId = address.Id;
        return customer;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WhenAddressExists_ShouldDeleteAndReturnDto()
    {
        // Arrange
        var existingCustomer = CreateTestCustomerWithAddress(out var addressId);

        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new DeleteCustomerAddressCommand(existingCustomer.Id, addressId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FullName.Should().Be("John Doe");
        result.Value.Phone.Should().Be("0901234567");
        result.Value.AddressLine1.Should().Be("123 Main St");

        existingCustomer.Addresses.Should().BeEmpty();

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRemoveOnlyTargetedAddress()
    {
        // Arrange
        var customer = Customer.Create(null, "john@example.com", "John", "Doe", null, "tenant-123");

        var address1 = CustomerAddress.Create(
            customer.Id, AddressType.Shipping, "Address 1", "0900000001",
            "Line 1", "Province 1", tenantId: "tenant-123");

        var address2 = CustomerAddress.Create(
            customer.Id, AddressType.Billing, "Address 2", "0900000002",
            "Line 2", "Province 2", tenantId: "tenant-123");

        customer.Addresses.Add(address1);
        customer.Addresses.Add(address2);

        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var command = new DeleteCustomerAddressCommand(customer.Id, address1.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        customer.Addresses.Should().HaveCount(1);
        customer.Addresses.First().Id.Should().Be(address2.Id);
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenCustomerNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var command = new DeleteCustomerAddressCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-CUSTOMER-002");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAddressNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var customer = Customer.Create(null, "john@example.com", "John", "Doe", null, "tenant-123");

        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var command = new DeleteCustomerAddressCommand(customer.Id, Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-CUSTOMER-005");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var existingCustomer = CreateTestCustomerWithAddress(out var addressId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByIdForUpdateSpec>(),
                token))
            .ReturnsAsync(existingCustomer);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(token))
            .ReturnsAsync(1);

        var command = new DeleteCustomerAddressCommand(existingCustomer.Id, addressId);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _customerRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<CustomerByIdForUpdateSpec>(), token),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    #endregion
}

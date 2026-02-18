namespace NOIR.Application.UnitTests.Features.Customers;

/// <summary>
/// Unit tests for UpdateCustomerAddressCommandHandler.
/// Tests updating customer addresses with mocked dependencies.
/// </summary>
public class UpdateCustomerAddressCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Customer, Guid>> _customerRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UpdateCustomerAddressCommandHandler _handler;

    public UpdateCustomerAddressCommandHandlerTests()
    {
        _customerRepositoryMock = new Mock<IRepository<Customer, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new UpdateCustomerAddressCommandHandler(
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
    public async Task Handle_WithValidCommand_ShouldUpdateAddress()
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

        var command = new UpdateCustomerAddressCommand(
            existingCustomer.Id,
            addressId,
            AddressType.Billing,
            "Jane Smith",
            "0909876543",
            "456 Oak Ave",
            "Ha Noi",
            "Suite 200",
            "Ward 5",
            "District 3",
            "10000",
            true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AddressType.Should().Be(AddressType.Billing);
        result.Value.FullName.Should().Be("Jane Smith");
        result.Value.Phone.Should().Be("0909876543");
        result.Value.AddressLine1.Should().Be("456 Oak Ave");
        result.Value.Province.Should().Be("Ha Noi");
        result.Value.AddressLine2.Should().Be("Suite 200");
        result.Value.Ward.Should().Be("Ward 5");
        result.Value.District.Should().Be("District 3");
        result.Value.PostalCode.Should().Be("10000");
        result.Value.IsDefault.Should().BeTrue();

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSettingDefault_ShouldRemoveDefaultFromOtherAddresses()
    {
        // Arrange
        var customer = Customer.Create(null, "john@example.com", "John", "Doe", null, "tenant-123");

        var defaultAddress = CustomerAddress.Create(
            customer.Id,
            AddressType.Shipping,
            "Default",
            "0900000000",
            "Default Address",
            "Province",
            isDefault: true,
            tenantId: "tenant-123");

        var otherAddress = CustomerAddress.Create(
            customer.Id,
            AddressType.Shipping,
            "Other",
            "0900000001",
            "Other Address",
            "Province",
            isDefault: false,
            tenantId: "tenant-123");

        customer.Addresses.Add(defaultAddress);
        customer.Addresses.Add(otherAddress);

        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var command = new UpdateCustomerAddressCommand(
            customer.Id,
            otherAddress.Id,
            AddressType.Shipping,
            "Other Updated",
            "0900000001",
            "Other Address Updated",
            "Province",
            IsDefault: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsDefault.Should().BeTrue();

        // The previously default address should no longer be default
        defaultAddress.IsDefault.Should().BeFalse();
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

        var command = new UpdateCustomerAddressCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            AddressType.Shipping,
            "Name",
            "0901234567",
            "Address",
            "Province");

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

        var command = new UpdateCustomerAddressCommand(
            customer.Id,
            Guid.NewGuid(), // Non-existent address
            AddressType.Shipping,
            "Name",
            "0901234567",
            "Address",
            "Province");

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

        var command = new UpdateCustomerAddressCommand(
            existingCustomer.Id,
            addressId,
            AddressType.Shipping,
            "Name",
            "0901234567",
            "Address",
            "Province");

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

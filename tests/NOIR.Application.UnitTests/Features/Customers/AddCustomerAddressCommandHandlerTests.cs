namespace NOIR.Application.UnitTests.Features.Customers;

/// <summary>
/// Unit tests for AddCustomerAddressCommandHandler.
/// Tests adding addresses to customers with mocked dependencies.
/// </summary>
public class AddCustomerAddressCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Customer, Guid>> _customerRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly AddCustomerAddressCommandHandler _handler;

    public AddCustomerAddressCommandHandlerTests()
    {
        _customerRepositoryMock = new Mock<IRepository<Customer, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns("tenant-123");

        _handler = new AddCustomerAddressCommandHandler(
            _customerRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object);
    }

    private static Customer CreateTestCustomer()
    {
        return Customer.Create(null, "john@example.com", "John", "Doe", null, "tenant-123");
    }

    private static AddCustomerAddressCommand CreateValidCommand(
        Guid? customerId = null,
        AddressType addressType = AddressType.Shipping,
        string fullName = "John Doe",
        string phone = "0901234567",
        string addressLine1 = "123 Main St",
        string province = "Ho Chi Minh",
        string? addressLine2 = null,
        string? ward = null,
        string? district = null,
        string? postalCode = null,
        bool isDefault = false)
    {
        return new AddCustomerAddressCommand(
            customerId ?? Guid.NewGuid(),
            addressType,
            fullName,
            phone,
            addressLine1,
            province,
            addressLine2,
            ward,
            district,
            postalCode,
            isDefault);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldAddAddress()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var existingCustomer = CreateTestCustomer();

        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = CreateValidCommand(customerId: customerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FullName.Should().Be("John Doe");
        result.Value.Phone.Should().Be("0901234567");
        result.Value.AddressLine1.Should().Be("123 Main St");
        result.Value.Province.Should().Be("Ho Chi Minh");
        result.Value.AddressType.Should().Be(AddressType.Shipping);

        existingCustomer.Addresses.Should().HaveCount(1);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithOptionalFields_ShouldSetAllFields()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var existingCustomer = CreateTestCustomer();

        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);

        var command = CreateValidCommand(
            customerId: customerId,
            addressType: AddressType.Billing,
            addressLine2: "Apt 4B",
            ward: "Ward 1",
            district: "District 1",
            postalCode: "70000");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AddressType.Should().Be(AddressType.Billing);
        result.Value.AddressLine2.Should().Be("Apt 4B");
        result.Value.Ward.Should().Be("Ward 1");
        result.Value.District.Should().Be("District 1");
        result.Value.PostalCode.Should().Be("70000");
    }

    [Fact]
    public async Task Handle_WhenIsDefault_ShouldRemoveDefaultFromExistingAddresses()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var existingCustomer = CreateTestCustomer();

        // Add an existing default address
        var existingAddress = CustomerAddress.Create(
            customerId,
            AddressType.Shipping,
            "Existing",
            "0900000000",
            "Old Address",
            "Old Province",
            isDefault: true,
            tenantId: "tenant-123");

        existingCustomer.Addresses.Add(existingAddress);

        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);

        var command = CreateValidCommand(customerId: customerId, isDefault: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsDefault.Should().BeTrue();

        // The old address should no longer be default
        existingAddress.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenNotDefault_ShouldNotAffectExistingDefaults()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var existingCustomer = CreateTestCustomer();

        var existingAddress = CustomerAddress.Create(
            customerId,
            AddressType.Shipping,
            "Existing",
            "0900000000",
            "Old Address",
            "Old Province",
            isDefault: true,
            tenantId: "tenant-123");

        existingCustomer.Addresses.Add(existingAddress);

        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);

        var command = CreateValidCommand(customerId: customerId, isDefault: false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsDefault.Should().BeFalse();

        // The existing default should remain default
        existingAddress.IsDefault.Should().BeTrue();
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

        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-CUSTOMER-002");

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
        var customerId = Guid.NewGuid();
        var existingCustomer = CreateTestCustomer();
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

        var command = CreateValidCommand(customerId: customerId);

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

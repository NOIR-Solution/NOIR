namespace NOIR.Application.UnitTests.Features.Customers;

/// <summary>
/// Unit tests for GetCustomerByIdQueryHandler.
/// Tests customer retrieval by ID with mocked dependencies.
/// </summary>
public class GetCustomerByIdQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Customer, Guid>> _customerRepositoryMock;
    private readonly GetCustomerByIdQueryHandler _handler;

    public GetCustomerByIdQueryHandlerTests()
    {
        _customerRepositoryMock = new Mock<IRepository<Customer, Guid>>();

        _handler = new GetCustomerByIdQueryHandler(_customerRepositoryMock.Object);
    }

    private static Customer CreateTestCustomer(
        string email = "john@example.com",
        string firstName = "John",
        string lastName = "Doe",
        string? phone = "0901234567")
    {
        return Customer.Create(null, email, firstName, lastName, phone, "tenant-123");
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WhenCustomerExists_ShouldReturnCustomerDto()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var existingCustomer = CreateTestCustomer();

        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);

        var query = new GetCustomerByIdQuery(customerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("john@example.com");
        result.Value.FirstName.Should().Be("John");
        result.Value.LastName.Should().Be("Doe");
        result.Value.Phone.Should().Be("0901234567");
        result.Value.Segment.Should().Be(CustomerSegment.New);
        result.Value.Tier.Should().Be(CustomerTier.Standard);
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithCustomerAddresses_ShouldIncludeAddresses()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var existingCustomer = CreateTestCustomer();

        var address = CustomerAddress.Create(
            existingCustomer.Id,
            AddressType.Shipping,
            "John Doe",
            "0901234567",
            "123 Main St",
            "Ho Chi Minh",
            isDefault: true,
            tenantId: "tenant-123");

        existingCustomer.Addresses.Add(address);

        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);

        var query = new GetCustomerByIdQuery(customerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Addresses.Should().HaveCount(1);
        result.Value.Addresses[0].FullName.Should().Be("John Doe");
        result.Value.Addresses[0].AddressType.Should().Be(AddressType.Shipping);
        result.Value.Addresses[0].IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithMinimalCustomer_ShouldReturnDto()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var existingCustomer = Customer.Create(null, "min@example.com", "Min", "Customer", null, "tenant-123");

        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);

        var query = new GetCustomerByIdQuery(customerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("min@example.com");
        result.Value.Phone.Should().BeNull();
        result.Value.Tags.Should().BeNull();
        result.Value.Notes.Should().BeNull();
        result.Value.Addresses.Should().BeEmpty();
        result.Value.LoyaltyPoints.Should().Be(0);
        result.Value.TotalOrders.Should().Be(0);
        result.Value.TotalSpent.Should().Be(0);
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenCustomerNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var query = new GetCustomerByIdQuery(customerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-CUSTOMER-002");
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
                It.IsAny<CustomerByIdSpec>(),
                token))
            .ReturnsAsync(existingCustomer);

        var query = new GetCustomerByIdQuery(customerId);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _customerRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<CustomerByIdSpec>(), token),
            Times.Once);
    }

    #endregion
}

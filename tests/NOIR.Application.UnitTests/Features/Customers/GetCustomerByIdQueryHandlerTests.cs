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
        result.IsSuccess.ShouldBe(true);
        result.Value.Email.ShouldBe("john@example.com");
        result.Value.FirstName.ShouldBe("John");
        result.Value.LastName.ShouldBe("Doe");
        result.Value.Phone.ShouldBe("0901234567");
        result.Value.Segment.ShouldBe(CustomerSegment.New);
        result.Value.Tier.ShouldBe(CustomerTier.Standard);
        result.Value.IsActive.ShouldBe(true);
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
        result.IsSuccess.ShouldBe(true);
        result.Value.Addresses.Count().ShouldBe(1);
        result.Value.Addresses[0].FullName.ShouldBe("John Doe");
        result.Value.Addresses[0].AddressType.ShouldBe(AddressType.Shipping);
        result.Value.Addresses[0].IsDefault.ShouldBe(true);
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
        result.IsSuccess.ShouldBe(true);
        result.Value.Email.ShouldBe("min@example.com");
        result.Value.Phone.ShouldBeNull();
        result.Value.Tags.ShouldBeNull();
        result.Value.Notes.ShouldBeNull();
        result.Value.Addresses.ShouldBeEmpty();
        result.Value.LoyaltyPoints.ShouldBe(0);
        result.Value.TotalOrders.ShouldBe(0);
        result.Value.TotalSpent.ShouldBe(0);
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
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe("NOIR-CUSTOMER-002");
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

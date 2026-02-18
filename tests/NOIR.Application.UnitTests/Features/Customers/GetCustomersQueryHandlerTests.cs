namespace NOIR.Application.UnitTests.Features.Customers;

/// <summary>
/// Unit tests for GetCustomersQueryHandler.
/// Tests paged customer list retrieval with mocked dependencies.
/// </summary>
public class GetCustomersQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Customer, Guid>> _customerRepositoryMock;
    private readonly GetCustomersQueryHandler _handler;

    public GetCustomersQueryHandlerTests()
    {
        _customerRepositoryMock = new Mock<IRepository<Customer, Guid>>();

        _handler = new GetCustomersQueryHandler(_customerRepositoryMock.Object);
    }

    private static Customer CreateTestCustomer(
        string email,
        string firstName,
        string lastName)
    {
        return Customer.Create(null, email, firstName, lastName, null, "tenant-123");
    }

    private static List<Customer> CreateTestCustomers(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateTestCustomer($"customer{i}@example.com", $"First{i}", $"Last{i}"))
            .ToList();
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithDefaultPaging_ShouldReturnPagedResult()
    {
        // Arrange
        var customers = CreateTestCustomers(5);

        _customerRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CustomersFilterSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers);

        _customerRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<CustomersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var query = new GetCustomersQuery(Page: 1, PageSize: 20);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.TotalCount.Should().Be(5);
        result.Value.PageIndex.Should().Be(0);
        result.Value.PageNumber.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Handle_WithPaging_ShouldReturnCorrectPage()
    {
        // Arrange
        var page2Customers = CreateTestCustomers(10);

        _customerRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CustomersFilterSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page2Customers);

        _customerRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<CustomersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);

        var query = new GetCustomersQuery(Page: 2, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(10);
        result.Value.TotalCount.Should().Be(25);
        result.Value.PageIndex.Should().Be(1);
        result.Value.PageNumber.Should().Be(2);
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WithEmptyResult_ShouldReturnEmptyPagedResult()
    {
        // Arrange
        _customerRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CustomersFilterSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer>());

        _customerRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<CustomersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetCustomersQuery(Page: 1, PageSize: 20);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldMapCustomersToSummaryDto()
    {
        // Arrange
        var customer = CreateTestCustomer("vip@example.com", "VIP", "Customer");

        _customerRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CustomersFilterSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer> { customer });

        _customerRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<CustomersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var query = new GetCustomersQuery(Page: 1, PageSize: 20);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var item = result.Value.Items.First();
        item.Email.Should().Be("vip@example.com");
        item.FirstName.Should().Be("VIP");
        item.LastName.Should().Be("Customer");
        item.Segment.Should().Be(CustomerSegment.New);
        item.Tier.Should().Be(CustomerTier.Standard);
        item.IsActive.Should().BeTrue();
    }

    #endregion

    #region Filter Scenarios

    [Fact]
    public async Task Handle_WithSearchFilter_ShouldPassToSpecification()
    {
        // Arrange
        _customerRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CustomersFilterSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer>());

        _customerRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<CustomersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetCustomersQuery(Search: "John", Page: 1, PageSize: 20);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _customerRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<CustomersFilterSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _customerRepositoryMock.Verify(
            x => x.CountAsync(It.IsAny<CustomersCountSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithSegmentFilter_ShouldPassToSpecification()
    {
        // Arrange
        _customerRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CustomersFilterSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer>());

        _customerRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<CustomersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetCustomersQuery(Segment: CustomerSegment.VIP, Page: 1, PageSize: 20);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _customerRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<CustomersFilterSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithTierFilter_ShouldPassToSpecification()
    {
        // Arrange
        _customerRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CustomersFilterSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer>());

        _customerRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<CustomersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetCustomersQuery(Tier: CustomerTier.Gold, Page: 1, PageSize: 20);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _customerRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<CustomersFilterSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithIsActiveFilter_ShouldPassToSpecification()
    {
        // Arrange
        _customerRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CustomersFilterSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer>());

        _customerRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<CustomersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetCustomersQuery(IsActive: true, Page: 1, PageSize: 20);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _customerRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<CustomersFilterSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _customerRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CustomersFilterSpec>(),
                token))
            .ReturnsAsync(new List<Customer>());

        _customerRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<CustomersCountSpec>(),
                token))
            .ReturnsAsync(0);

        var query = new GetCustomersQuery(Page: 1, PageSize: 20);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _customerRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<CustomersFilterSpec>(), token),
            Times.Once);

        _customerRepositoryMock.Verify(
            x => x.CountAsync(It.IsAny<CustomersCountSpec>(), token),
            Times.Once);
    }

    #endregion
}

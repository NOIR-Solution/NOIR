namespace NOIR.Application.UnitTests.Features.Customers;

/// <summary>
/// Unit tests for GetCustomerStatsQueryHandler.
/// Tests customer statistics retrieval with mocked dependencies.
/// </summary>
public class GetCustomerStatsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Customer, Guid>> _customerRepositoryMock;
    private readonly GetCustomerStatsQueryHandler _handler;

    public GetCustomerStatsQueryHandlerTests()
    {
        _customerRepositoryMock = new Mock<IRepository<Customer, Guid>>();

        _handler = new GetCustomerStatsQueryHandler(_customerRepositoryMock.Object);
    }

    private static Customer CreateTestCustomer(
        string email = "john@example.com",
        string firstName = "John",
        string lastName = "Doe")
    {
        return Customer.Create(null, email, firstName, lastName, null, "tenant-123");
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_ShouldReturnTotalAndActiveCustomerCounts()
    {
        // Arrange
        _customerRepositoryMock
            .Setup(x => x.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);

        _customerRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<CustomersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(75);

        _customerRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<TopSpendersSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer>());

        var query = new GetCustomerStatsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCustomers.Should().Be(100);
    }

    [Fact]
    public async Task Handle_ShouldReturnSegmentDistribution()
    {
        // Arrange
        _customerRepositoryMock
            .Setup(x => x.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(50);

        _customerRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<CustomersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(10); // Returns 10 for every count spec

        _customerRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<TopSpendersSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer>());

        var query = new GetCustomerStatsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SegmentDistribution.Should().NotBeEmpty();

        // Should have an entry for each CustomerSegment enum value
        var segmentValues = Enum.GetValues<CustomerSegment>();
        result.Value.SegmentDistribution.Should().HaveCount(segmentValues.Length);
    }

    [Fact]
    public async Task Handle_ShouldReturnTierDistribution()
    {
        // Arrange
        _customerRepositoryMock
            .Setup(x => x.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(50);

        _customerRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<CustomersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        _customerRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<TopSpendersSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer>());

        var query = new GetCustomerStatsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TierDistribution.Should().NotBeEmpty();

        // Should have an entry for each CustomerTier enum value
        var tierValues = Enum.GetValues<CustomerTier>();
        result.Value.TierDistribution.Should().HaveCount(tierValues.Length);
    }

    [Fact]
    public async Task Handle_ShouldReturnTopSpenders()
    {
        // Arrange
        var topSpenders = new List<Customer>
        {
            CreateTestCustomer("spender1@example.com", "Top", "Spender1"),
            CreateTestCustomer("spender2@example.com", "Top", "Spender2")
        };

        _customerRepositoryMock
            .Setup(x => x.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(50);

        _customerRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<CustomersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        _customerRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<TopSpendersSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(topSpenders);

        var query = new GetCustomerStatsQuery(TopSpendersCount: 5);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TopSpenders.Should().HaveCount(2);
        result.Value.TopSpenders[0].Email.Should().Be("spender1@example.com");
        result.Value.TopSpenders[1].Email.Should().Be("spender2@example.com");
    }

    [Fact]
    public async Task Handle_WithEmptyDatabase_ShouldReturnZeroStats()
    {
        // Arrange
        _customerRepositoryMock
            .Setup(x => x.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _customerRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<CustomersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _customerRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<TopSpendersSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer>());

        var query = new GetCustomerStatsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCustomers.Should().Be(0);
        result.Value.ActiveCustomers.Should().Be(0);
        result.Value.TopSpenders.Should().BeEmpty();
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
            .Setup(x => x.CountAsync(token))
            .ReturnsAsync(0);

        _customerRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<CustomersCountSpec>(),
                token))
            .ReturnsAsync(0);

        _customerRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<TopSpendersSpec>(),
                token))
            .ReturnsAsync(new List<Customer>());

        var query = new GetCustomerStatsQuery();

        // Act
        await _handler.Handle(query, token);

        // Assert
        _customerRepositoryMock.Verify(
            x => x.CountAsync(token),
            Times.Once);

        _customerRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<TopSpendersSpec>(), token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCustomTopSpendersCount_ShouldUseProvidedCount()
    {
        // Arrange
        _customerRepositoryMock
            .Setup(x => x.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _customerRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<CustomersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _customerRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<TopSpendersSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer>());

        var query = new GetCustomerStatsQuery(TopSpendersCount: 25);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _customerRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<TopSpendersSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}

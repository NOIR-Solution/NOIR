using NOIR.Application.Features.Orders.DTOs;
using NOIR.Application.Features.Orders.Specifications;

namespace NOIR.Application.UnitTests.Features.Customers;

/// <summary>
/// Unit tests for GetCustomerOrdersQueryHandler.
/// Tests retrieving customer order history with mocked dependencies.
/// </summary>
public class GetCustomerOrdersQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Customer, Guid>> _customerRepositoryMock;
    private readonly Mock<IRepository<Order, Guid>> _orderRepositoryMock;
    private readonly GetCustomerOrdersQueryHandler _handler;

    public GetCustomerOrdersQueryHandlerTests()
    {
        _customerRepositoryMock = new Mock<IRepository<Customer, Guid>>();
        _orderRepositoryMock = new Mock<IRepository<Order, Guid>>();

        _handler = new GetCustomerOrdersQueryHandler(
            _customerRepositoryMock.Object,
            _orderRepositoryMock.Object);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WhenCustomerExistsWithOrders_ShouldReturnPagedResult()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        _customerRepositoryMock
            .Setup(x => x.ExistsAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _orderRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<OrdersByCustomerIdCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        _orderRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersByCustomerIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var query = new GetCustomerOrdersQuery(customerId, Page: 1, PageSize: 20);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WhenCustomerHasNoOrders_ShouldReturnEmptyResult()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        _customerRepositoryMock
            .Setup(x => x.ExistsAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _orderRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<OrdersByCustomerIdCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _orderRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersByCustomerIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var query = new GetCustomerOrdersQuery(customerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenCustomerNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        _customerRepositoryMock
            .Setup(x => x.ExistsAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var query = new GetCustomerOrdersQuery(customerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-CUSTOMER-002");

        // Should not query orders if customer doesn't exist
        _orderRepositoryMock.Verify(
            x => x.CountAsync(It.IsAny<OrdersByCustomerIdCountSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _orderRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<OrdersByCustomerIdSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepositories()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _customerRepositoryMock
            .Setup(x => x.ExistsAsync(customerId, token))
            .ReturnsAsync(true);

        _orderRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<OrdersByCustomerIdCountSpec>(),
                token))
            .ReturnsAsync(0);

        _orderRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersByCustomerIdSpec>(),
                token))
            .ReturnsAsync(new List<Order>());

        var query = new GetCustomerOrdersQuery(customerId);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _customerRepositoryMock.Verify(
            x => x.ExistsAsync(customerId, token),
            Times.Once);

        _orderRepositoryMock.Verify(
            x => x.CountAsync(It.IsAny<OrdersByCustomerIdCountSpec>(), token),
            Times.Once);

        _orderRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<OrdersByCustomerIdSpec>(), token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithPaging_ShouldCalculateCorrectPageIndex()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        _customerRepositoryMock
            .Setup(x => x.ExistsAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _orderRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<OrdersByCustomerIdCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(50);

        _orderRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersByCustomerIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var query = new GetCustomerOrdersQuery(customerId, Page: 3, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PageIndex.Should().Be(2); // 0-based internal index
        result.Value.PageNumber.Should().Be(3);
        result.Value.TotalCount.Should().Be(50);
        result.Value.TotalPages.Should().Be(5);
    }

    #endregion
}

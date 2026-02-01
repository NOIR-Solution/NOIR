using NOIR.Application.Features.Orders.DTOs;
using NOIR.Application.Features.Orders.Queries.GetOrders;
using NOIR.Application.Features.Orders.Specifications;

namespace NOIR.Application.UnitTests.Features.Orders.Queries.GetOrders;

/// <summary>
/// Unit tests for GetOrdersQueryHandler.
/// Tests order list retrieval scenarios with pagination and filtering.
/// </summary>
public class GetOrdersQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Order, Guid>> _repositoryMock;
    private readonly GetOrdersQueryHandler _handler;

    private const string TestTenantId = "test-tenant";

    public GetOrdersQueryHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<Order, Guid>>();
        _handler = new GetOrdersQueryHandler(_repositoryMock.Object);
    }

    private static Order CreateTestOrder(
        string orderNumber = "ORD-20250126-0001",
        string customerEmail = "customer@example.com",
        decimal subTotal = 100.00m,
        decimal grandTotal = 110.00m,
        OrderStatus status = OrderStatus.Pending)
    {
        var order = Order.Create(orderNumber, customerEmail, subTotal, grandTotal, "VND", TestTenantId);

        // Transition to desired status
        if (status >= OrderStatus.Confirmed)
        {
            order.Confirm();
        }
        if (status >= OrderStatus.Processing)
        {
            order.StartProcessing();
        }
        if (status >= OrderStatus.Shipped)
        {
            order.Ship("TRACK-123", "GHTK");
        }
        if (status >= OrderStatus.Delivered)
        {
            order.MarkAsDelivered();
        }
        if (status >= OrderStatus.Completed)
        {
            order.Complete();
        }

        return order;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithNoFilters_ShouldReturnAllOrders()
    {
        // Arrange
        var orders = new List<Order>
        {
            CreateTestOrder(orderNumber: "ORD-20250126-0001"),
            CreateTestOrder(orderNumber: "ORD-20250126-0002"),
            CreateTestOrder(orderNumber: "ORD-20250126-0003")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<OrdersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders.Count);

        var query = new GetOrdersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ShouldPassFilterToSpec()
    {
        // Arrange
        var orders = new List<Order>
        {
            CreateTestOrder(status: OrderStatus.Confirmed)
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<OrdersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders.Count);

        var query = new GetOrdersQuery(Status: OrderStatus.Confirmed);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public async Task Handle_WithCustomerEmailFilter_ShouldPassFilterToSpec()
    {
        // Arrange
        var orders = new List<Order>
        {
            CreateTestOrder(customerEmail: "john@example.com")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<OrdersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders.Count);

        var query = new GetOrdersQuery(CustomerEmail: "john@example.com");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].CustomerEmail.Should().Be("john@example.com");
    }

    [Fact]
    public async Task Handle_WithDateFilters_ShouldPassFiltersToSpec()
    {
        // Arrange
        var fromDate = DateTimeOffset.UtcNow.AddDays(-7);
        var toDate = DateTimeOffset.UtcNow;
        var orders = new List<Order>
        {
            CreateTestOrder(orderNumber: "ORD-20250126-0001")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<OrdersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders.Count);

        var query = new GetOrdersQuery(FromDate: fromDate, ToDate: toDate);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var order = CreateTestOrder(
            orderNumber: "ORD-20250126-0042",
            customerEmail: "jane@example.com",
            subTotal: 200.00m,
            grandTotal: 220.00m,
            status: OrderStatus.Confirmed);
        order.SetCustomerInfo(Guid.NewGuid(), "Jane Doe", "0902345678");
        order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product 1", "Size: L", 100m, 2, "SKU-001", null, null);

        var orders = new List<Order> { order };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<OrdersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders.Count);

        var query = new GetOrdersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Items[0];
        dto.OrderNumber.Should().Be("ORD-20250126-0042");
        dto.CustomerEmail.Should().Be("jane@example.com");
        dto.CustomerName.Should().Be("Jane Doe");
        // GrandTotal is recalculated when AddItem is called: SubTotal (200) - Discount (0) + Shipping (0) + Tax (0) = 200
        dto.GrandTotal.Should().Be(200.00m);
        dto.Status.Should().Be(OrderStatus.Confirmed);
        dto.ItemCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithEmptyResult_ShouldReturnEmptyList()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<OrdersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetOrdersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    #endregion

    #region Pagination Scenarios

    [Fact]
    public async Task Handle_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var orders = new List<Order>
        {
            CreateTestOrder(orderNumber: "ORD-20250126-0011"),
            CreateTestOrder(orderNumber: "ORD-20250126-0012")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<OrdersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(25); // Total 25 items

        var query = new GetOrdersQuery(Page: 2, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PageNumber.Should().Be(2);
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalCount.Should().Be(25);
        result.Value.TotalPages.Should().Be(3);
        result.Value.HasPreviousPage.Should().BeTrue();
        result.Value.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithFirstPage_ShouldNotHavePreviousPage()
    {
        // Arrange
        var orders = new List<Order>
        {
            CreateTestOrder()
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<OrdersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);

        var query = new GetOrdersQuery(Page: 1, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasPreviousPage.Should().BeFalse();
        result.Value.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithLastPage_ShouldNotHaveNextPage()
    {
        // Arrange
        var orders = new List<Order>
        {
            CreateTestOrder()
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<OrdersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);

        var query = new GetOrdersQuery(Page: 3, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasPreviousPage.Should().BeTrue();
        result.Value.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithCustomPageSize_ShouldRespectPageSize()
    {
        // Arrange
        var orders = Enumerable.Range(1, 5)
            .Select(i => CreateTestOrder(orderNumber: $"ORD-20250126-{i:D4}"))
            .ToList();

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<OrdersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(50);

        var query = new GetOrdersQuery(Page: 1, PageSize: 5);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.PageSize.Should().Be(5);
        result.Value.TotalPages.Should().Be(10);
    }

    [Fact]
    public async Task Handle_WithDefaultPagination_ShouldUseDefaultValues()
    {
        // Arrange
        var orders = new List<Order> { CreateTestOrder() };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<OrdersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var query = new GetOrdersQuery(); // Uses defaults: Page=1, PageSize=20

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PageNumber.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    #endregion

    #region Combined Filters

    [Fact]
    public async Task Handle_WithMultipleFilters_ShouldApplyAllFilters()
    {
        // Arrange
        var orders = new List<Order>
        {
            CreateTestOrder(
                orderNumber: "ORD-20250126-0001",
                customerEmail: "john@example.com",
                status: OrderStatus.Confirmed)
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<OrdersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders.Count);

        var query = new GetOrdersQuery(
            Status: OrderStatus.Confirmed,
            CustomerEmail: "john@example.com",
            Page: 1,
            PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Status.Should().Be(OrderStatus.Confirmed);
        result.Value.Items[0].CustomerEmail.Should().Be("john@example.com");
    }

    [Fact]
    public async Task Handle_WithAllFilters_ShouldApplyCorrectly()
    {
        // Arrange
        var fromDate = DateTimeOffset.UtcNow.AddDays(-30);
        var toDate = DateTimeOffset.UtcNow;

        var orders = new List<Order>
        {
            CreateTestOrder(
                orderNumber: "ORD-20250126-0001",
                customerEmail: "filtered@example.com",
                status: OrderStatus.Processing)
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<OrdersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders.Count);

        var query = new GetOrdersQuery(
            Page: 1,
            PageSize: 20,
            Status: OrderStatus.Processing,
            CustomerEmail: "filtered@example.com",
            FromDate: fromDate,
            ToDate: toDate);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var orders = new List<Order> { CreateTestOrder() };
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<OrdersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var query = new GetOrdersQuery();

        // Act
        await _handler.Handle(query, token);

        // Assert
        _repositoryMock.Verify(
            x => x.ListAsync(It.IsAny<OrdersListSpec>(), token),
            Times.Once);
        _repositoryMock.Verify(
            x => x.CountAsync(It.IsAny<OrdersCountSpec>(), token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnOrdersWithCorrectItemCount()
    {
        // Arrange
        var order1 = CreateTestOrder(orderNumber: "ORD-001");
        order1.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product 1", "Size M", 50m, 1, null, null, null);
        order1.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product 2", "Size L", 75m, 2, null, null, null);

        var order2 = CreateTestOrder(orderNumber: "ORD-002");
        order2.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product 3", "Size S", 30m, 3, null, null, null);

        var orders = new List<Order> { order1, order2 };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<OrdersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders.Count);

        var query = new GetOrdersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.First(o => o.OrderNumber == "ORD-001").ItemCount.Should().Be(2);
        result.Value.Items.First(o => o.OrderNumber == "ORD-002").ItemCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectCurrency()
    {
        // Arrange
        var order = CreateTestOrder();
        var orders = new List<Order> { order };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<OrdersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders.Count);

        var query = new GetOrdersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items[0].Currency.Should().Be("VND");
    }

    [Fact]
    public async Task Handle_WithVariousOrderStatuses_ShouldReturnCorrectStatuses()
    {
        // Arrange
        var orders = new List<Order>
        {
            CreateTestOrder(orderNumber: "ORD-001", status: OrderStatus.Pending),
            CreateTestOrder(orderNumber: "ORD-002", status: OrderStatus.Confirmed),
            CreateTestOrder(orderNumber: "ORD-003", status: OrderStatus.Processing),
            CreateTestOrder(orderNumber: "ORD-004", status: OrderStatus.Shipped)
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<OrdersCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders.Count);

        var query = new GetOrdersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().Contain(o => o.Status == OrderStatus.Pending);
        result.Value.Items.Should().Contain(o => o.Status == OrderStatus.Confirmed);
        result.Value.Items.Should().Contain(o => o.Status == OrderStatus.Processing);
        result.Value.Items.Should().Contain(o => o.Status == OrderStatus.Shipped);
    }

    #endregion
}

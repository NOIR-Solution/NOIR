using NOIR.Application.Features.Orders.DTOs;
using NOIR.Application.Features.Orders.Queries.GetOrderById;
using NOIR.Application.Features.Orders.Specifications;

namespace NOIR.Application.UnitTests.Features.Orders.Queries.GetOrderById;

/// <summary>
/// Unit tests for GetOrderByIdQueryHandler.
/// Tests order retrieval scenarios with mocked dependencies.
/// </summary>
public class GetOrderByIdQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Order, Guid>> _orderRepositoryMock;
    private readonly GetOrderByIdQueryHandler _handler;

    private const string TestTenantId = "test-tenant";

    public GetOrderByIdQueryHandlerTests()
    {
        _orderRepositoryMock = new Mock<IRepository<Order, Guid>>();

        _handler = new GetOrderByIdQueryHandler(_orderRepositoryMock.Object);
    }

    private static GetOrderByIdQuery CreateTestQuery(Guid? orderId = null)
    {
        return new GetOrderByIdQuery(orderId ?? Guid.NewGuid());
    }

    private static Order CreateTestOrder(
        string orderNumber = "ORD-20250126-0001",
        string customerEmail = "customer@example.com",
        decimal subTotal = 100.00m,
        decimal grandTotal = 110.00m)
    {
        return Order.Create(orderNumber, customerEmail, subTotal, grandTotal, "VND", TestTenantId);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithExistingOrder_ShouldReturnOrderDto()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateTestOrder();
        var query = CreateTestQuery(orderId);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<OrderByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.OrderNumber.Should().Be("ORD-20250126-0001");
        result.Value.CustomerEmail.Should().Be("customer@example.com");
        result.Value.SubTotal.Should().Be(100.00m);
        result.Value.GrandTotal.Should().Be(110.00m);
        result.Value.Currency.Should().Be("VND");
    }

    [Fact]
    public async Task Handle_WithOrderContainingItems_ShouldReturnOrderWithItems()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateTestOrder();
        existingOrder.AddItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Product",
            "Size: M",
            50.00m,
            2,
            "SKU-001",
            "https://example.com/image.jpg",
            null);
        var query = CreateTestQuery(orderId);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<OrderByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().ProductName.Should().Be("Test Product");
        result.Value.Items.First().VariantName.Should().Be("Size: M");
        result.Value.Items.First().UnitPrice.Should().Be(50.00m);
        result.Value.Items.First().Quantity.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WithConfirmedOrder_ShouldReturnCorrectStatus()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateTestOrder();
        existingOrder.Confirm();
        var query = CreateTestQuery(orderId);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<OrderByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(OrderStatus.Confirmed);
        result.Value.ConfirmedAt.Should().NotBeNull();
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenOrderNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var query = CreateTestQuery();

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<OrderByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be(ErrorCodes.Order.NotFound);
        result.Error.Message.Should().Contain("not found");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateTestOrder();
        var query = CreateTestQuery(orderId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<OrderByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _orderRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<OrderByIdSpec>(), token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithOrderHavingAddresses_ShouldReturnAddressInfo()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateTestOrder();
        var address = new NOIR.Domain.ValueObjects.Address
        {
            FullName = "John Doe",
            Phone = "0901234567",
            AddressLine1 = "123 Test Street",
            Ward = "Ward 1",
            District = "District 1",
            Province = "Ho Chi Minh City",
            Country = "Vietnam"
        };
        existingOrder.SetShippingAddress(address);
        existingOrder.SetBillingAddress(address);
        var query = CreateTestQuery(orderId);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<OrderByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ShippingAddress.Should().NotBeNull();
        result.Value.ShippingAddress!.FullName.Should().Be("John Doe");
        result.Value.BillingAddress.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithCancelledOrder_ShouldReturnCancellationInfo()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateTestOrder();
        existingOrder.Cancel("Customer requested cancellation");
        var query = CreateTestQuery(orderId);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<OrderByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(OrderStatus.Cancelled);
        result.Value.CancellationReason.Should().Be("Customer requested cancellation");
        result.Value.CancelledAt.Should().NotBeNull();
    }

    #endregion
}

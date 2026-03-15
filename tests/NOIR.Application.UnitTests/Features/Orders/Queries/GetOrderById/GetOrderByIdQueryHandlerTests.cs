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
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldNotBeNull();
        result.Value.OrderNumber.ShouldBe("ORD-20250126-0001");
        result.Value.CustomerEmail.ShouldBe("customer@example.com");
        result.Value.SubTotal.ShouldBe(100.00m);
        result.Value.GrandTotal.ShouldBe(110.00m);
        result.Value.Currency.ShouldBe("VND");
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
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(1);
        result.Value.Items.First().ProductName.ShouldBe("Test Product");
        result.Value.Items.First().VariantName.ShouldBe("Size: M");
        result.Value.Items.First().UnitPrice.ShouldBe(50.00m);
        result.Value.Items.First().Quantity.ShouldBe(2);
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
        result.IsSuccess.ShouldBe(true);
        result.Value.Status.ShouldBe(OrderStatus.Confirmed);
        result.Value.ConfirmedAt.ShouldNotBeNull();
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
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe(ErrorCodes.Order.NotFound);
        result.Error.Message.ShouldContain("not found");
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
        result.IsSuccess.ShouldBe(true);
        result.Value.ShippingAddress.ShouldNotBeNull();
        result.Value.ShippingAddress!.FullName.ShouldBe("John Doe");
        result.Value.BillingAddress.ShouldNotBeNull();
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
        result.IsSuccess.ShouldBe(true);
        result.Value.Status.ShouldBe(OrderStatus.Cancelled);
        result.Value.CancellationReason.ShouldBe("Customer requested cancellation");
        result.Value.CancelledAt.ShouldNotBeNull();
    }

    #endregion
}

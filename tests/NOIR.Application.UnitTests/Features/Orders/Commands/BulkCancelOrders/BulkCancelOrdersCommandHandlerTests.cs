using NOIR.Application.Common.DTOs;
using NOIR.Application.Features.Orders.Commands.BulkCancelOrders;

namespace NOIR.Application.UnitTests.Features.Orders.Commands.BulkCancelOrders;

/// <summary>
/// Unit tests for BulkCancelOrdersCommandHandler.
/// Tests bulk order cancel scenarios with mocked dependencies.
/// </summary>
public class BulkCancelOrdersCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Order, Guid>> _orderRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly BulkCancelOrdersCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public BulkCancelOrdersCommandHandlerTests()
    {
        _orderRepositoryMock = new Mock<IRepository<Order, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new BulkCancelOrdersCommandHandler(
            _orderRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static BulkCancelOrdersCommand CreateTestCommand(List<Guid>? orderIds = null, string? reason = null)
    {
        return new BulkCancelOrdersCommand(orderIds ?? new List<Guid> { Guid.NewGuid() }, reason);
    }

    private static Order CreateTestOrder(
        Guid? id = null,
        string orderNumber = "ORD-20260301-0001",
        string customerEmail = "customer@example.com",
        decimal subTotal = 100.00m,
        decimal grandTotal = 110.00m)
    {
        var order = Order.Create(orderNumber, customerEmail, subTotal, grandTotal, "VND", TestTenantId);

        if (id.HasValue)
        {
            typeof(Order).GetProperty("Id")!.SetValue(order, id.Value);
        }

        return order;
    }

    private static Order CreateConfirmedOrder(Guid? id = null, string orderNumber = "ORD-20260301-0002")
    {
        var order = CreateTestOrder(id, orderNumber);
        order.Confirm();
        return order;
    }

    private static Order CreateProcessingOrder(Guid? id = null, string orderNumber = "ORD-20260301-0003")
    {
        var order = CreateTestOrder(id, orderNumber);
        order.Confirm();
        order.StartProcessing();
        return order;
    }

    private static Order CreateShippedOrder(Guid? id = null, string orderNumber = "ORD-20260301-0004")
    {
        var order = CreateTestOrder(id, orderNumber);
        order.Confirm();
        order.StartProcessing();
        order.Ship("TRACK-123", "GHTK");
        return order;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithCancellableOrders_ShouldCancelAll()
    {
        // Arrange - Pending, Confirmed, Processing are all cancellable
        var pendingId = Guid.NewGuid();
        var confirmedId = Guid.NewGuid();
        var processingId = Guid.NewGuid();
        var orderIds = new List<Guid> { pendingId, confirmedId, processingId };

        var pendingOrder = CreateTestOrder(pendingId, "ORD-001");
        var confirmedOrder = CreateConfirmedOrder(confirmedId, "ORD-002");
        var processingOrder = CreateProcessingOrder(processingId, "ORD-003");

        var command = CreateTestCommand(orderIds, "Bulk cancellation");

        _orderRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { pendingOrder, confirmedOrder, processingOrder });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Success.ShouldBe(3);
        result.Value.Failed.ShouldBe(0);
        result.Value.Errors.ShouldBeEmpty();

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WithNonCancellableOrders_ShouldReturnErrors()
    {
        // Arrange - Shipped orders cannot be cancelled
        var shippedId = Guid.NewGuid();
        var orderIds = new List<Guid> { shippedId };

        var shippedOrder = CreateShippedOrder(shippedId, "ORD-001");

        var command = CreateTestCommand(orderIds, "Want to cancel");

        _orderRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { shippedOrder });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Success.ShouldBe(0);
        result.Value.Failed.ShouldBe(1);
        result.Value.Errors.Count().ShouldBe(1);
        result.Value.Errors[0].EntityId.ShouldBe(shippedId);
        result.Value.Errors[0].Message.ShouldContain("cannot be cancelled");
    }

    #endregion

    #region Reason Propagation

    [Fact]
    public async Task Handle_WithReason_ShouldPassReasonToEntity()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var orderIds = new List<Guid> { orderId };
        var reason = "Customer requested cancellation";

        var order = CreateTestOrder(orderId, "ORD-001");

        var command = CreateTestCommand(orderIds, reason);

        _orderRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { order });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Success.ShouldBe(1);
        order.Status.ShouldBe(OrderStatus.Cancelled);
        order.CancellationReason.ShouldBe(reason);
    }

    #endregion
}

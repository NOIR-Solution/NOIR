using NOIR.Application.Common.DTOs;
using NOIR.Application.Features.Orders.Commands.BulkConfirmOrders;

namespace NOIR.Application.UnitTests.Features.Orders.Commands.BulkConfirmOrders;

/// <summary>
/// Unit tests for BulkConfirmOrdersCommandHandler.
/// Tests bulk order confirm scenarios with mocked dependencies.
/// </summary>
public class BulkConfirmOrdersCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Order, Guid>> _orderRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly BulkConfirmOrdersCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public BulkConfirmOrdersCommandHandlerTests()
    {
        _orderRepositoryMock = new Mock<IRepository<Order, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new BulkConfirmOrdersCommandHandler(
            _orderRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static BulkConfirmOrdersCommand CreateTestCommand(List<Guid>? orderIds = null)
    {
        return new BulkConfirmOrdersCommand(orderIds ?? new List<Guid> { Guid.NewGuid() });
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

    private static Order CreateShippedOrder(Guid? id = null, string orderNumber = "ORD-20260301-0003")
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
    public async Task Handle_WithPendingOrders_ShouldConfirmAll()
    {
        // Arrange
        var orderId1 = Guid.NewGuid();
        var orderId2 = Guid.NewGuid();
        var orderId3 = Guid.NewGuid();
        var orderIds = new List<Guid> { orderId1, orderId2, orderId3 };

        var order1 = CreateTestOrder(orderId1, "ORD-001");
        var order2 = CreateTestOrder(orderId2, "ORD-002");
        var order3 = CreateTestOrder(orderId3, "ORD-003");

        var command = CreateTestCommand(orderIds);

        _orderRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { order1, order2, order3 });

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

    #region Partial Success Scenarios

    [Fact]
    public async Task Handle_WithMixedStatuses_ShouldConfirmOnlyPending()
    {
        // Arrange
        var pendingId = Guid.NewGuid();
        var confirmedId = Guid.NewGuid();
        var orderIds = new List<Guid> { pendingId, confirmedId };

        var pendingOrder = CreateTestOrder(pendingId, "ORD-001");
        var confirmedOrder = CreateConfirmedOrder(confirmedId, "ORD-002");

        var command = CreateTestCommand(orderIds);

        _orderRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { pendingOrder, confirmedOrder });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Success.ShouldBe(1);
        result.Value.Failed.ShouldBe(1);
        result.Value.Errors.Count().ShouldBe(1);
        result.Value.Errors[0].EntityId.ShouldBe(confirmedId);
        result.Value.Errors[0].Message.ShouldContain("not in Pending status");
    }

    [Fact]
    public async Task Handle_WithNonExistentIds_ShouldReturnErrors()
    {
        // Arrange
        var existingId = Guid.NewGuid();
        var nonExistentId = Guid.NewGuid();
        var orderIds = new List<Guid> { existingId, nonExistentId };

        var existingOrder = CreateTestOrder(existingId, "ORD-001");
        var command = CreateTestCommand(orderIds);

        _orderRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<OrdersByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { existingOrder });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Success.ShouldBe(1);
        result.Value.Failed.ShouldBe(1);
        result.Value.Errors.Count().ShouldBe(1);
        result.Value.Errors[0].EntityId.ShouldBe(nonExistentId);
        result.Value.Errors[0].Message.ShouldContain("not found");
    }

    #endregion
}

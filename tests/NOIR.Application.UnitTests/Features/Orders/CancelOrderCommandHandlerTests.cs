using NOIR.Application.Features.Orders.Commands.CancelOrder;
using NOIR.Application.Features.Orders.DTOs;
using NOIR.Application.Features.Orders.Specifications;

namespace NOIR.Application.UnitTests.Features.Orders;

/// <summary>
/// Unit tests for CancelOrderCommandHandler.
/// Tests order cancellation scenarios with mocked dependencies.
/// </summary>
public class CancelOrderCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Order, Guid>> _orderRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CancelOrderCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public CancelOrderCommandHandlerTests()
    {
        _orderRepositoryMock = new Mock<IRepository<Order, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new CancelOrderCommandHandler(
            _orderRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static CancelOrderCommand CreateTestCommand(Guid? orderId = null, string? reason = null)
    {
        return new CancelOrderCommand(orderId ?? Guid.NewGuid(), reason);
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
    public async Task Handle_WithPendingOrder_ShouldCancelOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateTestOrder();
        var command = CreateTestCommand(orderId, "Customer requested cancellation");

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<OrderByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(OrderStatus.Cancelled);
        result.Value.CancellationReason.Should().Be("Customer requested cancellation");
        result.Value.CancelledAt.Should().NotBeNull();

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithConfirmedOrder_ShouldCancelOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateTestOrder();
        existingOrder.Confirm();
        var command = CreateTestCommand(orderId, "Out of stock");

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<OrderByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(OrderStatus.Cancelled);
        result.Value.CancellationReason.Should().Be("Out of stock");
    }

    [Fact]
    public async Task Handle_WithProcessingOrder_ShouldCancelOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateTestOrder();
        existingOrder.Confirm();
        existingOrder.StartProcessing();
        var command = CreateTestCommand(orderId, "Payment verification failed");

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<OrderByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task Handle_WithNullReason_ShouldCancelOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateTestOrder();
        var command = CreateTestCommand(orderId, null);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<OrderByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(OrderStatus.Cancelled);
        result.Value.CancellationReason.Should().BeNull();
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenOrderNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = CreateTestCommand();

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<OrderByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("NOIR-ORDER-002");
        result.Error.Message.Should().Contain("not found");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Validation Scenarios

    [Fact]
    public async Task Handle_WithShippedOrder_ShouldReturnValidationError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateTestOrder();
        existingOrder.Confirm();
        existingOrder.StartProcessing();
        existingOrder.Ship("TRK-123", "Test Carrier");
        var command = CreateTestCommand(orderId, "Customer changed mind");

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<OrderByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("NOIR-ORDER-005");
        result.Error.Message.Should().Contain("Cannot cancel order");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithDeliveredOrder_ShouldReturnValidationError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateTestOrder();
        existingOrder.Confirm();
        existingOrder.StartProcessing();
        existingOrder.Ship("TRK-123", "Test Carrier");
        existingOrder.MarkAsDelivered();
        var command = CreateTestCommand(orderId, "Want refund");

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<OrderByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("NOIR-ORDER-005");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithAlreadyCancelledOrder_ShouldReturnValidationError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateTestOrder();
        existingOrder.Cancel("First cancellation");
        var command = CreateTestCommand(orderId, "Second attempt");

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<OrderByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("NOIR-ORDER-005");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithCompletedOrder_ShouldReturnValidationError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateTestOrder();
        existingOrder.Confirm();
        existingOrder.StartProcessing();
        existingOrder.Ship("TRK-123", "Test Carrier");
        existingOrder.MarkAsDelivered();
        existingOrder.Complete();
        var command = CreateTestCommand(orderId, "Test reason");

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<OrderByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("NOIR-ORDER-005");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateTestOrder();
        var command = CreateTestCommand(orderId, "Test");
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<OrderByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _orderRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<OrderByIdForUpdateSpec>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectOrderDetails()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateTestOrder(
            orderNumber: "ORD-20250126-0042",
            customerEmail: "john@example.com",
            subTotal: 200.00m,
            grandTotal: 220.00m);
        existingOrder.SetCustomerInfo(Guid.NewGuid(), "John Doe", "0901234567");
        var command = CreateTestCommand(orderId, "Customer requested");

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<OrderByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OrderNumber.Should().Be("ORD-20250126-0042");
        result.Value.CustomerEmail.Should().Be("john@example.com");
        result.Value.CustomerName.Should().Be("John Doe");
        result.Value.Status.Should().Be(OrderStatus.Cancelled);
        result.Value.CancellationReason.Should().Be("Customer requested");
    }

    #endregion
}

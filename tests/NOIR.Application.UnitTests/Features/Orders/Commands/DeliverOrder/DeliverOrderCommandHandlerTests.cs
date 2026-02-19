using NOIR.Application.Features.Orders.Commands.DeliverOrder;
using NOIR.Application.Features.Orders.DTOs;
using NOIR.Application.Features.Orders.Specifications;

namespace NOIR.Application.UnitTests.Features.Orders.Commands.DeliverOrder;

/// <summary>
/// Unit tests for DeliverOrderCommandHandler.
/// Tests order delivery scenarios with mocked dependencies.
/// </summary>
public class DeliverOrderCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Order, Guid>> _orderRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly DeliverOrderCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public DeliverOrderCommandHandlerTests()
    {
        _orderRepositoryMock = new Mock<IRepository<Order, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new DeliverOrderCommandHandler(
            _orderRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static DeliverOrderCommand CreateTestCommand(Guid? orderId = null)
    {
        return new DeliverOrderCommand(orderId ?? Guid.NewGuid());
    }

    private static Order CreateTestOrder(
        string orderNumber = "ORD-20250126-0001",
        string customerEmail = "customer@example.com",
        decimal subTotal = 100.00m,
        decimal grandTotal = 110.00m)
    {
        return Order.Create(orderNumber, customerEmail, subTotal, grandTotal, "VND", TestTenantId);
    }

    private static Order CreateShippedOrder(
        string orderNumber = "ORD-20250126-0001",
        string customerEmail = "customer@example.com")
    {
        var order = CreateTestOrder(orderNumber, customerEmail);
        order.Confirm();
        order.StartProcessing();
        order.Ship("TRACK-123", "GHTK");
        return order;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithShippedOrder_ShouldDeliverOrderSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateShippedOrder();
        var command = CreateTestCommand(orderId);

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
        result.Value.Status.Should().Be(OrderStatus.Delivered);
        result.Value.DeliveredAt.Should().NotBeNull();

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSetDeliveredAtTimestamp()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateShippedOrder();
        var command = CreateTestCommand(orderId);
        var beforeDeliver = DateTimeOffset.UtcNow;

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
        result.Value.DeliveredAt.Should().NotBeNull();
        result.Value.DeliveredAt.Should().BeOnOrAfter(beforeDeliver);
        result.Value.DeliveredAt.Should().BeOnOrBefore(DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectOrderDetails()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateShippedOrder(
            orderNumber: "ORD-20250126-0042",
            customerEmail: "john@example.com");
        existingOrder.SetCustomerInfo(Guid.NewGuid(), "John Doe", "0901234567");
        var command = CreateTestCommand(orderId);

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
        result.Value.Status.Should().Be(OrderStatus.Delivered);
        result.Value.TrackingNumber.Should().Be("TRACK-123");
        result.Value.ShippingCarrier.Should().Be("GHTK");
    }

    [Fact]
    public async Task Handle_WithOrderContainingItems_ShouldReturnOrderWithItems()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateShippedOrder();
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
        var command = CreateTestCommand(orderId);

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
        result.Value.Items.Should().HaveCount(1);
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
        result.Error.Code.Should().Be(ErrorCodes.Order.NotFound);
        result.Error.Message.Should().Contain("not found");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Business Rule Violations

    [Fact]
    public async Task Handle_WithPendingOrder_ShouldReturnValidationError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateTestOrder(); // Still in Pending status
        var command = CreateTestCommand(orderId);

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
        result.Error.Code.Should().Be(ErrorCodes.Order.InvalidDeliverTransition);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithConfirmedOrder_ShouldReturnValidationError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateTestOrder();
        existingOrder.Confirm();
        var command = CreateTestCommand(orderId);

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
        result.Error.Code.Should().Be(ErrorCodes.Order.InvalidDeliverTransition);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithProcessingOrder_ShouldReturnValidationError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateTestOrder();
        existingOrder.Confirm();
        existingOrder.StartProcessing();
        var command = CreateTestCommand(orderId);

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
        result.Error.Code.Should().Be(ErrorCodes.Order.InvalidDeliverTransition);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithAlreadyDeliveredOrder_ShouldReturnValidationError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateShippedOrder();
        existingOrder.MarkAsDelivered(); // Already delivered
        var command = CreateTestCommand(orderId);

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
        result.Error.Code.Should().Be(ErrorCodes.Order.InvalidDeliverTransition);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithCompletedOrder_ShouldReturnValidationError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateShippedOrder();
        existingOrder.MarkAsDelivered();
        existingOrder.Complete();
        var command = CreateTestCommand(orderId);

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
        result.Error.Code.Should().Be(ErrorCodes.Order.InvalidDeliverTransition);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithCancelledOrder_ShouldReturnValidationError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateTestOrder();
        existingOrder.Cancel("Test cancellation");
        var command = CreateTestCommand(orderId);

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
        result.Error.Code.Should().Be(ErrorCodes.Order.InvalidDeliverTransition);

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
        var existingOrder = CreateShippedOrder();
        var command = CreateTestCommand(orderId);
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

    #endregion
}

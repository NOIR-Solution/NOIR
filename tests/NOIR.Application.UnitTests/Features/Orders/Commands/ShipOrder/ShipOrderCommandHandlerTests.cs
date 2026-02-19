using NOIR.Application.Features.Orders.Commands.ShipOrder;
using NOIR.Application.Features.Orders.DTOs;
using NOIR.Application.Features.Orders.Specifications;

namespace NOIR.Application.UnitTests.Features.Orders.Commands.ShipOrder;

/// <summary>
/// Unit tests for ShipOrderCommandHandler.
/// Tests order shipping scenarios with mocked dependencies.
/// </summary>
public class ShipOrderCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Order, Guid>> _orderRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ShipOrderCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public ShipOrderCommandHandlerTests()
    {
        _orderRepositoryMock = new Mock<IRepository<Order, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new ShipOrderCommandHandler(
            _orderRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static ShipOrderCommand CreateTestCommand(
        Guid? orderId = null,
        string trackingNumber = "VN123456789",
        string shippingCarrier = "GHTK")
    {
        return new ShipOrderCommand(orderId ?? Guid.NewGuid(), trackingNumber, shippingCarrier);
    }

    private static Order CreateTestOrder(
        string orderNumber = "ORD-20250126-0001",
        string customerEmail = "customer@example.com",
        decimal subTotal = 100.00m,
        decimal grandTotal = 110.00m)
    {
        return Order.Create(orderNumber, customerEmail, subTotal, grandTotal, "VND", TestTenantId);
    }

    private static Order CreateConfirmedAndProcessingOrder(
        string orderNumber = "ORD-20250126-0001",
        string customerEmail = "customer@example.com")
    {
        var order = CreateTestOrder(orderNumber, customerEmail);
        order.Confirm();
        order.StartProcessing();
        return order;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithProcessingOrder_ShouldShipOrderSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateConfirmedAndProcessingOrder();
        var command = CreateTestCommand(orderId, "VN123456789", "GHTK");

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
        result.Value.Status.Should().Be(OrderStatus.Shipped);
        result.Value.TrackingNumber.Should().Be("VN123456789");
        result.Value.ShippingCarrier.Should().Be("GHTK");
        result.Value.ShippedAt.Should().NotBeNull();

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithConfirmedOrder_ShouldAutoTransitionToProcessingThenShip()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateTestOrder();
        existingOrder.Confirm(); // Only confirmed, not processing
        var command = CreateTestCommand(orderId, "GHN987654321", "GHN");

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
        result.Value.Status.Should().Be(OrderStatus.Shipped);
        result.Value.TrackingNumber.Should().Be("GHN987654321");
        result.Value.ShippingCarrier.Should().Be("GHN");
    }

    [Fact]
    public async Task Handle_ShouldReturnOrderWithItems()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateConfirmedAndProcessingOrder();
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

    [Fact]
    public async Task Handle_ShouldReturnCorrectOrderDetails()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateConfirmedAndProcessingOrder(
            orderNumber: "ORD-20250126-0042",
            customerEmail: "john@example.com");
        existingOrder.SetCustomerInfo(Guid.NewGuid(), "John Doe", "0901234567");
        var command = CreateTestCommand(orderId, "TRACK12345", "ViettelPost");

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
        result.Value.TrackingNumber.Should().Be("TRACK12345");
        result.Value.ShippingCarrier.Should().Be("ViettelPost");
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
        result.Error.Code.Should().Be(ErrorCodes.Order.InvalidShipTransition);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithAlreadyShippedOrder_ShouldReturnValidationError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateConfirmedAndProcessingOrder();
        existingOrder.Ship("EXISTING-TRACKING", "ExistingCarrier"); // Already shipped
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
        result.Error.Code.Should().Be(ErrorCodes.Order.InvalidShipTransition);
        result.Error.Message.Should().Contain("Shipped");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithDeliveredOrder_ShouldReturnValidationError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateConfirmedAndProcessingOrder();
        existingOrder.Ship("TRACKING-123", "Carrier");
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
        result.Error.Code.Should().Be(ErrorCodes.Order.InvalidShipTransition);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithCompletedOrder_ShouldReturnValidationError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateConfirmedAndProcessingOrder();
        existingOrder.Ship("TRACKING-123", "Carrier");
        existingOrder.MarkAsDelivered();
        existingOrder.Complete(); // Already completed
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
        result.Error.Code.Should().Be(ErrorCodes.Order.InvalidShipTransition);

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
        existingOrder.Cancel("Test cancellation"); // Cancelled
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
        result.Error.Code.Should().Be(ErrorCodes.Order.InvalidShipTransition);

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
        var existingOrder = CreateConfirmedAndProcessingOrder();
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

    [Fact]
    public async Task Handle_WithDifferentShippingCarriers_ShouldSupportMultipleCarriers()
    {
        // Arrange
        var testCases = new[]
        {
            ("GHTK12345", "GHTK"),
            ("GHN12345", "GHN"),
            ("VIETTEL12345", "ViettelPost"),
            ("JT12345", "J&T Express"),
            ("BEST12345", "BEST Express")
        };

        foreach (var (trackingNumber, carrier) in testCases)
        {
            var orderId = Guid.NewGuid();
            var existingOrder = CreateConfirmedAndProcessingOrder();
            var command = CreateTestCommand(orderId, trackingNumber, carrier);

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
            result.Value.TrackingNumber.Should().Be(trackingNumber);
            result.Value.ShippingCarrier.Should().Be(carrier);
        }
    }

    [Fact]
    public async Task Handle_ShouldSetShippedAtTimestamp()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateConfirmedAndProcessingOrder();
        var command = CreateTestCommand(orderId);
        var beforeShip = DateTimeOffset.UtcNow;

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
        result.Value.ShippedAt.Should().NotBeNull();
        result.Value.ShippedAt.Should().BeOnOrAfter(beforeShip);
        result.Value.ShippedAt.Should().BeOnOrBefore(DateTimeOffset.UtcNow.AddSeconds(1));
    }

    #endregion
}

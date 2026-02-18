using NOIR.Application.Features.Orders.Commands.ReturnOrder;
using NOIR.Application.Features.Orders.DTOs;
using NOIR.Application.Features.Orders.Specifications;
using NOIR.Application.Features.Products.Specifications;
using NOIR.Domain.Entities.Product;

namespace NOIR.Application.UnitTests.Features.Orders.Commands.ReturnOrder;

/// <summary>
/// Unit tests for ReturnOrderCommandHandler.
/// Tests order return scenarios including inventory release with mocked dependencies.
/// </summary>
public class ReturnOrderCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Order, Guid>> _orderRepositoryMock;
    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IInventoryMovementLogger> _movementLoggerMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ReturnOrderCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public ReturnOrderCommandHandlerTests()
    {
        _orderRepositoryMock = new Mock<IRepository<Order, Guid>>();
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _movementLoggerMock = new Mock<IInventoryMovementLogger>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new ReturnOrderCommandHandler(
            _orderRepositoryMock.Object,
            _productRepositoryMock.Object,
            _movementLoggerMock.Object,
            _unitOfWorkMock.Object);
    }

    private static ReturnOrderCommand CreateTestCommand(
        Guid? orderId = null,
        string? reason = null)
    {
        return new ReturnOrderCommand(orderId ?? Guid.NewGuid(), reason);
    }

    private static Order CreateTestOrder(
        string orderNumber = "ORD-20250126-0001",
        string customerEmail = "customer@example.com",
        decimal subTotal = 100.00m,
        decimal grandTotal = 110.00m)
    {
        return Order.Create(orderNumber, customerEmail, subTotal, grandTotal, "VND", TestTenantId);
    }

    private static Order CreateDeliveredOrder(
        string orderNumber = "ORD-20250126-0001",
        string customerEmail = "customer@example.com")
    {
        var order = CreateTestOrder(orderNumber, customerEmail);
        order.Confirm();
        order.StartProcessing();
        order.Ship("TRACK-123", "GHTK");
        order.MarkAsDelivered();
        return order;
    }

    private static Order CreateCompletedOrder(
        string orderNumber = "ORD-20250126-0001",
        string customerEmail = "customer@example.com")
    {
        var order = CreateDeliveredOrder(orderNumber, customerEmail);
        order.Complete();
        return order;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithDeliveredOrder_ShouldReturnOrderSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateDeliveredOrder();
        var command = CreateTestCommand(orderId, "Defective product");

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
        result.Value.Status.Should().Be(OrderStatus.Returned);
        result.Value.ReturnReason.Should().Be("Defective product");
        result.Value.ReturnedAt.Should().NotBeNull();

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCompletedOrder_ShouldReturnOrderSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateCompletedOrder();
        var command = CreateTestCommand(orderId, "Wrong size");

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
        result.Value.Status.Should().Be(OrderStatus.Returned);
        result.Value.ReturnReason.Should().Be("Wrong size");
    }

    [Fact]
    public async Task Handle_WithNullReason_ShouldReturnOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateDeliveredOrder();
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
        result.Value.Status.Should().Be(OrderStatus.Returned);
        result.Value.ReturnReason.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldSetReturnedAtTimestamp()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateDeliveredOrder();
        var command = CreateTestCommand(orderId, "Test reason");
        var beforeReturn = DateTimeOffset.UtcNow;

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
        result.Value.ReturnedAt.Should().NotBeNull();
        result.Value.ReturnedAt.Should().BeOnOrAfter(beforeReturn);
        result.Value.ReturnedAt.Should().BeOnOrBefore(DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task Handle_WithOrderItems_ShouldReleaseInventoryForEachItem()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        // Create products first so we can use the real variant IDs
        var product1 = Product.Create("Product 1", "product-1", 50m, "VND", TestTenantId);
        var variant1 = product1.AddVariant("Size: M", 50m, "SKU-001");
        variant1.SetStock(100);

        var product2 = Product.Create("Product 2", "product-2", 75m, "VND", TestTenantId);
        var variant2 = product2.AddVariant("Size: L", 75m, "SKU-002");
        variant2.SetStock(50);

        // Use the actual product/variant IDs when adding items to the order
        var existingOrder = CreateDeliveredOrder();
        existingOrder.AddItem(product1.Id, variant1.Id, "Product 1", "Size: M", 50m, 2, "SKU-001", null, null);
        existingOrder.AddItem(product2.Id, variant2.Id, "Product 2", "Size: L", 75m, 3, "SKU-002", null, null);
        var command = CreateTestCommand(orderId, "Customer returned all items") with { UserId = "user-123" };

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<OrderByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        // Set up sequential returns for product lookups
        _productRepositoryMock
            .SetupSequence(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product1)
            .ReturnsAsync(product2);

        _movementLoggerMock
            .Setup(x => x.LogMovementAsync(
                It.IsAny<ProductVariant>(),
                It.IsAny<InventoryMovementType>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(OrderStatus.Returned);

        // Verify inventory movement logger was called for each item
        _movementLoggerMock.Verify(
            x => x.LogMovementAsync(
                It.IsAny<ProductVariant>(),
                InventoryMovementType.Return,
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                "user-123",
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ShouldSkipInventoryRelease()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateDeliveredOrder();
        existingOrder.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Deleted Product", "Size: M", 50m, 2, "SKU-001", null, null);
        var command = CreateTestCommand(orderId, "Return deleted product");

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<OrderByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(OrderStatus.Returned);

        // Inventory movement should not be logged since product was not found
        _movementLoggerMock.Verify(
            x => x.LogMovementAsync(
                It.IsAny<ProductVariant>(),
                It.IsAny<InventoryMovementType>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectOrderDetails()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateDeliveredOrder(
            orderNumber: "ORD-20250126-0042",
            customerEmail: "john@example.com");
        existingOrder.SetCustomerInfo(Guid.NewGuid(), "John Doe", "0901234567");
        var command = CreateTestCommand(orderId, "Customer changed mind");

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
        result.Value.Status.Should().Be(OrderStatus.Returned);
        result.Value.ReturnReason.Should().Be("Customer changed mind");
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

    #region Business Rule Violations

    [Fact]
    public async Task Handle_WithPendingOrder_ShouldReturnValidationError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateTestOrder();
        var command = CreateTestCommand(orderId, "Return reason");

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
        result.Error.Code.Should().Be("NOIR-ORDER-008");

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
        var command = CreateTestCommand(orderId, "Return reason");

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
        result.Error.Code.Should().Be("NOIR-ORDER-008");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithShippedOrder_ShouldReturnValidationError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateTestOrder();
        existingOrder.Confirm();
        existingOrder.StartProcessing();
        existingOrder.Ship("TRACK-123", "GHTK");
        var command = CreateTestCommand(orderId, "Return reason");

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
        result.Error.Code.Should().Be("NOIR-ORDER-008");

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
        existingOrder.Cancel("Already cancelled");
        var command = CreateTestCommand(orderId, "Return reason");

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
        result.Error.Code.Should().Be("NOIR-ORDER-008");

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
        var existingOrder = CreateDeliveredOrder();
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
    public async Task Handle_WithOrderHavingNoItems_ShouldNotAttemptInventoryRelease()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateDeliveredOrder(); // No items added
        var command = CreateTestCommand(orderId, "Empty order return");

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
        result.Value.Status.Should().Be(OrderStatus.Returned);

        // No product lookups should have happened
        _productRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion
}

using NOIR.Application.Features.Orders.Commands.ConfirmOrder;
using NOIR.Application.Features.Orders.DTOs;
using NOIR.Application.Features.Orders.Specifications;

namespace NOIR.Application.UnitTests.Features.Orders.Commands.ConfirmOrder;

/// <summary>
/// Unit tests for ConfirmOrderCommandHandler.
/// Tests order confirmation scenarios with mocked dependencies.
/// </summary>
public class ConfirmOrderCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Order, Guid>> _orderRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly ConfirmOrderCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public ConfirmOrderCommandHandlerTests()
    {
        _orderRepositoryMock = new Mock<IRepository<Order, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new ConfirmOrderCommandHandler(
            _orderRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _entityUpdateHubMock.Object);
    }

    private static ConfirmOrderCommand CreateTestCommand(Guid? orderId = null)
    {
        return new ConfirmOrderCommand(orderId ?? Guid.NewGuid());
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
    public async Task Handle_WithPendingOrder_ShouldConfirmOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateTestOrder();
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
        result.IsSuccess.ShouldBe(true);
        result.Value.Status.ShouldBe(OrderStatus.Confirmed);
        result.Value.ConfirmedAt.ShouldNotBeNull();

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
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
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(1);
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
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe(ErrorCodes.Order.NotFound);
        result.Error.Message.ShouldContain("not found");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Validation Scenarios

    [Fact]
    public async Task Handle_WithAlreadyConfirmedOrder_ShouldReturnValidationError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var existingOrder = CreateTestOrder();
        existingOrder.Confirm(); // Already confirmed
        var command = CreateTestCommand(orderId);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<OrderByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe(ErrorCodes.Order.InvalidConfirmTransition);
        result.Error.Message.ShouldContain("Cannot confirm order");

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
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe(ErrorCodes.Order.InvalidConfirmTransition);

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
        result.IsSuccess.ShouldBe(true);
        result.Value.OrderNumber.ShouldBe("ORD-20250126-0042");
        result.Value.CustomerEmail.ShouldBe("john@example.com");
        result.Value.CustomerName.ShouldBe("John Doe");
        result.Value.CustomerPhone.ShouldBe("0901234567");
        result.Value.SubTotal.ShouldBe(200.00m);
        result.Value.GrandTotal.ShouldBe(220.00m);
        result.Value.Status.ShouldBe(OrderStatus.Confirmed);
    }

    #endregion
}

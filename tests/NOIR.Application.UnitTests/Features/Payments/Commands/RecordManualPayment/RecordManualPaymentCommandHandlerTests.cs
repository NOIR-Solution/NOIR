namespace NOIR.Application.UnitTests.Features.Payments.Commands.RecordManualPayment;

using NOIR.Application.Features.Payments.Commands.RecordManualPayment;
using NOIR.Application.Features.Payments.DTOs;
using NOIR.Application.Features.Payments.Specifications;

/// <summary>
/// Unit tests for RecordManualPaymentCommandHandler.
/// </summary>
public class RecordManualPaymentCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Order, Guid>> _orderRepositoryMock;
    private readonly Mock<IRepository<PaymentTransaction, Guid>> _paymentRepositoryMock;
    private readonly Mock<IRepository<PaymentGateway, Guid>> _gatewayRepositoryMock;
    private readonly Mock<IPaymentService> _paymentServiceMock;
    private readonly Mock<IPaymentOperationLogger> _operationLoggerMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly RecordManualPaymentCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestTransactionNumber = "PAY-20260220-001";
    private static readonly Guid TestOrderId = Guid.NewGuid();
    private static readonly Guid TestCustomerId = Guid.NewGuid();

    public RecordManualPaymentCommandHandlerTests()
    {
        _orderRepositoryMock = new Mock<IRepository<Order, Guid>>();
        _paymentRepositoryMock = new Mock<IRepository<PaymentTransaction, Guid>>();
        _gatewayRepositoryMock = new Mock<IRepository<PaymentGateway, Guid>>();
        _paymentServiceMock = new Mock<IPaymentService>();
        _operationLoggerMock = new Mock<IPaymentOperationLogger>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _paymentServiceMock.Setup(x => x.GenerateTransactionNumber()).Returns(TestTransactionNumber);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _operationLoggerMock
            .Setup(x => x.LogOperationAsync(
                It.IsAny<PaymentOperationType>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<object?>(), It.IsAny<object?>(),
                It.IsAny<int?>(), It.IsAny<long?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new RecordManualPaymentCommandHandler(
            _orderRepositoryMock.Object,
            _paymentRepositoryMock.Object,
            _gatewayRepositoryMock.Object,
            _paymentServiceMock.Object,
            _operationLoggerMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object);
    }

    private static RecordManualPaymentCommand CreateTestCommand(
        Guid? orderId = null,
        decimal amount = 500000m,
        string currency = "VND",
        PaymentMethod paymentMethod = PaymentMethod.BankTransfer,
        string? referenceNumber = "REF-001",
        string? notes = "Manual payment recorded",
        DateTimeOffset? paidAt = null)
    {
        return new RecordManualPaymentCommand(
            orderId ?? TestOrderId,
            amount,
            currency,
            paymentMethod,
            referenceNumber,
            notes,
            paidAt);
    }

    private static Order CreateTestOrder(OrderStatus status = OrderStatus.Pending)
    {
        // Use reflection to create Order since it has private constructor
        var order = (Order)Activator.CreateInstance(typeof(Order), true)!;
        typeof(Order).GetProperty("Id")?.SetValue(order, TestOrderId);
        typeof(Order).GetProperty("Status")?.SetValue(order, status);
        typeof(Order).GetProperty("CustomerId")?.SetValue(order, TestCustomerId);
        return order;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreatePaidPayment()
    {
        // Arrange
        var command = CreateTestCommand();
        var order = CreateTestOrder(OrderStatus.Pending);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<OrderByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByProviderSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentGateway?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Status.Should().Be(PaymentStatus.Paid);
        result.Value.Amount.Should().Be(500000m);
        result.Value.Currency.Should().Be("VND");
        result.Value.TransactionNumber.Should().Be(TestTransactionNumber);
    }

    [Fact]
    public async Task Handle_ShouldSetPaidAtTimestamp()
    {
        // Arrange
        var command = CreateTestCommand();
        var order = CreateTestOrder(OrderStatus.Pending);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<OrderByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByProviderSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentGateway?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithConfirmedOrder_ShouldCreatePayment()
    {
        // Arrange
        var command = CreateTestCommand();
        var order = CreateTestOrder(OrderStatus.Confirmed);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<OrderByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByProviderSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentGateway?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PaymentStatus.Paid);
    }

    [Fact]
    public async Task Handle_ShouldGenerateTransactionNumber()
    {
        // Arrange
        var command = CreateTestCommand();
        var order = CreateTestOrder(OrderStatus.Pending);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<OrderByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByProviderSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentGateway?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _paymentServiceMock.Verify(x => x.GenerateTransactionNumber(), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChangesAsync()
    {
        // Arrange
        var command = CreateTestCommand();
        var order = CreateTestOrder(OrderStatus.Pending);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<OrderByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByProviderSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentGateway?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenOrderNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = CreateTestCommand();

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<OrderByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be(ErrorCodes.Order.NotFound);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOrderInInvalidStatus_ShouldReturnError()
    {
        // Arrange
        var command = CreateTestCommand();
        var order = CreateTestOrder(OrderStatus.Shipped);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<OrderByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be(ErrorCodes.Payment.OrderNotPayable);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}

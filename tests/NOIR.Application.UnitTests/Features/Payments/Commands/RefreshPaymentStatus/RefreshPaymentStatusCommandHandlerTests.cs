namespace NOIR.Application.UnitTests.Features.Payments.Commands.RefreshPaymentStatus;

using NOIR.Application.Features.Payments.Commands.RefreshPaymentStatus;
using NOIR.Application.Features.Payments.DTOs;
using NOIR.Application.Features.Payments.Specifications;

/// <summary>
/// Unit tests for RefreshPaymentStatusCommandHandler.
/// </summary>
public class RefreshPaymentStatusCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PaymentTransaction, Guid>> _paymentRepositoryMock;
    private readonly Mock<IPaymentGatewayFactory> _gatewayFactoryMock;
    private readonly Mock<IPaymentOperationLogger> _operationLoggerMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly RefreshPaymentStatusCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestTransactionNumber = "PAY-20260131-001";
    private const string TestGatewayTransactionId = "GW-TXN-001";
    private static readonly Guid TestPaymentId = Guid.NewGuid();
    private static readonly Guid TestGatewayId = Guid.NewGuid();
    private static readonly Guid TestOperationLogId = Guid.NewGuid();

    public RefreshPaymentStatusCommandHandlerTests()
    {
        _paymentRepositoryMock = new Mock<IRepository<PaymentTransaction, Guid>>();
        _gatewayFactoryMock = new Mock<IPaymentGatewayFactory>();
        _operationLoggerMock = new Mock<IPaymentOperationLogger>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _operationLoggerMock
            .Setup(x => x.StartOperationAsync(
                It.IsAny<PaymentOperationType>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestOperationLogId);

        _operationLoggerMock
            .Setup(x => x.SetRequestDataAsync(It.IsAny<Guid>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _operationLoggerMock
            .Setup(x => x.CompleteSuccessAsync(It.IsAny<Guid>(), It.IsAny<object?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _operationLoggerMock
            .Setup(x => x.CompleteFailedAsync(
                It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<object?>(), It.IsAny<int?>(), It.IsAny<Exception?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new RefreshPaymentStatusCommandHandler(
            _paymentRepositoryMock.Object,
            _gatewayFactoryMock.Object,
            _operationLoggerMock.Object,
            _unitOfWorkMock.Object);
    }

    private static RefreshPaymentStatusCommand CreateTestCommand(Guid? paymentTransactionId = null)
    {
        return new RefreshPaymentStatusCommand(paymentTransactionId ?? TestPaymentId);
    }

    private static PaymentTransaction CreateTestPayment(
        PaymentStatus status = PaymentStatus.Pending,
        string? gatewayTransactionId = TestGatewayTransactionId)
    {
        var payment = PaymentTransaction.Create(
            TestTransactionNumber,
            TestGatewayId,
            "vnpay",
            500000m,
            "VND",
            PaymentMethod.CreditCard,
            "idempotency-key",
            TestTenantId);

        typeof(PaymentTransaction).GetProperty("Id")?.SetValue(payment, TestPaymentId);

        if (gatewayTransactionId != null)
        {
            payment.SetGatewayTransactionId(gatewayTransactionId);
        }

        switch (status)
        {
            case PaymentStatus.Processing:
                payment.MarkAsProcessing();
                break;
            case PaymentStatus.RequiresAction:
                payment.MarkAsRequiresAction();
                break;
            case PaymentStatus.Paid:
                payment.MarkAsPaid("GW-TXN-PAID");
                break;
            case PaymentStatus.Failed:
                payment.MarkAsFailed("Test failure");
                break;
            case PaymentStatus.Cancelled:
                payment.MarkAsCancelled();
                break;
            case PaymentStatus.Expired:
                payment.MarkAsExpired();
                break;
        }

        return payment;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidPendingPayment_ShouldRefreshAndUpdateStatus()
    {
        // Arrange
        var command = CreateTestCommand();
        var payment = CreateTestPayment(PaymentStatus.Pending);
        var providerMock = new Mock<IPaymentGatewayProvider>();

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        _gatewayFactoryMock
            .Setup(x => x.GetProviderWithCredentialsAsync("vnpay", It.IsAny<CancellationToken>()))
            .ReturnsAsync(providerMock.Object);

        providerMock
            .Setup(x => x.GetPaymentStatusAsync(TestGatewayTransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentStatusResult(true, PaymentStatus.Paid, TestGatewayTransactionId));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PaymentStatus.Paid);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenStatusChanged_ShouldUpdatePayment()
    {
        // Arrange
        var command = CreateTestCommand();
        var payment = CreateTestPayment(PaymentStatus.Processing);
        var providerMock = new Mock<IPaymentGatewayProvider>();

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        _gatewayFactoryMock
            .Setup(x => x.GetProviderWithCredentialsAsync("vnpay", It.IsAny<CancellationToken>()))
            .ReturnsAsync(providerMock.Object);

        providerMock
            .Setup(x => x.GetPaymentStatusAsync(TestGatewayTransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentStatusResult(true, PaymentStatus.Failed, TestGatewayTransactionId, "Insufficient funds"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PaymentStatus.Failed);
    }

    [Fact]
    public async Task Handle_WhenStatusUnchanged_ShouldReturnSuccessWithNoUpdate()
    {
        // Arrange
        var command = CreateTestCommand();
        var payment = CreateTestPayment(PaymentStatus.Pending);
        var providerMock = new Mock<IPaymentGatewayProvider>();

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        _gatewayFactoryMock
            .Setup(x => x.GetProviderWithCredentialsAsync("vnpay", It.IsAny<CancellationToken>()))
            .ReturnsAsync(providerMock.Object);

        providerMock
            .Setup(x => x.GetPaymentStatusAsync(TestGatewayTransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentStatusResult(true, PaymentStatus.Pending, TestGatewayTransactionId));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PaymentStatus.Pending);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldLogOperationWithRequestResponse()
    {
        // Arrange
        var command = CreateTestCommand();
        var payment = CreateTestPayment(PaymentStatus.Pending);
        var providerMock = new Mock<IPaymentGatewayProvider>();

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        _gatewayFactoryMock
            .Setup(x => x.GetProviderWithCredentialsAsync("vnpay", It.IsAny<CancellationToken>()))
            .ReturnsAsync(providerMock.Object);

        providerMock
            .Setup(x => x.GetPaymentStatusAsync(TestGatewayTransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentStatusResult(true, PaymentStatus.Pending, TestGatewayTransactionId));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _operationLoggerMock.Verify(x => x.StartOperationAsync(
            PaymentOperationType.ManualRefresh,
            "vnpay",
            TestTransactionNumber,
            TestPaymentId,
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _operationLoggerMock.Verify(x => x.SetRequestDataAsync(
            TestOperationLogId,
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _operationLoggerMock.Verify(x => x.CompleteSuccessAsync(
            TestOperationLogId,
            It.IsAny<object?>(),
            It.IsAny<int?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChangesAsync()
    {
        // Arrange
        var command = CreateTestCommand();
        var payment = CreateTestPayment(PaymentStatus.Pending);
        var providerMock = new Mock<IPaymentGatewayProvider>();

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        _gatewayFactoryMock
            .Setup(x => x.GetProviderWithCredentialsAsync("vnpay", It.IsAny<CancellationToken>()))
            .ReturnsAsync(providerMock.Object);

        providerMock
            .Setup(x => x.GetPaymentStatusAsync(TestGatewayTransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentStatusResult(true, PaymentStatus.Pending, TestGatewayTransactionId));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenPaymentNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = CreateTestCommand();

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentTransaction?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be(ErrorCodes.Payment.TransactionNotFound);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPaymentInTerminalState_ShouldReturnError()
    {
        // Arrange
        var command = CreateTestCommand();
        var paidPayment = CreateTestPayment(PaymentStatus.Paid);

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paidPayment);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be(ErrorCodes.Payment.InvalidStatusTransition);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenProviderNotConfigured_ShouldReturnError()
    {
        // Arrange
        var command = CreateTestCommand();
        var payment = CreateTestPayment(PaymentStatus.Pending);

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        _gatewayFactoryMock
            .Setup(x => x.GetProviderWithCredentialsAsync("vnpay", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IPaymentGatewayProvider?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be(ErrorCodes.Payment.ProviderNotConfigured);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNoGatewayTransactionId_ShouldReturnError()
    {
        // Arrange
        var command = CreateTestCommand();
        var payment = CreateTestPayment(PaymentStatus.Pending, gatewayTransactionId: null);

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be(ErrorCodes.Payment.NoGatewayTransaction);
    }

    [Fact]
    public async Task Handle_WhenGatewayCallFails_ShouldLogErrorAndReturnFailure()
    {
        // Arrange
        var command = CreateTestCommand();
        var payment = CreateTestPayment(PaymentStatus.Pending);
        var providerMock = new Mock<IPaymentGatewayProvider>();

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        _gatewayFactoryMock
            .Setup(x => x.GetProviderWithCredentialsAsync("vnpay", It.IsAny<CancellationToken>()))
            .ReturnsAsync(providerMock.Object);

        providerMock
            .Setup(x => x.GetPaymentStatusAsync(TestGatewayTransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentStatusResult(false, PaymentStatus.Pending, null, "Gateway timeout"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Payment.GatewayError);

        _operationLoggerMock.Verify(x => x.CompleteFailedAsync(
            TestOperationLogId,
            ErrorCodes.Payment.GatewayError,
            "Gateway timeout",
            It.IsAny<object?>(),
            It.IsAny<int?>(),
            It.IsAny<Exception?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}

namespace NOIR.Application.UnitTests.Features.Payments.Commands.CancelPayment;

using NOIR.Application.Features.Payments.Commands.CancelPayment;
using NOIR.Application.Features.Payments.DTOs;
using NOIR.Application.Features.Payments.Specifications;

/// <summary>
/// Unit tests for CancelPaymentCommandHandler.
/// Tests payment cancellation scenarios with mocked dependencies.
/// </summary>
public class CancelPaymentCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PaymentTransaction, Guid>> _paymentRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPaymentHubContext> _paymentHubContextMock;
    private readonly CancelPaymentCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestTransactionNumber = "PAY-20260131-001";
    private static readonly Guid TestPaymentId = Guid.NewGuid();
    private static readonly Guid TestGatewayId = Guid.NewGuid();
    private static readonly Guid TestOrderId = Guid.NewGuid();

    public CancelPaymentCommandHandlerTests()
    {
        _paymentRepositoryMock = new Mock<IRepository<PaymentTransaction, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _paymentHubContextMock = new Mock<IPaymentHubContext>();

        // Default setup
        _paymentHubContextMock
            .Setup(x => x.SendPaymentStatusUpdateAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _paymentHubContextMock
            .Setup(x => x.SendOrderPaymentUpdateAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new CancelPaymentCommandHandler(
            _paymentRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _paymentHubContextMock.Object);
    }

    private static CancelPaymentCommand CreateTestCommand(
        Guid? paymentTransactionId = null,
        string? reason = "User requested cancellation")
    {
        return new CancelPaymentCommand(
            paymentTransactionId ?? TestPaymentId,
            reason);
    }

    private static PaymentTransaction CreateTestPayment(
        PaymentStatus status = PaymentStatus.Pending,
        bool withOrder = false)
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

        if (withOrder)
        {
            payment.SetOrderId(TestOrderId);
        }

        // Set the status using the appropriate method
        switch (status)
        {
            case PaymentStatus.RequiresAction:
                payment.MarkAsRequiresAction();
                break;
            case PaymentStatus.Processing:
                payment.MarkAsProcessing();
                break;
            case PaymentStatus.Paid:
                payment.MarkAsPaid("GW-TXN-001");
                break;
            case PaymentStatus.Failed:
                payment.MarkAsFailed("Test failure");
                break;
            case PaymentStatus.Cancelled:
                payment.MarkAsCancelled();
                break;
        }

        return payment;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithPendingPayment_ShouldCancelSuccessfully()
    {
        // Arrange
        var command = CreateTestCommand();
        var pendingPayment = CreateTestPayment(PaymentStatus.Pending);

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingPayment);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Status.Should().Be(PaymentStatus.Cancelled);
        result.Value.TransactionNumber.Should().Be(TestTransactionNumber);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _paymentHubContextMock.Verify(x => x.SendPaymentStatusUpdateAsync(
            TestPaymentId,
            TestTransactionNumber,
            It.IsAny<string>(),
            "Cancelled",
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithRequiresActionPayment_ShouldCancelSuccessfully()
    {
        // Arrange
        var command = CreateTestCommand();
        var requiresActionPayment = CreateTestPayment(PaymentStatus.RequiresAction);

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(requiresActionPayment);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(PaymentStatus.Cancelled);
    }

    [Fact]
    public async Task Handle_WithOrderAssociated_ShouldSendOrderPaymentUpdate()
    {
        // Arrange
        var command = CreateTestCommand();
        var paymentWithOrder = CreateTestPayment(PaymentStatus.Pending, withOrder: true);

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paymentWithOrder);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _paymentHubContextMock.Verify(x => x.SendOrderPaymentUpdateAsync(
            TestOrderId,
            TestPaymentId,
            TestTransactionNumber,
            "Cancelled",
            It.IsAny<CancellationToken>()), Times.Once);
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
        _paymentHubContextMock.Verify(x => x.SendPaymentStatusUpdateAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPaymentAlreadyPaid_ShouldReturnValidationError()
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
        result.Error.Message.Should().Contain("pending");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPaymentAlreadyFailed_ShouldReturnValidationError()
    {
        // Arrange
        var command = CreateTestCommand();
        var failedPayment = CreateTestPayment(PaymentStatus.Failed);

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedPayment);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be(ErrorCodes.Payment.InvalidStatusTransition);
    }

    [Fact]
    public async Task Handle_WhenPaymentProcessing_ShouldReturnValidationError()
    {
        // Arrange
        var command = CreateTestCommand();
        var processingPayment = CreateTestPayment(PaymentStatus.Processing);

        _paymentRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentTransactionByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(processingPayment);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be(ErrorCodes.Payment.InvalidStatusTransition);
    }

    #endregion
}

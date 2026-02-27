using NOIR.Application.Features.Payments.EventHandlers;
using NOIR.Domain.Events.Payment;

namespace NOIR.Application.UnitTests.Features.Payments.EventHandlers;

/// <summary>
/// Unit tests for PaymentNotificationHandler.
/// Verifies SignalR hub updates and in-app admin notifications for payment lifecycle events.
/// </summary>
public class PaymentNotificationHandlerTests
{
    private readonly Mock<IPaymentHubContext> _paymentHub = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<IRepository<PaymentTransaction, Guid>> _transactionRepository = new();
    private readonly Mock<ILogger<PaymentNotificationHandler>> _logger = new();
    private readonly PaymentNotificationHandler _sut;

    public PaymentNotificationHandlerTests()
    {
        _paymentHub
            .Setup(x => x.SendPaymentStatusUpdateAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _paymentHub
            .Setup(x => x.SendOrderPaymentUpdateAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _paymentHub
            .Setup(x => x.SendRefundStatusUpdateAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<decimal>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _notificationService
            .Setup(x => x.SendToRoleAsync(
                It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<NotificationCategory>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<IEnumerable<NOIR.Application.Features.Notifications.DTOs.NotificationActionDto>?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(1));

        _sut = new PaymentNotificationHandler(
            _paymentHub.Object,
            _notificationService.Object,
            _transactionRepository.Object,
            _logger.Object);
    }

    private static PaymentTransaction CreateTransaction(Guid id, Guid? orderId = null)
    {
        var tx = PaymentTransaction.Create(
            transactionNumber: $"TXN-{id:N}",
            paymentGatewayId: Guid.NewGuid(),
            provider: "MoMo",
            amount: 500_000m,
            currency: "VND",
            paymentMethod: PaymentMethod.EWallet,
            idempotencyKey: id.ToString("N"));

        if (orderId.HasValue)
            tx.SetOrderId(orderId.Value);

        return tx;
    }

    #region PaymentSucceededEvent

    [Fact]
    public async Task Handle_PaymentSucceeded_WhenTransactionExists_ShouldSendSignalRStatusUpdate()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var evt = new PaymentSucceededEvent(transactionId, "MoMo", 500_000m, "GW-TX-001");
        var tx = CreateTransaction(transactionId);
        _transactionRepository.Setup(x => x.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _paymentHub.Verify(x => x.SendPaymentStatusUpdateAsync(
            transactionId,
            It.IsAny<string>(),
            PaymentStatus.Pending.ToString(),
            PaymentStatus.Paid.ToString(),
            It.IsAny<string?>(),
            "GW-TX-001",
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PaymentSucceeded_WhenTransactionExists_ShouldSendAdminNotification()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var evt = new PaymentSucceededEvent(transactionId, "VNPay", 300_000m, null);
        var tx = CreateTransaction(transactionId);
        _transactionRepository.Setup(x => x.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _notificationService.Verify(x => x.SendToRoleAsync(
            "admin",
            NotificationType.Success,
            NotificationCategory.Workflow,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<IEnumerable<NOIR.Application.Features.Notifications.DTOs.NotificationActionDto>?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PaymentSucceeded_WhenTransactionNotFound_ShouldSkipAndLogWarning()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var evt = new PaymentSucceededEvent(transactionId, "MoMo", 100_000m, null);
        _transactionRepository.Setup(x => x.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentTransaction?)null);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _paymentHub.Verify(x => x.SendPaymentStatusUpdateAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _notificationService.Verify(x => x.SendToRoleAsync(
            It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<NotificationCategory>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<IEnumerable<NOIR.Application.Features.Notifications.DTOs.NotificationActionDto>?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_PaymentSucceeded_WhenTransactionHasOrderId_ShouldSendOrderPaymentUpdate()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var evt = new PaymentSucceededEvent(transactionId, "Stripe", 1_000_000m, "pi_abc123");
        var tx = CreateTransaction(transactionId, orderId);
        _transactionRepository.Setup(x => x.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _paymentHub.Verify(x => x.SendOrderPaymentUpdateAsync(
            orderId,
            transactionId,
            It.IsAny<string>(),
            PaymentStatus.Paid.ToString(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region PaymentFailedEvent

    [Fact]
    public async Task Handle_PaymentFailed_WhenTransactionExists_ShouldSendSignalRFailureUpdate()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var evt = new PaymentFailedEvent(transactionId, "Insufficient funds", "INSUF");
        var tx = CreateTransaction(transactionId);
        _transactionRepository.Setup(x => x.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _paymentHub.Verify(x => x.SendPaymentStatusUpdateAsync(
            transactionId,
            It.IsAny<string>(),
            PaymentStatus.Pending.ToString(),
            PaymentStatus.Failed.ToString(),
            "Insufficient funds",
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PaymentFailed_WhenTransactionExists_ShouldSendAdminErrorNotification()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var evt = new PaymentFailedEvent(transactionId, "Card declined", null);
        var tx = CreateTransaction(transactionId);
        _transactionRepository.Setup(x => x.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _notificationService.Verify(x => x.SendToRoleAsync(
            "admin",
            NotificationType.Error,
            NotificationCategory.Workflow,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<IEnumerable<NOIR.Application.Features.Notifications.DTOs.NotificationActionDto>?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PaymentFailed_WhenTransactionNotFound_ShouldSkipNotifications()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var evt = new PaymentFailedEvent(transactionId, "Timeout", null);
        _transactionRepository.Setup(x => x.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentTransaction?)null);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _paymentHub.Verify(x => x.SendPaymentStatusUpdateAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region RefundCompletedEvent

    [Fact]
    public async Task Handle_RefundCompleted_WhenTransactionExists_ShouldSendSignalRRefundUpdate()
    {
        // Arrange
        var refundId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var evt = new RefundCompletedEvent(refundId, transactionId, 200_000m);
        var tx = CreateTransaction(transactionId);
        _transactionRepository.Setup(x => x.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _paymentHub.Verify(x => x.SendRefundStatusUpdateAsync(
            refundId,
            It.IsAny<string>(),
            transactionId,
            "Completed",
            200_000m,
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_RefundCompleted_WhenTransactionExists_ShouldSendAdminInfoNotification()
    {
        // Arrange
        var refundId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var evt = new RefundCompletedEvent(refundId, transactionId, 150_000m);
        var tx = CreateTransaction(transactionId);
        _transactionRepository.Setup(x => x.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tx);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _notificationService.Verify(x => x.SendToRoleAsync(
            "admin",
            NotificationType.Info,
            NotificationCategory.Workflow,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<IEnumerable<NOIR.Application.Features.Notifications.DTOs.NotificationActionDto>?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_RefundCompleted_WhenTransactionNotFound_ShouldStillSendRefundUpdateWithFallback()
    {
        // Arrange — handler proceeds even without a matching transaction (uses "N/A" fallback)
        var refundId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var evt = new RefundCompletedEvent(refundId, transactionId, 50_000m);
        _transactionRepository.Setup(x => x.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentTransaction?)null);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert — hub should still be called with "N/A" as refundNumber
        _paymentHub.Verify(x => x.SendRefundStatusUpdateAsync(
            refundId,
            "N/A",
            transactionId,
            "Completed",
            50_000m,
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}

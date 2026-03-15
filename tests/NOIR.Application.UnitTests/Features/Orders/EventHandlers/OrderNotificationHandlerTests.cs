using NOIR.Application.Features.Orders.EventHandlers;
using NOIR.Domain.Events.Order;

namespace NOIR.Application.UnitTests.Features.Orders.EventHandlers;

/// <summary>
/// Unit tests for OrderNotificationHandler.
/// Verifies email and in-app notification dispatch for all order lifecycle events.
/// </summary>
public class OrderNotificationHandlerTests
{
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<IRepository<Order, Guid>> _orderRepository = new();
    private readonly Mock<IRepository<Customer, Guid>> _customerRepository = new();
    private readonly Mock<ILogger<OrderNotificationHandler>> _logger = new();
    private readonly OrderNotificationHandler _sut;

    public OrderNotificationHandlerTests()
    {
        _emailService
            .Setup(x => x.SendTemplateAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _notificationService
            .Setup(x => x.SendToUserAsync(
                It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<NotificationCategory>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<IEnumerable<NOIR.Application.Features.Notifications.DTOs.NotificationActionDto>?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new NOIR.Application.Features.Notifications.DTOs.NotificationDto(
                Guid.NewGuid(), NotificationType.Info, NotificationCategory.Workflow,
                "Title", "Message", null, false, null, null,
                Enumerable.Empty<NOIR.Application.Features.Notifications.DTOs.NotificationActionDto>(),
                DateTimeOffset.UtcNow)));

        _sut = new OrderNotificationHandler(
            _emailService.Object,
            _notificationService.Object,
            _orderRepository.Object,
            _customerRepository.Object,
            _logger.Object);
    }

    #region OrderCreatedEvent

    [Fact]
    public async Task Handle_OrderCreated_ShouldCallEmailServiceWithOrderConfirmationTemplate()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var evt = new OrderCreatedEvent(orderId, "ORD-001", "customer@example.com", 500_000m, "VND");

        // Stub GetByIdAsync for the in-app notification path
        var order = Order.Create("ORD-001", "customer@example.com", 500_000m, 500_000m, tenantId: null);
        _orderRepository.Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _emailService.Verify(x => x.SendTemplateAsync(
            "customer@example.com",
            It.Is<string>(s => s.Contains("ORD-001")),
            "order_confirmation",
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_OrderCreated_WhenEmailServiceThrows_ShouldLogWarningAndNotRethrow()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var evt = new OrderCreatedEvent(orderId, "ORD-002", "fail@example.com", 100_000m, "VND");

        _emailService
            .Setup(x => x.SendTemplateAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP unavailable"));

        var order = Order.Create("ORD-002", "fail@example.com", 100_000m, 100_000m, tenantId: null);
        _orderRepository.Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var act = async () => await _sut.Handle(evt, CancellationToken.None);

        // Assert — must not throw
        act.ShouldNotThrow();
        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region OrderShippedEvent

    [Fact]
    public async Task Handle_OrderShipped_WhenOrderExists_ShouldCallEmailServiceWithOrderShippedTemplate()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var evt = new OrderShippedEvent(orderId, "ORD-003", "TRK-123", "GHN");
        var order = Order.Create("ORD-003", "ship@example.com", 200_000m, 200_000m, tenantId: null);
        _orderRepository.Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _emailService.Verify(x => x.SendTemplateAsync(
            "ship@example.com",
            It.Is<string>(s => s.Contains("ORD-003")),
            "order_shipped",
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_OrderShipped_WhenOrderNotFound_ShouldSkipAndLogWarning()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var evt = new OrderShippedEvent(orderId, "ORD-404", "TRK-999", "GHTK");
        _orderRepository.Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert — email must NOT be sent
        _emailService.Verify(x => x.SendTemplateAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<object>(), It.IsAny<CancellationToken>()),
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
    public async Task Handle_OrderShipped_WhenEmailThrows_ShouldNotRethrow()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var evt = new OrderShippedEvent(orderId, "ORD-005", "TRK-555", "VNPost");
        var order = Order.Create("ORD-005", "throw@example.com", 300_000m, 300_000m, tenantId: null);
        _orderRepository.Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _emailService
            .Setup(x => x.SendTemplateAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Timeout"));

        // Act & Assert
        await _sut.Handle(evt, CancellationToken.None);
    }

    #endregion

    #region OrderCancelledEvent

    [Fact]
    public async Task Handle_OrderCancelled_WhenOrderExists_ShouldCallEmailServiceWithOrderCancelledTemplate()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var evt = new OrderCancelledEvent(orderId, "ORD-006", "Out of stock");
        var order = Order.Create("ORD-006", "cancel@example.com", 150_000m, 150_000m, tenantId: null);
        _orderRepository.Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _emailService.Verify(x => x.SendTemplateAsync(
            "cancel@example.com",
            It.Is<string>(s => s.Contains("ORD-006")),
            "order_cancelled",
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region OrderRefundedEvent

    [Fact]
    public async Task Handle_OrderRefunded_WhenOrderExists_ShouldCallEmailServiceWithOrderRefundedTemplate()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var evt = new OrderRefundedEvent(orderId, "ORD-007", 75_000m);
        var order = Order.Create("ORD-007", "refund@example.com", 150_000m, 150_000m, tenantId: null);
        _orderRepository.Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _emailService.Verify(x => x.SendTemplateAsync(
            "refund@example.com",
            It.Is<string>(s => s.Contains("ORD-007")),
            "order_refunded",
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region In-App Notification — CustomerId gate

    [Fact]
    public async Task Handle_OrderCreated_WhenOrderHasNoCustomerId_ShouldNotSendInAppNotification()
    {
        // Arrange — create an order that has no CustomerId (guest order)
        var orderId = Guid.NewGuid();
        var evt = new OrderCreatedEvent(orderId, "ORD-GUEST", "guest@example.com", 99_000m, "VND");

        var order = Order.Create("ORD-GUEST", "guest@example.com", 99_000m, 99_000m, tenantId: null);
        // CustomerId remains null — no SetCustomerInfo call
        _orderRepository.Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert — in-app notification MUST NOT be sent since CustomerId is null
        _notificationService.Verify(x => x.SendToUserAsync(
            It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<NotificationCategory>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<IEnumerable<NOIR.Application.Features.Notifications.DTOs.NotificationActionDto>?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion
}

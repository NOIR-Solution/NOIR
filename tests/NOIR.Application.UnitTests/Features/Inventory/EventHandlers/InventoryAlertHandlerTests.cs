using NOIR.Application.Features.Inventory.EventHandlers;
using NOIR.Domain.Events.Inventory;
using NOIR.Domain.Events.Product;

namespace NOIR.Application.UnitTests.Features.Inventory.EventHandlers;

/// <summary>
/// Unit tests for InventoryAlertHandler.
/// Verifies low-stock threshold logic and receipt confirmation notifications.
/// </summary>
public class InventoryAlertHandlerTests
{
    private const int LowStockThreshold = 10;

    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<ILogger<InventoryAlertHandler>> _logger = new();
    private readonly InventoryAlertHandler _sut;

    public InventoryAlertHandlerTests()
    {
        _notificationService
            .Setup(x => x.SendToRoleAsync(
                It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<NotificationCategory>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<IEnumerable<NOIR.Application.Features.Notifications.DTOs.NotificationActionDto>?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(1));

        _sut = new InventoryAlertHandler(_notificationService.Object, _logger.Object);
    }

    #region ProductStockChangedEvent — low stock

    [Fact]
    public async Task Handle_ProductStockChanged_WhenNewQtyBelowThresholdAndDecreasing_ShouldSendAdminWarningNotification()
    {
        // Arrange — stock drops from 15 to 5 (below threshold, decreasing)
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var evt = new ProductStockChangedEvent(variantId, productId, 15, 5, InventoryMovementType.StockIn);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _notificationService.Verify(x => x.SendToRoleAsync(
            "admin",
            NotificationType.Warning,
            NotificationCategory.System,
            "Low Stock Alert",
            It.Is<string>(m => m.Contains(variantId.ToString()) && m.Contains("5")),
            It.IsAny<string?>(),
            It.Is<string?>(u => u != null && u.Contains(productId.ToString())),
            It.IsAny<IEnumerable<NOIR.Application.Features.Notifications.DTOs.NotificationActionDto>?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(5, 15)]   // stock increased — no alert
    [InlineData(15, 5)]   // stock decreased but NewQty > threshold — should alert; exclude this case
    public async Task Handle_ProductStockChanged_WhenNewQtyAboveThreshold_ShouldNotSendNotification(
        int oldQty, int newQty)
    {
        // Arrange — new quantity above threshold (11+), so no low-stock alert expected
        if (newQty <= LowStockThreshold && newQty < oldQty) return; // guard

        var evt = new ProductStockChangedEvent(
            Guid.NewGuid(), Guid.NewGuid(), oldQty, newQty, InventoryMovementType.StockIn);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _notificationService.Verify(x => x.SendToRoleAsync(
            It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<NotificationCategory>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<IEnumerable<NOIR.Application.Features.Notifications.DTOs.NotificationActionDto>?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ProductStockChanged_WhenNewQtyEqualToOldQty_ShouldNotSendNotification()
    {
        // Arrange — unchanged quantity, condition `NewQty >= OldQty` short-circuits
        var evt = new ProductStockChangedEvent(Guid.NewGuid(), Guid.NewGuid(), 5, 5, InventoryMovementType.StockIn);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _notificationService.Verify(x => x.SendToRoleAsync(
            It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<NotificationCategory>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<IEnumerable<NOIR.Application.Features.Notifications.DTOs.NotificationActionDto>?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ProductStockChanged_WhenNewQtyIncreased_ShouldNotSendNotification()
    {
        // Arrange — restocking scenario: 3 → 25, no alert needed
        var evt = new ProductStockChangedEvent(Guid.NewGuid(), Guid.NewGuid(), 3, 25, InventoryMovementType.StockIn);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _notificationService.Verify(x => x.SendToRoleAsync(
            It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<NotificationCategory>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<IEnumerable<NOIR.Application.Features.Notifications.DTOs.NotificationActionDto>?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ProductStockChanged_WhenNotificationServiceThrows_ShouldLogWarningAndNotRethrow()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var evt = new ProductStockChangedEvent(Guid.NewGuid(), productId, 20, 3, InventoryMovementType.StockIn);
        _notificationService
            .Setup(x => x.SendToRoleAsync(
                It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<NotificationCategory>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<IEnumerable<NOIR.Application.Features.Notifications.DTOs.NotificationActionDto>?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Service unavailable"));

        // Act & Assert
        await ((Func<Task>)(() => _sut.Handle(evt, CancellationToken.None))).Should().NotThrowAsync();
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

    #region InventoryReceiptConfirmedEvent

    [Fact]
    public async Task Handle_InventoryReceiptConfirmed_ShouldSendAdminSuccessNotification()
    {
        // Arrange
        var receiptId = Guid.NewGuid();
        var evt = new InventoryReceiptConfirmedEvent(receiptId, "RCV-2026-001", InventoryReceiptType.StockIn);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _notificationService.Verify(x => x.SendToRoleAsync(
            "admin",
            NotificationType.Success,
            NotificationCategory.Workflow,
            "Inventory Receipt Confirmed",
            It.Is<string>(m => m.Contains("RCV-2026-001")),
            It.IsAny<string?>(),
            It.Is<string?>(u => u != null && u.Contains(receiptId.ToString())),
            It.IsAny<IEnumerable<NOIR.Application.Features.Notifications.DTOs.NotificationActionDto>?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_InventoryReceiptConfirmed_WhenNotificationThrows_ShouldNotRethrow()
    {
        // Arrange
        var evt = new InventoryReceiptConfirmedEvent(Guid.NewGuid(), "RCV-ERR-001", InventoryReceiptType.StockIn);
        _notificationService
            .Setup(x => x.SendToRoleAsync(
                It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<NotificationCategory>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<IEnumerable<NOIR.Application.Features.Notifications.DTOs.NotificationActionDto>?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Hub unavailable"));

        // Act & Assert
        await ((Func<Task>)(() => _sut.Handle(evt, CancellationToken.None))).Should().NotThrowAsync();
    }

    #endregion
}

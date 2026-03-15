using NOIR.Application.Features.Notifications.DTOs;
using NOIR.Application.Features.Customers.EventHandlers;
using NOIR.Domain.Events.Customer;

namespace NOIR.Application.UnitTests.Features.Customers.EventHandlers;

/// <summary>
/// Unit tests for CustomerNotificationHandler.
/// Verifies welcome emails and tier-change notifications for customer lifecycle events.
/// </summary>
public class CustomerNotificationHandlerTests
{
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<IRepository<Customer, Guid>> _customerRepository = new();
    private readonly Mock<ILogger<CustomerNotificationHandler>> _logger = new();
    private readonly CustomerNotificationHandler _sut;

    public CustomerNotificationHandlerTests()
    {
        _emailService
            .Setup(x => x.SendTemplateAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _notificationService
            .Setup(x => x.SendToRoleAsync(
                It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<NotificationCategory>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<IEnumerable<NotificationActionDto>?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(1));

        _sut = new CustomerNotificationHandler(
            _emailService.Object,
            _notificationService.Object,
            _customerRepository.Object,
            _logger.Object);
    }

    #region CustomerCreatedEvent

    [Fact]
    public async Task Handle_CustomerCreated_ShouldSendWelcomeEmailWithCorrectTemplate()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var evt = new CustomerCreatedEvent(customerId, "newbie@example.com", "Alice", "Nguyen");

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _emailService.Verify(x => x.SendTemplateAsync(
            "newbie@example.com",
            "Welcome to NOIR",
            "customer_welcome",
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_CustomerCreated_WhenEmailServiceThrows_ShouldLogWarningAndNotRethrow()
    {
        // Arrange
        var evt = new CustomerCreatedEvent(Guid.NewGuid(), "fail@example.com", "Bob", "Tran");
        _emailService
            .Setup(x => x.SendTemplateAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP connection refused"));

        // Act & Assert
        await _sut.Handle(evt, CancellationToken.None);
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

    #region CustomerTierChangedEvent

    [Fact]
    public async Task Handle_CustomerTierChanged_WhenCustomerExists_ShouldSendTierChangeEmail()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var evt = new CustomerTierChangedEvent(customerId, CustomerTier.Standard, CustomerTier.Silver);

        var customer = Customer.Create("user-abc", "vip@example.com", "Charlie", "Le", tenantId: null);
        _customerRepository.Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _emailService.Verify(x => x.SendTemplateAsync(
            "vip@example.com",
            It.Is<string>(s => s.Contains("Silver")),
            "customer_tier_upgrade",
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_CustomerTierChanged_WhenCustomerExists_ShouldSendAdminInfoNotification()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var evt = new CustomerTierChangedEvent(customerId, CustomerTier.Silver, CustomerTier.Gold);

        var customer = Customer.Create("user-gold", "gold@example.com", "Diana", "Pham", tenantId: null);
        _customerRepository.Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _notificationService.Verify(x => x.SendToRoleAsync(
            "admin",
            NotificationType.Info,
            NotificationCategory.Workflow,
            "Customer Tier Changed",
            It.Is<string>(m => m.Contains("Diana") && m.Contains("Gold")),
            It.IsAny<string?>(),
            It.Is<string?>(u => u != null && u.Contains(customerId.ToString())),
            It.IsAny<IEnumerable<NotificationActionDto>?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_CustomerTierChanged_WhenCustomerNotFound_ShouldSkipAndLogWarning()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var evt = new CustomerTierChangedEvent(customerId, CustomerTier.Standard, CustomerTier.Silver);
        _customerRepository.Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _emailService.Verify(x => x.SendTemplateAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _notificationService.Verify(x => x.SendToRoleAsync(
            It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<NotificationCategory>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<IEnumerable<NotificationActionDto>?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
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
    public async Task Handle_CustomerTierChanged_WhenEmailThrows_ShouldStillAttemptAdminNotification()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var evt = new CustomerTierChangedEvent(customerId, CustomerTier.Standard, CustomerTier.Silver);

        var customer = Customer.Create("user-partial", "partial@example.com", "Eve", "Vo", tenantId: null);
        _customerRepository.Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _emailService
            .Setup(x => x.SendTemplateAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Email down"));

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert — email threw but admin notification should still be attempted
        _notificationService.Verify(x => x.SendToRoleAsync(
            "admin",
            It.IsAny<NotificationType>(),
            It.IsAny<NotificationCategory>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<IEnumerable<NotificationActionDto>?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}

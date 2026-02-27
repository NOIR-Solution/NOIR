using NOIR.Application.Features.Webhooks.EventHandlers;
using NOIR.Application.Common.Interfaces;
using NOIR.Domain.Events.Order;

namespace NOIR.Application.UnitTests.Features.Webhooks;

/// <summary>
/// Unit tests for WebhookBridgeHandler.
/// Verifies that domain events are forwarded to the dispatcher only when the Webhooks feature is enabled.
/// </summary>
public class WebhookBridgeHandlerTests
{
    private readonly Mock<IFeatureChecker> _featureCheckerMock;
    private readonly Mock<IWebhookDispatcher> _dispatcherMock;
    private readonly Mock<ILogger<WebhookBridgeHandler>> _loggerMock;
    private readonly WebhookBridgeHandler _handler;

    public WebhookBridgeHandlerTests()
    {
        _featureCheckerMock = new Mock<IFeatureChecker>();
        _dispatcherMock = new Mock<IWebhookDispatcher>();
        _loggerMock = new Mock<ILogger<WebhookBridgeHandler>>();

        _handler = new WebhookBridgeHandler(
            _featureCheckerMock.Object,
            _dispatcherMock.Object,
            _loggerMock.Object);
    }

    #region Feature Disabled

    [Fact]
    public async Task Handle_OrderCreatedEvent_WhenFeatureDisabled_ShouldNotCallDispatcher()
    {
        // Arrange
        _featureCheckerMock
            .Setup(x => x.IsEnabledAsync(NOIR.Application.Modules.ModuleNames.Integrations.Webhooks, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var evt = new OrderCreatedEvent(Guid.NewGuid(), "ORD-001", "customer@example.com", 100m, "USD");

        // Act
        await _handler.Handle(evt, CancellationToken.None);

        // Assert
        _dispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_OrderCreatedEvent_WhenFeatureDisabled_ShouldCompleteWithoutException()
    {
        // Arrange
        _featureCheckerMock
            .Setup(x => x.IsEnabledAsync(NOIR.Application.Modules.ModuleNames.Integrations.Webhooks, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var evt = new OrderCreatedEvent(Guid.NewGuid(), "ORD-002", "customer@example.com", 50m, "USD");

        // Act
        var act = async () => await _handler.Handle(evt, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Feature Enabled

    [Fact]
    public async Task Handle_OrderCreatedEvent_WhenFeatureEnabled_ShouldCallDispatcher()
    {
        // Arrange
        _featureCheckerMock
            .Setup(x => x.IsEnabledAsync(NOIR.Application.Modules.ModuleNames.Integrations.Webhooks, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var evt = new OrderCreatedEvent(Guid.NewGuid(), "ORD-003", "customer@example.com", 200m, "USD");

        // Act
        await _handler.Handle(evt, CancellationToken.None);

        // Assert
        _dispatcherMock.Verify(
            x => x.DispatchAsync(evt, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_OrderCreatedEvent_WhenFeatureEnabled_ShouldPassCorrectEventToDispatcher()
    {
        // Arrange
        _featureCheckerMock
            .Setup(x => x.IsEnabledAsync(NOIR.Application.Modules.ModuleNames.Integrations.Webhooks, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        IDomainEvent? capturedEvent = null;
        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Callback<IDomainEvent, CancellationToken>((e, _) => capturedEvent = e)
            .Returns(Task.CompletedTask);

        var orderId = Guid.NewGuid();
        var evt = new OrderCreatedEvent(orderId, "ORD-004", "test@example.com", 350m, "VND");

        // Act
        await _handler.Handle(evt, CancellationToken.None);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent.Should().BeOfType<OrderCreatedEvent>();
        ((OrderCreatedEvent)capturedEvent!).OrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task Handle_OrderCreatedEvent_WhenFeatureEnabled_ShouldPassCancellationToken()
    {
        // Arrange
        _featureCheckerMock
            .Setup(x => x.IsEnabledAsync(NOIR.Application.Modules.ModuleNames.Integrations.Webhooks, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var evt = new OrderCreatedEvent(Guid.NewGuid(), "ORD-005", "customer@example.com", 100m, "USD");
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(evt, token);

        // Assert
        _dispatcherMock.Verify(
            x => x.DispatchAsync(evt, token),
            Times.Once);
    }

    #endregion
}

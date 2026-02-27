using NOIR.Application.Features.Webhooks.Commands.DeliverWebhook;
using NOIR.Application.Features.Webhooks.Commands.TestWebhookSubscription;
using NOIR.Application.Features.Webhooks.DTOs;
using NOIR.Application.Features.Webhooks.Specifications;
using NOIR.Domain.Entities.Webhook;

namespace NOIR.Application.UnitTests.Features.Webhooks;

/// <summary>
/// Unit tests for TestWebhookSubscriptionCommandHandler.
/// Tests sending a test ping to a webhook subscription URL.
/// </summary>
public class TestWebhookSubscriptionCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<WebhookSubscription, Guid>> _repositoryMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IMessageBus> _messageBusMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly TestWebhookSubscriptionCommandHandler _handler;

    public TestWebhookSubscriptionCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<WebhookSubscription, Guid>>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _messageBusMock = new Mock<IMessageBus>();
        _currentUserMock = new Mock<ICurrentUser>();
        var loggerMock = new Mock<ILogger<TestWebhookSubscriptionCommandHandler>>();

        _currentUserMock.Setup(x => x.TenantId).Returns("tenant-123");

        var deliveryLogsDbSetMock = new Mock<DbSet<WebhookDeliveryLog>>();
        _dbContextMock.Setup(x => x.WebhookDeliveryLogs).Returns(deliveryLogsDbSetMock.Object);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new TestWebhookSubscriptionCommandHandler(
            _repositoryMock.Object,
            _dbContextMock.Object,
            _messageBusMock.Object,
            _currentUserMock.Object,
            loggerMock.Object);
    }

    private static WebhookSubscription CreateTestSubscription(
        string name = "Order Notifications",
        string url = "https://api.example.com/webhooks",
        string eventPatterns = "order.*")
    {
        return WebhookSubscription.Create(name, url, eventPatterns, tenantId: "tenant-123");
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WhenSubscriptionExists_ShouldReturnSuccessWithDeliveryLogDto()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        var command = new TestWebhookSubscriptionCommand(subscription.Id);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EventType.Should().Be("webhook.test");
        result.Value.WebhookSubscriptionId.Should().Be(subscription.Id);
        result.Value.RequestUrl.Should().Be(subscription.Url);
    }

    [Fact]
    public async Task Handle_ShouldPublishDeliverWebhookCommand()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        var command = new TestWebhookSubscriptionCommand(subscription.Id);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _messageBusMock.Verify(
            x => x.PublishAsync(It.IsAny<DeliverWebhookCommand>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCallDbContextSaveChangesAsync()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        var command = new TestWebhookSubscriptionCommand(subscription.Id);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dbContextMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenSubscriptionNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new TestWebhookSubscriptionCommand(Guid.NewGuid());

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookSubscription?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-WEBHOOK-002");
    }

    #endregion
}

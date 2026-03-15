using NOIR.Application.Features.Webhooks.Commands.UpdateWebhookSubscription;
using NOIR.Application.Features.Webhooks.DTOs;
using NOIR.Application.Features.Webhooks.Specifications;
using NOIR.Domain.Entities.Webhook;

namespace NOIR.Application.UnitTests.Features.Webhooks;

/// <summary>
/// Unit tests for UpdateWebhookSubscriptionCommandHandler.
/// Tests webhook subscription update with duplicate URL detection.
/// </summary>
public class UpdateWebhookSubscriptionCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<WebhookSubscription, Guid>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly UpdateWebhookSubscriptionCommandHandler _handler;

    public UpdateWebhookSubscriptionCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<WebhookSubscription, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new UpdateWebhookSubscriptionCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _entityUpdateHubMock.Object);
    }

    private static WebhookSubscription CreateTestSubscription(
        string name = "Order Notifications",
        string url = "https://api.example.com/webhooks",
        string eventPatterns = "order.*")
    {
        return WebhookSubscription.Create(name, url, eventPatterns, tenantId: "tenant-123");
    }

    private static UpdateWebhookSubscriptionCommand CreateValidCommand(
        Guid? id = null,
        string name = "Updated Webhook",
        string url = "https://api.example.com/webhooks",
        string eventPatterns = "order.*,payment.*",
        string? description = "Updated description",
        string? customHeaders = null,
        int maxRetries = 3,
        int timeoutSeconds = 15)
    {
        return new UpdateWebhookSubscriptionCommand(
            id ?? Guid.NewGuid(), name, url, eventPatterns, description, customHeaders, maxRetries, timeoutSeconds);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnSuccessWithDto()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        var command = CreateValidCommand(
            id: subscription.Id,
            name: "Updated Webhook",
            url: subscription.Url,
            eventPatterns: "order.*,payment.*");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Name.ShouldBe("Updated Webhook");
        result.Value.EventPatterns.ShouldBe("order.*,payment.*");
    }

    [Fact]
    public async Task Handle_WhenUrlUnchanged_ShouldNotCheckForDuplicateUrl()
    {
        // Arrange
        const string sameUrl = "https://api.example.com/webhooks";
        var subscription = CreateTestSubscription(url: sameUrl);
        var command = CreateValidCommand(id: subscription.Id, url: sameUrl);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - URL duplicate check should NOT be called
        _repositoryMock.Verify(
            x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByUrlSpec>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUrlChangedToUniqueUrl_ShouldSucceed()
    {
        // Arrange
        var subscription = CreateTestSubscription(url: "https://api.example.com/old-hook");
        var command = CreateValidCommand(id: subscription.Id, url: "https://api.example.com/new-hook");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByUrlSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookSubscription?)null);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Url.ShouldBe("https://api.example.com/new-hook");
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChangesAsync()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        var command = CreateValidCommand(id: subscription.Id, url: subscription.Url);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenSubscriptionNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = CreateValidCommand();

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookSubscription?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe("NOIR-WEBHOOK-002");
    }

    [Fact]
    public async Task Handle_WhenUrlChangedToDuplicate_ShouldReturnFailure()
    {
        // Arrange
        var subscription = CreateTestSubscription(url: "https://api.example.com/old-hook");
        const string duplicateUrl = "https://api.example.com/taken-hook";
        var command = CreateValidCommand(id: subscription.Id, url: duplicateUrl);

        var existingWithUrl = CreateTestSubscription(url: duplicateUrl);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByUrlSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingWithUrl);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe("NOIR-WEBHOOK-001");
    }

    #endregion
}

using NOIR.Application.Features.Webhooks.Commands.RotateWebhookSecret;
using NOIR.Application.Features.Webhooks.DTOs;
using NOIR.Application.Features.Webhooks.Specifications;
using NOIR.Domain.Entities.Webhook;

namespace NOIR.Application.UnitTests.Features.Webhooks;

/// <summary>
/// Unit tests for RotateWebhookSecretCommandHandler.
/// Tests webhook secret rotation with new secret generation.
/// </summary>
public class RotateWebhookSecretCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<WebhookSubscription, Guid>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly RotateWebhookSecretCommandHandler _handler;

    public RotateWebhookSecretCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<WebhookSubscription, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new RotateWebhookSecretCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object);
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
    public async Task Handle_WhenSubscriptionExists_ShouldReturnSuccessWithNewSecret()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        var command = new RotateWebhookSecretCommand(subscription.Id);

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
        result.Value.Secret.ShouldNotBeNullOrWhiteSpace();
        result.Value.Secret.Length.ShouldBe(64); // 32 bytes = 64 hex chars
    }

    [Fact]
    public async Task Handle_ShouldReturnNewSecretDifferentFromOriginal()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        var originalSecret = subscription.Secret;
        var command = new RotateWebhookSecretCommand(subscription.Id);

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
        result.Value.Secret.ShouldNotBe(originalSecret);
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChangesAsync()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        var command = new RotateWebhookSecretCommand(subscription.Id);

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
        var command = new RotateWebhookSecretCommand(Guid.NewGuid());

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

    #endregion
}

using NOIR.Application.Features.Webhooks.Commands.ActivateWebhookSubscription;
using NOIR.Application.Features.Webhooks.DTOs;
using NOIR.Application.Features.Webhooks.Specifications;
using NOIR.Domain.Entities.Webhook;

namespace NOIR.Application.UnitTests.Features.Webhooks;

/// <summary>
/// Unit tests for ActivateWebhookSubscriptionCommandHandler.
/// Tests webhook subscription activation with status transitions.
/// </summary>
public class ActivateWebhookSubscriptionCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<WebhookSubscription, Guid>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly ActivateWebhookSubscriptionCommandHandler _handler;

    public ActivateWebhookSubscriptionCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<WebhookSubscription, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new ActivateWebhookSubscriptionCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _entityUpdateHubMock.Object);
    }

    private static WebhookSubscription CreateDeactivatedSubscription()
    {
        var subscription = WebhookSubscription.Create(
            "Order Notifications",
            "https://api.example.com/webhooks",
            "order.*",
            tenantId: "tenant-123");

        subscription.Deactivate();
        return subscription;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WhenSubscriptionExists_ShouldReturnSuccessWithActiveDto()
    {
        // Arrange
        var subscription = CreateDeactivatedSubscription();
        var command = new ActivateWebhookSubscriptionCommand(subscription.Id);

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
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeTrue();
        result.Value.Status.Should().Be(WebhookSubscriptionStatus.Active);
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChangesAsync()
    {
        // Arrange
        var subscription = CreateDeactivatedSubscription();
        var command = new ActivateWebhookSubscriptionCommand(subscription.Id);

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
        var command = new ActivateWebhookSubscriptionCommand(Guid.NewGuid());

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

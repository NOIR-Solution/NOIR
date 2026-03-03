using NOIR.Application.Features.Webhooks.Commands.DeactivateWebhookSubscription;
using NOIR.Application.Features.Webhooks.DTOs;
using NOIR.Application.Features.Webhooks.Specifications;
using NOIR.Domain.Entities.Webhook;

namespace NOIR.Application.UnitTests.Features.Webhooks;

/// <summary>
/// Unit tests for DeactivateWebhookSubscriptionCommandHandler.
/// Tests webhook subscription deactivation with status transitions.
/// </summary>
public class DeactivateWebhookSubscriptionCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<WebhookSubscription, Guid>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly DeactivateWebhookSubscriptionCommandHandler _handler;

    public DeactivateWebhookSubscriptionCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<WebhookSubscription, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new DeactivateWebhookSubscriptionCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _entityUpdateHubMock.Object);
    }

    private static WebhookSubscription CreateActiveSubscription()
    {
        return WebhookSubscription.Create(
            "Order Notifications",
            "https://api.example.com/webhooks",
            "order.*",
            tenantId: "tenant-123");
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WhenSubscriptionExists_ShouldReturnSuccessWithInactiveDto()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        var command = new DeactivateWebhookSubscriptionCommand(subscription.Id);

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
        result.Value.IsActive.Should().BeFalse();
        result.Value.Status.Should().Be(WebhookSubscriptionStatus.Inactive);
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChangesAsync()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        var command = new DeactivateWebhookSubscriptionCommand(subscription.Id);

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
        var command = new DeactivateWebhookSubscriptionCommand(Guid.NewGuid());

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

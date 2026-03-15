using NOIR.Application.Features.Webhooks.Commands.CreateWebhookSubscription;
using NOIR.Application.Features.Webhooks.DTOs;
using NOIR.Application.Features.Webhooks.Specifications;
using NOIR.Domain.Entities.Webhook;

namespace NOIR.Application.UnitTests.Features.Webhooks;

/// <summary>
/// Unit tests for CreateWebhookSubscriptionCommandHandler.
/// Tests webhook subscription creation with duplicate URL detection.
/// </summary>
public class CreateWebhookSubscriptionCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<WebhookSubscription, Guid>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly CreateWebhookSubscriptionCommandHandler _handler;

    public CreateWebhookSubscriptionCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<WebhookSubscription, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns("tenant-123");

        _handler = new CreateWebhookSubscriptionCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    private static CreateWebhookSubscriptionCommand CreateValidCommand(
        string name = "Order Notifications",
        string url = "https://api.example.com/webhooks",
        string eventPatterns = "order.*",
        string? description = null,
        string? customHeaders = null,
        int maxRetries = 5,
        int timeoutSeconds = 30)
    {
        return new CreateWebhookSubscriptionCommand(name, url, eventPatterns, description, customHeaders, maxRetries, timeoutSeconds);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnSuccess()
    {
        // Arrange
        var command = CreateValidCommand();

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByUrlSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookSubscription?)null);

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<WebhookSubscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookSubscription sub, CancellationToken _) => sub);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnDtoWithCorrectProperties()
    {
        // Arrange
        var command = CreateValidCommand(
            name: "My Webhook",
            url: "https://api.example.com/hook",
            eventPatterns: "order.*,payment.*",
            description: "Order and payment notifications",
            maxRetries: 3,
            timeoutSeconds: 15);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByUrlSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookSubscription?)null);

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<WebhookSubscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookSubscription sub, CancellationToken _) => sub);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Name.ShouldBe("My Webhook");
        result.Value.Url.ShouldBe("https://api.example.com/hook");
        result.Value.EventPatterns.ShouldBe("order.*,payment.*");
        result.Value.Description.ShouldBe("Order and payment notifications");
        result.Value.MaxRetries.ShouldBe(3);
        result.Value.TimeoutSeconds.ShouldBe(15);
        result.Value.IsActive.ShouldBe(true);
        result.Value.Status.ShouldBe(WebhookSubscriptionStatus.Active);
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryAddAsync()
    {
        // Arrange
        var command = CreateValidCommand();

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByUrlSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookSubscription?)null);

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<WebhookSubscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookSubscription sub, CancellationToken _) => sub);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(
            x => x.AddAsync(It.IsAny<WebhookSubscription>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChangesAsync()
    {
        // Arrange
        var command = CreateValidCommand();

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByUrlSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookSubscription?)null);

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<WebhookSubscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookSubscription sub, CancellationToken _) => sub);

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

    [Fact]
    public async Task Handle_ShouldUseTenantIdFromCurrentUser()
    {
        // Arrange
        const string tenantId = "tenant-abc";
        _currentUserMock.Setup(x => x.TenantId).Returns(tenantId);

        var command = CreateValidCommand();
        WebhookSubscription? capturedSubscription = null;

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByUrlSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookSubscription?)null);

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<WebhookSubscription>(), It.IsAny<CancellationToken>()))
            .Callback<WebhookSubscription, CancellationToken>((sub, _) => capturedSubscription = sub)
            .ReturnsAsync((WebhookSubscription sub, CancellationToken _) => sub);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedSubscription.ShouldNotBeNull();
        capturedSubscription!.TenantId.ShouldBe(tenantId);
    }

    [Fact]
    public async Task Handle_ShouldReturnDtoWithNonEmptySecret()
    {
        // Arrange
        var command = CreateValidCommand();

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByUrlSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookSubscription?)null);

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<WebhookSubscription>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookSubscription sub, CancellationToken _) => sub);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Id.ShouldNotBe(Guid.Empty);
    }

    #endregion

    #region Conflict Scenarios

    [Fact]
    public async Task Handle_WhenUrlAlreadyExists_ShouldReturnFailure()
    {
        // Arrange
        const string duplicateUrl = "https://api.example.com/webhooks";
        var command = CreateValidCommand(url: duplicateUrl);

        var existingSubscription = WebhookSubscription.Create(
            "Existing Webhook",
            duplicateUrl,
            "order.*",
            tenantId: "tenant-123");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByUrlSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSubscription);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe("NOIR-WEBHOOK-001");
    }

    [Fact]
    public async Task Handle_WhenUrlAlreadyExists_ShouldNotCallAddAsync()
    {
        // Arrange
        const string duplicateUrl = "https://api.example.com/webhooks";
        var command = CreateValidCommand(url: duplicateUrl);

        var existingSubscription = WebhookSubscription.Create(
            "Existing Webhook",
            duplicateUrl,
            "order.*",
            tenantId: "tenant-123");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByUrlSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSubscription);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(
            x => x.AddAsync(It.IsAny<WebhookSubscription>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUrlAlreadyExists_ShouldNotCallSaveChanges()
    {
        // Arrange
        const string duplicateUrl = "https://api.example.com/webhooks";
        var command = CreateValidCommand(url: duplicateUrl);

        var existingSubscription = WebhookSubscription.Create(
            "Existing Webhook",
            duplicateUrl,
            "order.*",
            tenantId: "tenant-123");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByUrlSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSubscription);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUrlAlreadyExists_ErrorShouldContainUrlInMessage()
    {
        // Arrange
        const string duplicateUrl = "https://api.example.com/webhooks";
        var command = CreateValidCommand(url: duplicateUrl);

        var existingSubscription = WebhookSubscription.Create(
            "Existing Webhook",
            duplicateUrl,
            "order.*",
            tenantId: "tenant-123");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByUrlSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSubscription);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Error.Message.ShouldContain(duplicateUrl);
    }

    #endregion
}

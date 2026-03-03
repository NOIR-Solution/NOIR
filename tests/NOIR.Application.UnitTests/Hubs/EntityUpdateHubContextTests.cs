namespace NOIR.Application.UnitTests.Hubs;

using Microsoft.AspNetCore.SignalR;
using NOIR.Infrastructure.Hubs;

/// <summary>
/// Unit tests for EntityUpdateHubContext.
/// Tests entity update signal publishing via SignalR groups.
/// </summary>
public class EntityUpdateHubContextTests
{
    private readonly Mock<IHubContext<NotificationHub, INotificationClient>> _mockHubContext;
    private readonly Mock<IHubClients<INotificationClient>> _mockClients;
    private readonly Mock<INotificationClient> _mockCollectionGroup;
    private readonly Mock<INotificationClient> _mockInstanceGroup;
    private readonly Mock<ILogger<EntityUpdateHubContext>> _mockLogger;
    private readonly EntityUpdateHubContext _sut;

    public EntityUpdateHubContextTests()
    {
        _mockHubContext = new Mock<IHubContext<NotificationHub, INotificationClient>>();
        _mockClients = new Mock<IHubClients<INotificationClient>>();
        _mockCollectionGroup = new Mock<INotificationClient>();
        _mockInstanceGroup = new Mock<INotificationClient>();
        _mockLogger = new Mock<ILogger<EntityUpdateHubContext>>();

        _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);

        _sut = new EntityUpdateHubContext(_mockHubContext.Object, _mockLogger.Object);
    }

    #region PublishEntityUpdatedAsync Tests

    [Fact]
    public async Task PublishEntityUpdatedAsync_SendsToCollectionGroup()
    {
        // Arrange
        var entityType = "Product";
        var entityId = Guid.NewGuid();
        var tenantId = "tenant-1";

        _mockClients
            .Setup(c => c.Group($"entity_list_{entityType}_{tenantId}"))
            .Returns(_mockCollectionGroup.Object);
        _mockClients
            .Setup(c => c.Group($"entity_{entityType}_{entityId}_{tenantId}"))
            .Returns(_mockInstanceGroup.Object);

        // Act
        await _sut.PublishEntityUpdatedAsync(entityType, entityId, EntityOperation.Created, tenantId);

        // Assert
        _mockCollectionGroup.Verify(
            c => c.EntityCollectionUpdated(It.Is<EntityUpdateSignal>(s =>
                s.EntityType == entityType &&
                s.EntityId == entityId.ToString() &&
                s.Operation == EntityOperation.Created)),
            Times.Once);
    }

    [Fact]
    public async Task PublishEntityUpdatedAsync_SendsToInstanceGroup()
    {
        // Arrange
        var entityType = "Product";
        var entityId = Guid.NewGuid();
        var tenantId = "tenant-1";

        _mockClients
            .Setup(c => c.Group($"entity_list_{entityType}_{tenantId}"))
            .Returns(_mockCollectionGroup.Object);
        _mockClients
            .Setup(c => c.Group($"entity_{entityType}_{entityId}_{tenantId}"))
            .Returns(_mockInstanceGroup.Object);

        // Act
        await _sut.PublishEntityUpdatedAsync(entityType, entityId, EntityOperation.Updated, tenantId);

        // Assert
        _mockInstanceGroup.Verify(
            c => c.EntityUpdated(It.Is<EntityUpdateSignal>(s =>
                s.EntityType == entityType &&
                s.EntityId == entityId.ToString() &&
                s.Operation == EntityOperation.Updated)),
            Times.Once);
    }

    [Fact]
    public async Task PublishEntityUpdatedAsync_DoesNotThrow_WhenHubThrows()
    {
        // Arrange
        var entityType = "Order";
        var entityId = Guid.NewGuid();
        var tenantId = "tenant-1";

        _mockClients
            .Setup(c => c.Group($"entity_list_{entityType}_{tenantId}"))
            .Returns(_mockCollectionGroup.Object);

        _mockCollectionGroup
            .Setup(c => c.EntityCollectionUpdated(It.IsAny<EntityUpdateSignal>()))
            .ThrowsAsync(new InvalidOperationException("Hub connection lost"));

        // Act
        var act = () => _sut.PublishEntityUpdatedAsync(entityType, entityId, EntityOperation.Deleted, tenantId);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishEntityUpdatedAsync_LogsError_WhenHubThrows()
    {
        // Arrange
        var entityType = "Order";
        var entityId = Guid.NewGuid();
        var tenantId = "tenant-1";

        _mockClients
            .Setup(c => c.Group($"entity_list_{entityType}_{tenantId}"))
            .Returns(_mockCollectionGroup.Object);

        _mockCollectionGroup
            .Setup(c => c.EntityCollectionUpdated(It.IsAny<EntityUpdateSignal>()))
            .ThrowsAsync(new InvalidOperationException("Hub connection lost"));

        // Act
        await _sut.PublishEntityUpdatedAsync(entityType, entityId, EntityOperation.Deleted, tenantId);

        // Assert
        _mockLogger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(entityType)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("Product")]
    [InlineData("Order")]
    [InlineData("Customer")]
    public async Task PublishEntityUpdatedAsync_SignalHasCorrectEntityType(string entityType)
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var tenantId = "tenant-1";

        _mockClients
            .Setup(c => c.Group(It.IsAny<string>()))
            .Returns(_mockCollectionGroup.Object);

        // Act
        await _sut.PublishEntityUpdatedAsync(entityType, entityId, EntityOperation.Created, tenantId);

        // Assert
        _mockCollectionGroup.Verify(
            c => c.EntityCollectionUpdated(It.Is<EntityUpdateSignal>(s => s.EntityType == entityType)),
            Times.Once);
    }

    [Theory]
    [InlineData(EntityOperation.Created)]
    [InlineData(EntityOperation.Updated)]
    [InlineData(EntityOperation.Deleted)]
    public async Task PublishEntityUpdatedAsync_SignalHasCorrectOperation(EntityOperation operation)
    {
        // Arrange
        var entityType = "Product";
        var entityId = Guid.NewGuid();
        var tenantId = "tenant-1";

        _mockClients
            .Setup(c => c.Group(It.IsAny<string>()))
            .Returns(_mockCollectionGroup.Object);

        // Act
        await _sut.PublishEntityUpdatedAsync(entityType, entityId, operation, tenantId);

        // Assert
        _mockCollectionGroup.Verify(
            c => c.EntityCollectionUpdated(It.Is<EntityUpdateSignal>(s => s.Operation == operation)),
            Times.Once);
    }

    [Fact]
    public async Task PublishEntityUpdatedAsync_SignalHasEntityIdAsString()
    {
        // Arrange
        var entityType = "Product";
        var entityId = Guid.NewGuid();
        var tenantId = "tenant-1";

        _mockClients
            .Setup(c => c.Group(It.IsAny<string>()))
            .Returns(_mockCollectionGroup.Object);

        // Act
        await _sut.PublishEntityUpdatedAsync(entityType, entityId, EntityOperation.Updated, tenantId);

        // Assert
        _mockCollectionGroup.Verify(
            c => c.EntityCollectionUpdated(It.Is<EntityUpdateSignal>(s => s.EntityId == entityId.ToString())),
            Times.Once);
    }

    [Fact]
    public async Task PublishEntityUpdatedAsync_SignalHasUpdatedAtTimestamp()
    {
        // Arrange
        var entityType = "Product";
        var entityId = Guid.NewGuid();
        var tenantId = "tenant-1";
        var before = DateTimeOffset.UtcNow;

        _mockClients
            .Setup(c => c.Group(It.IsAny<string>()))
            .Returns(_mockCollectionGroup.Object);

        // Act
        await _sut.PublishEntityUpdatedAsync(entityType, entityId, EntityOperation.Created, tenantId);

        // Assert
        var after = DateTimeOffset.UtcNow;
        _mockCollectionGroup.Verify(
            c => c.EntityCollectionUpdated(It.Is<EntityUpdateSignal>(s =>
                s.UpdatedAt >= before && s.UpdatedAt <= after)),
            Times.Once);
    }

    #endregion

    #region Service Implementation Tests

    [Fact]
    public void EntityUpdateHubContext_ShouldImplementIScopedService()
    {
        // Assert
        _sut.Should().BeAssignableTo<IScopedService>();
    }

    [Fact]
    public void EntityUpdateHubContext_ShouldImplementIEntityUpdateHubContext()
    {
        // Assert
        _sut.Should().BeAssignableTo<IEntityUpdateHubContext>();
    }

    #endregion
}

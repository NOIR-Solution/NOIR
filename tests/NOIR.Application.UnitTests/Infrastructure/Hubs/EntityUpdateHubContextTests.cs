namespace NOIR.Application.UnitTests.Infrastructure.Hubs;

using Microsoft.AspNetCore.SignalR;
using NOIR.Infrastructure.Hubs;

/// <summary>
/// Unit tests for EntityUpdateHubContext.
/// Verifies signals are published to the correct SignalR groups.
/// </summary>
public class EntityUpdateHubContextTests
{
    private readonly Mock<IHubContext<NotificationHub, INotificationClient>> _hubContextMock;
    private readonly Mock<IHubClients<INotificationClient>> _clientsMock;
    private readonly Mock<INotificationClient> _clientGroupMock;
    private readonly Mock<ILogger<EntityUpdateHubContext>> _loggerMock;
    private readonly EntityUpdateHubContext _sut;

    private const string EntityType = "Product";
    private const string TenantId = "tenant-abc";

    public EntityUpdateHubContextTests()
    {
        _hubContextMock = new Mock<IHubContext<NotificationHub, INotificationClient>>();
        _clientsMock = new Mock<IHubClients<INotificationClient>>();
        _clientGroupMock = new Mock<INotificationClient>();
        _loggerMock = new Mock<ILogger<EntityUpdateHubContext>>();

        _hubContextMock.Setup(x => x.Clients).Returns(_clientsMock.Object);
        _clientsMock.Setup(x => x.Group(It.IsAny<string>())).Returns(_clientGroupMock.Object);
        _clientGroupMock.Setup(x => x.EntityCollectionUpdated(It.IsAny<EntityUpdateSignal>())).Returns(Task.CompletedTask);
        _clientGroupMock.Setup(x => x.EntityUpdated(It.IsAny<EntityUpdateSignal>())).Returns(Task.CompletedTask);

        _sut = new EntityUpdateHubContext(_hubContextMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task PublishEntityUpdatedAsync_SendsEntityCollectionUpdated_ToListGroup()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var expectedGroup = $"entity_list_{EntityType}_{TenantId}";

        // Act
        await _sut.PublishEntityUpdatedAsync(EntityType, entityId, EntityOperation.Updated, TenantId);

        // Assert
        _clientsMock.Verify(x => x.Group(expectedGroup), Times.Once);
        _clientGroupMock.Verify(x => x.EntityCollectionUpdated(It.IsAny<EntityUpdateSignal>()), Times.Once);
    }

    [Fact]
    public async Task PublishEntityUpdatedAsync_SendsEntityUpdated_ToInstanceGroup()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var expectedGroup = $"entity_{EntityType}_{entityId}_{TenantId}";

        // Act
        await _sut.PublishEntityUpdatedAsync(EntityType, entityId, EntityOperation.Updated, TenantId);

        // Assert
        _clientsMock.Verify(x => x.Group(expectedGroup), Times.Once);
        _clientGroupMock.Verify(x => x.EntityUpdated(It.IsAny<EntityUpdateSignal>()), Times.Once);
    }

    [Fact]
    public async Task PublishEntityUpdatedAsync_Signal_HasCorrectEntityType()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        EntityUpdateSignal? capturedSignal = null;
        _clientGroupMock
            .Setup(x => x.EntityUpdated(It.IsAny<EntityUpdateSignal>()))
            .Callback<EntityUpdateSignal>(s => capturedSignal = s)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.PublishEntityUpdatedAsync(EntityType, entityId, EntityOperation.Created, TenantId);

        // Assert
        capturedSignal.Should().NotBeNull();
        capturedSignal!.EntityType.Should().Be(EntityType);
        capturedSignal.EntityId.Should().Be(entityId.ToString());
        capturedSignal.Operation.Should().Be(EntityOperation.Created);
    }

    [Theory]
    [InlineData(EntityOperation.Created)]
    [InlineData(EntityOperation.Updated)]
    [InlineData(EntityOperation.Deleted)]
    public async Task PublishEntityUpdatedAsync_MapsAllOperations_Correctly(EntityOperation operation)
    {
        // Arrange
        var entityId = Guid.NewGuid();
        EntityUpdateSignal? capturedSignal = null;
        _clientGroupMock
            .Setup(x => x.EntityCollectionUpdated(It.IsAny<EntityUpdateSignal>()))
            .Callback<EntityUpdateSignal>(s => capturedSignal = s)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.PublishEntityUpdatedAsync(EntityType, entityId, operation, TenantId);

        // Assert
        capturedSignal!.Operation.Should().Be(operation);
    }

    [Fact]
    public async Task PublishEntityUpdatedAsync_DoesNotThrow_WhenHubThrows()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        _clientGroupMock
            .Setup(x => x.EntityCollectionUpdated(It.IsAny<EntityUpdateSignal>()))
            .ThrowsAsync(new InvalidOperationException("SignalR unavailable"));

        // Act — should not throw (exception is caught and logged)
        var act = async () => await _sut.PublishEntityUpdatedAsync(EntityType, entityId, EntityOperation.Updated, TenantId);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishEntityUpdatedAsync_Signal_UpdatedAt_IsRecent()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        EntityUpdateSignal? capturedSignal = null;
        _clientGroupMock
            .Setup(x => x.EntityUpdated(It.IsAny<EntityUpdateSignal>()))
            .Callback<EntityUpdateSignal>(s => capturedSignal = s)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.PublishEntityUpdatedAsync(EntityType, entityId, EntityOperation.Updated, TenantId);

        // Assert
        capturedSignal!.UpdatedAt.Should().BeAfter(before);
        capturedSignal.UpdatedAt.Should().BeBefore(DateTimeOffset.UtcNow.AddSeconds(1));
    }
}

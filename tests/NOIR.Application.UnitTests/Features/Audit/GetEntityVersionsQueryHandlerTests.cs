using NOIR.Application.Features.Audit.DTOs;
using NOIR.Application.Features.Audit.Queries.GetEntityVersions;

namespace NOIR.Application.UnitTests.Features.Audit;

/// <summary>
/// Unit tests for GetEntityVersionsQueryHandler.
/// Tests entity version retrieval scenarios for version comparison.
/// </summary>
public class GetEntityVersionsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IAuditLogQueryService> _auditLogQueryServiceMock;
    private readonly GetEntityVersionsQueryHandler _handler;

    public GetEntityVersionsQueryHandlerTests()
    {
        _auditLogQueryServiceMock = new Mock<IAuditLogQueryService>();
        _handler = new GetEntityVersionsQueryHandler(_auditLogQueryServiceMock.Object);
    }

    private static EntityVersionDto CreateTestVersion(
        int version,
        string operation = "Update",
        string? userId = null)
    {
        return new EntityVersionDto(
            Version: version,
            Timestamp: DateTimeOffset.UtcNow.AddHours(-version),
            Operation: operation,
            UserId: userId ?? Guid.NewGuid().ToString(),
            UserEmail: "admin@noir.local",
            State: new Dictionary<string, object?>
            {
                { "Name", $"Name at version {version}" },
                { "Email", $"email{version}@example.com" },
                { "IsActive", true }
            });
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidEntityTypeAndId_ShouldReturnVersions()
    {
        // Arrange
        var versions = new List<EntityVersionDto>
        {
            CreateTestVersion(1, "Create"),
            CreateTestVersion(2, "Update"),
            CreateTestVersion(3, "Update")
        };

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityVersionsAsync("User", "123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(versions);

        var query = new GetEntityVersionsQuery("User", "123");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Select(v => v.Version).Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task Handle_ShouldReturnVersionsWithAllFields()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var versions = new List<EntityVersionDto>
        {
            CreateTestVersion(1, "Create", userId)
        };

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityVersionsAsync("Tenant", "456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(versions);

        var query = new GetEntityVersionsQuery("Tenant", "456");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var version = result.Value[0];
        version.Version.Should().Be(1);
        version.Operation.Should().Be("Create");
        version.UserId.Should().Be(userId);
        version.UserEmail.Should().Be("admin@noir.local");
        version.State.Should().ContainKey("Name");
        version.State.Should().ContainKey("Email");
        version.State.Should().ContainKey("IsActive");
    }

    [Fact]
    public async Task Handle_ShouldReturnVersionsInOrder()
    {
        // Arrange
        var versions = new List<EntityVersionDto>
        {
            CreateTestVersion(1, "Create"),
            CreateTestVersion(2, "Update"),
            CreateTestVersion(3, "Update"),
            CreateTestVersion(4, "Update"),
            CreateTestVersion(5, "Update")
        };

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityVersionsAsync("Post", "789", It.IsAny<CancellationToken>()))
            .ReturnsAsync(versions);

        var query = new GetEntityVersionsQuery("Post", "789");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(5);
        result.Value.Select(v => v.Version).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Handle_WithSingleVersion_ShouldReturnSingleItem()
    {
        // Arrange
        var versions = new List<EntityVersionDto>
        {
            CreateTestVersion(1, "Create")
        };

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityVersionsAsync("Role", "admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(versions);

        var query = new GetEntityVersionsQuery("Role", "admin");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Version.Should().Be(1);
        result.Value[0].Operation.Should().Be("Create");
    }

    [Fact]
    public async Task Handle_WithManyVersions_ShouldReturnAllVersions()
    {
        // Arrange
        var versions = Enumerable.Range(1, 50)
            .Select(i => CreateTestVersion(i, i == 1 ? "Create" : "Update"))
            .ToList();

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityVersionsAsync("User", "active-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(versions);

        var query = new GetEntityVersionsQuery("User", "active-user");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(50);
    }

    [Fact]
    public async Task Handle_WithDifferentOperations_ShouldReturnAllOperationTypes()
    {
        // Arrange
        var versions = new List<EntityVersionDto>
        {
            CreateTestVersion(1, "Create"),
            CreateTestVersion(2, "Update"),
            CreateTestVersion(3, "Update"),
            new EntityVersionDto(
                Version: 4,
                Timestamp: DateTimeOffset.UtcNow,
                Operation: "Delete",
                UserId: Guid.NewGuid().ToString(),
                UserEmail: "admin@noir.local",
                State: new Dictionary<string, object?>())
        };

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityVersionsAsync("Category", "cat-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(versions);

        var query = new GetEntityVersionsQuery("Category", "cat-1");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(4);
        result.Value.Select(v => v.Operation).Should().Contain("Create");
        result.Value.Select(v => v.Operation).Should().Contain("Update");
        result.Value.Select(v => v.Operation).Should().Contain("Delete");
    }

    #endregion

    #region Empty Results Scenarios

    [Fact]
    public async Task Handle_WithNoVersions_ShouldReturnEmptyList()
    {
        // Arrange
        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityVersionsAsync("User", "nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EntityVersionDto>());

        var query = new GetEntityVersionsQuery("User", "nonexistent");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithUnknownEntityType_ShouldReturnEmptyList()
    {
        // Arrange
        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityVersionsAsync("UnknownEntity", "123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EntityVersionDto>());

        var query = new GetEntityVersionsQuery("UnknownEntity", "123");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region CancellationToken Scenarios

    [Fact]
    public async Task Handle_ShouldPassCancellationTokenToService()
    {
        // Arrange
        var versions = new List<EntityVersionDto>
        {
            CreateTestVersion(1, "Create")
        };
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityVersionsAsync("User", "123", token))
            .ReturnsAsync(versions);

        var query = new GetEntityVersionsQuery("User", "123");

        // Act
        await _handler.Handle(query, token);

        // Assert
        _auditLogQueryServiceMock.Verify(
            x => x.GetEntityVersionsAsync("User", "123", token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityVersionsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var query = new GetEntityVersionsQuery("User", "123");

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _handler.Handle(query, cts.Token));
    }

    #endregion

    #region Service Call Verification

    [Fact]
    public async Task Handle_ShouldCallServiceWithCorrectParameters()
    {
        // Arrange
        var versions = new List<EntityVersionDto>
        {
            CreateTestVersion(1, "Create")
        };

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityVersionsAsync("Tenant", "tenant-456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(versions);

        var query = new GetEntityVersionsQuery("Tenant", "tenant-456");

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _auditLogQueryServiceMock.Verify(
            x => x.GetEntityVersionsAsync("Tenant", "tenant-456", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCallServiceExactlyOnce()
    {
        // Arrange
        var versions = new List<EntityVersionDto>();

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityVersionsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(versions);

        var query = new GetEntityVersionsQuery("User", "123");

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _auditLogQueryServiceMock.Verify(
            x => x.GetEntityVersionsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region State Dictionary Tests

    [Fact]
    public async Task Handle_ShouldReturnVersionsWithCorrectState()
    {
        // Arrange
        var state = new Dictionary<string, object?>
        {
            { "Id", Guid.NewGuid().ToString() },
            { "Name", "Test User" },
            { "Email", "test@example.com" },
            { "IsActive", true },
            { "CreatedAt", DateTimeOffset.UtcNow.ToString("O") },
            { "Roles", new[] { "Admin", "User" } }
        };

        var versions = new List<EntityVersionDto>
        {
            new EntityVersionDto(
                Version: 1,
                Timestamp: DateTimeOffset.UtcNow,
                Operation: "Create",
                UserId: Guid.NewGuid().ToString(),
                UserEmail: "admin@noir.local",
                State: state)
        };

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityVersionsAsync("User", "complex-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(versions);

        var query = new GetEntityVersionsQuery("User", "complex-user");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var versionState = result.Value[0].State;
        versionState.Should().ContainKey("Id");
        versionState.Should().ContainKey("Name");
        versionState.Should().ContainKey("Email");
        versionState.Should().ContainKey("IsActive");
        versionState.Should().ContainKey("CreatedAt");
        versionState.Should().ContainKey("Roles");
        versionState["Name"].Should().Be("Test User");
        versionState["IsActive"].Should().Be(true);
    }

    [Fact]
    public async Task Handle_WithNullValuesInState_ShouldHandleCorrectly()
    {
        // Arrange
        var state = new Dictionary<string, object?>
        {
            { "Name", "Test User" },
            { "MiddleName", null },
            { "DeletedAt", null }
        };

        var versions = new List<EntityVersionDto>
        {
            new EntityVersionDto(
                Version: 1,
                Timestamp: DateTimeOffset.UtcNow,
                Operation: "Create",
                UserId: Guid.NewGuid().ToString(),
                UserEmail: "admin@noir.local",
                State: state)
        };

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityVersionsAsync("User", "null-values-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(versions);

        var query = new GetEntityVersionsQuery("User", "null-values-user");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var versionState = result.Value[0].State;
        versionState["MiddleName"].Should().BeNull();
        versionState["DeletedAt"].Should().BeNull();
    }

    #endregion
}

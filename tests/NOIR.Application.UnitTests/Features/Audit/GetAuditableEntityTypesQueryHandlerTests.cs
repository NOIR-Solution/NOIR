using NOIR.Application.Features.Audit.Queries.GetAuditableEntityTypes;

namespace NOIR.Application.UnitTests.Features.Audit;

/// <summary>
/// Unit tests for GetAuditableEntityTypesQueryHandler.
/// Tests entity type list retrieval scenarios.
/// </summary>
public class GetAuditableEntityTypesQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IAuditLogQueryService> _auditLogQueryServiceMock;
    private readonly GetAuditableEntityTypesQueryHandler _handler;

    public GetAuditableEntityTypesQueryHandlerTests()
    {
        _auditLogQueryServiceMock = new Mock<IAuditLogQueryService>();
        _handler = new GetAuditableEntityTypesQueryHandler(_auditLogQueryServiceMock.Object);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_ShouldReturnEntityTypes()
    {
        // Arrange
        var entityTypes = new List<string> { "User", "Tenant", "Role", "Permission" };

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityTypesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entityTypes);

        var query = new GetAuditableEntityTypesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(4);
        result.Value.Should().Contain("User");
        result.Value.Should().Contain("Tenant");
        result.Value.Should().Contain("Role");
        result.Value.Should().Contain("Permission");
    }

    [Fact]
    public async Task Handle_WithSingleEntityType_ShouldReturnSingleItem()
    {
        // Arrange
        var entityTypes = new List<string> { "User" };

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityTypesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entityTypes);

        var query = new GetAuditableEntityTypesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.Should().ContainSingle().Which.Should().Be("User");
    }

    [Fact]
    public async Task Handle_ShouldReturnDistinctEntityTypes()
    {
        // Arrange
        var entityTypes = new List<string>
        {
            "ApplicationUser",
            "Tenant",
            "ApplicationRole",
            "NotificationPreference",
            "Post",
            "Category",
            "Tag"
        };

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityTypesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entityTypes);

        var query = new GetAuditableEntityTypesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(7);
        result.Value.Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region Empty Results Scenarios

    [Fact]
    public async Task Handle_WithNoEntityTypes_ShouldReturnEmptyList()
    {
        // Arrange
        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityTypesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var query = new GetAuditableEntityTypesQuery();

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
        var entityTypes = new List<string> { "User", "Tenant" };
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityTypesAsync(token))
            .ReturnsAsync(entityTypes);

        var query = new GetAuditableEntityTypesQuery();

        // Act
        await _handler.Handle(query, token);

        // Assert
        _auditLogQueryServiceMock.Verify(
            x => x.GetEntityTypesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityTypesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var query = new GetAuditableEntityTypesQuery();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _handler.Handle(query, cts.Token));
    }

    #endregion

    #region Service Call Verification

    [Fact]
    public async Task Handle_ShouldCallServiceExactlyOnce()
    {
        // Arrange
        var entityTypes = new List<string> { "User" };

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityTypesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entityTypes);

        var query = new GetAuditableEntityTypesQuery();

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _auditLogQueryServiceMock.Verify(
            x => x.GetEntityTypesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}

using NOIR.Application.Features.Audit.DTOs;
using NOIR.Application.Features.Audit.Queries.GetEntityHistory;

namespace NOIR.Application.UnitTests.Features.Audit;

/// <summary>
/// Unit tests for GetEntityHistoryQueryHandler.
/// Tests entity history retrieval scenarios with filtering and pagination.
/// </summary>
public class GetEntityHistoryQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IAuditLogQueryService> _auditLogQueryServiceMock;
    private readonly GetEntityHistoryQueryHandler _handler;

    public GetEntityHistoryQueryHandlerTests()
    {
        _auditLogQueryServiceMock = new Mock<IAuditLogQueryService>();
        _handler = new GetEntityHistoryQueryHandler(_auditLogQueryServiceMock.Object);
    }

    private static EntityHistoryEntryDto CreateTestHistoryEntry(
        string operation = "Update",
        int version = 1,
        string? userId = null)
    {
        return new EntityHistoryEntryDto(
            Id: Guid.NewGuid(),
            Timestamp: DateTimeOffset.UtcNow,
            Operation: operation,
            UserId: userId ?? Guid.NewGuid().ToString(),
            UserEmail: "admin@noir.local",
            HandlerName: "UpdateUserCommandHandler",
            CorrelationId: Guid.NewGuid().ToString(),
            Changes: new List<FieldChangeDto>
            {
                new FieldChangeDto("Name", "Old Name", "New Name", ChangeOperation.Modified)
            },
            Version: version);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidEntityTypeAndId_ShouldReturnHistory()
    {
        // Arrange
        var entries = new List<EntityHistoryEntryDto>
        {
            CreateTestHistoryEntry(operation: "Create", version: 1),
            CreateTestHistoryEntry(operation: "Update", version: 2),
            CreateTestHistoryEntry(operation: "Update", version: 3)
        };

        var pagedResult = PagedResult<EntityHistoryEntryDto>.Create(entries, 3, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityHistoryAsync(
                "User",
                "123",
                null,
                null,
                null,
                1,
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new GetEntityHistoryQuery("User", "123");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ShouldReturnHistoryWithAllFields()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var entry = CreateTestHistoryEntry(operation: "Update", version: 5, userId: userId);
        var pagedResult = PagedResult<EntityHistoryEntryDto>.Create(new List<EntityHistoryEntryDto> { entry }, 1, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityHistoryAsync(
                "Tenant",
                "456",
                null,
                null,
                null,
                1,
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new GetEntityHistoryQuery("Tenant", "456");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var historyEntry = result.Value.Items[0];
        historyEntry.Operation.Should().Be("Update");
        historyEntry.Version.Should().Be(5);
        historyEntry.UserId.Should().Be(userId);
        historyEntry.UserEmail.Should().Be("admin@noir.local");
        historyEntry.HandlerName.Should().Be("UpdateUserCommandHandler");
        historyEntry.Changes.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithVersionedChanges_ShouldReturnCorrectVersions()
    {
        // Arrange
        var entries = new List<EntityHistoryEntryDto>
        {
            CreateTestHistoryEntry(operation: "Create", version: 1),
            CreateTestHistoryEntry(operation: "Update", version: 2),
            CreateTestHistoryEntry(operation: "Update", version: 3),
            CreateTestHistoryEntry(operation: "Update", version: 4),
            CreateTestHistoryEntry(operation: "Delete", version: 5)
        };

        var pagedResult = PagedResult<EntityHistoryEntryDto>.Create(entries, 5, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityHistoryAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                null,
                null,
                null,
                1,
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new GetEntityHistoryQuery("Post", "789");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.Items.Select(e => e.Version).Should().BeInAscendingOrder();
    }

    #endregion

    #region Empty Results Scenarios

    [Fact]
    public async Task Handle_WithNoHistory_ShouldReturnEmptyResult()
    {
        // Arrange
        var pagedResult = PagedResult<EntityHistoryEntryDto>.Empty(0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityHistoryAsync(
                "User",
                "nonexistent",
                null,
                null,
                null,
                1,
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new GetEntityHistoryQuery("User", "nonexistent");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    #endregion

    #region Date Range Filtering

    [Fact]
    public async Task Handle_WithFromDateFilter_ShouldPassFilterToService()
    {
        // Arrange
        var fromDate = DateTimeOffset.UtcNow.AddDays(-7);
        var entries = new List<EntityHistoryEntryDto>
        {
            CreateTestHistoryEntry(operation: "Update", version: 3)
        };
        var pagedResult = PagedResult<EntityHistoryEntryDto>.Create(entries, 1, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityHistoryAsync(
                "User",
                "123",
                fromDate,
                null,
                null,
                1,
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new GetEntityHistoryQuery("User", "123", FromDate: fromDate);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _auditLogQueryServiceMock.Verify(
            x => x.GetEntityHistoryAsync(
                "User",
                "123",
                fromDate,
                null,
                null,
                1,
                20,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithToDateFilter_ShouldPassFilterToService()
    {
        // Arrange
        var toDate = DateTimeOffset.UtcNow;
        var entries = new List<EntityHistoryEntryDto>
        {
            CreateTestHistoryEntry(operation: "Update", version: 2)
        };
        var pagedResult = PagedResult<EntityHistoryEntryDto>.Create(entries, 1, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityHistoryAsync(
                "Tenant",
                "456",
                null,
                toDate,
                null,
                1,
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new GetEntityHistoryQuery("Tenant", "456", ToDate: toDate);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _auditLogQueryServiceMock.Verify(
            x => x.GetEntityHistoryAsync(
                "Tenant",
                "456",
                null,
                toDate,
                null,
                1,
                20,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithDateRangeFilter_ShouldPassBothDatesToService()
    {
        // Arrange
        var fromDate = DateTimeOffset.UtcNow.AddDays(-30);
        var toDate = DateTimeOffset.UtcNow.AddDays(-1);
        var entries = new List<EntityHistoryEntryDto>
        {
            CreateTestHistoryEntry(operation: "Update", version: 2)
        };
        var pagedResult = PagedResult<EntityHistoryEntryDto>.Create(entries, 1, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityHistoryAsync(
                "Role",
                "789",
                fromDate,
                toDate,
                null,
                1,
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new GetEntityHistoryQuery("Role", "789", FromDate: fromDate, ToDate: toDate);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _auditLogQueryServiceMock.Verify(
            x => x.GetEntityHistoryAsync(
                "Role",
                "789",
                fromDate,
                toDate,
                null,
                1,
                20,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region User Filter

    [Fact]
    public async Task Handle_WithUserIdFilter_ShouldPassFilterToService()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var entries = new List<EntityHistoryEntryDto>
        {
            CreateTestHistoryEntry(operation: "Update", version: 2, userId: userId)
        };
        var pagedResult = PagedResult<EntityHistoryEntryDto>.Create(entries, 1, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityHistoryAsync(
                "User",
                "123",
                null,
                null,
                userId,
                1,
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new GetEntityHistoryQuery("User", "123", UserId: userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _auditLogQueryServiceMock.Verify(
            x => x.GetEntityHistoryAsync(
                "User",
                "123",
                null,
                null,
                userId,
                1,
                20,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Pagination Scenarios

    [Fact]
    public async Task Handle_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var entries = new List<EntityHistoryEntryDto>
        {
            CreateTestHistoryEntry(operation: "Update", version: 11),
            CreateTestHistoryEntry(operation: "Update", version: 12)
        };
        var pagedResult = PagedResult<EntityHistoryEntryDto>.Create(entries, 25, 1, 10);

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityHistoryAsync(
                "User",
                "123",
                null,
                null,
                null,
                2,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new GetEntityHistoryQuery("User", "123", Page: 2, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PageNumber.Should().Be(2);
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalCount.Should().Be(25);
        result.Value.TotalPages.Should().Be(3);
        result.Value.HasPreviousPage.Should().BeTrue();
        result.Value.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithFirstPage_ShouldNotHavePreviousPage()
    {
        // Arrange
        var entries = new List<EntityHistoryEntryDto>
        {
            CreateTestHistoryEntry(operation: "Create", version: 1)
        };
        var pagedResult = PagedResult<EntityHistoryEntryDto>.Create(entries, 25, 0, 10);

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityHistoryAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                null,
                null,
                null,
                1,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new GetEntityHistoryQuery("User", "123", Page: 1, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasPreviousPage.Should().BeFalse();
        result.Value.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithLastPage_ShouldNotHaveNextPage()
    {
        // Arrange
        var entries = new List<EntityHistoryEntryDto>
        {
            CreateTestHistoryEntry(operation: "Update", version: 25)
        };
        var pagedResult = PagedResult<EntityHistoryEntryDto>.Create(entries, 25, 2, 10);

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityHistoryAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                null,
                null,
                null,
                3,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new GetEntityHistoryQuery("User", "123", Page: 3, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasPreviousPage.Should().BeTrue();
        result.Value.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithCustomPageSize_ShouldRespectPageSize()
    {
        // Arrange
        var entries = Enumerable.Range(1, 5)
            .Select(i => CreateTestHistoryEntry(operation: "Update", version: i))
            .ToList();
        var pagedResult = PagedResult<EntityHistoryEntryDto>.Create(entries, 50, 0, 5);

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityHistoryAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                null,
                null,
                null,
                1,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new GetEntityHistoryQuery("User", "123", Page: 1, PageSize: 5);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.PageSize.Should().Be(5);
        result.Value.TotalPages.Should().Be(10);
    }

    #endregion

    #region CancellationToken Scenarios

    [Fact]
    public async Task Handle_ShouldPassCancellationTokenToService()
    {
        // Arrange
        var pagedResult = PagedResult<EntityHistoryEntryDto>.Empty(0, 20);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityHistoryAsync(
                "User",
                "123",
                null,
                null,
                null,
                1,
                20,
                token))
            .ReturnsAsync(pagedResult);

        var query = new GetEntityHistoryQuery("User", "123");

        // Act
        await _handler.Handle(query, token);

        // Assert
        _auditLogQueryServiceMock.Verify(
            x => x.GetEntityHistoryAsync(
                "User",
                "123",
                null,
                null,
                null,
                1,
                20,
                token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityHistoryAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var query = new GetEntityHistoryQuery("User", "123");

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _handler.Handle(query, cts.Token));
    }

    #endregion

    #region Combined Filters

    [Fact]
    public async Task Handle_WithAllFilters_ShouldPassAllFiltersToService()
    {
        // Arrange
        var fromDate = DateTimeOffset.UtcNow.AddDays(-30);
        var toDate = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid().ToString();
        var entries = new List<EntityHistoryEntryDto>
        {
            CreateTestHistoryEntry(operation: "Update", version: 2, userId: userId)
        };
        var pagedResult = PagedResult<EntityHistoryEntryDto>.Create(entries, 1, 0, 15);

        _auditLogQueryServiceMock
            .Setup(x => x.GetEntityHistoryAsync(
                "Tenant",
                "tenant-123",
                fromDate,
                toDate,
                userId,
                1,
                15,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new GetEntityHistoryQuery(
            "Tenant",
            "tenant-123",
            FromDate: fromDate,
            ToDate: toDate,
            UserId: userId,
            Page: 1,
            PageSize: 15);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _auditLogQueryServiceMock.Verify(
            x => x.GetEntityHistoryAsync(
                "Tenant",
                "tenant-123",
                fromDate,
                toDate,
                userId,
                1,
                15,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}

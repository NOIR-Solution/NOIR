using NOIR.Application.Features.Audit.DTOs;
using NOIR.Application.Features.Audit.Queries.SearchActivityTimeline;

namespace NOIR.Application.UnitTests.Features.Audit;

/// <summary>
/// Unit tests for SearchActivityTimelineQueryHandler.
/// Tests activity timeline search scenarios with filtering and pagination.
/// </summary>
public class SearchActivityTimelineQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IAuditLogQueryService> _auditLogQueryServiceMock;
    private readonly SearchActivityTimelineQueryHandler _handler;

    public SearchActivityTimelineQueryHandlerTests()
    {
        _auditLogQueryServiceMock = new Mock<IAuditLogQueryService>();
        _handler = new SearchActivityTimelineQueryHandler(_auditLogQueryServiceMock.Object);
    }

    private static ActivityTimelineEntryDto CreateTestTimelineEntry(
        string displayContext = "Users",
        string operationType = "Update",
        bool isSuccess = true,
        string? userId = null,
        string? targetId = null,
        string? correlationId = null)
    {
        return new ActivityTimelineEntryDto(
            Id: Guid.NewGuid(),
            Timestamp: DateTimeOffset.UtcNow,
            UserEmail: "admin@noir.local",
            UserId: userId ?? Guid.NewGuid().ToString(),
            DisplayContext: displayContext,
            OperationType: operationType,
            ActionDescription: $"{operationType}d user John Doe",
            TargetDisplayName: "John Doe",
            TargetDtoType: "UserDto",
            TargetDtoId: targetId ?? Guid.NewGuid().ToString(),
            IsSuccess: isSuccess,
            DurationMs: 150,
            EntityChangeCount: 2,
            CorrelationId: correlationId ?? Guid.NewGuid().ToString(),
            HandlerName: $"{operationType}UserCommandHandler");
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithNoFilters_ShouldReturnAllActivities()
    {
        // Arrange
        var entries = new List<ActivityTimelineEntryDto>
        {
            CreateTestTimelineEntry(displayContext: "Users", operationType: "Create"),
            CreateTestTimelineEntry(displayContext: "Tenants", operationType: "Update"),
            CreateTestTimelineEntry(displayContext: "Roles", operationType: "Delete")
        };
        var pagedResult = PagedResult<ActivityTimelineEntryDto>.Create(entries, 3, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchActivityTimelineAsync(
                null, null, null, null, null, null, null, null, null,
                1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchActivityTimelineQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ShouldReturnEntriesWithAllFields()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var targetId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var entry = CreateTestTimelineEntry(
            displayContext: "Users",
            operationType: "Update",
            isSuccess: true,
            userId: userId,
            targetId: targetId,
            correlationId: correlationId);
        var pagedResult = PagedResult<ActivityTimelineEntryDto>.Create(new List<ActivityTimelineEntryDto> { entry }, 1, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchActivityTimelineAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(),
                It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchActivityTimelineQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var timelineEntry = result.Value.Items[0];
        timelineEntry.DisplayContext.Should().Be("Users");
        timelineEntry.OperationType.Should().Be("Update");
        timelineEntry.UserId.Should().Be(userId);
        timelineEntry.TargetDtoId.Should().Be(targetId);
        timelineEntry.CorrelationId.Should().Be(correlationId);
        timelineEntry.IsSuccess.Should().BeTrue();
        timelineEntry.UserEmail.Should().Be("admin@noir.local");
        timelineEntry.DurationMs.Should().Be(150);
        timelineEntry.EntityChangeCount.Should().Be(2);
    }

    #endregion

    #region Empty Results Scenarios

    [Fact]
    public async Task Handle_WithNoActivities_ShouldReturnEmptyResult()
    {
        // Arrange
        var pagedResult = PagedResult<ActivityTimelineEntryDto>.Empty(0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchActivityTimelineAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(),
                It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchActivityTimelineQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    #endregion

    #region PageContext Filter

    [Fact]
    public async Task Handle_WithPageContextFilter_ShouldPassFilterToService()
    {
        // Arrange
        var entries = new List<ActivityTimelineEntryDto>
        {
            CreateTestTimelineEntry(displayContext: "Users", operationType: "Create"),
            CreateTestTimelineEntry(displayContext: "Users", operationType: "Update")
        };
        var pagedResult = PagedResult<ActivityTimelineEntryDto>.Create(entries, 2, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchActivityTimelineAsync(
                "Users", null, null, null, null, null, null, null, null,
                1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchActivityTimelineQuery(PageContext: "Users");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        _auditLogQueryServiceMock.Verify(
            x => x.SearchActivityTimelineAsync(
                "Users", null, null, null, null, null, null, null, null,
                1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region OperationType Filter

    [Fact]
    public async Task Handle_WithOperationTypeFilter_ShouldPassFilterToService()
    {
        // Arrange
        var entries = new List<ActivityTimelineEntryDto>
        {
            CreateTestTimelineEntry(displayContext: "Users", operationType: "Create"),
            CreateTestTimelineEntry(displayContext: "Tenants", operationType: "Create")
        };
        var pagedResult = PagedResult<ActivityTimelineEntryDto>.Create(entries, 2, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchActivityTimelineAsync(
                null, "Create", null, null, null, null, null, null, null,
                1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchActivityTimelineQuery(OperationType: "Create");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _auditLogQueryServiceMock.Verify(
            x => x.SearchActivityTimelineAsync(
                null, "Create", null, null, null, null, null, null, null,
                1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region UserId Filter

    [Fact]
    public async Task Handle_WithUserIdFilter_ShouldPassFilterToService()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var entries = new List<ActivityTimelineEntryDto>
        {
            CreateTestTimelineEntry(userId: userId)
        };
        var pagedResult = PagedResult<ActivityTimelineEntryDto>.Create(entries, 1, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchActivityTimelineAsync(
                null, null, userId, null, null, null, null, null, null,
                1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchActivityTimelineQuery(UserId: userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _auditLogQueryServiceMock.Verify(
            x => x.SearchActivityTimelineAsync(
                null, null, userId, null, null, null, null, null, null,
                1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region TargetId Filter

    [Fact]
    public async Task Handle_WithTargetIdFilter_ShouldPassFilterToService()
    {
        // Arrange
        var targetId = Guid.NewGuid().ToString();
        var entries = new List<ActivityTimelineEntryDto>
        {
            CreateTestTimelineEntry(targetId: targetId)
        };
        var pagedResult = PagedResult<ActivityTimelineEntryDto>.Create(entries, 1, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchActivityTimelineAsync(
                null, null, null, targetId, null, null, null, null, null,
                1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchActivityTimelineQuery(TargetId: targetId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _auditLogQueryServiceMock.Verify(
            x => x.SearchActivityTimelineAsync(
                null, null, null, targetId, null, null, null, null, null,
                1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region CorrelationId Filter

    [Fact]
    public async Task Handle_WithCorrelationIdFilter_ShouldPassFilterToService()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var entries = new List<ActivityTimelineEntryDto>
        {
            CreateTestTimelineEntry(correlationId: correlationId)
        };
        var pagedResult = PagedResult<ActivityTimelineEntryDto>.Create(entries, 1, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchActivityTimelineAsync(
                null, null, null, null, correlationId, null, null, null, null,
                1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchActivityTimelineQuery(CorrelationId: correlationId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _auditLogQueryServiceMock.Verify(
            x => x.SearchActivityTimelineAsync(
                null, null, null, null, correlationId, null, null, null, null,
                1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region SearchTerm Filter

    [Fact]
    public async Task Handle_WithSearchTermFilter_ShouldPassFilterToService()
    {
        // Arrange
        var entries = new List<ActivityTimelineEntryDto>
        {
            CreateTestTimelineEntry()
        };
        var pagedResult = PagedResult<ActivityTimelineEntryDto>.Create(entries, 1, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchActivityTimelineAsync(
                null, null, null, null, null, "John", null, null, null,
                1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchActivityTimelineQuery(SearchTerm: "John");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _auditLogQueryServiceMock.Verify(
            x => x.SearchActivityTimelineAsync(
                null, null, null, null, null, "John", null, null, null,
                1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Date Range Filtering

    [Fact]
    public async Task Handle_WithFromDateFilter_ShouldPassFilterToService()
    {
        // Arrange
        var fromDate = DateTimeOffset.UtcNow.AddDays(-7);
        var entries = new List<ActivityTimelineEntryDto>
        {
            CreateTestTimelineEntry()
        };
        var pagedResult = PagedResult<ActivityTimelineEntryDto>.Create(entries, 1, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchActivityTimelineAsync(
                null, null, null, null, null, null, fromDate, null, null,
                1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchActivityTimelineQuery(FromDate: fromDate);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _auditLogQueryServiceMock.Verify(
            x => x.SearchActivityTimelineAsync(
                null, null, null, null, null, null, fromDate, null, null,
                1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithToDateFilter_ShouldPassFilterToService()
    {
        // Arrange
        var toDate = DateTimeOffset.UtcNow;
        var entries = new List<ActivityTimelineEntryDto>
        {
            CreateTestTimelineEntry()
        };
        var pagedResult = PagedResult<ActivityTimelineEntryDto>.Create(entries, 1, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchActivityTimelineAsync(
                null, null, null, null, null, null, null, toDate, null,
                1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchActivityTimelineQuery(ToDate: toDate);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _auditLogQueryServiceMock.Verify(
            x => x.SearchActivityTimelineAsync(
                null, null, null, null, null, null, null, toDate, null,
                1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithDateRangeFilter_ShouldPassBothDatesToService()
    {
        // Arrange
        var fromDate = DateTimeOffset.UtcNow.AddDays(-30);
        var toDate = DateTimeOffset.UtcNow;
        var entries = new List<ActivityTimelineEntryDto>
        {
            CreateTestTimelineEntry()
        };
        var pagedResult = PagedResult<ActivityTimelineEntryDto>.Create(entries, 1, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchActivityTimelineAsync(
                null, null, null, null, null, null, fromDate, toDate, null,
                1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchActivityTimelineQuery(FromDate: fromDate, ToDate: toDate);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _auditLogQueryServiceMock.Verify(
            x => x.SearchActivityTimelineAsync(
                null, null, null, null, null, null, fromDate, toDate, null,
                1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region OnlyFailed Filter

    [Fact]
    public async Task Handle_WithOnlyFailedTrue_ShouldPassFilterToService()
    {
        // Arrange
        var entries = new List<ActivityTimelineEntryDto>
        {
            CreateTestTimelineEntry(isSuccess: false)
        };
        var pagedResult = PagedResult<ActivityTimelineEntryDto>.Create(entries, 1, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchActivityTimelineAsync(
                null, null, null, null, null, null, null, null, true,
                1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchActivityTimelineQuery(OnlyFailed: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _auditLogQueryServiceMock.Verify(
            x => x.SearchActivityTimelineAsync(
                null, null, null, null, null, null, null, null, true,
                1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithOnlyFailedFalse_ShouldPassFilterToService()
    {
        // Arrange
        var entries = new List<ActivityTimelineEntryDto>
        {
            CreateTestTimelineEntry(isSuccess: true)
        };
        var pagedResult = PagedResult<ActivityTimelineEntryDto>.Create(entries, 1, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchActivityTimelineAsync(
                null, null, null, null, null, null, null, null, false,
                1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchActivityTimelineQuery(OnlyFailed: false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _auditLogQueryServiceMock.Verify(
            x => x.SearchActivityTimelineAsync(
                null, null, null, null, null, null, null, null, false,
                1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Pagination Scenarios

    [Fact]
    public async Task Handle_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var entries = new List<ActivityTimelineEntryDto>
        {
            CreateTestTimelineEntry(),
            CreateTestTimelineEntry()
        };
        var pagedResult = PagedResult<ActivityTimelineEntryDto>.Create(entries, 25, 1, 10);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchActivityTimelineAsync(
                null, null, null, null, null, null, null, null, null,
                2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchActivityTimelineQuery(Page: 2, PageSize: 10);

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
        var entries = new List<ActivityTimelineEntryDto>
        {
            CreateTestTimelineEntry()
        };
        var pagedResult = PagedResult<ActivityTimelineEntryDto>.Create(entries, 25, 0, 10);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchActivityTimelineAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(),
                It.IsAny<bool?>(), 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchActivityTimelineQuery(Page: 1, PageSize: 10);

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
        var entries = new List<ActivityTimelineEntryDto>
        {
            CreateTestTimelineEntry()
        };
        var pagedResult = PagedResult<ActivityTimelineEntryDto>.Create(entries, 25, 2, 10);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchActivityTimelineAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(),
                It.IsAny<bool?>(), 3, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchActivityTimelineQuery(Page: 3, PageSize: 10);

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
            .Select(_ => CreateTestTimelineEntry())
            .ToList();
        var pagedResult = PagedResult<ActivityTimelineEntryDto>.Create(entries, 50, 0, 5);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchActivityTimelineAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(),
                It.IsAny<bool?>(), 1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchActivityTimelineQuery(Page: 1, PageSize: 5);

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
        var pagedResult = PagedResult<ActivityTimelineEntryDto>.Empty(0, 20);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _auditLogQueryServiceMock
            .Setup(x => x.SearchActivityTimelineAsync(
                null, null, null, null, null, null, null, null, null,
                1, 20, token))
            .ReturnsAsync(pagedResult);

        var query = new SearchActivityTimelineQuery();

        // Act
        await _handler.Handle(query, token);

        // Assert
        _auditLogQueryServiceMock.Verify(
            x => x.SearchActivityTimelineAsync(
                null, null, null, null, null, null, null, null, null,
                1, 20, token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _auditLogQueryServiceMock
            .Setup(x => x.SearchActivityTimelineAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(),
                It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var query = new SearchActivityTimelineQuery();

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
        var userId = Guid.NewGuid().ToString();
        var targetId = Guid.NewGuid().ToString();
        var correlationId = Guid.NewGuid().ToString();
        var fromDate = DateTimeOffset.UtcNow.AddDays(-30);
        var toDate = DateTimeOffset.UtcNow;
        var entries = new List<ActivityTimelineEntryDto>
        {
            CreateTestTimelineEntry(
                displayContext: "Users",
                operationType: "Update",
                isSuccess: false,
                userId: userId,
                targetId: targetId,
                correlationId: correlationId)
        };
        var pagedResult = PagedResult<ActivityTimelineEntryDto>.Create(entries, 1, 0, 15);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchActivityTimelineAsync(
                "Users", "Update", userId, targetId, correlationId, "John",
                fromDate, toDate, true, 1, 15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchActivityTimelineQuery(
            PageContext: "Users",
            OperationType: "Update",
            UserId: userId,
            TargetId: targetId,
            CorrelationId: correlationId,
            SearchTerm: "John",
            FromDate: fromDate,
            ToDate: toDate,
            OnlyFailed: true,
            Page: 1,
            PageSize: 15);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _auditLogQueryServiceMock.Verify(
            x => x.SearchActivityTimelineAsync(
                "Users", "Update", userId, targetId, correlationId, "John",
                fromDate, toDate, true, 1, 15, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}

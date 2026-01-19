using NOIR.Application.Features.Audit.DTOs;
using NOIR.Application.Features.Audit.Queries.SearchEntitiesWithHistory;

namespace NOIR.Application.UnitTests.Features.Audit;

/// <summary>
/// Unit tests for SearchEntitiesWithHistoryQueryHandler.
/// Tests entity search scenarios with filtering and pagination.
/// </summary>
public class SearchEntitiesWithHistoryQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IAuditLogQueryService> _auditLogQueryServiceMock;
    private readonly SearchEntitiesWithHistoryQueryHandler _handler;

    public SearchEntitiesWithHistoryQueryHandlerTests()
    {
        _auditLogQueryServiceMock = new Mock<IAuditLogQueryService>();
        _handler = new SearchEntitiesWithHistoryQueryHandler(_auditLogQueryServiceMock.Object);
    }

    private static EntitySearchResultDto CreateTestSearchResult(
        string entityType = "User",
        string entityId = "123",
        string displayName = "John Doe",
        int totalChanges = 5)
    {
        return new EntitySearchResultDto(
            EntityType: entityType,
            EntityId: entityId,
            DisplayName: displayName,
            Description: $"{entityType} entity",
            LastModified: DateTimeOffset.UtcNow,
            LastModifiedBy: "admin@noir.local",
            TotalChanges: totalChanges);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithNoFilters_ShouldReturnAllEntities()
    {
        // Arrange
        var entities = new List<EntitySearchResultDto>
        {
            CreateTestSearchResult(entityType: "User", entityId: "1", displayName: "John Doe"),
            CreateTestSearchResult(entityType: "Tenant", entityId: "2", displayName: "Acme Corp"),
            CreateTestSearchResult(entityType: "Role", entityId: "3", displayName: "Admin")
        };
        var pagedResult = PagedResult<EntitySearchResultDto>.Create(entities, 3, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchEntitiesAsync(null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchEntitiesWithHistoryQuery(null, null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ShouldReturnEntitiesWithAllFields()
    {
        // Arrange
        var lastModified = DateTimeOffset.UtcNow;
        var entity = new EntitySearchResultDto(
            EntityType: "User",
            EntityId: "user-123",
            DisplayName: "Jane Smith",
            Description: "System administrator",
            LastModified: lastModified,
            LastModifiedBy: "admin@noir.local",
            TotalChanges: 15);
        var pagedResult = PagedResult<EntitySearchResultDto>.Create(new List<EntitySearchResultDto> { entity }, 1, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchEntitiesAsync(null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchEntitiesWithHistoryQuery(null, null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var searchResult = result.Value.Items[0];
        searchResult.EntityType.Should().Be("User");
        searchResult.EntityId.Should().Be("user-123");
        searchResult.DisplayName.Should().Be("Jane Smith");
        searchResult.Description.Should().Be("System administrator");
        searchResult.LastModified.Should().Be(lastModified);
        searchResult.LastModifiedBy.Should().Be("admin@noir.local");
        searchResult.TotalChanges.Should().Be(15);
    }

    [Fact]
    public async Task Handle_WithMultipleEntityTypes_ShouldReturnMixedResults()
    {
        // Arrange
        var entities = new List<EntitySearchResultDto>
        {
            CreateTestSearchResult(entityType: "User", entityId: "1"),
            CreateTestSearchResult(entityType: "Tenant", entityId: "2"),
            CreateTestSearchResult(entityType: "Role", entityId: "3"),
            CreateTestSearchResult(entityType: "Post", entityId: "4"),
            CreateTestSearchResult(entityType: "Category", entityId: "5")
        };
        var pagedResult = PagedResult<EntitySearchResultDto>.Create(entities, 5, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchEntitiesAsync(null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchEntitiesWithHistoryQuery(null, null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.Items.Select(e => e.EntityType).Should().Contain("User");
        result.Value.Items.Select(e => e.EntityType).Should().Contain("Tenant");
        result.Value.Items.Select(e => e.EntityType).Should().Contain("Role");
    }

    #endregion

    #region Empty Results Scenarios

    [Fact]
    public async Task Handle_WithNoEntities_ShouldReturnEmptyResult()
    {
        // Arrange
        var pagedResult = PagedResult<EntitySearchResultDto>.Empty(0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchEntitiesAsync(null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchEntitiesWithHistoryQuery(null, null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithNoMatchingSearch_ShouldReturnEmptyResult()
    {
        // Arrange
        var pagedResult = PagedResult<EntitySearchResultDto>.Empty(0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchEntitiesAsync("User", "nonexistent", 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchEntitiesWithHistoryQuery("User", "nonexistent");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    #endregion

    #region EntityType Filter

    [Fact]
    public async Task Handle_WithEntityTypeFilter_ShouldPassFilterToService()
    {
        // Arrange
        var entities = new List<EntitySearchResultDto>
        {
            CreateTestSearchResult(entityType: "User", entityId: "1"),
            CreateTestSearchResult(entityType: "User", entityId: "2")
        };
        var pagedResult = PagedResult<EntitySearchResultDto>.Create(entities, 2, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchEntitiesAsync("User", null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchEntitiesWithHistoryQuery("User", null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        _auditLogQueryServiceMock.Verify(
            x => x.SearchEntitiesAsync("User", null, 1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithDifferentEntityTypes_ShouldFilterCorrectly()
    {
        // Arrange
        var tenants = new List<EntitySearchResultDto>
        {
            CreateTestSearchResult(entityType: "Tenant", entityId: "1", displayName: "Acme Corp"),
            CreateTestSearchResult(entityType: "Tenant", entityId: "2", displayName: "Beta Inc")
        };
        var pagedResult = PagedResult<EntitySearchResultDto>.Create(tenants, 2, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchEntitiesAsync("Tenant", null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchEntitiesWithHistoryQuery("Tenant", null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().OnlyContain(e => e.EntityType == "Tenant");
    }

    #endregion

    #region SearchTerm Filter

    [Fact]
    public async Task Handle_WithSearchTermFilter_ShouldPassFilterToService()
    {
        // Arrange
        var entities = new List<EntitySearchResultDto>
        {
            CreateTestSearchResult(displayName: "John Doe")
        };
        var pagedResult = PagedResult<EntitySearchResultDto>.Create(entities, 1, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchEntitiesAsync(null, "John", 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchEntitiesWithHistoryQuery(null, "John");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _auditLogQueryServiceMock.Verify(
            x => x.SearchEntitiesAsync(null, "John", 1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithPartialSearchTerm_ShouldPassFilterToService()
    {
        // Arrange
        var entities = new List<EntitySearchResultDto>
        {
            CreateTestSearchResult(displayName: "John Doe"),
            CreateTestSearchResult(displayName: "Johnny Smith")
        };
        var pagedResult = PagedResult<EntitySearchResultDto>.Create(entities, 2, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchEntitiesAsync(null, "John", 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchEntitiesWithHistoryQuery(null, "John");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithCaseInsensitiveSearch_ShouldReturnResults()
    {
        // Arrange
        var entities = new List<EntitySearchResultDto>
        {
            CreateTestSearchResult(displayName: "ADMIN User")
        };
        var pagedResult = PagedResult<EntitySearchResultDto>.Create(entities, 1, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchEntitiesAsync(null, "admin", 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchEntitiesWithHistoryQuery(null, "admin");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    #endregion

    #region Combined Filters

    [Fact]
    public async Task Handle_WithEntityTypeAndSearchTerm_ShouldPassBothFiltersToService()
    {
        // Arrange
        var entities = new List<EntitySearchResultDto>
        {
            CreateTestSearchResult(entityType: "User", displayName: "John Admin")
        };
        var pagedResult = PagedResult<EntitySearchResultDto>.Create(entities, 1, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchEntitiesAsync("User", "Admin", 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchEntitiesWithHistoryQuery("User", "Admin");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _auditLogQueryServiceMock.Verify(
            x => x.SearchEntitiesAsync("User", "Admin", 1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Pagination Scenarios

    [Fact]
    public async Task Handle_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var entities = new List<EntitySearchResultDto>
        {
            CreateTestSearchResult(entityId: "11"),
            CreateTestSearchResult(entityId: "12")
        };
        var pagedResult = PagedResult<EntitySearchResultDto>.Create(entities, 25, 1, 10);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchEntitiesAsync(null, null, 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchEntitiesWithHistoryQuery(null, null, Page: 2, PageSize: 10);

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
        var entities = new List<EntitySearchResultDto>
        {
            CreateTestSearchResult()
        };
        var pagedResult = PagedResult<EntitySearchResultDto>.Create(entities, 25, 0, 10);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchEntitiesAsync(null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchEntitiesWithHistoryQuery(null, null, Page: 1, PageSize: 10);

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
        var entities = new List<EntitySearchResultDto>
        {
            CreateTestSearchResult()
        };
        var pagedResult = PagedResult<EntitySearchResultDto>.Create(entities, 25, 2, 10);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchEntitiesAsync(null, null, 3, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchEntitiesWithHistoryQuery(null, null, Page: 3, PageSize: 10);

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
        var entities = Enumerable.Range(1, 5)
            .Select(i => CreateTestSearchResult(entityId: i.ToString()))
            .ToList();
        var pagedResult = PagedResult<EntitySearchResultDto>.Create(entities, 50, 0, 5);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchEntitiesAsync(null, null, 1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchEntitiesWithHistoryQuery(null, null, Page: 1, PageSize: 5);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.PageSize.Should().Be(5);
        result.Value.TotalPages.Should().Be(10);
    }

    [Fact]
    public async Task Handle_WithDefaultPagination_ShouldUseDefaults()
    {
        // Arrange
        var entities = new List<EntitySearchResultDto>
        {
            CreateTestSearchResult()
        };
        var pagedResult = PagedResult<EntitySearchResultDto>.Create(entities, 1, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchEntitiesAsync(null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchEntitiesWithHistoryQuery(null, null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _auditLogQueryServiceMock.Verify(
            x => x.SearchEntitiesAsync(null, null, 1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region CancellationToken Scenarios

    [Fact]
    public async Task Handle_ShouldPassCancellationTokenToService()
    {
        // Arrange
        var pagedResult = PagedResult<EntitySearchResultDto>.Empty(0, 20);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _auditLogQueryServiceMock
            .Setup(x => x.SearchEntitiesAsync(null, null, 1, 20, token))
            .ReturnsAsync(pagedResult);

        var query = new SearchEntitiesWithHistoryQuery(null, null);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _auditLogQueryServiceMock.Verify(
            x => x.SearchEntitiesAsync(null, null, 1, 20, token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _auditLogQueryServiceMock
            .Setup(x => x.SearchEntitiesAsync(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var query = new SearchEntitiesWithHistoryQuery(null, null);

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
        var pagedResult = PagedResult<EntitySearchResultDto>.Empty(0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchEntitiesAsync(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchEntitiesWithHistoryQuery(null, null);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _auditLogQueryServiceMock.Verify(
            x => x.SearchEntitiesAsync(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region TotalChanges Verification

    [Fact]
    public async Task Handle_ShouldReturnEntitiesWithTotalChangesCount()
    {
        // Arrange
        var entities = new List<EntitySearchResultDto>
        {
            CreateTestSearchResult(totalChanges: 10),
            CreateTestSearchResult(totalChanges: 25),
            CreateTestSearchResult(totalChanges: 3)
        };
        var pagedResult = PagedResult<EntitySearchResultDto>.Create(entities, 3, 0, 20);

        _auditLogQueryServiceMock
            .Setup(x => x.SearchEntitiesAsync(null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var query = new SearchEntitiesWithHistoryQuery(null, null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items[0].TotalChanges.Should().Be(10);
        result.Value.Items[1].TotalChanges.Should().Be(25);
        result.Value.Items[2].TotalChanges.Should().Be(3);
    }

    #endregion
}

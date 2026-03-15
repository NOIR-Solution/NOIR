using NOIR.Application.Features.Tenants.DTOs;
using NOIR.Application.Features.Tenants.Queries.GetTenants;

namespace NOIR.Application.UnitTests.Features.Tenants;

/// <summary>
/// Unit tests for GetTenantsQueryHandler.
/// Tests paginated tenant list retrieval scenarios with filtering.
/// </summary>
public class GetTenantsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IMultiTenantStore<Tenant>> _tenantStoreMock;
    private readonly GetTenantsQueryHandler _handler;

    public GetTenantsQueryHandlerTests()
    {
        _tenantStoreMock = new Mock<IMultiTenantStore<Tenant>>();
        _handler = new GetTenantsQueryHandler(_tenantStoreMock.Object);
    }

    private static Tenant CreateTestTenant(
        string identifier = "test-tenant",
        string name = "Test Tenant",
        bool isActive = true,
        bool isDeleted = false)
    {
        var tenant = Tenant.Create(identifier, name, isActive: isActive);

        if (isDeleted)
        {
            tenant.IsDeleted = true;
            tenant.DeletedAt = DateTimeOffset.UtcNow;
        }

        return tenant;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithNoFilters_ShouldReturnAllNonDeletedTenants()
    {
        // Arrange
        var tenants = new List<Tenant>
        {
            CreateTestTenant(identifier: "tenant-1", name: "Tenant One"),
            CreateTestTenant(identifier: "tenant-2", name: "Tenant Two"),
            CreateTestTenant(identifier: "tenant-3", name: "Tenant Three")
        };

        _tenantStoreMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(tenants);

        var query = new GetTenantsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(3);
        result.Value.TotalCount.ShouldBe(3);
    }

    [Fact]
    public async Task Handle_WithSearchFilter_ShouldFilterByName()
    {
        // Arrange
        var tenants = new List<Tenant>
        {
            CreateTestTenant(identifier: "acme-corp", name: "Acme Corporation"),
            CreateTestTenant(identifier: "other-tenant", name: "Other Tenant"),
            CreateTestTenant(identifier: "acme-inc", name: "Acme Inc")
        };

        _tenantStoreMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(tenants);

        var query = new GetTenantsQuery(Search: "acme");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(2);
        result.Value.Items.ShouldAllBe(t =>
            t.Name!.ToLowerInvariant().Contains("acme") ||
            t.Identifier!.ToLowerInvariant().Contains("acme"));
    }

    [Fact]
    public async Task Handle_WithSearchFilter_ShouldFilterByIdentifier()
    {
        // Arrange
        var tenants = new List<Tenant>
        {
            CreateTestTenant(identifier: "acme-corp", name: "Company One"),
            CreateTestTenant(identifier: "other-tenant", name: "Company Two")
        };

        _tenantStoreMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(tenants);

        var query = new GetTenantsQuery(Search: "acme");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(1);
        result.Value.Items[0].Identifier.ShouldBe("acme-corp");
    }

    [Fact]
    public async Task Handle_WithIsActiveTrue_ShouldReturnOnlyActiveTenants()
    {
        // Arrange
        var tenants = new List<Tenant>
        {
            CreateTestTenant(identifier: "active-1", name: "Active One", isActive: true),
            CreateTestTenant(identifier: "inactive-1", name: "Inactive One", isActive: false),
            CreateTestTenant(identifier: "active-2", name: "Active Two", isActive: true)
        };

        _tenantStoreMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(tenants);

        var query = new GetTenantsQuery(IsActive: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(2);
        result.Value.Items.ShouldAllBe(t => t.IsActive == true);
    }

    [Fact]
    public async Task Handle_WithIsActiveFalse_ShouldReturnOnlyInactiveTenants()
    {
        // Arrange
        var tenants = new List<Tenant>
        {
            CreateTestTenant(identifier: "active-1", name: "Active One", isActive: true),
            CreateTestTenant(identifier: "inactive-1", name: "Inactive One", isActive: false),
            CreateTestTenant(identifier: "inactive-2", name: "Inactive Two", isActive: false)
        };

        _tenantStoreMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(tenants);

        var query = new GetTenantsQuery(IsActive: false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(2);
        result.Value.Items.ShouldAllBe(t => t.IsActive == false);
    }

    [Fact]
    public async Task Handle_ShouldExcludeDeletedTenants()
    {
        // Arrange
        var tenants = new List<Tenant>
        {
            CreateTestTenant(identifier: "active-tenant", name: "Active Tenant"),
            CreateTestTenant(identifier: "deleted-tenant", name: "Deleted Tenant", isDeleted: true)
        };

        _tenantStoreMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(tenants);

        var query = new GetTenantsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(1);
        result.Value.Items[0].Identifier.ShouldBe("active-tenant");
    }

    [Fact]
    public async Task Handle_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var tenant = CreateTestTenant(
            identifier: "full-tenant",
            name: "Full Tenant",
            isActive: true);

        _tenantStoreMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Tenant> { tenant });

        var query = new GetTenantsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var dto = result.Value.Items[0];
        dto.Id.ShouldBe(tenant.Id);
        dto.Identifier.ShouldBe(tenant.Identifier);
        dto.Name.ShouldBe(tenant.Name);
        dto.IsActive.ShouldBe(tenant.IsActive);
        dto.CreatedAt.ShouldBe(tenant.CreatedAt);
    }

    [Fact]
    public async Task Handle_ShouldOrderByName()
    {
        // Arrange
        var tenants = new List<Tenant>
        {
            CreateTestTenant(identifier: "z-tenant", name: "Zebra Corp"),
            CreateTestTenant(identifier: "a-tenant", name: "Alpha Inc"),
            CreateTestTenant(identifier: "m-tenant", name: "Middle Corp")
        };

        _tenantStoreMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(tenants);

        var query = new GetTenantsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items[0].Name.ShouldBe("Alpha Inc");
        result.Value.Items[1].Name.ShouldBe("Middle Corp");
        result.Value.Items[2].Name.ShouldBe("Zebra Corp");
    }

    #endregion

    #region Pagination Scenarios

    [Fact]
    public async Task Handle_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var tenants = Enumerable.Range(1, 25)
            .Select(i => CreateTestTenant(
                identifier: $"tenant-{i:D2}",
                name: $"Tenant {i:D2}"))
            .ToList();

        _tenantStoreMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(tenants);

        var query = new GetTenantsQuery(Page: 2, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(10);
        result.Value.TotalCount.ShouldBe(25);
        result.Value.PageNumber.ShouldBe(2);
        result.Value.TotalPages.ShouldBe(3);
    }

    [Fact]
    public async Task Handle_WithCustomPageSize_ShouldReturnCorrectCount()
    {
        // Arrange
        var tenants = Enumerable.Range(1, 15)
            .Select(i => CreateTestTenant(
                identifier: $"tenant-{i}",
                name: $"Tenant {i}"))
            .ToList();

        _tenantStoreMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(tenants);

        var query = new GetTenantsQuery(PageSize: 5);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(5);
        result.Value.TotalCount.ShouldBe(15);
        result.Value.TotalPages.ShouldBe(3);
    }

    [Fact]
    public async Task Handle_WithLastPage_ShouldReturnRemainingItems()
    {
        // Arrange
        var tenants = Enumerable.Range(1, 7)
            .Select(i => CreateTestTenant(
                identifier: $"tenant-{i}",
                name: $"Tenant {i}"))
            .ToList();

        _tenantStoreMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(tenants);

        var query = new GetTenantsQuery(Page: 2, PageSize: 5);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(2);
        result.Value.TotalCount.ShouldBe(7);
        result.Value.HasNextPage.ShouldBe(false);
    }

    [Fact]
    public async Task Handle_FirstPageShouldHaveHasPreviousPageFalse()
    {
        // Arrange
        var tenants = Enumerable.Range(1, 10)
            .Select(i => CreateTestTenant(
                identifier: $"tenant-{i}",
                name: $"Tenant {i}"))
            .ToList();

        _tenantStoreMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(tenants);

        var query = new GetTenantsQuery(Page: 1, PageSize: 5);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.HasPreviousPage.ShouldBe(false);
        result.Value.HasNextPage.ShouldBe(true);
    }

    #endregion

    #region Empty Results Scenarios

    [Fact]
    public async Task Handle_WithNoTenants_ShouldReturnEmptyList()
    {
        // Arrange
        _tenantStoreMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Tenant>());

        var query = new GetTenantsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_WithSearchNoMatch_ShouldReturnEmptyList()
    {
        // Arrange
        var tenants = new List<Tenant>
        {
            CreateTestTenant(identifier: "acme", name: "Acme Corp"),
            CreateTestTenant(identifier: "beta", name: "Beta Inc")
        };

        _tenantStoreMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(tenants);

        var query = new GetTenantsQuery(Search: "nonexistent");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_WithAllTenantsDeleted_ShouldReturnEmptyList()
    {
        // Arrange
        var tenants = new List<Tenant>
        {
            CreateTestTenant(identifier: "deleted-1", name: "Deleted One", isDeleted: true),
            CreateTestTenant(identifier: "deleted-2", name: "Deleted Two", isDeleted: true)
        };

        _tenantStoreMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(tenants);

        var query = new GetTenantsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
    }

    #endregion

    #region CancellationToken Scenarios

    [Fact]
    public async Task Handle_ShouldCallTenantStore()
    {
        // Arrange
        _tenantStoreMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Tenant>());

        var query = new GetTenantsQuery();

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _tenantStoreMock.Verify(x => x.GetAllAsync(), Times.Once);
    }

    #endregion

    #region Combined Filters Scenarios

    [Fact]
    public async Task Handle_WithSearchAndIsActive_ShouldApplyBothFilters()
    {
        // Arrange
        var tenants = new List<Tenant>
        {
            CreateTestTenant(identifier: "acme-active", name: "Acme Active", isActive: true),
            CreateTestTenant(identifier: "acme-inactive", name: "Acme Inactive", isActive: false),
            CreateTestTenant(identifier: "beta-active", name: "Beta Active", isActive: true)
        };

        _tenantStoreMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(tenants);

        var query = new GetTenantsQuery(Search: "acme", IsActive: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(1);
        result.Value.Items[0].Identifier.ShouldBe("acme-active");
        result.Value.Items[0].IsActive.ShouldBe(true);
    }

    [Fact]
    public async Task Handle_WithAllFilters_ShouldApplyAllFiltersAndPagination()
    {
        // Arrange
        var tenants = Enumerable.Range(1, 20)
            .Select(i => CreateTestTenant(
                identifier: $"acme-{i:D2}",
                name: $"Acme Tenant {i:D2}",
                isActive: i % 2 == 0)) // Even numbers are active
            .ToList();

        _tenantStoreMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(tenants);

        // Search for "acme", only active, page 2 with page size 3
        var query = new GetTenantsQuery(Search: "acme", IsActive: true, Page: 2, PageSize: 3);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        // 10 active tenants matching "acme", page 2 with size 3 = 3 items
        result.Value.Items.Count().ShouldBe(3);
        result.Value.TotalCount.ShouldBe(10);
        result.Value.Items.ShouldAllBe(t => t.IsActive == true);
    }

    #endregion

    #region Search Case Insensitivity

    [Fact]
    public async Task Handle_WithSearchUpperCase_ShouldFindLowerCaseMatch()
    {
        // Arrange
        var tenants = new List<Tenant>
        {
            CreateTestTenant(identifier: "acme", name: "acme corp")
        };

        _tenantStoreMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(tenants);

        var query = new GetTenantsQuery(Search: "ACME");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(1);
    }

    [Fact]
    public async Task Handle_WithSearchLowerCase_ShouldFindUpperCaseMatch()
    {
        // Arrange
        var tenants = new List<Tenant>
        {
            CreateTestTenant(identifier: "ACME", name: "ACME CORP")
        };

        _tenantStoreMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(tenants);

        var query = new GetTenantsQuery(Search: "acme");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(1);
    }

    #endregion
}

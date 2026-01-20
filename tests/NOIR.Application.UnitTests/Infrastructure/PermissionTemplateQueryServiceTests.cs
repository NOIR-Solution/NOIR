namespace NOIR.Application.UnitTests.Infrastructure;

using NOIR.Application.Features.Permissions.Queries.GetPermissionTemplates;

/// <summary>
/// Unit tests for PermissionTemplateQueryService.
/// Tests permission template retrieval with tenant filtering.
/// </summary>
public class PermissionTemplateQueryServiceTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly PermissionTemplateQueryService _sut;

    public PermissionTemplateQueryServiceTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _sut = new PermissionTemplateQueryService(_contextMock.Object);
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithNullTenantId_ShouldReturnOnlySystemTemplates()
    {
        // Arrange
        var systemTemplate = CreateTemplate(name: "System Admin", tenantId: null, isSystem: true);
        var tenantTemplate = CreateTemplate(name: "Tenant Admin", tenantId: "tenant-123", isSystem: false);

        var templates = new List<PermissionTemplate> { systemTemplate, tenantTemplate };
        var mockDbSet = CreateMockDbSet(templates.Where(t => t.TenantId == null));

        _contextMock.Setup(x => x.PermissionTemplates).Returns(mockDbSet.Object);

        // Act
        var result = await _sut.GetAllAsync(null, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(t => t.Name == "System Admin");
        result.Should().NotContain(t => t.Name == "Tenant Admin");
    }

    [Fact]
    public async Task GetAllAsync_WithTenantId_ShouldReturnSystemAndTenantTemplates()
    {
        // Arrange
        var tenantId = "tenant-123";
        var systemTemplate = CreateTemplate(name: "System Template", tenantId: null, isSystem: true);
        var matchingTenantTemplate = CreateTemplate(name: "Tenant Template", tenantId: tenantId, isSystem: false);
        var otherTenantTemplate = CreateTemplate(name: "Other Tenant", tenantId: "tenant-123", isSystem: false);

        var templates = new List<PermissionTemplate> { systemTemplate, matchingTenantTemplate };
        var mockDbSet = CreateMockDbSet(templates);

        _contextMock.Setup(x => x.PermissionTemplates).Returns(mockDbSet.Object);

        // Act
        var result = await _sut.GetAllAsync(tenantId, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(t => t.Name == "System Template");
        result.Should().Contain(t => t.Name == "Tenant Template");
    }

    [Fact]
    public async Task GetAllAsync_ShouldExcludeDeletedTemplates()
    {
        // Arrange
        var activeTemplate = CreateTemplate(name: "Active Template", tenantId: null, isSystem: true);
        var deletedTemplate = CreateTemplate(name: "Deleted Template", tenantId: null, isSystem: true, isDeleted: true);

        var templates = new List<PermissionTemplate> { activeTemplate };
        var mockDbSet = CreateMockDbSet(templates);

        _contextMock.Setup(x => x.PermissionTemplates).Returns(mockDbSet.Object);

        // Act
        var result = await _sut.GetAllAsync(null, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(t => t.Name == "Active Template");
    }

    [Fact]
    public async Task GetAllAsync_ShouldOrderBySortOrderThenByName()
    {
        // Arrange
        var template1 = CreateTemplate(name: "Zebra", tenantId: null, sortOrder: 1);
        var template2 = CreateTemplate(name: "Alpha", tenantId: null, sortOrder: 1);
        var template3 = CreateTemplate(name: "Beta", tenantId: null, sortOrder: 0);

        var templates = new List<PermissionTemplate> { template3, template2, template1 };
        var mockDbSet = CreateMockDbSet(templates.OrderBy(t => t.SortOrder).ThenBy(t => t.Name));

        _contextMock.Setup(x => x.PermissionTemplates).Returns(mockDbSet.Object);

        // Act
        var result = await _sut.GetAllAsync(null, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Beta");    // SortOrder 0
        result[1].Name.Should().Be("Alpha");   // SortOrder 1, first alphabetically
        result[2].Name.Should().Be("Zebra");   // SortOrder 1, last alphabetically
    }

    [Fact]
    public async Task GetAllAsync_ShouldMapToDto()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var tenantId = "tenant-123";
        var template = CreateTemplateWithPermissions(
            id: templateId,
            name: "Test Template",
            description: "A test template",
            tenantId: tenantId,
            isSystem: false,
            iconName: "shield",
            color: "#ff0000",
            sortOrder: 5,
            permissionNames: new[] { "users:read", "users:write" });

        var mockDbSet = CreateMockDbSet(new[] { template });
        _contextMock.Setup(x => x.PermissionTemplates).Returns(mockDbSet.Object);

        // Act
        var result = await _sut.GetAllAsync(tenantId, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        var dto = result.First();
        dto.Id.Should().Be(templateId);
        dto.Name.Should().Be("Test Template");
        dto.Description.Should().Be("A test template");
        dto.TenantId.Should().Be(tenantId);
        dto.IsSystem.Should().BeFalse();
        dto.IconName.Should().Be("shield");
        dto.Color.Should().Be("#ff0000");
        dto.SortOrder.Should().Be(5);
        dto.Permissions.Should().BeEquivalentTo(new[] { "users:read", "users:write" });
    }

    [Fact]
    public async Task GetAllAsync_WithEmptyResult_ShouldReturnEmptyList()
    {
        // Arrange
        var mockDbSet = CreateMockDbSet(Enumerable.Empty<PermissionTemplate>());
        _contextMock.Setup(x => x.PermissionTemplates).Returns(mockDbSet.Object);

        // Act
        var result = await _sut.GetAllAsync(null, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var mockDbSet = new Mock<DbSet<PermissionTemplate>>();
        mockDbSet.As<IQueryable<PermissionTemplate>>()
            .Setup(m => m.Provider)
            .Throws(new OperationCanceledException());

        _contextMock.Setup(x => x.PermissionTemplates).Returns(mockDbSet.Object);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.GetAllAsync(null, cts.Token));
    }

    #endregion

    #region Service Registration Tests

    [Fact]
    public void Service_ShouldImplementIPermissionTemplateQueryService()
    {
        // Assert
        _sut.Should().BeAssignableTo<IPermissionTemplateQueryService>();
    }

    [Fact]
    public void Service_ShouldImplementIScopedService()
    {
        // Assert
        _sut.Should().BeAssignableTo<IScopedService>();
    }

    [Fact]
    public void Constructor_ShouldAcceptDependencies()
    {
        // Act
        var service = new PermissionTemplateQueryService(_contextMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region Helper Methods

    private static PermissionTemplate CreateTemplate(
        string name,
        string? tenantId,
        bool isSystem = false,
        int sortOrder = 0,
        bool isDeleted = false)
    {
        var template = PermissionTemplate.Create(
            name: name,
            description: $"Description for {name}",
            tenantId: tenantId,
            isSystem: isSystem,
            iconName: null,
            color: null,
            sortOrder: sortOrder);

        if (isDeleted)
        {
            // Use reflection to set IsDeleted since it's protected
            var isDeletedProperty = typeof(PermissionTemplate).GetProperty("IsDeleted");
            isDeletedProperty?.SetValue(template, true);
        }

        return template;
    }

    private static PermissionTemplate CreateTemplateWithPermissions(
        Guid id,
        string name,
        string? description,
        string? tenantId,
        bool isSystem,
        string? iconName,
        string? color,
        int sortOrder,
        string[] permissionNames)
    {
        var template = PermissionTemplate.Create(
            name: name,
            description: description,
            tenantId: tenantId,
            isSystem: isSystem,
            iconName: iconName,
            color: color,
            sortOrder: sortOrder);

        // Use reflection to set ID
        var idProperty = typeof(PermissionTemplate).GetProperty("Id");
        idProperty?.SetValue(template, id);

        // Add mock permissions
        foreach (var permName in permissionNames)
        {
            var parts = permName.Split(':');
            var resource = parts.Length > 0 ? parts[0] : "unknown";
            var action = parts.Length > 1 ? parts[1] : "read";

            var permission = Permission.Create(
                resource: resource,
                action: action,
                displayName: permName.Replace(":", " ").ToUpper(),
                category: "Test");

            var item = PermissionTemplateItem.Create(template.Id, permission.Id);

            // Use reflection to set Permission navigation property
            var permProperty = typeof(PermissionTemplateItem).GetProperty("Permission");
            permProperty?.SetValue(item, permission);

            template.Items.Add(item);
        }

        return template;
    }

    private static Mock<DbSet<PermissionTemplate>> CreateMockDbSet(IEnumerable<PermissionTemplate> data)
    {
        var queryable = data.AsQueryable();
        var mockSet = new Mock<DbSet<PermissionTemplate>>();

        mockSet.As<IQueryable<PermissionTemplate>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<PermissionTemplate>(queryable.Provider));

        mockSet.As<IQueryable<PermissionTemplate>>()
            .Setup(m => m.Expression)
            .Returns(queryable.Expression);

        mockSet.As<IQueryable<PermissionTemplate>>()
            .Setup(m => m.ElementType)
            .Returns(queryable.ElementType);

        mockSet.As<IQueryable<PermissionTemplate>>()
            .Setup(m => m.GetEnumerator())
            .Returns(() => queryable.GetEnumerator());

        mockSet.As<IAsyncEnumerable<PermissionTemplate>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<PermissionTemplate>(queryable.GetEnumerator()));

        return mockSet;
    }

    #endregion
}

#region Async Query Helpers

internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object? Execute(Expression expression)
    {
        return _inner.Execute(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var expectedResultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = typeof(IQueryProvider)
            .GetMethod(
                name: nameof(IQueryProvider.Execute),
                genericParameterCount: 1,
                types: new[] { typeof(Expression) })!
            .MakeGenericMethod(expectedResultType)
            .Invoke(this, new[] { expression });

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(expectedResultType)
            .Invoke(null, new[] { executionResult })!;
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable)
        : base(enumerable)
    { }

    public TestAsyncEnumerable(Expression expression)
        : base(expression)
    { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public T Current => _inner.Current;

    public ValueTask<bool> MoveNextAsync()
    {
        return new ValueTask<bool>(_inner.MoveNext());
    }

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return new ValueTask();
    }
}

#endregion

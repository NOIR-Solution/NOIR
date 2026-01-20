namespace NOIR.Application.UnitTests.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using NOIR.Domain.Common;
using NOIR.Infrastructure.Persistence.Interceptors;
using Xunit;

/// <summary>
/// Unit tests for Tenant IdSetterInterceptor.
/// Tests that tenant ID is correctly set on new entities.
/// </summary>
public class TenantIdSetterInterceptorTests
{
    #region Test Entities

    private class TestTenantEntity : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? TenantId { get; set; }
        public string? Name { get; set; }
    }

    private class TestNonTenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? Name { get; set; }
    }

    private class TestDbContext : DbContext
    {
        public DbSet<TestTenantEntity> TenantEntities => Set<TestTenantEntity>();
        public DbSet<TestNonTenantEntity> NonTenantEntities => Set<TestNonTenantEntity>();

        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    }

    #endregion

    #region Helper Methods

    private static TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    private static Mock<IMultiTenantContextAccessor<Tenant>> CreateTenantAccessor(string? tenantId)
    {
        var mock = new Mock<IMultiTenantContextAccessor<Tenant>>();

        if (tenantId != null)
        {
            var tenant = new Tenant(tenantId, tenantId, "Test Tenant");
            var multiTenantContext = new MultiTenantContext<Tenant>(tenant);
            mock.Setup(x => x.MultiTenantContext).Returns(multiTenantContext);
        }
        else
        {
            mock.Setup(x => x.MultiTenantContext).Returns(default(IMultiTenantContext<Tenant>)!);
        }

        return mock;
    }

    private static DbContextEventData CreateEventData(DbContext context)
    {
        var eventDefinition = new EventDefinition(
            Mock.Of<ILoggingOptions>(),
            CoreEventId.SaveChangesStarting,
            LogLevel.Debug,
            "SavingChanges",
            level => (logger, ex) => { }
        );

        return new DbContextEventData(
            eventDefinition,
            (d, p) => "",
            context
        );
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldAcceptTenantContextAccessor()
    {
        // Arrange
        var accessor = CreateTenantAccessor("tenant-1");

        // Act
        var interceptor = new TenantIdSetterInterceptor(accessor.Object, Mock.Of<ILogger<TenantIdSetterInterceptor>>());

        // Assert
        interceptor.Should().NotBeNull();
    }

    #endregion

    #region SavingChanges Tests

    [Fact]
    public void SavingChanges_WithNewTenantEntity_ShouldSetTenantId()
    {
        // Arrange
        using var context = CreateContext();
        var accessor = CreateTenantAccessor("tenant-123");
        var interceptor = new TenantIdSetterInterceptor(accessor.Object, Mock.Of<ILogger<TenantIdSetterInterceptor>>());

        var entity = new TestTenantEntity { Name = "Test" };
        context.TenantEntities.Add(entity);

        var eventData = CreateEventData(context);

        // Act
        interceptor.SavingChanges(eventData, default);

        // Assert
        entity.TenantId.Should().Be("tenant-123");
    }

    [Fact]
    public void SavingChanges_WithNewTenantEntityWithPresetTenantId_ShouldNotOverride()
    {
        // Arrange
        using var context = CreateContext();
        var accessor = CreateTenantAccessor("tenant-123");
        var interceptor = new TenantIdSetterInterceptor(accessor.Object, Mock.Of<ILogger<TenantIdSetterInterceptor>>());

        var entity = new TestTenantEntity { Name = "Test", TenantId = "preset-tenant" };
        context.TenantEntities.Add(entity);

        var eventData = CreateEventData(context);

        // Act
        interceptor.SavingChanges(eventData, default);

        // Assert
        entity.TenantId.Should().Be("preset-tenant");
    }

    [Fact]
    public void SavingChanges_WithNullTenantContext_ShouldNotSetTenantId()
    {
        // Arrange
        using var context = CreateContext();
        var accessor = CreateTenantAccessor(null);
        var interceptor = new TenantIdSetterInterceptor(accessor.Object, Mock.Of<ILogger<TenantIdSetterInterceptor>>());

        var entity = new TestTenantEntity { Name = "Test" };
        context.TenantEntities.Add(entity);

        var eventData = CreateEventData(context);

        // Act
        interceptor.SavingChanges(eventData, default);

        // Assert
        entity.TenantId.Should().BeNull();
    }

    [Fact]
    public void SavingChanges_WithEmptyTenantId_ShouldNotSetTenantId()
    {
        // Arrange
        using var context = CreateContext();
        var accessor = CreateTenantAccessor("");
        var interceptor = new TenantIdSetterInterceptor(accessor.Object, Mock.Of<ILogger<TenantIdSetterInterceptor>>());

        var entity = new TestTenantEntity { Name = "Test" };
        context.TenantEntities.Add(entity);

        var eventData = CreateEventData(context);

        // Act
        interceptor.SavingChanges(eventData, default);

        // Assert
        entity.TenantId.Should().BeNull();
    }

    [Fact]
    public void SavingChanges_WithModifiedEntity_ShouldNotChangeTenantId()
    {
        // Arrange
        using var context = CreateContext();
        var accessor = CreateTenantAccessor("new-tenant");
        var interceptor = new TenantIdSetterInterceptor(accessor.Object, Mock.Of<ILogger<TenantIdSetterInterceptor>>());

        // First, add and save entity with original tenant
        var entity = new TestTenantEntity { Name = "Test", TenantId = "original-tenant" };
        context.TenantEntities.Add(entity);
        context.SaveChanges();

        // Modify the entity
        entity.Name = "Updated";
        context.Entry(entity).State = EntityState.Modified;

        var eventData = CreateEventData(context);

        // Act
        interceptor.SavingChanges(eventData, default);

        // Assert - TenantId should remain unchanged
        entity.TenantId.Should().Be("original-tenant");
    }

    [Fact]
    public void SavingChanges_WithNonTenantEntity_ShouldNotThrow()
    {
        // Arrange
        using var context = CreateContext();
        var accessor = CreateTenantAccessor("tenant-123");
        var interceptor = new TenantIdSetterInterceptor(accessor.Object, Mock.Of<ILogger<TenantIdSetterInterceptor>>());

        var entity = new TestNonTenantEntity { Name = "Test" };
        context.NonTenantEntities.Add(entity);

        var eventData = CreateEventData(context);

        // Act
        var act = () => interceptor.SavingChanges(eventData, default);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region SavingChangesAsync Tests

    [Fact]
    public async Task SavingChangesAsync_WithNewTenantEntity_ShouldSetTenantId()
    {
        // Arrange
        using var context = CreateContext();
        var accessor = CreateTenantAccessor("tenant-456");
        var interceptor = new TenantIdSetterInterceptor(accessor.Object, Mock.Of<ILogger<TenantIdSetterInterceptor>>());

        var entity = new TestTenantEntity { Name = "Test" };
        context.TenantEntities.Add(entity);

        var eventData = CreateEventData(context);

        // Act
        await interceptor.SavingChangesAsync(eventData, default, CancellationToken.None);

        // Assert
        entity.TenantId.Should().Be("tenant-456");
    }

    [Fact]
    public async Task SavingChangesAsync_WithNullContext_ShouldNotThrow()
    {
        // Arrange
        var accessor = CreateTenantAccessor("tenant-123");
        var interceptor = new TenantIdSetterInterceptor(accessor.Object, Mock.Of<ILogger<TenantIdSetterInterceptor>>());

        var eventDefinition = new EventDefinition(
            Mock.Of<ILoggingOptions>(),
            CoreEventId.SaveChangesStarting,
            LogLevel.Debug,
            "SavingChanges",
            level => (logger, ex) => { }
        );

        var eventData = new DbContextEventData(
            eventDefinition,
            (d, p) => "",
            null!
        );

        // Act
        var act = async () => await interceptor.SavingChangesAsync(eventData, default, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SavingChangesAsync_ShouldReturnInterceptionResult()
    {
        // Arrange
        using var context = CreateContext();
        var accessor = CreateTenantAccessor("tenant-789");
        var interceptor = new TenantIdSetterInterceptor(accessor.Object, Mock.Of<ILogger<TenantIdSetterInterceptor>>());

        var entity = new TestTenantEntity { Name = "Test" };
        context.TenantEntities.Add(entity);

        var eventData = CreateEventData(context);
        var expectedResult = InterceptionResult<int>.SuppressWithResult(1);

        // Act
        var result = await interceptor.SavingChangesAsync(eventData, expectedResult, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
    }

    #endregion

    #region Multiple Entities Tests

    [Fact]
    public void SavingChanges_WithMultipleNewEntities_ShouldSetTenantIdOnAll()
    {
        // Arrange
        using var context = CreateContext();
        var accessor = CreateTenantAccessor("multi-tenant");
        var interceptor = new TenantIdSetterInterceptor(accessor.Object, Mock.Of<ILogger<TenantIdSetterInterceptor>>());

        var entity1 = new TestTenantEntity { Name = "Test1" };
        var entity2 = new TestTenantEntity { Name = "Test2" };
        var entity3 = new TestTenantEntity { Name = "Test3" };

        context.TenantEntities.AddRange(entity1, entity2, entity3);

        var eventData = CreateEventData(context);

        // Act
        interceptor.SavingChanges(eventData, default);

        // Assert
        entity1.TenantId.Should().Be("multi-tenant");
        entity2.TenantId.Should().Be("multi-tenant");
        entity3.TenantId.Should().Be("multi-tenant");
    }

    [Fact]
    public void SavingChanges_WithMixedNewAndModified_ShouldOnlySetOnNew()
    {
        // Arrange
        using var context = CreateContext();
        var accessor = CreateTenantAccessor("new-tenant");
        var interceptor = new TenantIdSetterInterceptor(accessor.Object, Mock.Of<ILogger<TenantIdSetterInterceptor>>());

        // Create existing entity
        var existingEntity = new TestTenantEntity { Name = "Existing", TenantId = "old-tenant" };
        context.TenantEntities.Add(existingEntity);
        context.SaveChanges();

        // Modify existing and add new
        existingEntity.Name = "Modified";
        var newEntity = new TestTenantEntity { Name = "New" };
        context.TenantEntities.Add(newEntity);

        var eventData = CreateEventData(context);

        // Act
        interceptor.SavingChanges(eventData, default);

        // Assert
        existingEntity.TenantId.Should().Be("old-tenant"); // Unchanged
        newEntity.TenantId.Should().Be("new-tenant"); // Set
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void TenantIdSetterInterceptor_ShouldInheritFromSaveChangesInterceptor()
    {
        // Arrange
        var accessor = CreateTenantAccessor("tenant");
        var interceptor = new TenantIdSetterInterceptor(accessor.Object, Mock.Of<ILogger<TenantIdSetterInterceptor>>());

        // Assert
        interceptor.Should().BeAssignableTo<SaveChangesInterceptor>();
    }

    #endregion
}

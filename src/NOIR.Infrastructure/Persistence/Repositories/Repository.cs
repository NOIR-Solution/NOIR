namespace NOIR.Infrastructure.Persistence.Repositories;

/// <summary>
/// Base repository implementation using Entity Framework Core.
/// Implements specification pattern with soft delete as default behavior.
///
/// All entities must inherit from <see cref="AggregateRoot{TId}"/>, which includes:
/// - <see cref="IAuditableEntity"/> for audit fields (CreatedAt, ModifiedAt, DeletedAt, etc.)
/// - Domain event support via <see cref="IDomainEvent"/>
/// - Soft delete with automatic global query filtering
///
/// The <see cref="AuditableEntityInterceptor"/> automatically manages:
/// - Setting CreatedAt/CreatedBy on insert
/// - Setting ModifiedAt/ModifiedBy on update
/// - Converting Delete to soft delete (IsDeleted = true, DeletedAt, DeletedBy)
///
/// Based on best practices from:
/// - Microsoft eShopOnWeb reference architecture
/// - Ardalis.Specification patterns
/// - Milan Jovanović's implementation guides
/// </summary>
/// <typeparam name="TEntity">The aggregate root type (inherits AggregateRoot which includes IAuditableEntity).</typeparam>
/// <typeparam name="TId">The identifier type.</typeparam>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Abstract repository - tested via integration tests using DbContext")]
public abstract class Repository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : AggregateRoot<TId>
    where TId : notnull
{
    protected readonly ApplicationDbContext DbContext;
    protected readonly DbSet<TEntity> DbSet;
    protected readonly ICurrentUser CurrentUser;
    protected readonly IDateTime DateTime;

    protected Repository(
        ApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTime dateTime)
    {
        DbContext = dbContext;
        DbSet = dbContext.Set<TEntity>();
        CurrentUser = currentUser;
        DateTime = dateTime;
    }

    #region Basic Queries

    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    public virtual async Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(e => e.Id.Equals(id), cancellationToken);
    }

    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.CountAsync(cancellationToken);
    }

    #endregion

    #region Predicate-based Queries

    public virtual async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking().FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public virtual async Task<TEntity?> SingleOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking().SingleOrDefaultAsync(predicate, cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        return predicate is null
            ? await DbSet.AnyAsync(cancellationToken)
            : await DbSet.AnyAsync(predicate, cancellationToken);
    }

    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.CountAsync(predicate, cancellationToken);
    }

    #endregion

    #region Specification-based Queries

    public virtual async Task<TEntity?> FirstOrDefaultAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator.GetQuery(DbSet.AsQueryable(), specification);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator.GetQuery(DbSet.AsQueryable(), specification);
        return await query.ToListAsync(cancellationToken);
    }

    public virtual async Task<int> CountAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator.GetQueryForCount(DbSet.AsQueryable(), specification);
        return await query.CountAsync(cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator.GetQueryForCount(DbSet.AsQueryable(), specification);
        return await query.AnyAsync(cancellationToken);
    }

    #endregion

    #region Projection Queries

    public virtual async Task<TResult?> FirstOrDefaultAsync<TResult>(
        ISpecification<TEntity, TResult> specification,
        CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator.GetQuery(DbSet.AsQueryable(), specification);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TResult>> ListAsync<TResult>(
        ISpecification<TEntity, TResult> specification,
        CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator.GetQuery(DbSet.AsQueryable(), specification);
        return await query.ToListAsync(cancellationToken);
    }

    #endregion

    #region Create Operations

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(entities, cancellationToken);
    }

    #endregion

    #region Update Operations

    public virtual void Update(TEntity entity)
    {
        DbSet.Update(entity);
    }

    public virtual void UpdateRange(IEnumerable<TEntity> entities)
    {
        DbSet.UpdateRange(entities);
    }

    #endregion

    #region Soft Delete Operations (Default)

    /// <summary>
    /// Soft deletes the entity. The AuditableEntityInterceptor sets IsDeleted = true.
    /// The entity remains in the database but is excluded from normal queries.
    /// </summary>
    public virtual void Remove(TEntity entity)
    {
        DbSet.Remove(entity);
    }

    /// <summary>
    /// Soft deletes multiple entities.
    /// </summary>
    public virtual void RemoveRange(IEnumerable<TEntity> entities)
    {
        DbSet.RemoveRange(entities);
    }

    /// <summary>
    /// Gets an entity by ID, including soft-deleted entities.
    /// Bypasses the global query filter.
    /// </summary>
    public virtual async Task<TEntity?> GetByIdIncludingDeletedAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
    }

    /// <summary>
    /// Gets all entities, including soft-deleted ones.
    /// Bypasses the global query filter.
    /// </summary>
    public virtual async Task<IReadOnlyList<TEntity>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets only soft-deleted entities.
    /// </summary>
    public virtual async Task<IReadOnlyList<TEntity>> GetDeletedOnlyAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => e.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Restores a soft-deleted entity by setting IsDeleted = false.
    /// </summary>
    public virtual Task RestoreAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var entry = DbContext.Entry(entity);

        entry.Property(nameof(IAuditableEntity.IsDeleted)).CurrentValue = false;
        entry.Property(nameof(IAuditableEntity.DeletedAt)).CurrentValue = null;
        entry.Property(nameof(IAuditableEntity.DeletedBy)).CurrentValue = null;
        entry.Property(nameof(IAuditableEntity.ModifiedAt)).CurrentValue = DateTime.UtcNow;
        entry.Property(nameof(IAuditableEntity.ModifiedBy)).CurrentValue = CurrentUser.UserId;

        return Task.CompletedTask;
    }

    #endregion

    #region Hard Delete (GDPR Compliance)

    /// <summary>
    /// Permanently deletes an entity from the database.
    /// Uses ExecuteDeleteAsync to bypass the soft delete interceptor.
    /// WARNING: This cannot be undone. Use for GDPR "right to be forgotten" compliance only.
    /// </summary>
    public virtual async Task HardDeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await DbSet
            .IgnoreQueryFilters()
            .Where(e => e.Id.Equals(entity.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }

    #endregion

    #region Bulk Operations (Specification-based - Native EF Core)

    /// <summary>
    /// Bulk deletes entities matching the specification (hard delete).
    /// Uses ExecuteDeleteAsync for optimal performance - bypasses change tracking.
    /// NOTE: This does NOT trigger soft delete - entities are permanently removed.
    /// WARNING: This cannot be undone. Use for cleanup operations or GDPR compliance.
    /// </summary>
    public virtual async Task<int> BulkDeleteAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var query = SpecificationEvaluator.GetQueryForCount(DbSet.AsQueryable(), specification);

        // Apply IgnoreQueryFilters if specified (to also delete soft-deleted entities)
        if (specification.IgnoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        return await query.ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>
    /// Bulk soft-deletes entities matching the specification.
    /// Sets IsDeleted = true, DeletedAt, and DeletedBy on matching entities.
    /// Uses ExecuteUpdateAsync for optimal performance - bypasses change tracking.
    /// NOTE: This does NOT trigger interceptors or domain events.
    /// </summary>
    public virtual async Task<int> BulkSoftDeleteAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var userId = CurrentUser.UserId;

        var query = SpecificationEvaluator.GetQueryForCount(DbSet.AsQueryable(), specification);

        return await query.ExecuteUpdateAsync(
            setters => setters
                .SetProperty(e => e.IsDeleted, true)
                .SetProperty(e => e.DeletedAt, utcNow)
                .SetProperty(e => e.DeletedBy, userId)
                .SetProperty(e => e.ModifiedAt, utcNow)
                .SetProperty(e => e.ModifiedBy, userId),
            cancellationToken);
    }

    #endregion

    #region Bulk Operations (Collection-based - High Performance via EFCore.BulkExtensions)

    /// <summary>
    /// Validates bulk operation configuration for conflicts.
    /// </summary>
    private static void ValidateBulkOperationConfig(BulkOperationConfig? config)
    {
        if (config == null)
            return;

        // Validate mutually exclusive properties
        if (config.PropertiesToInclude?.Count > 0 && config.PropertiesToExclude?.Count > 0)
        {
            throw new InvalidOperationException(
                "Cannot specify both PropertiesToInclude and PropertiesToExclude. " +
                "Use one or the other to control which properties are affected.");
        }

        // Validate batch size
        if (config.BatchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(config.BatchSize),
                config.BatchSize,
                "BatchSize must be greater than zero.");
        }
    }

    /// <summary>
    /// Validates that all entities in the collection belong to the current tenant.
    /// Prevents cross-tenant data manipulation via bulk operations.
    /// </summary>
    /// <exception cref="InvalidOperationException">When entities contain mismatched TenantId.</exception>
    private void ValidateTenantContext(IEnumerable<TEntity> entities)
    {
        // Skip validation for non-tenant entities
        if (!typeof(ITenantEntity).IsAssignableFrom(typeof(TEntity)))
            return;

        var currentTenantId = CurrentUser.TenantId;

        foreach (var entity in entities)
        {
            if (entity is ITenantEntity tenantEntity && tenantEntity.TenantId != currentTenantId)
            {
                throw new InvalidOperationException(
                    $"Bulk operation contains entity with TenantId '{tenantEntity.TenantId}' " +
                    $"but current tenant is '{currentTenantId}'. " +
                    "Cross-tenant data manipulation is not allowed. " +
                    "Ensure all entities belong to the current tenant context.");
            }
        }
    }

    /// <summary>
    /// Bulk inserts entities using SqlBulkCopy for maximum performance.
    /// 10-15x faster than AddRange for large datasets (1000+ records).
    /// NOTE: Bypasses change tracking and interceptors (no audit logging).
    /// </summary>
    /// <exception cref="InvalidOperationException">When config has conflicting properties.</exception>
    public virtual async Task BulkInsertAsync(
        IEnumerable<TEntity> entities,
        BulkOperationConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        ValidateBulkOperationConfig(config);

        var entityList = entities.ToList();
        if (entityList.Count == 0)
            return;

        ValidateTenantContext(entityList);

        var stopwatch = Stopwatch.StartNew();
        var bulkConfig = BulkConfigMapper.ToBulkConfig(config, DbContext);

        await DbContext.BulkInsertAsync(entityList, bulkConfig, cancellationToken: cancellationToken);

        stopwatch.Stop();
        BulkConfigMapper.UpdateStats(config, bulkConfig, stopwatch, entityList.Count);
    }

    /// <summary>
    /// Bulk updates entities using MERGE for maximum performance.
    /// 4-5x faster than UpdateRange for large datasets.
    /// NOTE: Bypasses change tracking and interceptors (no audit logging).
    /// </summary>
    /// <exception cref="InvalidOperationException">When config has conflicting properties.</exception>
    public virtual async Task BulkUpdateAsync(
        IEnumerable<TEntity> entities,
        BulkOperationConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        ValidateBulkOperationConfig(config);

        var entityList = entities.ToList();
        if (entityList.Count == 0)
            return;

        ValidateTenantContext(entityList);

        var stopwatch = Stopwatch.StartNew();
        var bulkConfig = BulkConfigMapper.ToBulkConfig(config, DbContext);

        await DbContext.BulkUpdateAsync(entityList, bulkConfig, cancellationToken: cancellationToken);

        stopwatch.Stop();
        BulkConfigMapper.UpdateStats(config, bulkConfig, stopwatch, entityList.Count);
    }

    /// <summary>
    /// Bulk inserts or updates entities (upsert) based on key matching.
    /// If entity exists (by PK or UpdateByProperties), it's updated; otherwise inserted.
    /// NOTE: Bypasses change tracking and interceptors.
    /// </summary>
    /// <exception cref="InvalidOperationException">When config has conflicting properties.</exception>
    public virtual async Task BulkInsertOrUpdateAsync(
        IEnumerable<TEntity> entities,
        BulkOperationConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        ValidateBulkOperationConfig(config);

        var entityList = entities.ToList();
        if (entityList.Count == 0)
            return;

        ValidateTenantContext(entityList);

        var stopwatch = Stopwatch.StartNew();
        var bulkConfig = BulkConfigMapper.ToBulkConfig(config, DbContext);

        await DbContext.BulkInsertOrUpdateAsync(entityList, bulkConfig, cancellationToken: cancellationToken);

        stopwatch.Stop();
        BulkConfigMapper.UpdateStats(config, bulkConfig, stopwatch, entityList.Count);
    }

    /// <summary>
    /// Bulk deletes the provided entities by their primary keys.
    /// Uses bulk delete for maximum performance.
    /// NOTE: This is HARD delete - entities are permanently removed.
    /// </summary>
    /// <exception cref="InvalidOperationException">When config has conflicting properties.</exception>
    public virtual async Task BulkDeleteEntitiesAsync(
        IEnumerable<TEntity> entities,
        BulkOperationConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        ValidateBulkOperationConfig(config);

        var entityList = entities.ToList();
        if (entityList.Count == 0)
            return;

        ValidateTenantContext(entityList);

        var stopwatch = Stopwatch.StartNew();
        var bulkConfig = BulkConfigMapper.ToBulkConfig(config, DbContext);

        await DbContext.BulkDeleteAsync(entityList, bulkConfig, cancellationToken: cancellationToken);

        stopwatch.Stop();
        BulkConfigMapper.UpdateStats(config, bulkConfig, stopwatch, entityList.Count);
    }

    /// <summary>
    /// Synchronizes the database table with the provided collection.
    /// Inserts new, updates existing, and DELETES those not in the collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>DANGEROUS OPERATION:</strong> This method will permanently DELETE any entities
    /// in the database that are not present in the provided collection.
    /// </para>
    /// <para>
    /// You MUST explicitly confirm this behavior by setting:
    /// <code>config.ConfirmSyncWillDeleteMissingRecords = true</code>
    /// </para>
    /// <para>
    /// If passing an empty collection (which will DELETE ALL records), you must also set:
    /// <code>config.ConfirmSyncWithEmptyCollection = true</code>
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// When ConfirmSyncWillDeleteMissingRecords is not set to true, or when passing
    /// an empty collection without ConfirmSyncWithEmptyCollection = true.
    /// </exception>
    public virtual async Task BulkSyncAsync(
        IEnumerable<TEntity> entities,
        BulkOperationConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        ValidateBulkOperationConfig(config);

        // CRITICAL: Require explicit confirmation for sync operations
        if (config?.ConfirmSyncWillDeleteMissingRecords != true)
        {
            throw new InvalidOperationException(
                "BulkSyncAsync will DELETE all records NOT in the provided collection. " +
                "This is a DESTRUCTIVE operation that cannot be undone. " +
                "To proceed, you must explicitly set: config.ConfirmSyncWillDeleteMissingRecords = true " +
                "or use the fluent API: new BulkOperationConfig().ConfirmSyncDeletion()");
        }

        var entityList = entities.ToList();

        // Additional safety check for empty collection
        if (entityList.Count == 0 && config?.ConfirmSyncWithEmptyCollection != true)
        {
            throw new InvalidOperationException(
                "BulkSyncAsync was called with an EMPTY collection, which will DELETE ALL records in the table. " +
                "If this is intentional, set: config.ConfirmSyncWithEmptyCollection = true");
        }

        if (entityList.Count > 0)
            ValidateTenantContext(entityList);

        var stopwatch = Stopwatch.StartNew();
        var bulkConfig = BulkConfigMapper.ToBulkConfig(config, DbContext);

        // BulkInsertOrUpdateOrDelete = full sync (insert new, update existing, delete missing)
        await DbContext.BulkInsertOrUpdateOrDeleteAsync(entityList, bulkConfig, cancellationToken: cancellationToken);

        stopwatch.Stop();
        BulkConfigMapper.UpdateStats(config, bulkConfig, stopwatch, entityList.Count);
    }

    /// <summary>
    /// Bulk reads entities by their keys for efficient large-scale lookups.
    /// More efficient than multiple GetByIdAsync calls for large datasets.
    /// </summary>
    /// <exception cref="InvalidOperationException">When config has conflicting properties.</exception>
    public virtual async Task<IReadOnlyList<TEntity>> BulkReadAsync(
        IEnumerable<TEntity> entities,
        BulkOperationConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        ValidateBulkOperationConfig(config);

        var entityList = entities.ToList();
        if (entityList.Count == 0)
            return [];

        ValidateTenantContext(entityList);

        var bulkConfig = BulkConfigMapper.ToBulkConfig(config, DbContext);

        await DbContext.BulkReadAsync(entityList, bulkConfig, cancellationToken: cancellationToken);

        return entityList;
    }

    #endregion
}
